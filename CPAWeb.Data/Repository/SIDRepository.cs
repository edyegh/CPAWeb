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
        private readonly string _connectionString;

        public SIDRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> AddSIDAsync(SID sid)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"INSERT INTO cpa_sid (Name, Number) 
                      VALUES (@Name, @Number)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@Name", SqlDbType.NVarChar, 11).Value = sid.Name;
                    command.Parameters.Add("@Number", SqlDbType.NVarChar, 11).Value = sid.Number;

                    await connection.OpenAsync();
                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    return rowsAffected > 0;
                }
            }
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

        public async Task<string?> GetFullNumberBySuffixAsync(string suffix)  // sa patasxanatu e linelu hetagayum shiti hamarov service id n u account id n talu hamar 
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                // LIKE '%9008' պայմանով գտնում ենք ամբողջական Number-ը
                var query = "SELECT TOP 1 Number FROM cpa_sid WHERE Number LIKE @Suffix";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@Suffix", SqlDbType.NVarChar, 50).Value = "%" + suffix;

                    await connection.OpenAsync();
                    var result = await command.ExecuteScalarAsync();

                    return result?.ToString();
                }
            }
        }
    }
}
