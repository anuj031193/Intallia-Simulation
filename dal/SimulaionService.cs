//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//using JobSimulation.DAL.Interfaces;
//using JobSimulation.Models;
//using System;
//using System.Linq;
//using System.Threading.Tasks;

//namespace JobSimulation.DAL.Interfaces
//{



//    public class SimulationService : ISimulationService
//    {

//        private readonly ITaskRepository _taskRepository;

//        public SimulationService(
//            IActivityRepository activityRepository  ,
//            ISkillMatrixRepository skillMatrixRepository,
//            ITaskRepository taskRepository)
//        {
//            _activityRepository = activityRepository;
//            _skillMatrixRepository = skillMatrixRepository;
//            _taskRepository = taskRepository;
//        }

//        public async Task<Activity> StartNewActivityAsync(string userId, string simulationId, string sectionId)
//        {
//            var activityId = await _activityRepository.GenerateNewActivityIdAsync(userId, simulationId, sectionId);
//            var activity = new Activity
//            {
//                ActivityId = activityId,
//                UserId = userId,
//                SimulationId = simulationId,
//                SectionId = sectionId,
//                Status = StatusTypes.NotStarted,
//                CreateDate = DateTime.UtcNow,
//                ModifyDate = DateTime.UtcNow
//            };

//            await _activityRepository.CreateAsync(activity);
//            return activity;
//        }

//        public async Task<Activity> ContinueActivityAsync(string activityId)
//        {
//            return await _activityRepository.GetByIdAsync(activityId)
//                ?? throw new InvalidOperationException("Activity not found");
//        }

//        public async Task CompleteActivityAsync(string activityId)
//        {
//            var activity = await _activityRepository.GetByIdAsync(activityId);
//            activity.Status = StatusTypes.Completed;
//            await _activityRepository.UpdateAsync(activity);
//        }

//        public async Task<bool> CanRetrySectionAsync(string userId, string simulationId, string sectionId)
//        {
//            return await _activityRepository.CanRetrySectionAsync(userId, simulationId, sectionId);
//        }

//        public async Task<Activity> RetrySectionAsync(string userId, string simulationId, string sectionId)
//        {
//            if (!await CanRetrySectionAsync(userId, simulationId, sectionId))
//                throw new RetryLimitExceededException();

//            var newActivityId = await _activityRepository.CreateRetryActivityAsync(userId, simulationId, sectionId);
//            return await _activityRepository.GetByIdAsync(newActivityId);
//        }
//        public async Task SaveTaskProgressAsync(TaskProgress progress)
//        {
//            var existing = await _skillMatrixRepository.GetByTaskAsync(progress.ActivityId, progress.TaskId);

//            var skillMatrix = new SkillMatrix
//            {
//                ActivityId = progress.ActivityId,
//                TaskId = progress.TaskId,
//                HintsChecked = progress.HintsUsed,
//                TotalTime = progress.TimeSpent,
//                AttemptstoSolve = progress.Attempts,
//                Status = progress.IsCompleted ? StatusTypes.Completed : StatusTypes.Attempted,
//                TaskAttempt = (existing?.TaskAttempt ?? 0) + 1,
//                ModifyBy = progress.UserId,
//                ModifyDate = DateTime.UtcNow
//            };

//            await _skillMatrixRepository.UpsertAsync(skillMatrix);

//            // Update activity status if task was completed
//            if (progress.IsCompleted)
//            {
//                await UpdateActivityStatus(progress.ActivityId);
//            }
//        }

//        private async Task UpdateActivityStatus(string activityId)
//        {
//            var activity = await _activityRepository.GetByIdAsync(activityId);
//            var skillMatrixEntries = await _skillMatrixRepository.GetForActivityAsync(activityId);
//            var tasks = await _taskRepository.GetForActivityAsync(activityId);

//            var completedCount = skillMatrixEntries.Count(sm => sm.Status == StatusTypes.Completed);
//            var total = totalTasks.Count();
//            var completionRatio = (double)completedCount / total;
//            activity.Status = completionRatio switch
//            {
//                1 => StatusTypes.Completed,
//                >= 0.7 => StatusTypes.PartiallyCompleted,
//                > 0 => StatusTypes.InProgress,
//                _ => StatusTypes.NotStarted
//            };

//            activity.Result = await _activityRepository.CalculateResultAsync(activityId);
//            await _activityRepository.UpdateAsync(activity);
//        }

//        public async Task<SectionProgress> LoadProgressAsync(string activityId)
//        {
//            var activity = await _activityRepository.GetByIdAsync(activityId);
//            var tasks = await _taskRepository.GetForActivityAsync(activityId);
//            var skillMatrices = await _skillMatrixRepository.GetForActivityAsync(activityId);

//            return new SectionProgress
//            {
//                CurrentTaskIndex = activity.CurrentTaskIndex,
//                Tasks = tasks.Select(t => new TaskProgress
//                {
//                    Status = skillMatrices.FirstOrDefault(sm => sm.TaskId == t.TaskId)?.Status ?? "Not Started"
//                }).ToList()
//            };
//        }
//    }

//}