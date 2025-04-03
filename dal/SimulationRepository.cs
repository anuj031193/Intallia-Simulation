using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using JobSimulation.Models;

namespace JobSimulation.DAL
{
    public class SimulationRepository
    {
        private readonly string _connectionString;

        public SimulationRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<Simulation>> GetAllSimulationsAsync()
        {
            var simulations = new List<Simulation>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Remove SoftwareId from the SELECT query
            var query = "SELECT SimulationId, CompanyId, Name, Description FROM JobSimulation";
            using var command = new SqlCommand(query, connection);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                simulations.Add(new Simulation
                {
                    SimulationId = reader["SimulationId"].ToString(),
                    CompanyId = reader["CompanyId"].ToString(),
                    Name = reader["Name"].ToString(),
                    Description = reader["Description"].ToString()
                });
            }

            return simulations;
        }
    }
}