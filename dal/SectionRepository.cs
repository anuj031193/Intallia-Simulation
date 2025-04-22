using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using JobSimulation.Models;

namespace JobSimulation.DAL
{
    public class SectionRepository
    {
        private readonly string _connectionString;

        public SectionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Section> GetFirstSectionAsync(string simulationId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT TOP 1 * FROM Section WHERE SimulationId = @SimulationId ORDER BY [Order]";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SimulationId", simulationId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Section
                {
                    SectionId = reader["SectionId"].ToString(),
                    Title = reader["Title"].ToString(),
                    SoftwareId = reader["SoftwareId"].ToString(),
                    Order = Convert.ToInt32(reader["Order"]),
                    StudentFile = reader["StudentFile"].ToString(),
                    SimulationId = reader["SimulationId"].ToString()
                };
            }

            return null;
        }

        public async Task<bool> IsLastSectionAsync(string currentSectionId, string simulationId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM Section WHERE SimulationId = @SimulationId AND SectionId > @CurrentSectionId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SimulationId", simulationId);
            command.Parameters.AddWithValue("@CurrentSectionId", currentSectionId);

            var count = (int)await command.ExecuteScalarAsync();
            return count == 0;
        }

        public async Task<Section> GetNextSectionAsync(string simulationId, string currentSectionId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT TOP 1 * FROM Section WHERE SimulationId = @SimulationId AND SectionId > @CurrentSectionId ORDER BY [Order]";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SimulationId", simulationId);
            command.Parameters.AddWithValue("@CurrentSectionId", currentSectionId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Section
                {
                    SectionId = reader["SectionId"].ToString(),
                    Title = reader["Title"].ToString(),
                    SoftwareId = reader["SoftwareId"].ToString(),
                    Order = Convert.ToInt32(reader["Order"]),
                    StudentFile = reader["StudentFile"].ToString(),
                    SimulationId = reader["SimulationId"].ToString()
                };
            }

            return null;
        }
        public async Task<bool> HasNextSectionAsync(string simulationId, int currentOrder)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM Section WHERE SimulationId = @SimulationId AND [Order] > @CurrentOrder";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SimulationId", simulationId);
            command.Parameters.AddWithValue("@CurrentOrder", currentOrder);

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }

        public async Task<bool> HasPreviousSectionAsync(string simulationId, int currentOrder)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM Section WHERE SimulationId = @SimulationId AND [Order] < @CurrentOrder";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SimulationId", simulationId);
            command.Parameters.AddWithValue("@CurrentOrder", currentOrder);

            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }
        public async Task<Section> GetSectionByIdAsync(string sectionId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM Section WHERE SectionId = @SectionId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SectionId", sectionId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Section
                {
                    SectionId = reader["SectionId"].ToString(),
                    Title = reader["Title"].ToString(),
                    SoftwareId = reader["SoftwareId"].ToString(),
                    Order = Convert.ToInt32(reader["Order"]),
                    StudentFile = reader["StudentFile"].ToString(),
                    SimulationId = reader["SimulationId"].ToString()
                };
            }

            return null;
        }

        public async Task<Section> GetNextSectionByOrderAsync(string simulationId, int currentOrder)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT TOP 1 * FROM Section WHERE SimulationId = @SimulationId AND [Order] > @CurrentOrder ORDER BY [Order]";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SimulationId", simulationId);
            command.Parameters.AddWithValue("@CurrentOrder", currentOrder);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Section
                {
                    SectionId = reader["SectionId"].ToString(),
                    Title = reader["Title"].ToString(),
                    SoftwareId = reader["SoftwareId"].ToString(),
                    Order = Convert.ToInt32(reader["Order"]),
                    StudentFile = reader["StudentFile"].ToString(),
                    SimulationId = reader["SimulationId"].ToString()
                };
            }

            return null;
        }

        public async Task<List<JobTask>> GetTasksForSectionAsync(string sectionId)
        {
            var tasks = new List<JobTask>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM Task WHERE SectionId = @SectionId ORDER BY [Order]";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SectionId", sectionId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tasks.Add(new JobTask
                {
                    TaskId = reader["TaskId"].ToString(),
                    SectionId = reader["SectionId"].ToString(),
                    Order = Convert.ToInt32(reader["Order"]),
                    Description = reader["Description"].ToString(),
                    //Hint = reader["Hint"].ToString(),
                    //From = reader["From"].ToString(),
                    //To = reader["To"].ToString(),
                    //SheetName = reader["SheetName"].ToString()
                });
            }

            return tasks;
        }

        public void SaveProgress(string sectionId, int taskIndex, int timeElapsed)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("UPDATE SectionProgress SET TaskIndex = @TaskIndex, TimeElapsed = @TimeElapsed WHERE SectionId = @SectionId", connection))
                {
                    command.Parameters.AddWithValue("@SectionId", sectionId);
                    command.Parameters.AddWithValue("@TaskIndex", taskIndex);
                    command.Parameters.AddWithValue("@TimeElapsed", timeElapsed);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void SaveSkillMatrix(SkillMatrix skillMatrix)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("UPDATE SkillMatrix SET HintsChecked = @HintsChecked, TotalTime = @TotalTime, AttemptstoSolve = @AttemptstoSolve, Status = @Status, CreateBy = @CreateBy, CreateDate = @CreateDate, ModifyBy = @ModifyBy, ModifyDate = @ModifyDate, TaskAttempt = @TaskAttempt WHERE ActivityId = @ActivityId", connection))
                {
                    command.Parameters.AddWithValue("@ActivityId", skillMatrix.ActivityId);
                    command.Parameters.AddWithValue("@HintsChecked", skillMatrix.HintsChecked);
                    command.Parameters.AddWithValue("@TotalTime", skillMatrix.TotalTime);
                    command.Parameters.AddWithValue("@AttemptstoSolve", skillMatrix.AttemptstoSolve);
                    command.Parameters.AddWithValue("@Status", skillMatrix.Status);
                    command.Parameters.AddWithValue("@CreateBy", skillMatrix.CreateBy);
                    command.Parameters.AddWithValue("@CreateDate", skillMatrix.CreateDate);
                    command.Parameters.AddWithValue("@ModifyBy", skillMatrix.ModifyBy);
                    command.Parameters.AddWithValue("@ModifyDate", skillMatrix.ModifyDate);
                    command.Parameters.AddWithValue("@TaskAttempt", skillMatrix.TaskAttempt);
                    command.ExecuteNonQuery();
                }
            }
        }

        public async Task SaveStudentFileAsync(string sectionId, string userId, string base64File)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                IF EXISTS (SELECT 1 FROM Activity WHERE SectionId = @SectionId AND UserId = @UserId)
                BEGIN
                    UPDATE Activity 
                    SET StudentFile = @StudentFile, ModifyBy = @ModifyBy, ModifyDate = @ModifyDate
                    WHERE SectionId = @SectionId AND UserId = @UserId
                END
                ELSE
                BEGIN
                    INSERT INTO Activity (SectionId, UserId, StudentFile, CreateBy, CreateDate, ModifyBy, ModifyDate)
                    VALUES (@SectionId, @UserId, @StudentFile, @CreateBy, @CreateDate, @ModifyBy, @ModifyDate)
                END";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SectionId", sectionId);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@StudentFile", base64File);
            command.Parameters.AddWithValue("@CreateBy", userId);
            command.Parameters.AddWithValue("@CreateDate", DateTime.UtcNow);
            command.Parameters.AddWithValue("@ModifyBy", userId);
            command.Parameters.AddWithValue("@ModifyDate", DateTime.UtcNow);

            await command.ExecuteNonQueryAsync();
        }

        public async Task MarkSectionComplete(string userId, string sectionId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE SectionProgress SET IsCompleted = 1 WHERE UserId = @UserId AND SectionId = @SectionId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@SectionId", sectionId);

            await command.ExecuteNonQueryAsync();
        }
        public async Task<Section> GetPreviousSectionByOrderAsync(string simulationId, int currentOrder)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT TOP 1 * FROM Section WHERE SimulationId = @SimulationId AND [Order] < @CurrentOrder ORDER BY [Order] DESC";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SimulationId", simulationId);
            command.Parameters.AddWithValue("@CurrentOrder", currentOrder);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Section
                {
                    SectionId = reader["SectionId"].ToString(),
                    Title = reader["Title"].ToString(),
                    SoftwareId = reader["SoftwareId"].ToString(),
                    Order = Convert.ToInt32(reader["Order"]),
                    StudentFile = reader["StudentFile"].ToString(),
                    SimulationId = reader["SimulationId"].ToString()
                };
            }

            return null;
        }
        public async Task<List<Section>> GetAllSectionsBySimulationIdAsync(string simulationId)
        {
            var sections = new List<Section>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM Section WHERE SimulationId = @SimulationId ORDER BY [Order]";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SimulationId", simulationId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                sections.Add(new Section
                {
                    SectionId = reader["SectionId"].ToString(),
                    Title = reader["Title"].ToString(),
                    SoftwareId = reader["SoftwareId"].ToString(),
                    Order = Convert.ToInt32(reader["Order"]),
                    StudentFile = reader["StudentFile"].ToString(),
                    SimulationId = reader["SimulationId"].ToString()
                });
            }

            return sections;
        }

    }
}