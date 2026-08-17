using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CPAWeb.Data.Interface;
using CPAWeb.Data.Model;
using Microsoft.Data.SqlClient;

namespace CPAWeb.Data.Repository
{
    public class SIDRepository : ISIDRepository
    {
        // Ժամանակավոր աղյուսակը, որտեղ պահվում են ընթացիկ sheet-ի անունները
        private const string StagingTable = "edyeghiazaryan_insertvalue";

        private readonly string _connectionString;

        public SIDRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<SID?> GetSIDByNameAsync(string name)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT Name, Number FROM cpa_sid WHERE Name = @name";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@name", name);

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new SID
                            {
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Number = reader.GetString(reader.GetOrdinal("Number"))
                            };
                        }
                    }
                } 
            }

            return null;
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

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    using (var truncate = new SqlCommand($"TRUNCATE TABLE {StagingTable}", connection, transaction))
                    {
                        await truncate.ExecuteNonQueryAsync();
                    }

                    int inserted = 0;

                    if (list.Count > 0)
                    {
                        var query = $"INSERT INTO {StagingTable} (Name) VALUES (@Name)";

                        using (var command = new SqlCommand(query, connection, transaction))
                        {
                            var parameter = command.Parameters.Add("@Name", SqlDbType.NVarChar);

                            foreach (var name in list)
                            {
                                parameter.Value = name;
                                inserted += await command.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    transaction.Commit();
                    return inserted;
                }
            }
        }

        public async Task<int> GetStagingCountAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand($"SELECT COUNT(*) FROM {StagingTable}", connection))
            {
                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();

                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        // SELECT ստուգում. ժամանակավոր աղյուսակի որ անուններն արդեն կան cpa_sid-ում
        public async Task<List<string>> GetStagedNamesAlreadyInSIDAsync()
        {
            var duplicates = new List<string>();

            var query = $@"SELECT DISTINCT s.Name
                           FROM {StagingTable} s
                           INNER JOIN cpa_sid c ON c.Name = s.Name";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
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
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    int inserted;

                    var query = $@"INSERT INTO cpa_sid (Name, Number)
                                   SELECT Name, @Number FROM {StagingTable}";

                    using (var command = new SqlCommand(query, connection, transaction))
                    {
                        command.Parameters.Add("@Number", SqlDbType.NVarChar).Value = number;
                        inserted = await command.ExecuteNonQueryAsync();
                    }

                    if (inserted > 0)
                    {
                        using (var truncate = new SqlCommand($"TRUNCATE TABLE {StagingTable}", connection, transaction))
                        {
                            await truncate.ExecuteNonQueryAsync();
                        }
                    }

                    transaction.Commit();
                    return inserted;
                }
            }
        }

        public async Task<string?> GetFullNumberBySuffixAsync(string suffix)// sa patasxanatu e linelu hetagayum shiti hamarov service id n u account id n talu hamar 
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                // "Nikita 5124" -> "5124", "5124" -> "5124"
                var digitGroups = System.Text.RegularExpressions.Regex.Matches(suffix ?? string.Empty, @"\d+");
                string cleanSuffix = digitGroups.Count > 0
                    ? digitGroups[digitGroups.Count - 1].Value
                    : (suffix ?? string.Empty).Trim();

                if (string.IsNullOrEmpty(cleanSuffix))
                    return null;

                // LIKE '%9008' պայմանով գտնում ենք ամբողջական Number-ը
                var query = "SELECT TOP 1 Number FROM cpa_sid WHERE Number LIKE @Suffix";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@Suffix", SqlDbType.NVarChar, 50).Value = "%" + cleanSuffix;

                    await connection.OpenAsync();
                    var result = await command.ExecuteScalarAsync();

                    return result?.ToString();
                }
            }
        }
    }
}
