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
        public async Task<string> ValidateUserAsync(string userId, string password)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                // Open the connection
                await connection.OpenAsync();

                if (connection.State == System.Data.ConnectionState.Open)
                {
                    Console.WriteLine("Connection to the database established successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to establish connection to the database.");
                    return null; // Return null or handle differently
                }

                // Prepare query to check user credentials (plain text comparison)
                var query = "SELECT UserId, Password FROM UserMaster WHERE UserId = @UserId";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    string storedPassword = reader["Password"].ToString(); // Get the plain text password

                    // Direct comparison of the provided password and the stored password
                    if (storedPassword == password)
                    {
                        return userId; // User validated successfully
                    }
                }

                return null; // If no match is found, return null
            }
            catch (SqlException sqlEx)
            {
                // Catch SQL exceptions and provide more specific error messages
                Console.WriteLine($"SQL Error: {sqlEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // Catch general exceptions (e.g., network-related issues)
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
