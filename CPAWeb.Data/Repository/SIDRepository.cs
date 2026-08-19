using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CPAWeb.Data.Interface;
using CPAWeb.Data.Model;
using Oracle.ManagedDataAccess.Client;

namespace CPAWeb.Data.Repository
{
    public class SIDRepository : ISIDRepository
    {
        private const string StagingTable = "edyeghiazaryan_insertvalue";

        // Սյունակը վերանվանվել է name -> locator_value (տես db/002_rename_staging_name_column.sql)
        private const string StagingNameColumn = "locator_value";

        // Այն սխեման, որում գտնվում են CPA_SERVICE_IDENT / CPA_ACCOUNT_SERVICE_IDENT / CPA_AUDIT_TRAIL
        private const string CpaSchema = "CPA_USER29";

        private readonly string _connectionString;

        public SIDRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Oracle-ը լռելյայն կապում է պարամետրերը դիրքով, ոչ թե անունով
        private static OracleCommand CreateCommand(string sql, OracleConnection connection, OracleTransaction? transaction = null)
        {
            var command = new OracleCommand(sql, connection) { BindByName = true };

            if (transaction != null)
            {
                command.Transaction = transaction;
            }

            return command;
        }

        // =====================================================================
        // ՈՐՈՆՈՒՄ ԸՍՏ SERVICE_LOCATOR_VALUE-Ի
        // =====================================================================
        public async Task<List<SIDSearchResult>> SearchByServiceLocatorAsync(string value)
        {
            var results = new List<SIDSearchResult>();

            string query = @"SELECT cp.NAME, cn.SERVICE_NAME, cn.UP, cs.SERVICE_LOCATOR_VALUE, cs.SERVICE_ID
                             FROM CPA_NUMBER cn
                             LEFT JOIN CPA_SERVICE_IDENT cs ON cn.SERVICE_ID = cs.SERVICE_ID
                             LEFT JOIN CPA_PROVIDER cp ON cp.N = cn.UP
                             WHERE UPPER(cs.SERVICE_LOCATOR_VALUE) LIKE UPPER(:search)";

            using (var connection = new OracleConnection(_connectionString))
            using (var command = CreateCommand(query, connection))
            {
                command.Parameters.Add("search", OracleDbType.NVarchar2).Value = value;

                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        results.Add(new SIDSearchResult
                        {
                            ProviderName = ReadText(reader, 0),
                            ServiceName = ReadText(reader, 1),
                            Up = ReadText(reader, 2),
                            ServiceLocatorValue = ReadText(reader, 3),
                            ServiceId = ReadText(reader, 4)
                        });
                    }
                }
            }

            return results;
        }

        // Սյունակները կարող են լինել NUMBER, DATE կամ տեքստ — կարդում ենք անվտանգ
        private static string ReadText(System.Data.Common.DbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
                return string.Empty;

            var value = reader.GetValue(ordinal);
            return value?.ToString() ?? string.Empty;
        }

        private static long ReadLong(System.Data.Common.DbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
                return 0L;

            return Convert.ToInt64(reader.GetValue(ordinal));
        }

        // =====================================================================
        // 1. ՀԱՄԱՐՈՎ ԳՏՆՈՒՄ ԵՆՔ SERVICE_ID-Ն
        //    select cp.name, cn.service_name, cn.service_id ... where cn.service_name like '%5124'
        // =====================================================================
        public async Task<List<ServiceLookup>> FindServicesByNumberAsync(string number)
        {
            var results = new List<ServiceLookup>();

            string query = $@"SELECT DISTINCT cn.SERVICE_ID, cn.SERVICE_NAME, cp.NAME
                             FROM {CpaSchema}.CPA_NUMBER cn
                             LEFT JOIN {CpaSchema}.CPA_PROVIDER cp ON cp.N = cn.UP
                             WHERE cn.SERVICE_NAME LIKE :pattern
                               AND cn.STATUS = 1";

            using (var connection = new OracleConnection(_connectionString))
            using (var command = CreateCommand(query, connection))
            {
                // '5124' -> '%5124' ; ամբողջական համարը նույնպես աշխատում է
                command.Parameters.Add("pattern", OracleDbType.NVarchar2).Value = "%" + number;

                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        results.Add(new ServiceLookup
                        {
                            ServiceId = ReadLong(reader, 0),
                            ServiceName = ReadText(reader, 1),
                            ProviderName = ReadText(reader, 2)
                        });
                    }
                }
            }

            return results;
        }

        // =====================================================================
        // 2. SERVICE_ID-ՈՎ ԳՏՆՈՒՄ ԵՆՔ ACCOUNT_ID-Ն
        //    select s.service_ident_id, s.account_id from CPA_ACCOUNT_SERVICE_IDENT s
        //    where s.SERVICE_IDENT_ID in (select c.ID from CPA_SERVICE_IDENT c where c.SERVICE_ID = ...)
        //    Վերադարձնում ենք ըստ հանդիպելու հաճախականության՝ ամենաշատը գործածվածն առաջինը
        // =====================================================================
        public async Task<List<long>> GetAccountIdsForServiceAsync(long serviceId)
        {
            var accountIds = new List<long>();

            string query = $@"SELECT s.ACCOUNT_ID
                              FROM {CpaSchema}.CPA_ACCOUNT_SERVICE_IDENT s
                              WHERE s.SERVICE_IDENT_ID IN (
                                    SELECT c.ID
                                    FROM {CpaSchema}.CPA_SERVICE_IDENT c
                                    WHERE c.SERVICE_ID = :serviceId)
                                AND s.ACCOUNT_ID IS NOT NULL
                              GROUP BY s.ACCOUNT_ID
                              ORDER BY COUNT(*) DESC, s.ACCOUNT_ID";

            using (var connection = new OracleConnection(_connectionString))
            using (var command = CreateCommand(query, connection))
            {
                command.Parameters.Add("serviceId", OracleDbType.Int64).Value = serviceId;

                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        accountIds.Add(ReadLong(reader, 0));
                    }
                }
            }

            return accountIds;
        }

        // =========================================================================
        // ԺԱՄԱՆԱԿԱՎՈՐ ԱՂՅՈՒՍԱԿ (edyeghiazaryan_insertvalue) — միայն Name սյունակ
        // =========================================================================

        // Մաքրում ենք ժամանակավոր աղյուսակը և լցնում ընթացիկ sheet-ի արժեքներով
        public async Task<int> ReplaceStagingNamesAsync(IEnumerable<string> names)
        {
            var list = names?.Where(n => !string.IsNullOrWhiteSpace(n))
                             .Select(n => n.Trim())
                             .ToList() ?? new List<string>();

            using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    // Oracle-ում TRUNCATE-ը DDL է և ինքնուրույն COMMIT է անում,
                    // ուստի գործարքի ներսում օգտագործում ենք DELETE
                    using (var clear = CreateCommand($"DELETE FROM {StagingTable}", connection, transaction))
                    {
                        await clear.ExecuteNonQueryAsync();
                    }

                    int inserted = 0;

                    if (list.Count > 0)
                    {
                        var query = $"INSERT INTO {StagingTable} ({StagingNameColumn}) VALUES (:name)";

                        using (var command = CreateCommand(query, connection, transaction))
                        {
                            // Array binding — բոլոր տողերը մեկ հարցումով
                            command.ArrayBindCount = list.Count;
                            command.Parameters.Add("name", OracleDbType.NVarchar2).Value = list.ToArray();

                            inserted = await command.ExecuteNonQueryAsync();
                        }
                    }

                    await transaction.CommitAsync();
                    return inserted;
                }
            }
        }

        public async Task<int> GetStagingCountAsync()
        {
            using (var connection = new OracleConnection(_connectionString))
            using (var command = CreateCommand($"SELECT COUNT(*) FROM {StagingTable}", connection))
            {
                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();

                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        // SELECT ստուգում. ժամանակավոր աղյուսակի որ անուններն արդեն գրանցված են։
        // Օգտագործում ենք որոնման նույն select-ը (CPA_NUMBER + CPA_SERVICE_IDENT + CPA_PROVIDER)
        public async Task<List<string>> GetStagedNamesAlreadyInSIDAsync()
        {
            var duplicates = new List<string>();

            var query = $@"SELECT DISTINCT s.{StagingNameColumn}
                           FROM {StagingTable} s
                           WHERE EXISTS (
                               SELECT 1
                               FROM CPA_NUMBER cn
                               LEFT JOIN CPA_SERVICE_IDENT cs ON cn.SERVICE_ID = cs.SERVICE_ID
                               LEFT JOIN CPA_PROVIDER cp ON cp.N = cn.UP
                               WHERE UPPER(cs.SERVICE_LOCATOR_VALUE) = UPPER(s.{StagingNameColumn})
                           )";

            using (var connection = new OracleConnection(_connectionString))
            using (var command = CreateCommand(query, connection))
            {
                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            duplicates.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return duplicates;
        }

        public async Task ClearStagingAsync()
        {
            using (var connection = new OracleConnection(_connectionString))
            using (var command = CreateCommand($"DELETE FROM {StagingTable}", connection))
            {
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }


        private const string RegisterStagedNamesBlock = $@"
declare
  v_service_id    number := :p_service_id;
  v_traffic_type  number := :p_traffic_type;
  v_account_id    number := :p_account_id;
  v_new_id        number;
  v_counter       number := 0;

  v_user_name     varchar2(100) := :p_user_name;
  v_servname      varchar2(100) := :p_serv_name;
  v_provider_n    varchar2(100);
  v_account_n     varchar2(100);

  v_audit_id      number;
begin

  -- վերցնում ենք պրովայդերի տվյալները՝ provider_n և account_n
  begin
    select cpa.provider_n, cpa.account_n
      into v_provider_n, v_account_n
      from cpa_provider_account cpa
     where cpa.account_id = v_account_id;
  exception
    when no_data_found then
      v_provider_n := null;
      v_account_n  := null;
  end;

  -- մուտքագրված անուններից վերցնում ենք նրանք, որոնք գրանցված չկան բազայում
  for rec in (
    select locator_value
      from edyeghiazaryan_insertvalue
     where locator_value is not null
       and locator_value not in (select service_locator_value
                                   from cpa_service_ident
                                  where service_locator_value is not null)
  ) loop

    select {CpaSchema}.q_service_range.nextval into v_new_id from dual;

    -- 1. ավելացնում ենք cpa_service_ident-ում
    insert into {CpaSchema}.cpa_service_ident (
      id, service_locator_type, service_locator_value, service_id, traffic_type_id
    ) values (
      v_new_id, 1, rec.locator_value, v_service_id, v_traffic_type
    );

    -- 2. ավելացնում ենք cpa_account_service_ident-ում
    insert into {CpaSchema}.cpa_account_service_ident (
      service_ident_id, account_id
    ) values (
      v_new_id, v_account_id
    );

    -- ամեն ցիկլի սկզբում բազայից վերցնում ենք այդ պահին եղած ամենավերջին ID-ն
    select nvl(max(id), 0) into v_audit_id from {CpaSchema}.cpa_audit_trail;

    -- տող 1: ent1 = 200, ent2 = 210
    v_audit_id := v_audit_id + 1;
    insert into {CpaSchema}.cpa_audit_trail (
      id, user_name, op_time, ent1, ent1_id, ent1_ref, ent2, ent2_id, ent2_ref, op, old_value, new_value, region_id, note
    ) values (
      v_audit_id, v_user_name, sysdate, 200, v_servname, v_service_id, 210, rec.locator_value, v_new_id, 0, null, null, 2, null
    );

    -- տող 3: ent1 = 210, ent2 = 211
    v_audit_id := v_audit_id + 1;
    insert into {CpaSchema}.cpa_audit_trail (
      id, user_name, op_time, ent1, ent1_id, ent1_ref, ent2, ent2_id, ent2_ref, op, old_value, new_value, region_id, note
    ) values (
      v_audit_id, v_user_name, sysdate, 210, rec.locator_value, v_new_id, 211, null, null, 0, null, 1, 2, null
    );

    -- տող 4: ent1 = 210, ent2 = 212
    v_audit_id := v_audit_id + 1;
    insert into {CpaSchema}.cpa_audit_trail (
      id, user_name, op_time, ent1, ent1_id, ent1_ref, ent2, ent2_id, ent2_ref, op, old_value, new_value, region_id, note
    ) values (
      v_audit_id, v_user_name, sysdate, 210, rec.locator_value, v_new_id, 212, null, null, 0, null, rec.locator_value, 2, null
    );

    -- տող 5: ent1 = 210, ent2 = 214
    v_audit_id := v_audit_id + 1;
    insert into {CpaSchema}.cpa_audit_trail (
      id, user_name, op_time, ent1, ent1_id, ent1_ref, ent2, ent2_id, ent2_ref, op, old_value, new_value, region_id, note
    ) values (
      v_audit_id, v_user_name, sysdate, 210, rec.locator_value, v_new_id, 214, null, null, 0, null, 'SMS', 2, null
    );

    -- տող 2: ent1 = 210, ent2 = 106
    v_audit_id := v_audit_id + 1;
    insert into {CpaSchema}.cpa_audit_trail (
      id, user_name, op_time, ent1, ent1_id, ent1_ref, ent2, ent2_id, ent2_ref, op, old_value, new_value, region_id, note
    ) values (
      v_audit_id, v_user_name, sysdate, 210, rec.locator_value, v_new_id, 106, v_account_n, v_provider_n, 1, null, null, 2, null
    );

    v_counter := v_counter + 1;

  end loop;

  if v_counter > 0 then
    commit;
  end if;

  :p_count := v_counter;

exception
  when others then
    rollback;
    raise;
end;";

        public async Task<int> RegisterStagedNamesAsync(long serviceId, long accountId, string serviceName, string userName, int trafficTypeId)
        {
            using (var connection = new OracleConnection(_connectionString))
            using (var command = CreateCommand(RegisterStagedNamesBlock, connection))
            {
                command.Parameters.Add("p_service_id", OracleDbType.Int64).Value = serviceId;
                command.Parameters.Add("p_traffic_type", OracleDbType.Int32).Value = trafficTypeId;
                command.Parameters.Add("p_account_id", OracleDbType.Int64).Value = accountId;
                command.Parameters.Add("p_user_name", OracleDbType.Varchar2, 100).Value = userName;
                command.Parameters.Add("p_serv_name", OracleDbType.Varchar2, 100).Value = serviceName;

                var countParameter = command.Parameters.Add("p_count", OracleDbType.Int32);
                countParameter.Direction = ParameterDirection.Output;

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                var value = countParameter.Value;
                if (value == null || value == DBNull.Value)
                    return 0;

                // Oracle-ի provider-ը վերադարձնում է OracleDecimal, ոչ թե int
                return Convert.ToInt32(value.ToString());
            }
        }
    }
}
