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
        // =====================================================================
        // ORACLE-Ի ԱՆՎԱՆՈՒՄՆԵՐ
        // NUMBER-ը Oracle-ում վերապահված բառ է, ուստի սյունակի անունը
        // գրվում է չակերտներով. Եթե ձեր բազայում այլ անուն է (օր.՝ SID_NUMBER),
        // բավական է փոխել միայն այս հաստատունը.
        // =====================================================================
        private const string SidTable = "cpa_sid";
        private const string SidNameColumn = "name";
        private const string SidNumberColumn = "\"NUMBER\"";

        private const string StagingTable = "edyeghiazaryan_insertvalue";
        private const string StagingNameColumn = "name";

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

        // Ժամանակավոր աղյուսակի Name-երը գրանցում ենք cpa_sid-ում տրված Number-ով,
        // ապա անմիջապես մաքրում ենք ժամանակավոր աղյուսակը
        public async Task<int> TransferStagingToSIDAsync(string number)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    int inserted;

                    var query = $@"INSERT INTO {SidTable} ({SidNameColumn}, {SidNumberColumn})
                                   SELECT {StagingNameColumn}, :number FROM {StagingTable}";

                    using (var command = CreateCommand(query, connection, transaction))
                    {
                        command.Parameters.Add("number", OracleDbType.NVarchar2).Value = number;
                        inserted = await command.ExecuteNonQueryAsync();
                    }

                    if (inserted > 0)
                    {
                        // DELETE, ոչ թե TRUNCATE, որպեսզի գործարքը մնա ամբողջական
                        using (var clear = CreateCommand($"DELETE FROM {StagingTable}", connection, transaction))
                        {
                            await clear.ExecuteNonQueryAsync();
                        }
                    }

                    await transaction.CommitAsync();
                    return inserted;
                }
            }
        }

    }
}
