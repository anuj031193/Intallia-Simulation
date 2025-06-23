
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JobSimulation.DAL;
using JobSimulation.Models;

namespace JobSimulation.BLL
{
    public class TaskService
    {
        private readonly SectionRepository _sectionRepository;
        private readonly SkillMatrixRepository _skillMatrixRepository;
        private readonly TaskRepository _taskRepository;

        public TaskService(
            SectionRepository sectionRepository,
            SkillMatrixRepository skillMatrixRepository,
            TaskRepository taskRepository)
        {
            _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
            _skillMatrixRepository = skillMatrixRepository ?? throw new ArgumentNullException(nameof(skillMatrixRepository));
            _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        }

        /// <summary>
        /// Loads all tasks for a given simulation section and determines the current task index and time elapsed.
        /// </summary>
        public async Task<(List<JobTask> tasks, int currentTaskIndex, int timeElapsed)>
            LoadTaskDetailsForActivityAsync(string simulationId, string sectionId, string activityId)
        {
            var tasks = await _taskRepository.GetTasksBySimulationAndSectionIdAsync(simulationId, sectionId, activityId);
            if (tasks == null || tasks.Count == 0)
                return (new List<JobTask>(), 0, 0);

            var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activityId);

            // Find current task based on existing SkillMatrix entries only
            JobTask currentTask = tasks.FirstOrDefault(t =>
                    skillMatrixEntries.Any(sm => sm.TaskId == t.TaskId && sm.Status == StatusTypes.InProgress)) ??
                tasks.FirstOrDefault(t =>
                    skillMatrixEntries.Any(sm => sm.TaskId == t.TaskId && sm.Status != StatusTypes.Completed)) ??
                null;
            //added to check if not necessary can remove-ANUJ
            //if (currentTask == null)
            //{
            //    currentTask = tasks.FirstOrDefault(t =>
            //        !skillMatrixEntries.Any(sm =>
            //            sm.TaskId == t.TaskId &&
            //            sm.Status == StatusTypes.Completed));
            //}

            int taskIndex = currentTask != null ? tasks.IndexOf(currentTask) : 0;
            int timeElapsed = skillMatrixEntries
                .FirstOrDefault(sm => sm.TaskId == currentTask?.TaskId)?.TotalTime ?? 0;

            return (tasks, taskIndex, timeElapsed);
        }

        /// <summary>
        /// Returns all tasks for a given simulation, section, and activity.
        /// </summary>
        public async Task<List<JobTask>> LoadTasksAsync(string simulationId, string sectionId, string activityId)
        {
            return await _taskRepository.GetTasksBySimulationAndSectionIdAsync(simulationId, sectionId, activityId);
        }

        /// <summary>
        /// Loads all tasks for a section and an activity, initializes SkillMatrix entries for untracked tasks,
        /// and returns the current task index and elapsed time.
        /// </summary>
        public async Task<(List<JobTask> tasks, int currentTaskIndex, int timeElapsed)>
       LoadTaskDetailsForSectionAsync(string sectionId, string activityId)
        {
            // Fetch tasks for the section
            var tasks = await _taskRepository.GetTasksByActivityIdAsync(sectionId, activityId);
            if (tasks == null || tasks.Count == 0)
                return (new List<JobTask>(), 0, 0);

            // Fetch existing progress from the SkillMatrix
            var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activityId);

            // Find current task - only one declaration
            JobTask currentTask = tasks.FirstOrDefault(t =>
                skillMatrixEntries.Any(sm => sm.TaskId == t.TaskId && sm.Status == StatusTypes.InProgress)
            ) ?? tasks.FirstOrDefault(t =>
                !skillMatrixEntries.Any(sm => sm.TaskId == t.TaskId && sm.Status == StatusTypes.Completed)
            );

            int taskIndex = currentTask != null ? tasks.IndexOf(currentTask) : 0;
            int timeElapsed = 0;

            // Initialize progress for any tasks that don't exist in SkillMatrix
            foreach (var task in tasks)
            {
                var existingEntry = skillMatrixEntries.FirstOrDefault(sm => sm.TaskId == task.TaskId);

                if (existingEntry == null)
                {
                    var newSkillMatrix = new SkillMatrix
                    {
                        ActivityId = activityId,
                        TaskId = task.TaskId,
                        Status = StatusTypes.NotStarted,
                        TotalTime = 0,
                        HintsChecked = 0,
                        AttemptstoSolve = 0,
                        TaskAttempt = 0,
                        CreateBy = "Admin",
                        CreateDate = DateTime.Now,
                        ModifyBy = "Admin",
                        ModifyDate = DateTime.Now
                    };

                    await _skillMatrixRepository.UpsertSkillMatrixAsync(newSkillMatrix, "Admin");
                }
            }

            // Get time elapsed for current task
            if (currentTask != null)
            {
                var skillEntry = skillMatrixEntries.FirstOrDefault(sm => sm.TaskId == currentTask.TaskId);
                timeElapsed = skillEntry?.TotalTime ?? 0;
            }

            return (tasks, taskIndex, timeElapsed);
        }


        public async Task LoadTasksForSection(string sectionId, string activityId)
        {
            // Load tasks and initialize SkillMatrix entries
            await LoadTaskDetailsForSectionAsync(sectionId, activityId);
        }

        public async Task UnloadTasksForSection(string activityId)
        {
            // Mark all tasks as complete or save progress
            var skillMatrixEntries = await _skillMatrixRepository
                .GetSkillMatrixEntriesForActivityAsync(activityId);

            foreach (var entry in skillMatrixEntries)
            {
                if (entry.Status != StatusTypes.Completed)
                {
                    entry.Status = StatusTypes.Completed;
                    await _skillMatrixRepository.UpsertSkillMatrixAsync(entry, "system");
                }
            }
        }

    }
}



