

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using JobSimulation.Models;

namespace JobSimulation.DAL
{
    public interface ITaskRepository
    {
        Task<JobTask> GetTaskByIdAsync(string taskId, string softwareId);
        Task<JobTask> GetNextTaskForSectionAsync(string sectionId, int currentTaskIndex);
        Task<List<JobTask>> GetTasksBySimulationAndSectionIdAsync(string simulationId, string sectionId, string activityId);
        Task<List<JobTask>> GetTasksByActivityIdAsync(string activityId, string userId);
        Task<string> GetSoftwareIdBySectionId(string sectionId);
        Task<List<JobTask>> GetTasksForSectionAsync(string sectionId);
        string GetConnectionString();
    }

    public class TaskRepository : ITaskRepository
    {
        private readonly string _connectionString;
        private static readonly Dictionary<string, string> SoftwareMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["s1"] = "DataOfExcel",
            ["s2"] = "DataOfWord",
            ["s3"] = "DataOfPPT",
            ["s7"] = "DataOfPowerBI"
        };
        public async Task<List<JobTask>> GetTasksForSectionAsync(string sectionId)
        {
            if (string.IsNullOrWhiteSpace(sectionId))
                throw new ArgumentException("SectionId cannot be null or empty.");

            const string query = @"
        SELECT TaskId, SectionId, [Order], Description, 
               CompanyId, CreateBy, CreateDate, ModifyBy, ModifyDate
        FROM Task
        WHERE SectionId = @SectionId
        ORDER BY [Order]";

            using var connection = new SqlConnection(_connectionString);
            return (await connection.QueryAsync<JobTask>(query, new { SectionId = sectionId })).ToList();
        }

        public TaskRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public string GetConnectionString() => _connectionString;

        public async Task<JobTask> GetTaskByIdAsync(string taskId, string softwareId)
        {
            if (string.IsNullOrWhiteSpace(taskId))
                throw new ArgumentException("Task ID cannot be null or empty.", nameof(taskId));

            if (!SoftwareMappings.TryGetValue(softwareId, out var tableName))
                throw new KeyNotFoundException($"Software type {softwareId} not supported.");

            const string baseQuery = @"
            SELECT 
                t.TaskId, t.SectionId, t.[Order], t.Description, t.CompanyId, 
                t.CreateBy, t.CreateDate, t.ModifyBy, t.ModifyDate,
                d.*
            FROM Task t
            LEFT JOIN {0} d ON t.TaskId = d.TaskId
            WHERE t.TaskId = @TaskId";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var query = string.Format(baseQuery, tableName);

            var record = await connection.QueryFirstOrDefaultAsync(query, new { TaskId = taskId });

            return record != null ? MapJobTaskWithDetails(record, softwareId) : null;
        }

        public async Task<JobTask> GetNextTaskForSectionAsync(string sectionId, int currentTaskIndex)
        {
            const string query = @"
            SELECT t.TaskId, s.SoftwareId
            FROM Task t
            INNER JOIN Section s ON t.SectionId = s.SectionId
            WHERE t.SectionId = @SectionId AND t.[Order] > @CurrentTaskIndex
            ORDER BY t.[Order]
            OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var result = await connection.QueryFirstOrDefaultAsync(query, new { SectionId = sectionId, CurrentTaskIndex = currentTaskIndex });
            return result != null ? await GetTaskByIdAsync(result.TaskId, result.SoftwareId) : null;
        }



        public async Task<List<JobTask>> GetTasksBySimulationAndSectionIdAsync(string simulationId, string sectionId, string activityId)
        {
            if (string.IsNullOrWhiteSpace(simulationId) || string.IsNullOrWhiteSpace(sectionId))
                throw new ArgumentException("SimulationId and SectionId cannot be null or empty.");

            var softwareId = await GetSoftwareIdBySectionId(sectionId);

            if (!SoftwareMappings.TryGetValue(softwareId, out var tableName))
                throw new KeyNotFoundException($"Software type {softwareId} not supported.");

            // Join only on TaskId, as software-specific tables don't have SectionId
            var query = $@"
        SELECT 
            t.TaskId, t.SectionId, t.[Order], t.Description,
            d.*,
            COALESCE(sm.Status, '{StatusTypes.NotStarted}') AS Status,
            COALESCE(sm.AttemptstoSolve, 0) AS AttemptstoSolve,
            COALESCE(sm.TaskAttempt, 0) AS TaskAttempt
        FROM Task t
        INNER JOIN Section s ON t.SectionId = s.SectionId
        INNER JOIN {tableName} d ON t.TaskId = d.TaskId
        LEFT JOIN SkillMatrix sm ON t.TaskId = sm.TaskId AND sm.ActivityId = @ActivityId
        WHERE s.SimulationId = @SimulationId 
          AND t.SectionId = @SectionId
        ORDER BY t.[Order]
    ";

            using var connection = new SqlConnection(_connectionString);

            var dynamicResults = await connection.QueryAsync(query, new
            {
                SimulationId = simulationId,
                SectionId = sectionId,
                ActivityId = activityId
            });

            var jobTasks = new List<JobTask>();
            foreach (var record in dynamicResults)
            {
                jobTasks.Add(MapJobTaskWithDetails(record, softwareId));
            }

            return jobTasks;
        }
        public async Task<List<JobTask>> GetTasksByActivityIdAsync(string activityId, string userId)
        {
            if (string.IsNullOrWhiteSpace(activityId) || string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("ActivityId and UserId cannot be null or empty.");

            const string activityInfoQuery = @"
                SELECT a.SectionId, s.SimulationId
                FROM Activity a
                INNER JOIN Section s ON a.SectionId = s.SectionId
                WHERE a.ActivityId = @ActivityId AND a.UserId = @UserId";

            using var connection = new SqlConnection(_connectionString);
            var activityInfo = await connection.QueryFirstOrDefaultAsync<(string SectionId, string SimulationId)>(
                activityInfoQuery,
                new { ActivityId = activityId, UserId = userId });

            if (activityInfo == default)
                return new List<JobTask>();

            var softwareId = await GetSoftwareIdBySectionId(activityInfo.SectionId);

            if (!SoftwareMappings.TryGetValue(softwareId, out var tableName))
                throw new KeyNotFoundException($"Software type {softwareId} not supported.");

            var query = $@"
                SELECT 
                    t.TaskId, t.SectionId, t.[Order], t.Description,
                    COALESCE(sm.Status, '{StatusTypes.NotStarted}') AS Status,
                    COALESCE(sm.AttemptstoSolve, 0) AS AttemptstoSolve,
                    COALESCE(sm.TaskAttempt, 0) AS TaskAttempt,
                    d.*
                FROM Task t
                INNER JOIN Section s ON t.SectionId = s.SectionId
                LEFT JOIN {tableName} d ON t.TaskId = d.TaskId
                LEFT JOIN SkillMatrix sm ON t.TaskId = sm.TaskId AND sm.ActivityId = @ActivityId
                WHERE s.SimulationId = @SimulationId 
                  AND t.SectionId = @SectionId
                ORDER BY t.[Order]";

            var results = await connection.QueryAsync(query, new
            {
                SimulationId = activityInfo.SimulationId,
                SectionId = activityInfo.SectionId,
                ActivityId = activityId
            });

            var jobTasks = new List<JobTask>();
            foreach (var record in results)
            {
                jobTasks.Add(MapJobTaskWithDetails(record, softwareId));
            }
            return jobTasks;
        }

        public async Task<string> GetSoftwareIdBySectionId(string sectionId)
        {
            if (string.IsNullOrWhiteSpace(sectionId))
                throw new ArgumentException("SectionId cannot be null or empty.");

            const string query = "SELECT SoftwareId FROM Section WHERE SectionId = @SectionId";

            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<string>(query, new { SectionId = sectionId });
        }

        private static JobTask MapJobTaskWithDetails(dynamic record, string softwareId)
        {
            var task = new JobTask
            {
                TaskId = record.TaskId,
                SectionId = record.SectionId,
                Order = record.Order,
                Description = record.Description,
                CompanyId = record.CompanyId,
                CreateBy = record.CreateBy,
                CreateDate = record.CreateDate,
                ModifyBy = record.ModifyBy,
                ModifyDate = record.ModifyDate
            };

            switch (softwareId?.ToLower())
            {
                case "s1": // Excel
                    task.Details = new ExcelTaskDetails
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
                    };
                    break;
                case "s2": // Word
                    task.Details = new WordTaskDetails
                    {
                        TaskDescription = record.TaskDescription,
                        TaskLocation = record.TaskLocation,
                        Hint = record.Hint,
                        SkillName = record.SkillName,
                        SkillScore = record.SkillScore
                    };
                    break;
                case "s3": // PowerPoint
                    task.Details = new PowerPointTaskDetails
                    {
                        TaskDescription = record.TaskDescription,
                        SlideName = record.SlideName,
                        SlideNo = record.SlideNo,
                        ObjectName = record.ObjectName,
                        Hint = record.Hint,
                        SkillName = record.SkillName,
                        SkillScore = record.SkillScore
                    };
                    break;
                case "s7": // PowerPoint
                    task.Details = new PowerBiDetails
                    {
                        TaskDescription = record.TaskDescription,
                        SelectTask = record.SelectTask,
                    
                        Hint = record.Hint,
                        SkillName = record.SkillName,
                        SkillScore = record.SkillScore
                    };
                    break;
                default:
                    throw new NotSupportedException($"Software type {softwareId} not supported.");
            }

            return task;
        }
    }
}

