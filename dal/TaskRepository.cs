using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using JobSimulation.Models;
using System.Data;

namespace JobSimulation.DAL
{
    public interface ITaskRepository
    {
        Task<JobTask> GetTaskByIdAsync(string taskId, string softwareId);
        Task<JobTask> GetNextTaskForSectionAsync(string sectionId, int currentTaskIndex);
        Task<List<JobTask>> GetTasksBySectionIdAsync(string sectionId, string activityId);
        Task<List<JobTask>> GetTasksForActivityAsync(string activityId);
        Task<int> GetCurrentTaskIndexAsync(string activityId);
        Task IncrementTaskAttemptAsync(string activityId, string taskId);
        Task SaveCurrentTaskIndexAsync(string activityId, string taskId, int taskIndex, string sectionId, string userId);
        Task<int> GetElapsedTimeForTaskAsync(string activityId, string taskId);
        Task<string> GetSoftwareIdBySectionId(string sectionId);
        string GetConnectionString();
    }

    public class TaskRepository : ITaskRepository
    {
        private readonly string _connectionString;
        private readonly Dictionary<string, string> _softwareMappings;

        public TaskRepository(string connectionString)
        {
            _connectionString = connectionString;
            _softwareMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["s1"] = "DataOfExcel",       // Software ID for Excel
                ["s2"] = "DataOfWord",        // Software ID for Word
                ["s3"] = "DataOfPowerPoint",  // Software ID for PowerPoint
                // Add other software mappings here
            };
        }

        public string GetConnectionString()
        {
            return _connectionString;
        }

        public async Task<JobTask> GetTaskByIdAsync(string taskId, string softwareId)
        {
            if (!_softwareMappings.TryGetValue(softwareId, out var tableName))
                throw new KeyNotFoundException($"Software type {softwareId} not supported");

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = $@"
                SELECT 
                    t.TaskId, t.SectionId, t.[Order], t.Description, t.CompanyId, t.CreateBy, t.CreateDate, t.ModifyBy, t.ModifyDate,
                    d.TaskDescription, d.SheetName, d.SelectTask, d.[From], d.[To], d.ResultCellLocation, d.Hint, d.SkillName, d.SkillScore
                FROM Task t
                LEFT JOIN {tableName} d ON t.TaskId = d.TaskId
                WHERE t.TaskId = @TaskId";

            var record = await connection.QueryFirstOrDefaultAsync(query, new { TaskId = taskId });

            if (record != null)
            {
                return new JobTask
                {
                    TaskId = record.TaskId,
                    SectionId = record.SectionId,
                    Order = record.Order,
                    Description = record.Description,
                    CompanyId = record.CompanyId,
                    CreateBy = record.CreateBy,
                    CreateDate = record.CreateDate,
                    ModifyBy = record.ModifyBy,
                    ModifyDate = record.ModifyDate,
                    Details = new
                    {
                        TaskDescription = record.TaskDescription,
                        SheetName = record.SheetName,
                        SelectTask = record.SelectTask,
                        From = record.From,
                        To = record.To,
                        ResultCellLocation = record.ResultCellLocation,
                        Hint = record.Hint,
                        SkillName = record.SkillName,
                        SkillScore = record.SkillScore
                    }
                };
            }

            return null;
        }

        public async Task<JobTask> GetNextTaskForSectionAsync(string sectionId, int currentTaskIndex)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT t.TaskId, s.SoftwareId
                FROM Task t
                INNER JOIN Section s ON t.SectionId = s.SectionId
                WHERE t.SectionId = @SectionId AND t.[Order] > @CurrentTaskIndex
                ORDER BY t.[Order]
                OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY";

            var result = await connection.QueryFirstOrDefaultAsync(query, new { SectionId = sectionId, CurrentTaskIndex = currentTaskIndex });

            if (result != null)
            {
                return await GetTaskByIdAsync(result.TaskId, result.SoftwareId);
            }

            return null;
        }

        public async Task<List<JobTask>> GetTasksBySectionIdAsync(string sectionId, string activityId)
        {
            using var connection = new SqlConnection(_connectionString);

            var softwareId = await GetSoftwareIdBySectionId(sectionId);
            if (!_softwareMappings.TryGetValue(softwareId, out var tableName))
                throw new KeyNotFoundException($"Software type {softwareId} not supported");

            var query = $@"
                SELECT 
                    t.TaskId, t.SectionId, t.[Order], t.Description,
                    COALESCE(sm.Status, '{StatusTypes.NotStarted}') AS Status,
                    COALESCE(sm.AttemptstoSolve, 0) AS AttemptstoSolve,
                    COALESCE(sm.TaskAttempt, 0) AS TaskAttempt,
                    d.TaskDescription, d.SheetName, d.SelectTask, d.[From], d.[To], 
                    d.ResultCellLocation, d.Hint, d.SkillName, d.SkillScore
                FROM Task t
                LEFT JOIN {tableName} d ON t.TaskId = d.TaskId
                LEFT JOIN SkillMatrix sm ON t.TaskId = sm.TaskId AND sm.ActivityId = @ActivityId
                WHERE t.SectionId = @SectionId
                ORDER BY t.[Order]";

            var results = await connection.QueryAsync(query, new
            {
                SectionId = sectionId,
                ActivityId = activityId
            });

            return results.Select(record => new JobTask
            {
                TaskId = record.TaskId,
                SectionId = record.SectionId,
                Order = record.Order,
                Description = record.Description,
                Details = new
                {
                    TaskDescription = record.TaskDescription,
                    SheetName = record.SheetName,
                    SelectTask = record.SelectTask,
                    From = record.From,
                    To = record.To,
                    ResultCellLocation = record.ResultCellLocation,
                    Hint = record.Hint,
                    SkillName = record.SkillName,
                    SkillScore = record.SkillScore,
                    Status = record.Status,
                    AttemptsToSolve = record.AttemptsToSolve,
                    TaskAttempt = record.TaskAttempt
                }
            }).ToList();
        }

        public async Task<List<JobTask>> GetTasksForActivityAsync(string activityId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Get SectionId from ActivityId
            var sectionIdQuery = "SELECT SectionId FROM Activity WHERE ActivityId = @ActivityId";
            var sectionId = await connection.QueryFirstOrDefaultAsync<string>(sectionIdQuery, new { ActivityId = activityId });

            if (string.IsNullOrEmpty(sectionId))
            {
                throw new Exception("SectionId not found for the given ActivityId.");
            }

            // Get SoftwareId using SectionId
            var softwareId = await GetSoftwareIdBySectionId(sectionId);

            if (string.IsNullOrEmpty(softwareId) || !_softwareMappings.TryGetValue(softwareId.ToLower(), out var tableName))
            {
                throw new Exception("No valid table mapping found for the given SoftwareId.");
            }

            // Replace {0} with the actual table name
            var query = $@"
        SELECT t.TaskId, t.SectionId, t.[Order], t.Description, 
            d.TaskDescription, d.SheetName, d.SelectTask, d.[From], d.[To], 
            d.ResultCellLocation, d.Hint, d.SkillName, d.SkillScore
        FROM Task t
        LEFT JOIN Activity a ON t.SectionId = a.SectionId
        LEFT JOIN SkillMatrix sm ON t.TaskId = sm.TaskId AND sm.ActivityId = a.ActivityId
        LEFT JOIN {tableName} d ON t.TaskId = d.TaskId
        WHERE a.ActivityId = @ActivityId
        ORDER BY t.[Order]";

            var results = await connection.QueryAsync(query, new { ActivityId = activityId });

            return results.Select(record => new JobTask
            {
                TaskId = record.TaskId,
                SectionId = record.SectionId,
                Order = record.Order,
                Description = record.Description,
                Details = new
                {
                    TaskDescription = record.TaskDescription,
                    SheetName = record.SheetName,
                    SelectTask = record.SelectTask,
                    From = record.From,
                    To = record.To,
                    ResultCellLocation = record.ResultCellLocation,
                    Hint = record.Hint,
                    SkillName = record.SkillName,
                    SkillScore = record.SkillScore
                }
            }).ToList();
        }

        public async Task<int> GetElapsedTimeForTaskAsync(string activityId, string taskId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT sm.TotalTime
                FROM SkillMatrix sm
                WHERE sm.ActivityId = @ActivityId AND sm.TaskId = @TaskId";

            var result = await connection.QueryFirstOrDefaultAsync<int?>(query, new { ActivityId = activityId, TaskId = taskId });

            return result ?? 0; // Return 0 if no record exists
        }

        public async Task<int> GetCurrentTaskIndexAsync(string activityId)
        {
            if (string.IsNullOrEmpty(activityId))
            {
                throw new ArgumentNullException(nameof(activityId), "ActivityId cannot be null or empty.");
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            const string query = @"
                SELECT ISNULL(MAX(t.[Order]), 0)
                FROM Task t
                INNER JOIN SkillMatrix sm ON t.TaskId = sm.TaskId
                WHERE sm.ActivityId = @ActivityId";

            try
            {
                var commandTimeout = 30;

                Console.WriteLine($"Starting query execution for ActivityId: {activityId} at {DateTime.UtcNow}");

                var result = await connection.ExecuteScalarAsync<int?>(
                    new CommandDefinition(query, new { ActivityId = activityId }, commandTimeout: commandTimeout)
                ).ConfigureAwait(false);

                Console.WriteLine($"Query execution completed for ActivityId: {activityId} at {DateTime.UtcNow}");

                return result ?? 0;
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL error executing query for ActivityId {activityId}: {sqlEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing query for ActivityId {activityId}: {ex.Message}");
                throw;
            }
            finally
            {
                if (connection.State != System.Data.ConnectionState.Closed)
                {
                    await connection.CloseAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task IncrementTaskAttemptAsync(string activityId, string taskId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE SkillMatrix SET TaskAttempt = TaskAttempt + 1 WHERE ActivityId = @ActivityId AND TaskId = @TaskId";
            await connection.ExecuteAsync(query, new { ActivityId = activityId, TaskId = taskId });
        }

        public async Task SaveCurrentTaskIndexAsync(string activityId, string taskId, int taskIndex, string sectionId, string userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            for (int retry = 0; retry < 3; retry++)
            {
                using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
                try
                {
                    var modifyDate = DateTime.UtcNow;

                    // Check existence with proper locking
                    var exists = await connection.ExecuteScalarAsync<bool>(
                        @"SELECT 1 FROM SkillMatrix WITH (UPDLOCK, ROWLOCK) 
                        WHERE ActivityId = @ActivityId AND TaskId = @TaskId",
                        new { ActivityId = activityId, TaskId = taskId },
                        transaction
                    );

                    if (exists)
                    {
                        // Update only metadata, preserve existing status
                        await connection.ExecuteAsync(
                            @"UPDATE SkillMatrix SET 
                                ModifyBy = @UserId, 
                                ModifyDate = @ModifyDate
                            WHERE ActivityId = @ActivityId AND TaskId = @TaskId",
                            new { ActivityId = activityId, TaskId = taskId, UserId = userId, ModifyDate = modifyDate },
                            transaction
                        );
                    }
                    else
                    {
                        // Insert with Visited status
                        await connection.ExecuteAsync(
                            @"INSERT INTO SkillMatrix 
                            (ActivityId, TaskId, HintsChecked, TotalTime, AttemptstoSolve, 
                             Status, CreateBy, CreateDate, ModifyBy, ModifyDate, TaskAttempt)
                            VALUES 
                            (@ActivityId, @TaskId, 0, 0, 0, @Status, 
                             @UserId, @ModifyDate, @UserId, @ModifyDate, 1)",
                            new
                            {
                                ActivityId = activityId,
                                TaskId = taskId,
                                UserId = userId,
                                ModifyDate = modifyDate,
                                Status = StatusTypes.Visited // Initial status
                            },
                            transaction
                        );
                    }

                    // Update Activity table (removed 'taskIndex')
                    await connection.ExecuteAsync(
                        "UPDATE Activity SET SectionId = @SectionId WHERE ActivityId = @ActivityId",
                        new { SectionId = sectionId, ActivityId = activityId },
                        transaction
                    );

                    transaction.Commit();
                    break;
                }
                catch (SqlException ex) when (ex.Number == 1205) // Deadlock
                {
                    transaction.Rollback();
                    if (retry == 2) throw;
                    await Task.Delay(100 * (retry + 1));
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
        public async Task<string> GetSoftwareIdBySectionId(string sectionId)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = "SELECT SoftwareId FROM Section WHERE SectionId = @SectionId";
            return await connection.QueryFirstOrDefaultAsync<string>(query, new { SectionId = sectionId });
        }
    }
}