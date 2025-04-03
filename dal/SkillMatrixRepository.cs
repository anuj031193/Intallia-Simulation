using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using JobSimulation.Models;
using System.Data;
using System.Diagnostics;

namespace JobSimulation.DAL
{
    public class SkillMatrixRepository
    {
        private readonly string _connectionString;
        public SkillMatrixRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<SkillMatrix> GetByTaskAsync(string activityId, string taskId)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<SkillMatrix>(
                "SELECT * FROM SkillMatrix WHERE ActivityId = @activityId AND TaskId = @taskId",
                new { activityId, taskId });
        }

        public async Task UpsertAsync(SkillMatrix skillMatrix)
        {
            using var connection = new SqlConnection(_connectionString);
            var existing = await GetByTaskAsync(skillMatrix.ActivityId, skillMatrix.TaskId);

            if (existing != null)
            {
                await connection.ExecuteAsync(
                    @"UPDATE SkillMatrix SET 
                    HintsChecked = @HintsChecked,
                    TotalTime = @TotalTime,
                    AttemptstoSolve = @AttemptstoSolve,
                    Status = @Status,
                    ModifyDate = @ModifyDate
                WHERE ActivityId = @ActivityId AND TaskId = @TaskId",
                    skillMatrix);
            }
            else
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO SkillMatrix 
                (ActivityId, TaskId, HintsChecked, TotalTime, AttemptstoSolve, Status, CreateDate, ModifyDate)
                VALUES (@ActivityId, @TaskId, @HintsChecked, @TotalTime, @AttemptstoSolve, @Status, @CreateDate, @ModifyDate)",
                    skillMatrix);
            }
        }

        public async Task<SkillMatrix> GetLastTaskProgressAsync(string userId, string sectionId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT TOP 1 sm.*
                FROM SkillMatrix sm
                INNER JOIN Activity a ON sm.ActivityId = a.ActivityId
                WHERE a.UserId = @UserId AND a.SectionId = @SectionId
                ORDER BY sm.ModifyDate DESC";

            return await connection.QueryFirstOrDefaultAsync<SkillMatrix>(query, new { UserId = userId, SectionId = sectionId });
        }

        public async Task SaveSkillMatrixAsync(SkillMatrix skillMatrix, string userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
        IF EXISTS (SELECT 1 FROM SkillMatrix WHERE ActivityId = @ActivityId AND TaskId = @TaskId)
        BEGIN
            UPDATE SkillMatrix 
            SET HintsChecked = @HintsChecked,
                TotalTime = @TotalTime,
                AttemptstoSolve = @AttemptstoSolve,
                Status = @Status,
                ModifyBy = @ModifyBy,
                ModifyDate = @ModifyDate,
                TaskAttempt = @TaskAttempt
            WHERE ActivityId = @ActivityId AND TaskId = @TaskId
        END
        ELSE
        BEGIN
            INSERT INTO SkillMatrix (ActivityId, TaskId, HintsChecked, TotalTime, AttemptstoSolve, Status, 
                                   CreateBy, CreateDate, ModifyBy, ModifyDate, TaskAttempt)
            VALUES (@ActivityId, @TaskId, @HintsChecked, @TotalTime, @AttemptstoSolve, 
                   @Status, @CreateBy, @CreateDate, @ModifyBy, @ModifyDate, @TaskAttempt)
        END";

            await connection.ExecuteAsync(query, new
            {
                skillMatrix.ActivityId,
                skillMatrix.TaskId,
                skillMatrix.HintsChecked,
                skillMatrix.TotalTime,
                skillMatrix.AttemptstoSolve,
                skillMatrix.Status,
                CreateBy = skillMatrix.CreateBy,
                CreateDate = ValidateDateTime(skillMatrix.CreateDate),
                ModifyBy = userId,
                ModifyDate = ValidateDateTime(skillMatrix.ModifyDate),
                skillMatrix.TaskAttempt
            });
        }

        public async Task UpdateSkillMatrixAsync(SkillMatrix skillMatrix, string userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
        UPDATE SkillMatrix 
        SET HintsChecked = @HintsChecked, TotalTime = @TotalTime, AttemptstoSolve = @AttemptstoSolve, 
            Status = @Status, ModifyBy = @ModifyBy, ModifyDate = @ModifyDate, TaskAttempt = @TaskAttempt
        WHERE ActivityId = @ActivityId AND TaskId = @TaskId";

            await connection.ExecuteAsync(query, new
            {
                skillMatrix.ActivityId,
                skillMatrix.TaskId,
                skillMatrix.HintsChecked,
                skillMatrix.TotalTime,
                skillMatrix.AttemptstoSolve,
                skillMatrix.Status,
                ModifyBy = userId,
                ModifyDate = ValidateDateTime(skillMatrix.ModifyDate), // Validate ModifyDate
                skillMatrix.TaskAttempt
            });
        }

        private DateTime ValidateDateTime(DateTime dateTime)
        {
            if (dateTime < (DateTime)System.Data.SqlTypes.SqlDateTime.MinValue)
            {
                return (DateTime)System.Data.SqlTypes.SqlDateTime.MinValue;
            }
            if (dateTime > (DateTime)System.Data.SqlTypes.SqlDateTime.MaxValue)
            {
                return (DateTime)System.Data.SqlTypes.SqlDateTime.MaxValue;
            }
            return dateTime;
        }

        public async Task AddSkillMatrixAsync(SkillMatrix skillMatrix, string userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                INSERT INTO SkillMatrix (ActivityId, TaskId, HintsChecked, TotalTime, AttemptstoSolve, Status, CreateBy, CreateDate, ModifyBy, ModifyDate, TaskAttempt)
                VALUES (@ActivityId, @TaskId, @HintsChecked, @TotalTime, @AttemptstoSolve, @Status, @CreateBy, @CreateDate, @ModifyBy, @ModifyDate, @TaskAttempt)";

            await connection.ExecuteAsync(query, new
            {
                skillMatrix.ActivityId,
                skillMatrix.TaskId,
                skillMatrix.HintsChecked,
                skillMatrix.TotalTime,
                skillMatrix.AttemptstoSolve,
                skillMatrix.Status,
                CreateBy = "admin",
                skillMatrix.CreateDate,
                ModifyBy = userId,
                skillMatrix.ModifyDate,
                skillMatrix.TaskAttempt
            });
        }

        public async Task<int> GetCurrentTaskIndexAsync(string activityId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT TaskAttempt FROM SkillMatrix WHERE ActivityId = @ActivityId ORDER BY ModifyDate DESC";
            var result = await connection.QueryFirstOrDefaultAsync<int?>(query, new { ActivityId = activityId });

            return result ?? 0;
        }

        public async Task<int> GetTimeSpentForTaskAsync(string activityId, string taskId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT TotalTime
                FROM SkillMatrix
                WHERE ActivityId = @ActivityId AND TaskId = @TaskId";

            var result = await connection.QueryFirstOrDefaultAsync<int?>(query, new { ActivityId = activityId, TaskId = taskId });

            return result ?? 0;
        }

        public async Task<SkillMatrix> GetSkillMatrixByTaskId(string activityId, string taskId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM SkillMatrix WHERE ActivityId = @ActivityId AND TaskId = @TaskId";
            return await connection.QueryFirstOrDefaultAsync<SkillMatrix>(query, new { ActivityId = activityId, TaskId = taskId });
        }

        public async Task<int> GetTaskAttemptCount(string taskId, string userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM SkillMatrix WHERE TaskId = @TaskId AND CreateBy = @UserId";

            var result = await connection.ExecuteScalarAsync<int?>(query, new { TaskId = taskId, UserId = userId });

            return result ?? 0;
        }

        public async Task SaveTaskTimeAsync(string activityId, string taskId, int timeElapsed, string userId)
        {
            var skillMatrix = await GetSkillMatrixByTaskId(activityId, taskId);
            if (skillMatrix != null)
            {
                skillMatrix.TotalTime = timeElapsed;
                skillMatrix.ModifyDate = DateTime.UtcNow;
                await UpdateSkillMatrixAsync(skillMatrix, userId);
            }
            else
            {
                var newSkillMatrix = new SkillMatrix
                {
                    ActivityId = activityId,
                    TaskId = taskId,
                    TotalTime = timeElapsed,
                    CreateDate = DateTime.UtcNow,
                    ModifyDate = DateTime.UtcNow,
                    CreateBy = "admin",
                    ModifyBy = userId
                };
                await AddSkillMatrixAsync(newSkillMatrix, userId);
            }
        }

        public async Task<IEnumerable<SkillMatrix>> GetSkillMatrixEntriesForActivityAsync(string activityId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM SkillMatrix WHERE ActivityId = @ActivityId";
            return await connection.QueryAsync<SkillMatrix>(query, new { ActivityId = activityId });
        }

        public async Task<bool> DoAllTasksHaveRecords(string activityId, List<JobTask> allTasks)
        {
            var entries = await GetSkillMatrixEntriesForActivityAsync(activityId);
            return allTasks.All(task => entries.Any(e => e.TaskId == task.TaskId));
        }

        public async Task<bool> HaveAllTasksBeenVisited(string activityId, List<JobTask> allTasks)
        {
            var skillMatrixEntries = await GetSkillMatrixEntriesForActivityAsync(activityId);

            return allTasks.All(task =>
                skillMatrixEntries.Any(entry =>
                    entry.TaskId == task.TaskId &&
                    (entry.Status == StatusTypes.Visited ||
                     entry.Status == StatusTypes.InComplete ||
                     entry.Status == StatusTypes.Completed)
                )
            );
        }

        public async Task<SkillMatrix> GetMostRecentIncompleteTask(string activityId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT TOP 1 *
                FROM SkillMatrix
                WHERE ActivityId = @ActivityId AND Status != @CompletedStatus
                ORDER BY ModifyDate DESC";

            return await connection.QueryFirstOrDefaultAsync<SkillMatrix>(
                query,
                new
                {
                    ActivityId = activityId,
                    CompletedStatus = StatusTypes.Completed
                });
        }

        public async Task BulkUpsertSkillMatrixAsync(IEnumerable<SkillMatrix> entries)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                await connection.ExecuteAsync(@"
            CREATE TABLE #TempSkillMatrix (
                ActivityId NVARCHAR(100),
                TaskId NVARCHAR(100),
                HintsChecked INT,
                TotalTime INT,
                AttemptstoSolve INT,
                Status NVARCHAR(50),
                TaskAttempt INT,
                ModifyBy NVARCHAR(100),
                ModifyDate DATETIME
            )", transaction: transaction);

                using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = "#TempSkillMatrix";
                    await bulkCopy.WriteToServerAsync(CreateDataTable(entries));
                }

                await connection.ExecuteAsync(@"
            MERGE SkillMatrix AS target
            USING #TempSkillMatrix AS source
            ON target.ActivityId = source.ActivityId AND target.TaskId = source.TaskId
            WHEN MATCHED THEN
                UPDATE SET 
                    HintsChecked = source.HintsChecked,
                    TotalTime = source.TotalTime,
                    AttemptstoSolve = source.AttemptstoSolve,
                    Status = source.Status,
                    TaskAttempt = source.TaskAttempt,
                    ModifyBy = source.ModifyBy,
                    ModifyDate = source.ModifyDate
            WHEN NOT MATCHED THEN
                INSERT (ActivityId, TaskId, HintsChecked, TotalTime, AttemptstoSolve, 
                       Status, TaskAttempt, CreateBy, CreateDate, ModifyBy, ModifyDate)
                VALUES (source.ActivityId, source.TaskId, source.HintsChecked, source.TotalTime, 
                       source.AttemptstoSolve, source.Status, source.TaskAttempt, 
                       source.ModifyBy, source.ModifyDate, source.ModifyBy, source.ModifyDate)",
                    transaction: transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private DataTable CreateDataTable(IEnumerable<SkillMatrix> entries)
        {
            var table = new DataTable();
            table.Columns.Add("ActivityId", typeof(string));
            table.Columns.Add("TaskId", typeof(string));
            table.Columns.Add("HintsChecked", typeof(int));
            table.Columns.Add("TotalTime", typeof(int));
            table.Columns.Add("AttemptstoSolve", typeof(int));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("TaskAttempt", typeof(int));
            table.Columns.Add("ModifyBy", typeof(string));
            table.Columns.Add("ModifyDate", typeof(DateTime));
            foreach (var entry in entries)
                table.Rows.Add(entry.ActivityId, entry.TaskId, entry.HintsChecked, entry.TotalTime, entry.AttemptstoSolve, entry.Status, entry.TaskAttempt, entry.ModifyBy, entry.ModifyDate);
            return table;
        }

        public async Task<IEnumerable<SkillMatrix>> GetForActivityAsync(string activityId)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<SkillMatrix>(
                "SELECT * FROM SkillMatrix WHERE ActivityId = @ActivityId",
                new { ActivityId = activityId });
        }

        public async Task<int> GetTimeSpentAsync(string activityId, string taskId)
        {
            using var connection = new SqlConnection(_connectionString);
            var result = await connection.ExecuteScalarAsync<int?>(
                "SELECT TotalTime FROM SkillMatrix WHERE ActivityId = @ActivityId AND TaskId = @TaskId",
                new { ActivityId = activityId, TaskId = taskId });
            return result ?? 0;
        }

        public async Task<int> GetAttemptCountAsync(string taskId, string userId)
        {
            using var connection = new SqlConnection(_connectionString);
            var result = await connection.ExecuteScalarAsync<int?>(
                "SELECT COUNT(*) FROM SkillMatrix WHERE TaskId = @TaskId AND CreateBy = @UserId",
                new { TaskId = taskId, UserId = userId });
            return result ?? 0;
        }

        public async Task<int> GetElapsedTimeForTaskAsync(string activityId, string taskId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT TotalTime FROM SkillMatrix WHERE ActivityId = @ActivityId AND TaskId = @TaskId";
            Debug.WriteLine($"Executing query: {query} with ActivityId: {activityId}, TaskId: {taskId}");
            var result = await connection.QueryFirstOrDefaultAsync<int?>(query, new { ActivityId = activityId, TaskId = taskId });

            return result ?? 0;
        }

        public async Task BatchUpdateSkillMatrixAsync(IEnumerable<SkillMatrix> entries)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var entry in entries)
            {
                await connection.ExecuteAsync(@"
            UPDATE SkillMatrix 
            SET Status = @Status,
                HintsChecked = @HintsChecked,
                TotalTime = @TotalTime,
                AttemptstoSolve = @AttemptstoSolve,
                ModifyDate = @ModifyDate
            WHERE ActivityId = @ActivityId AND TaskId = @TaskId",
                    new
                    {
                        entry.ActivityId,
                        entry.TaskId,
                        entry.Status,
                        entry.HintsChecked,
                        entry.TotalTime,
                        entry.AttemptstoSolve,
                        ModifyDate = DateTime.UtcNow
                    });
            }
        }
    }
}