using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JobSimulation.DAL
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        public void TestConnection()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            connection.Close();
        }
        public async Task<string> ValidateUserAsync(string userIdOrEmail, string password)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                if (connection.State != System.Data.ConnectionState.Open)
                {
                    Console.WriteLine("Failed to establish connection to the database.");
                    return null;
                }

                // Updated query: match by UserId OR Email
                var query = @"
            SELECT UserId, Password 
            FROM UserMaster 
            WHERE (UserId = @Input OR Email = @Input)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Input", userIdOrEmail);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    string storedPassword = reader["Password"].ToString();

                    // Plain text comparison (assumed)
                    if (storedPassword == password)
                    {
                        return reader["UserId"].ToString(); // Return the actual UserId (even if email was used to log in)
                    }
                }

                return null;
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error: {sqlEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }

        // This method hashes the password using SHA256 (Consider using a stronger hashing algorithm like bcrypt for production)
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Compute the hash of the password
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Convert the byte array to a hexadecimal string
                StringBuilder hashString = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    hashString.Append(b.ToString("x2"));
                }

                return hashString.ToString(); // Return the hashed password as a hex string
            }
        }
    }
}
