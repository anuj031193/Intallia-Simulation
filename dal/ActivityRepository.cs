using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using JobSimulation.BLL;
using JobSimulation.Models;
using Activity = JobSimulation.Models.Activity;

namespace JobSimulation.DAL
{
    public class ActivityRepository
    {
        private readonly string _connectionString;
        private readonly SectionService _sectionService;
        private readonly SkillMatrixRepository _skillMatrixRepository;
        private readonly TaskRepository _taskRepository;

        public ActivityRepository(string connectionString, SectionService sectionService, SkillMatrixRepository skillMatrixRepository, TaskRepository taskRepository)
        {
            _connectionString = connectionString;
            _sectionService = sectionService;
            _skillMatrixRepository = skillMatrixRepository;
            _taskRepository = taskRepository;
        }

        public async Task<string> CreateAsync(Activity activity)
        {
            using var connection = new SqlConnection(_connectionString);
            activity.ActivityId = GenerateActivityId(activity);
            activity.CreateDate = DateTime.UtcNow;
            activity.ModifyDate = DateTime.UtcNow;
            await connection.ExecuteAsync(@"
                INSERT INTO Activity (ActivityId, UserId, SimulationId, SectionId, Status, 
                    SectionAttempt, StudentFile, CreateDate, ModifyDate, CreateBy, ModifyBy)
                VALUES (@ActivityId, @UserId, @SimulationId, @SectionId, @Status, 
                    @SectionAttempt, @StudentFile, @CreateDate, @ModifyDate, @CreateBy, @ModifyBy)",
                activity);
            return activity.ActivityId;
        }

        public async Task UpdateAsync(Activity activity)
        {
            using var connection = new SqlConnection(_connectionString);
            activity.ModifyDate = DateTime.UtcNow;
            await connection.ExecuteAsync(@"
                UPDATE Activity SET 
                    Status = @Status,
                    SectionAttempt = @SectionAttempt,
                    StudentFile = @StudentFile,
                    ModifyDate = @ModifyDate,
                    ModifyBy = @ModifyBy,
                    Result = @Result
                WHERE ActivityId = @ActivityId", activity);
        }

        public async Task<Activity> GetByIdAsync(string activityId)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Activity>(
                "SELECT * FROM Activity WHERE ActivityId = @ActivityId",
                new { ActivityId = activityId });
        }

        private string GenerateActivityId(Activity activity)
        {
            return $"{activity.SimulationId}-{activity.SectionId}-{activity.UserId}-{DateTime.UtcNow:yyyyMMddHHmmss}-A1";
        }

        public async Task<Activity> GetLatestForUserAsync(string userId, string simulationId)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Activity>(
                @"SELECT TOP 1 * FROM Activity 
                WHERE UserId = @userId AND SimulationId = @simulationId
                ORDER BY ModifyDate DESC",
                new { userId, simulationId });
        }

        public async Task<string> CalculateResultAsync(string activityId)
        {
            var skillMatrices = await _skillMatrixRepository.GetForActivityAsync(activityId);
            if (skillMatrices == null || !skillMatrices.Any())
                return StatusTypes.NeedsImprovement;

            var taskResults = new List<(int attempt, int hints)>();

            foreach (var task in skillMatrices.GroupBy(sm => sm.TaskId))
            {
                var latest = task.OrderByDescending(sm => sm.ModifyDate).First();
                if (latest.Status == StatusTypes.Completed)
                {
                    taskResults.Add((latest.TaskAttempt, latest.HintsChecked));
                }
                else
                {
                    // Task was never completed successfully
                    return StatusTypes.NeedsImprovement;
                }
            }

            // Determine overall result based on worst performance
            if (taskResults.Any(r => r.attempt >= 3))
                return StatusTypes.Developing;

            if (taskResults.Any(r => r.attempt == 2))
                return taskResults.All(r => r.attempt == 2 && r.hints == 0)
                    ? StatusTypes.Proficient
                    : StatusTypes.Developing;

            if (taskResults.All(r => r.attempt == 1 && r.hints == 0))
                return StatusTypes.Mastered;

            if (taskResults.All(r => r.attempt == 1))
                return StatusTypes.Proficient;

            return StatusTypes.Developing;
        }
        public async Task<string> GetLastSectionForUserAsync(string userId, string simulationId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT TOP 1 SectionId FROM Activity WHERE UserId = @UserId AND SimulationId = @SimulationId ORDER BY ModifyDate DESC";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@SimulationId", simulationId);

            var sectionId = await command.ExecuteScalarAsync() as string;
            return sectionId;
        }

        public async Task SaveActivityAsync(Activity activity, string calculatedResult = null)
        {
            // Validate Status
            var validStatuses = new[]
            {
        StatusTypes.NotStarted,
        StatusTypes.InProgress,
        StatusTypes.Completed,
        StatusTypes.InComplete,
        StatusTypes.PartiallyCompleted
    };

            if (!validStatuses.Contains(activity.Status))
            {
                throw new ArgumentException("Invalid status value");
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string finalResult = calculatedResult ?? activity.Result;

            var query = @"
    IF EXISTS (SELECT 1 FROM Activity WHERE ActivityId = @ActivityId)
    BEGIN
        UPDATE Activity 
        SET Status = @Status,
            SectionAttempt = @SectionAttempt,
            StudentFile = @StudentFile,
            ModifyDate = @ModifyDate,
            ModifyBy = @ModifyBy,
            Result = @Result
        WHERE ActivityId = @ActivityId
    END
    ELSE
    BEGIN
        INSERT INTO Activity (
            ActivityId, UserId, SimulationId, SectionId, 
            Status, SectionAttempt, StudentFile, 
            CreateDate, ModifyDate, CreateBy, ModifyBy, Result
        )
        VALUES (
            @ActivityId, @UserId, @SimulationId, @SectionId,
            @Status, @SectionAttempt, @StudentFile,
            @CreateDate, @ModifyDate, @CreateBy, @ModifyBy, @Result
        )
    END";

            await connection.ExecuteAsync(query, new
            {
                activity.ActivityId,
                activity.UserId,
                activity.SimulationId,
                activity.SectionId,
                activity.Status,
                activity.SectionAttempt,
                activity.StudentFile,
                activity.CreateDate,
                activity.ModifyDate,
                activity.CreateBy,
                activity.ModifyBy,
                Result = finalResult
            });
        }
        private string CalculateDefaultResult(string status)
        {
            return status == StatusTypes.Completed ? "Completed" : null;
        }

        public async Task UpdateActivityAsync(Activity activity)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
            UPDATE Activity 
            SET 
                Status = @Status,
                Result = @Result,
                ModifyBy = @ModifyBy,
                ModifyDate = @ModifyDate
            WHERE 
                ActivityId = @ActivityId";

            Debug.WriteLine($"Executing Update: Status={activity.Status}, Result={activity.Result}, ActivityId={activity.ActivityId}");

            int affectedRows = await connection.ExecuteAsync(query, new
            {
                activity.ActivityId,
                activity.Status,
                activity.Result,
                activity.ModifyBy,
                ModifyDate = DateTime.UtcNow
            });

            if (affectedRows == 0)
            {
                throw new InvalidOperationException($"No activity found with ID {activity.ActivityId}");
            }
        }

        public async Task UpdateActivityResultAsync(string activityId, string result, string userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
            UPDATE Activity 
            SET Result = @Result, 
                ModifyDate = @ModifyDate, 
                ModifyBy = @ModifyBy 
            WHERE ActivityId = @ActivityId";

            await connection.ExecuteAsync(query, new
            {
                ActivityId = activityId,
                Result = result,
                ModifyDate = DateTime.UtcNow,
                ModifyBy = userId
            });
        }

        public async Task<Activity> GetLastSessionForUserAsync(string userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT TOP 1 * FROM Activity WHERE UserId = @UserId ORDER BY ModifyDate DESC";
                var activity = await connection.QueryFirstOrDefaultAsync<Activity>(query, new { UserId = userId });

                return activity;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetLastSessionForUserAsync: {ex.Message}");
                return null;
            }
        }
        public async Task<bool> HaveAllTasksBeenVisited(string activityId, int totalTasksInSection)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
            SELECT COUNT(*) 
            FROM SkillMatrix 
            WHERE ActivityId = @ActivityId 
            AND Status IN ('Visited', 'Incomplete', 'Completed')";

            var visitedCount = await connection.ExecuteScalarAsync<int>(query, new { ActivityId = activityId });

            return visitedCount >= totalTasksInSection;
        }

        public async Task<Activity> GetActivityBySimulationAndSection(string simulationId, string sectionId, string userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
            SELECT * 
            FROM Activity 
            WHERE SimulationId = @SimulationId 
              AND SectionId = @SectionId 
              AND UserId = @UserId 
            ORDER BY ModifyDate DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SimulationId", simulationId);
            command.Parameters.AddWithValue("@SectionId", sectionId);
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var activity = new Activity
                {
                    ActivityId = reader["ActivityId"].ToString(),
                    UserId = reader["UserId"].ToString(),
                    SimulationId = reader["SimulationId"].ToString(),
                    SectionId = reader["SectionId"].ToString(),
                    Status = reader["Status"].ToString(),
                    SectionAttempt = Convert.ToInt32(reader["SectionAttempt"]),
                    StudentFile = reader["StudentFile"].ToString(),
                    CreateDate = Convert.ToDateTime(reader["CreateDate"]),
                    ModifyDate = Convert.ToDateTime(reader["ModifyDate"]),
                    CreateBy = reader["CreateBy"].ToString(),
                    ModifyBy = reader["ModifyBy"].ToString(),
                    Result = reader["Result"].ToString()
                };

                var softwareId = await GetSoftwareIdBySectionId(sectionId);
                var currentTaskIndex = await GetLastTaskIndexForActivity(activity.ActivityId, sectionId, softwareId, userId);

                return activity;
            }

            return null;
        }

        private async Task<int> GetLastTaskIndexForActivity(string activityId, string sectionId, string softwareId, string userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
            SELECT TOP 1 TaskId 
            FROM SkillMatrix 
            WHERE ActivityId = @ActivityId 
            ORDER BY ModifyDate DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ActivityId", activityId);

            var lastTaskId = await command.ExecuteScalarAsync() as string;

            if (!string.IsNullOrEmpty(lastTaskId))
            {
                var tasks = await _sectionService.GetAllTasksForSectionAsync(sectionId, softwareId);
                var task = tasks.FirstOrDefault(t => t.TaskId == lastTaskId);
                if (task != null)
                {
                    return tasks.IndexOf(task);
                }
            }

            return 0; // Default to the first task if no task is found
        }

        public async Task<string> GetSoftwareIdBySectionId(string sectionId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT SoftwareId FROM Section WHERE SectionId = @SectionId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SectionId", sectionId);

            var softwareId = await command.ExecuteScalarAsync() as string;
            return softwareId;
        }

        public async Task<Activity> GetActivityByIdAsync(string activityId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM Activity WHERE ActivityId = @ActivityId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ActivityId", activityId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Activity
                {
                    ActivityId = reader["ActivityId"].ToString(),
                    UserId = reader["UserId"].ToString(),
                    SimulationId = reader["SimulationId"].ToString(),
                    SectionId = reader["SectionId"].ToString(),
                    Status = reader["Status"].ToString(),
                    SectionAttempt = Convert.ToInt32(reader["SectionAttempt"]),
                    StudentFile = reader["StudentFile"].ToString(),
                    CreateDate = Convert.ToDateTime(reader["CreateDate"]),
                    ModifyDate = Convert.ToDateTime(reader["ModifyDate"]),
                    CreateBy = reader["CreateBy"].ToString(),
                    ModifyBy = reader["ModifyBy"].ToString(),
                    Result = reader["Result"].ToString()
                };
            }

            return null;
        }

        public async Task<string> GenerateNewActivityIdAsync(string userId, string simulationId, string sectionId, bool increaseAttempt = false)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var count = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Activity WHERE UserId = @UserId AND SimulationId = @SimulationId AND SectionId = @SectionId",
                new { UserId = userId, SimulationId = simulationId, SectionId = sectionId });

            if (increaseAttempt)
            {
                await connection.ExecuteAsync(
                    "UPDATE Activity SET SectionAttempt = SectionAttempt + 1 WHERE UserId = @UserId AND SimulationId = @SimulationId AND SectionId = @SectionId",
                    new { UserId = userId, SimulationId = simulationId, SectionId = sectionId });
            }

            return $"{simulationId}-{sectionId}-{userId}-{DateTime.UtcNow:yyyyMMddHHmm}-A{count + 1}";
        }

        public async Task<bool> CanRetrySectionAsync(string userId, string simulationId, string sectionId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM Activity WHERE UserId = @UserId AND SimulationId = @SimulationId AND SectionId = @SectionId";
            using var command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@SimulationId", simulationId);
            command.Parameters.AddWithValue("@SectionId", sectionId);

            var attemptCount = (int)await command.ExecuteScalarAsync();

            return attemptCount < 3;
        }

        public async Task DuplicateSkillMatrixEntriesAsync(string oldActivityId, string newActivityId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
            INSERT INTO SkillMatrix (ActivityId, TaskId, HintsChecked, TotalTime, AttemptstoSolve, Status, CreateBy, CreateDate, ModifyBy, ModifyDate, TaskAttempt)
            SELECT @NewActivityId, TaskId, HintsChecked, TotalTime, AttemptstoSolve, Status, CreateBy, CreateDate, ModifyBy, ModifyDate, TaskAttempt
            FROM SkillMatrix
            WHERE ActivityId = @OldActivityId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@NewActivityId", newActivityId);
            command.Parameters.AddWithValue("@OldActivityId", oldActivityId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<string> CalculateActivityResult(string activityId)
        {
            var tasks = await _taskRepository.GetTasksForActivityAsync(activityId);
            if (tasks == null || !tasks.Any())
                return StatusTypes.NeedsImprovement;

            var skillMatrixEntries = await _skillMatrixRepository.GetForActivityAsync(activityId);
            if (skillMatrixEntries == null || !skillMatrixEntries.Any())
                return StatusTypes.NeedsImprovement;

            var taskResults = new List<(int attempt, int hints)>();

            foreach (var task in skillMatrixEntries.GroupBy(sm => sm.TaskId))
            {
                var latest = task.OrderByDescending(sm => sm.ModifyDate).First();
                if (latest.Status == StatusTypes.Completed)
                {
                    taskResults.Add((latest.TaskAttempt, latest.HintsChecked));
                }
                else
                {
                    // Task was never completed successfully
                    return StatusTypes.NeedsImprovement;
                }
            }

            // Determine overall result based on worst performance
            if (taskResults.Any(r => r.attempt >= 3))
                return StatusTypes.Developing;

            if (taskResults.Any(r => r.attempt == 2))
                return taskResults.All(r => r.attempt == 2 && r.hints == 0)
                    ? StatusTypes.Proficient
                    : StatusTypes.Developing;

            if (taskResults.All(r => r.attempt == 1 && r.hints == 0))
                return StatusTypes.Mastered;

            if (taskResults.All(r => r.attempt == 1))
                return StatusTypes.Proficient;

            return StatusTypes.Developing;
        }
        public async Task<Activity> GetLatestActivityAsync(string userId, string simulationId, string sectionId)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Activity>(
                "SELECT TOP 1 * FROM Activity WHERE UserId = @UserId AND SimulationId = @SimulationId AND SectionId = @SectionId ORDER BY ModifyDate DESC",
                new { UserId = userId, SimulationId = simulationId, SectionId = sectionId });
        }
        public async Task<Activity> GetPreviousActivityAsync(string userId, string simulationId, string currentSectionId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var activity = await connection.QueryFirstOrDefaultAsync<Activity>(@"
            SELECT TOP 1 * 
            FROM Activity 
            WHERE UserId = @UserId 
              AND SimulationId = @SimulationId 
              AND SectionId != @SectionId 
              AND ModifyDate < (
                  SELECT MAX(ModifyDate) 
                  FROM Activity 
                  WHERE UserId = @UserId 
                    AND SimulationId = @SimulationId 
                    AND SectionId = @SectionId
              )
            ORDER BY ModifyDate DESC",
                    new { UserId = userId, SimulationId = simulationId, SectionId = currentSectionId });

                return activity;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetPreviousActivityAsync: {ex.Message}");
                return null;
            }
        }
        public async Task<string> CreateRetryActivityAsync(string userId, string simulationId, string sectionId)
        {
            using var connection = new SqlConnection(_connectionString);

            var previousActivity = await GetLatestActivityAsync(userId, simulationId, sectionId);
            if (previousActivity == null) throw new InvalidOperationException("No previous activity found");

            var newActivityId = await GenerateNewActivityIdAsync(userId, simulationId, sectionId, true);

            // Duplicate skill matrix entries but reset their status
            await connection.ExecuteAsync(
                @"INSERT INTO SkillMatrix (ActivityId, TaskId, Status, HintsChecked, TotalTime, 
           AttemptstoSolve, CreateBy, CreateDate, ModifyBy, ModifyDate, TaskAttempt)
          SELECT @NewActivityId, TaskId, 
                 CASE WHEN Status = @Completed THEN @Visited ELSE Status END,
                 HintsChecked, TotalTime, AttemptstoSolve, 
                 CreateBy, CreateDate, ModifyBy, ModifyDate, TaskAttempt
          FROM SkillMatrix
          WHERE ActivityId = @PreviousActivityId",
                new
                {
                    NewActivityId = newActivityId,
                    PreviousActivityId = previousActivity.ActivityId,
                    Completed = StatusTypes.Completed,
                    Visited = StatusTypes.Visited
                });

            // Create new activity with NotStarted status
            var newActivity = new Activity
            {
                ActivityId = newActivityId,
                UserId = userId,
                SimulationId = simulationId,
                SectionId = sectionId,
                Status = StatusTypes.NotStarted, // Proper initial status
                SectionAttempt = previousActivity.SectionAttempt + 1,
                StudentFile = previousActivity.StudentFile,
                CreateDate = DateTime.UtcNow,
                ModifyDate = DateTime.UtcNow,
                CreateBy = userId,
                ModifyBy = userId,
                Result = StatusTypes.NotStarted
            };

            await SaveActivityAsync(newActivity);
            return newActivityId;
        }
        public async Task<Activity> GetLastActivityForSectionAsync(string userId, string simulationId, string sectionId)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Activity>(
                @"SELECT TOP 1 * FROM Activity 
        WHERE UserId = @UserId AND SimulationId = @SimulationId AND SectionId = @SectionId
        ORDER BY ModifyDate DESC",
                new { UserId = userId, SimulationId = simulationId, SectionId = sectionId });
        }

        public async Task UpdateActivityStudentFileAsync(string activityId, string studentFile, string userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE Activity 
        SET StudentFile = @StudentFile, 
            ModifyBy = @UserId, 
            ModifyDate = @ModifyDate 
        WHERE ActivityId = @ActivityId",
                new
                {
                    ActivityId = activityId,
                    StudentFile = studentFile,
                    UserId = userId,
                    ModifyDate = DateTime.UtcNow
                });
        }
    }
}