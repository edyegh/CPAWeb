using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using CPAWeb.Data.Interface;
using CPAWeb.Data.Model;
using Oracle.ManagedDataAccess.Client;

namespace CPAWeb.Data.Repository
{
    public class UserRepository : IUserRepository
    {
        // Աղյուսակը գտնվում է connection string-ի օգտատիրոջ սխեմայում (տես db/003_create_web_users.sql)
        private const string UserTable = "cpa_web_user";

        // ORA-00001 — եզակիության սահմանափակման խախտում
        private const int UniqueConstraintViolated = 1;

        private const string SelectColumns =
            "id, name, email, password_hash, role, is_active, created_at, created_by";

        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Oracle-ը լռելյայն կապում է պարամետրերը դիրքով, ոչ թե անունով
        private static OracleCommand CreateCommand(string sql, OracleConnection connection)
        {
            return new OracleCommand(sql, connection) { BindByName = true };
        }

        // =====================================================================
        // ՄՈՒՏՔ — ամբողջական email կամ միայն '@'-ից առաջվա մասը
        //   "test@gmail.com" -> ուղիղ համընկնում
        //   "test"           -> SUBSTR/INSTR-ով կտրում ենք '@'-ից առաջվա մասը
        // LIKE-ի փոխարեն SUBSTR, որ մուտքագրված '%'-ը wildcard չդառնա:
        // =====================================================================
        public async Task<List<AppUser>> FindByLoginAsync(string login)
        {
            var users = new List<AppUser>();

            string query = $@"SELECT {SelectColumns}
                              FROM {UserTable}
                              WHERE UPPER(email) = UPPER(:login)
                                 OR UPPER(SUBSTR(email, 1, INSTR(email, '@') - 1)) = UPPER(:login)
                              ORDER BY id";

            using (var connection = new OracleConnection(_connectionString))
            using (var command = CreateCommand(query, connection))
            {
                command.Parameters.Add("login", OracleDbType.NVarchar2).Value = login;

                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        users.Add(Map(reader));
                    }
                }
            }

            return users;
        }

        // =====================================================================
        // Ջնջելուց առաջ՝ ում ենք ջնջում
        // =====================================================================
        public async Task<AppUser?> GetByIdAsync(long id)
        {
            string query = $@"SELECT {SelectColumns}
                              FROM {UserTable}
                              WHERE id = :id";

            using (var connection = new OracleConnection(_connectionString))
            using (var command = CreateCommand(query, connection))
            {
                command.Parameters.Add("id", OracleDbType.Int64).Value = id;

                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return Map(reader);
                    }
                }
            }

            return null;
        }

        // =====================================================================
        // ՋՆՋՈՒՄ
        // =====================================================================
        public async Task<bool> DeleteAsync(long id)
        {
            string query = $"DELETE FROM {UserTable} WHERE id = :id";

            using (var connection = new OracleConnection(_connectionString))
            using (var command = CreateCommand(query, connection))
            {
                command.Parameters.Add("id", OracleDbType.Int64).Value = id;

                await connection.OpenAsync();

                return await command.ExecuteNonQueryAsync() > 0;
            }
        }

        // =====================================================================
        // "users" էջի ցանկը
        // =====================================================================
        public async Task<List<AppUser>> GetAllAsync()
        {
            var users = new List<AppUser>();

            string query = $@"SELECT {SelectColumns}
                              FROM {UserTable}
                              ORDER BY created_at DESC, id DESC";

            using (var connection = new OracleConnection(_connectionString))
            using (var command = CreateCommand(query, connection))
            {
                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        users.Add(Map(reader));
                    }
                }
            }

            return users;
        }

        // =====================================================================
        // ՆՈՐ ՕԳՏԱՏԵՐ — id-ն վերադարձնում ենք RETURNING ... INTO-ով
        // =====================================================================
        public async Task<long> CreateAsync(AppUser user)
        {
            string query = $@"INSERT INTO {UserTable}
                                  (name, email, password_hash, role, is_active, created_at, created_by)
                              VALUES
                                  (:name, :email, :password_hash, :role, :is_active, SYSTIMESTAMP, :created_by)
                              RETURNING id INTO :new_id";

            using (var connection = new OracleConnection(_connectionString))
            using (var command = CreateCommand(query, connection))
            {
                command.Parameters.Add("name", OracleDbType.NVarchar2).Value = user.Name;
                command.Parameters.Add("email", OracleDbType.NVarchar2).Value = user.Email;
                command.Parameters.Add("password_hash", OracleDbType.Varchar2).Value = user.PasswordHash;
                command.Parameters.Add("role", OracleDbType.Varchar2).Value = user.Role;
                command.Parameters.Add("is_active", OracleDbType.Int32).Value = user.IsActive ? 1 : 0;
                command.Parameters.Add("created_by", OracleDbType.NVarchar2).Value =
                    string.IsNullOrWhiteSpace(user.CreatedBy) ? (object)DBNull.Value : user.CreatedBy;

                var newId = new OracleParameter("new_id", OracleDbType.Int64)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(newId);

                await connection.OpenAsync();

                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch (OracleException ex) when (ex.Number == UniqueConstraintViolated)
                {
                    // cpa_web_user_email_uq — երկու հարցում միաժամանակ նույն email-ով
                    throw new ArgumentException($"a user with the email '{user.Email}' already exists.", ex);
                }

                return newId.Value == null || newId.Value == DBNull.Value
                    ? 0L
                    : Convert.ToInt64(newId.Value.ToString());
            }
        }

        // =====================================================================
        // Կրկնվող email-ի ստուգում
        // =====================================================================
        public async Task<bool> EmailExistsAsync(string email)
        {
            string query = $@"SELECT COUNT(*)
                              FROM {UserTable}
                              WHERE UPPER(email) = UPPER(:email)";

            using (var connection = new OracleConnection(_connectionString))
            using (var command = CreateCommand(query, connection))
            {
                command.Parameters.Add("email", OracleDbType.NVarchar2).Value = email;

                await connection.OpenAsync();

                var count = await command.ExecuteScalarAsync();
                return count != null && Convert.ToInt64(count.ToString()) > 0;
            }
        }

        private static AppUser Map(System.Data.Common.DbDataReader reader)
        {
            return new AppUser
            {
                Id = ReadLong(reader, 0),
                Name = ReadText(reader, 1),
                Email = ReadText(reader, 2),
                PasswordHash = ReadText(reader, 3),
                Role = ReadText(reader, 4),
                IsActive = ReadLong(reader, 5) == 1,
                CreatedAt = reader.IsDBNull(6) ? DateTime.MinValue : reader.GetDateTime(6),
                CreatedBy = ReadText(reader, 7)
            };
        }

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

            return Convert.ToInt64(reader.GetValue(ordinal).ToString());
        }
    }
}
