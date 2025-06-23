

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Transactions;
using System.Threading.Tasks;
using System.Windows.Forms;
using JobSimulation.BLL;
using JobSimulation.DAL;
using JobSimulation.Models;
using Activity = JobSimulation.Models.Activity;

namespace JobSimulation.Managers
{
    public class SimulationManager
    {
        private readonly FileService _fileService;
        private readonly SkillMatrixRepository _skillMatrixRepository;
        private readonly ActivityRepository _activityRepository;
        private readonly TaskRepository _taskRepository;
        private readonly TaskService _taskService;
        private readonly SectionRepository _sectionRepository;

        private readonly Dictionary<string, TaskProgress> _taskProgressCache = new();
        public SectionProgress SectionProgress { get; private set; }
        public List<JobTask> Tasks { get; private set; }
        public int CurrentTaskIndex { get; private set; }
        public string FilePath { get; private set; }
        public string ActivityId { get; private set; }
        public string UserId { get; }
        public string SimulationId { get; }
        public int Attempt { get; set; }
        public Section CurrentSection { get; private set; }
        private string _masterJson;


        private bool? _isLastSection = null;
        private bool? _isFirstSection = null;
        public int CurrentTaskElapsedTime =>
            SectionProgress.TaskProgresses[CurrentTaskIndex].TimeSpent;

        public SimulationManager(
      List<JobTask> tasks,
      string filePath,
      string simulationId,
      string userId,
      FileService fileService,
      SkillMatrixRepository skillMatrixRepository,
      ActivityRepository activityRepository,
      TaskRepository taskRepository,
      TaskService taskService,
        SectionRepository sectionRepository,
      DataSet progressDataSet, // Add this parameter
      int attempt,
      Section currentSection,
      string activityId,
      int initialTaskIndex = 0)
        {
            Tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
            FilePath = filePath;
            SimulationId = simulationId;
            _fileService = fileService;
            _skillMatrixRepository = skillMatrixRepository;
            _activityRepository = activityRepository;
            _taskRepository = taskRepository;
            _taskService = taskService;
            CurrentTaskIndex = initialTaskIndex;
            Attempt = attempt;
            _sectionRepository = sectionRepository; // Store the repository
            CurrentSection = currentSection ?? throw new ArgumentNullException(nameof(currentSection));
            UserId = userId;
            ActivityId = activityId;

            InitializeSectionProgress(tasks);
            LoadMasterData();
        }
        public async Task InitializeAsync()
        {
            await SyncWithExistingProgress();
            await UpdateSectionInfoAsync(); // Initialize section info
        }

        private void InitializeSectionProgress(List<JobTask> tasks)
        {
            SectionProgress = new SectionProgress
            {
                UserId = UserId,
                SectionId = CurrentSection.SectionId,
                ActivityId = ActivityId,
                TaskProgresses = tasks.Select(t => new TaskProgress
                {
                    TaskId = t.TaskId,
                    Status = StatusTypes.NotStarted, // Default to NotStarted
                    TimeSpent = 0,
                    HintsChecked = 0,
                    TaskAttempt = 0
                }).ToList()
            };
        }
        private async Task SyncWithExistingProgress()
        {
            // Fetch existing progress for the given activity
            var existingEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(ActivityId);

            foreach (var task in Tasks)
            {
                var progress = SectionProgress.TaskProgresses.FirstOrDefault(p => p.TaskId == task.TaskId);
                var existing = existingEntries.FirstOrDefault(e => e.TaskId == task.TaskId);

                if (existing != null)
                {
                    // Map database records to current session
                    progress.TimeSpent = existing.TotalTime;
                    progress.HintsChecked = existing.HintsChecked;
                    progress.TaskAttempt = existing.TaskAttempt;
                    progress.AttemptstoSolve = existing.AttemptstoSolve; // Map first correct attempt
                    progress.Status = existing.Status;

                    // Auto-resume incomplete tasks
                    if (existing.Status == StatusTypes.InProgress)
                    {
                        CurrentTaskIndex = Tasks.IndexOf(task);
                    }
                }
                else
                {
                    // If no existing entry, initialize as NotStarted
                    progress.Status = StatusTypes.NotStarted;
                    progress.TimeSpent = 0;
                    progress.HintsChecked = 0;
                    progress.TaskAttempt = 0;
                    progress.AttemptstoSolve = 0;
                }
            }
        }
        public async Task IncrementTimeElapsedAsync()
        {
            var progress = SectionProgress.TaskProgresses[CurrentTaskIndex];
            progress.TimeSpent++;

            if (progress.TimeSpent % 30 == 0) // Auto-save every 30 seconds
            {
                await SaveTaskProgress(progress);
            }
        }

        private async Task SaveTaskProgress(TaskProgress progress)
        {
            await _skillMatrixRepository.UpsertSkillMatrixAsync(new SkillMatrix
            {
                ActivityId = ActivityId,
                TaskId = progress.TaskId,
                TotalTime = progress.TimeSpent,
                HintsChecked = progress.HintsChecked, // Save hints checked
                AttemptstoSolve = progress.AttemptstoSolve, // Save first correct attempt
                TaskAttempt = progress.TaskAttempt, // Save total attempts
                Status = progress.Status, // Save task status
                ModifyBy = UserId,
                ModifyDate = DateTime.Now
            }, UserId);
        }
        public async Task UpdateSectionDataAsync(
            List<JobTask> newTasks,
            string newFilePath,
            string newSectionId,
            string newSoftwareId,
            string newActivityId,
            int newAttempt,
            Section newCurrentSection,
            int newTaskIndex)
        {
            // Update properties
            Tasks = newTasks;
            FilePath = newFilePath;
            Attempt = newAttempt;
            CurrentSection = newCurrentSection;
            CurrentTaskIndex = newTaskIndex;
            ActivityId = newActivityId;

            // Clear existing progress and reinitialize for the new section
            SectionProgress.TaskProgresses.Clear();
            SectionProgress.UserId = UserId;
            SectionProgress.SectionId = newSectionId;
            SectionProgress.ActivityId = newActivityId;

            foreach (var task in newTasks)
            {
                // Only update in-memory progress, do not write to DB or cache here!
                // If a DB entry exists, use its info, otherwise just NotStarted in memory.
                var existingEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(newActivityId, task.TaskId);

                if (existingEntry == null)
                {
                    SectionProgress.TaskProgresses.Add(new TaskProgress
                    {
                        TaskId = task.TaskId,
                        Status = StatusTypes.NotStarted,
                        TimeSpent = 0,
                        HintsChecked = 0,
                        TaskAttempt = 0
                    });
                }
                else
                {
                    SectionProgress.TaskProgresses.Add(new TaskProgress
                    {
                        TaskId = existingEntry.TaskId,
                        Status = existingEntry.Status,
                        TimeSpent = existingEntry.TotalTime,
                        HintsChecked = existingEntry.HintsChecked,
                        TaskAttempt = existingEntry.TaskAttempt
                    });
                }
            }

            // DO NOT update DB or cache here!
            LoadMasterData();
        }
        //public async Task<(List<JobTask> tasks, int currentTaskIndex, int timeElapsed)> LoadTaskDetailsForSectionAsync(string sectionId, string activityId)
        //{
        //    return await _taskService.LoadTaskDetailsForActivityAsync(simulationId, sectionId, activityId);

        //}

        public void LoadMasterData()
        {
            var taskSubmission = new TaskSubmission
            {
                SectionId = CurrentSection.SectionId,
                SoftwareId = CurrentSection.SoftwareId,
            };

            var validationForm = ValidationFormFactory.CreateValidationForm(taskSubmission, _fileService);
            _masterJson = validationForm.GetMasterJsonForSection(CurrentSection.SectionId);
        }

        //public Task<string> CheckAnswerAsync(int taskIndex)
        //{
        //    var task = Tasks[taskIndex];
        //    var progress = SectionProgress.TaskProgresses[taskIndex];

        //    // Update cache (not DB)
        //    if (!_taskProgressCache.ContainsKey(task.TaskId))
        //        _taskProgressCache[task.TaskId] = CloneProgress(progress);

        //    var cached = _taskProgressCache[task.TaskId];
        //    cached.TaskAttempt++;

        //    if (!cached.HasCheckedAnswer)
        //        cached.HasCheckedAnswer = true;

        //    if (cached.HasCheckedAnswer)
        //    {
        //        var isCorrect = ValidateTask(task);

        //        if (isCorrect)
        //        {
        //            if (cached.AttemptstoSolve == 0)
        //                cached.AttemptstoSolve = cached.TaskAttempt;
        //            cached.Status = StatusTypes.Completed;
        //        }
        //        else
        //        {
        //            cached.Status = StatusTypes.Attempted;
        //        }

        //        return Task.FromResult(isCorrect ? "Correct!" : "Incorrect!");
        //    }

        //    if (cached.Status == StatusTypes.NotStarted)
        //        cached.Status = StatusTypes.Visited;

        //    return Task.FromResult("You must click 'Check Answer' first.");
        //}

        // Modified CheckAnswerAsync - only updates cache
        public async Task<string> CheckAnswerAsync(int taskIndex)
        {
            var task = Tasks[taskIndex];

            // Only update cache
            if (!_taskProgressCache.ContainsKey(task.TaskId))
                _taskProgressCache[task.TaskId] = CloneProgress(SectionProgress.TaskProgresses[taskIndex]);

            var cached = _taskProgressCache[task.TaskId];
            cached.TaskAttempt++;

            if (!cached.HasCheckedAnswer)
                cached.HasCheckedAnswer = true;

            if (cached.HasCheckedAnswer)
            {
                var isCorrect = ValidateTask(task);

                // Always mark as completed regardless of correctness
                cached.Status = StatusTypes.Completed;

                // Only set first correct attempt if answer is correct
                if (isCorrect && cached.AttemptstoSolve == 0)
                {
                    cached.AttemptstoSolve = cached.TaskAttempt;
                }

                return isCorrect ? "Correct!" : "Incorrect!";
            }

            if (cached.Status == StatusTypes.NotStarted)
                cached.Status = StatusTypes.Visited;

            return "You must click 'Check Answer' first.";
        }
        private bool ValidateTask(JobTask task)
        {
            var taskSubmission = new TaskSubmission
            {
                FilePath = FilePath,
                SoftwareId = CurrentSection.SoftwareId,
                SimulationId = SimulationId,
                SectionId = CurrentSection.SectionId,
                Task = task
            };

            var validationForm = ValidationFormFactory.CreateValidationForm(taskSubmission, _fileService);
            return validationForm.ValidateTask(taskSubmission, _masterJson);
        }

        public async Task UpdateActivityStatusAsync()
        {
            var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(ActivityId);
            int completedCount = skillMatrixEntries.Count(e => e.Status == StatusTypes.Completed);
            int totalTasks = Tasks.Count;

            string newStatus = CalculateProgressStatus(completedCount, totalTasks);
            string result = CalculateResult(skillMatrixEntries.ToList());

            await _activityRepository.UpdateActivityAsync(new Activity
            {
                ActivityId = ActivityId,
                Status = newStatus,
                Result = result,
                ModifyBy = UserId,
                ModifyDate = DateTime.Now
            });
        }

        public async Task<Activity> GetActivityAsync()
        {
            return await _activityRepository.GetByIdAsync(ActivityId);
        }

        public async Task SaveProgressAsync(bool forceSaveAll = false)
        {
            var entriesToSave = SectionProgress.TaskProgresses
                .Where(p => forceSaveAll || p.Status != StatusTypes.NotStarted) // Save all except NotStarted
                .Select(p => new SkillMatrix
                {
                    ActivityId = ActivityId,
                    TaskId = p.TaskId,
                    TotalTime = p.TimeSpent,
                    HintsChecked = p.HintsChecked,
                    AttemptstoSolve = p.TaskAttempt,
                    TaskAttempt = p.TaskAttempt,
                    Status = p.Status,
                    ModifyBy = UserId,
                    ModifyDate = DateTime.Now
                }).ToList();

            foreach (var entry in entriesToSave)
            {
                await _skillMatrixRepository.UpsertSkillMatrixAsync(entry, UserId);
            }

            await SaveStudentFileAsync();
        }
        public async Task SaveStudentFileAsync()
        {
            if (!File.Exists(FilePath)) return;

            try
            {
                byte[] fileBytes;
                using (var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fileBytes = new byte[stream.Length];
                    await stream.ReadAsync(fileBytes, 0, (int)stream.Length);
                }

                string base64 = Convert.ToBase64String(fileBytes);
                await _activityRepository.UpdateActivityStudentFileAsync(ActivityId, base64, UserId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"File save error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public async Task<JobTask> LoadTaskAsync(int taskIndex)
        {
            if (taskIndex < 0 || taskIndex >= Tasks.Count) return null;

            var task = Tasks[taskIndex];
            var existingEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, task.TaskId);

            if (existingEntry != null)
            {
                var progress = SectionProgress.TaskProgresses.FirstOrDefault(p => p.TaskId == task.TaskId);

                // Map database values to progress
                progress.TimeSpent = existingEntry.TotalTime;
                progress.HintsChecked = existingEntry.HintsChecked;
                progress.TaskAttempt = existingEntry.TaskAttempt;
                progress.Status = existingEntry.Status;
            }

            return task;
        }
        public Task<string> GetHintAsync(int taskIndex)
        {
            var currentTask = Tasks[taskIndex];
            var progress = SectionProgress.TaskProgresses[taskIndex];

            // Update cache (not DB)
            if (!_taskProgressCache.ContainsKey(currentTask.TaskId))
                _taskProgressCache[currentTask.TaskId] = CloneProgress(progress);

            var cached = _taskProgressCache[currentTask.TaskId];
            if (cached.HintsChecked == 0)
            {
                cached.HintsChecked = 1;
                if (cached.Status == StatusTypes.Visited)
                    cached.Status = StatusTypes.InComplete;
            }

            return Task.FromResult(((dynamic)currentTask.Details).Hint);
        }
        private async Task SaveActivityDataAsync(TaskSubmission taskSubmission, bool isCorrect, int taskAttempt)
        {
            byte[] fileBytes;
            using (var fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fileBytes = new byte[fileStream.Length];
                await fileStream.ReadAsync(fileBytes, 0, (int)fileStream.Length);
            }

            var existingEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, taskSubmission.Task.TaskId);
            int attempts = existingEntry?.AttemptstoSolve ?? 0;

            if (isCorrect && existingEntry?.Status != StatusTypes.Completed)
            {
                attempts = taskAttempt;
            }

            var skillMatrix = new SkillMatrix
            {
                ActivityId = ActivityId,
                TaskId = taskSubmission.Task.TaskId,
                HintsChecked = taskSubmission.HintsChecked,
                Status = isCorrect ? StatusTypes.Completed : StatusTypes.Attempted,
                TaskAttempt = attempts,
                TotalTime = SectionProgress.TaskProgresses[CurrentTaskIndex].TimeSpent,
                ModifyBy = UserId,
                ModifyDate = DateTime.Now
            };

            await _skillMatrixRepository.UpsertSkillMatrixAsync(skillMatrix, UserId);
        }

        public async Task<bool> AreAllTasksCompleted()
        {
            var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(ActivityId);
            return Tasks.All(task =>
                skillMatrixEntries.Any(e =>
                    e.TaskId == task.TaskId &&
                    e.Status == StatusTypes.Completed));
        }
        public async Task MoveToNextTaskAsync()
        {
            CurrentTaskIndex = Math.Min(CurrentTaskIndex + 1, Tasks.Count - 1);
            await MarkTaskAsVisitedAsync(CurrentTaskIndex); // Mark as visited
        }

        public async Task MoveToPreviousTaskAsync()
        {
            CurrentTaskIndex = Math.Max(CurrentTaskIndex - 1, 0);
            await MarkTaskAsVisitedAsync(CurrentTaskIndex); // Mark as visited
        }

        private string CalculateProgressStatus(int completedCount, int totalTasks)
        {
            if (completedCount == 0) return StatusTypes.NotStarted;
            return completedCount == totalTasks ? StatusTypes.Completed : StatusTypes.InProgress;
        }

        private string CalculateResult(List<SkillMatrix> entries)
    => $"{entries.Count(e => e.Status == StatusTypes.Completed)}/{entries.Count} tasks completed";

        private async Task MarkTaskAsVisitedAsync(int taskIndex)
        {
            var progress = SectionProgress.TaskProgresses[taskIndex];

            // Mark as Visited if not already saved
            if (progress.Status == StatusTypes.NotStarted)
            {
                progress.Status = StatusTypes.Visited;
                await SaveTaskProgress(progress); // Save to database
            }
        }
        //public async Task MarkUnvisitedTasksAsInCompleted()
        //{
        //    // Fetch all activities (sections) for the simulation
        //    var allActivities = await GetAllActivitiesForSimulationAsync();

        //    foreach (var activity in allActivities)
        //    {
        //        // Fetch all tasks for this activity's section
        //        var tasks = await _taskRepository.GetTasksForSectionAsync(activity.SectionId);

        //        // Fetch all skill matrix entries for this activity
        //        var skillMatrices = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activity.ActivityId);

        //        foreach (var task in tasks)
        //        {
        //            var existingEntry = skillMatrices.FirstOrDefault(sm => sm.TaskId == task.TaskId);

        //            if (existingEntry == null)
        //            {
        //                // Task was never visited, mark as Incomplete
        //                var newEntry = new SkillMatrix
        //                {
        //                    ActivityId = activity.ActivityId,
        //                    TaskId = task.TaskId,
        //                    Status = StatusTypes.InComplete,
        //                    AttemptstoSolve = 0,
        //                    TaskAttempt = 0,
        //                    HintsChecked = 0,
        //                    TotalTime = 0,
        //                    CreateBy = UserId,
        //                    CreateDate = DateTime.Now,
        //                    ModifyBy = UserId,
        //                    ModifyDate = DateTime.Now
        //                };
        //                await _skillMatrixRepository.UpsertSkillMatrixAsync(newEntry, UserId);
        //            }
        //            else if (existingEntry.Status == StatusTypes.Visited)
        //            {
        //                // Retain the status for visited tasks
        //                existingEntry.ModifyBy = UserId;
        //                existingEntry.ModifyDate = DateTime.Now;
        //                await _skillMatrixRepository.UpsertSkillMatrixAsync(existingEntry, UserId);
        //            }
        //        }
        //    }
        //}

        //public async Task MarkUnvisitedTasksAsInCompleted()
        //{
        //    var allActivities = await GetAllActivitiesForSimulationAsync();

        //    foreach (var activity in allActivities)
        //    {
        //        var tasks = await _taskRepository.GetTasksForSectionAsync(activity.SectionId);
        //        var skillMatrices = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activity.ActivityId);

        //        foreach (var task in tasks)
        //        {
        //            var existingEntry = skillMatrices.FirstOrDefault(sm => sm.TaskId == task.TaskId);

        //            if (existingEntry == null)
        //            {
        //                // Mark unvisited tasks as Incomplete
        //                var newEntry = new SkillMatrix
        //                {
        //                    ActivityId = activity.ActivityId,
        //                    TaskId = task.TaskId,
        //                    Status = StatusTypes.InComplete,
        //                    AttemptstoSolve = 0,
        //                    TaskAttempt = 0,
        //                    HintsChecked = 0,
        //                    TotalTime = 0,
        //                    CreateBy = UserId,
        //                    CreateDate = DateTime.Now,
        //                    ModifyBy = UserId,
        //                    ModifyDate = DateTime.Now
        //                };
        //                await _skillMatrixRepository.UpsertSkillMatrixAsync(newEntry, UserId);
        //            }
        //        }
        //    }
        //}

        public async Task MarkUnvisitedTasksAsInCompleted(string userId)
        {
            var allActivities = await GetAllActivitiesForSimulationAsync();

            foreach (var activity in allActivities)
            {
                // Use the correctly injected repository
                var simulationSections = await _sectionRepository.GetSimulationSectionsForActivityAsync(activity.ActivityId);
                foreach (var simSection in simulationSections)
                {
                    var tasks = await _taskRepository.GetTasksBySimulationAndSectionIdAsync(
                        simSection.SimulationId,
                        simSection.SectionId,
                        activity.ActivityId);

                    var skillMatrices = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activity.ActivityId);

                    foreach (var task in tasks)
                    {
                        var existingEntry = skillMatrices.FirstOrDefault(sm => sm.TaskId == task.TaskId);

                        if (existingEntry == null)
                        {
                            var newEntry = new SkillMatrix
                            {
                                ActivityId = activity.ActivityId,
                                TaskId = task.TaskId,
                                Status = StatusTypes.InComplete,
                                AttemptstoSolve = 0,
                                TaskAttempt = 0,
                                HintsChecked = 0,
                                TotalTime = 0,
                                CreateBy = userId,
                                CreateDate = DateTime.Now,
                                ModifyBy = userId,
                                ModifyDate = DateTime.Now
                            };
                            await _skillMatrixRepository.UpsertSkillMatrixAsync(newEntry, userId);
                        }
                    }
                }
            }
        }




        public async Task<List<Activity>> GetAllActivitiesForSimulationAsync()
        {
            var activities = await _activityRepository.GetActivitiesForSimulationAsync(SimulationId, UserId);
            return activities.ToList(); // Convert IEnumerable<Activity> to List<Activity>
        }
        public async Task SaveActivityAsync(Activity activity)
    => await _activityRepository.SaveActivityAsync(activity);

        public async Task<string> CalculateSimulationResultAsync(string simulationId)
        {
            // Fetch all activities (sections) for the simulation
            var allActivities = await _activityRepository.GetActivitiesForSimulationAsync(simulationId, UserId);

            // Calculate section results
            var sectionResults = allActivities.Select(a => a.Result).ToList();

            if (!sectionResults.Any())
                return StatusTypes.NeedsImprovement;

            // Aggregate section results into a simulation result
            var simulationScore = sectionResults.Average(result => result switch
            {
                StatusTypes.Mastered => 100,
                StatusTypes.Proficient => 75,
                StatusTypes.Developing => 50,
                _ => 25 // Needs Improvement
            });

            // Map the score to a performance level
            return simulationScore switch
            {
                >= 90 => StatusTypes.Mastered,
                >= 75 => StatusTypes.Proficient,
                >= 50 => StatusTypes.Developing,
                _ => StatusTypes.NeedsImprovement
            };
        }

        private string CalculateSectionResult(List<SkillMatrix> entries)
        {
            var completed = entries.Count(e => e.Status == StatusTypes.Completed);
            var total = entries.Count;
            return $"{completed}/{total}";
        }
        public async Task<string> UpdateSectionResultAsync(string activityId)
        {
            var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activityId);
            var sectionResult = CalculateSectionResult(skillMatrixEntries.ToList());

            var activity = await _activityRepository.GetByIdAsync(activityId);
            activity.Result = sectionResult;
            activity.ModifyDate = DateTime.Now;

            await _activityRepository.UpdateActivityAsync(activity);
            return sectionResult;
        }
        //public async Task<string> CalculateSectionResultAsync(string activityId)
        //{
        //    // Fetch the activity and related data
        //    var activity = await _activityRepository.GetByIdAsync(activityId);
        //    var skillMatrices = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activityId);
        //    var tasks = await _taskRepository.GetTasksForSectionAsync(activity.SectionId);

        //    // If no tasks exist or no skill matrix entries are found, return "Needs Improvement"
        //    if (!tasks.Any() || !skillMatrices.Any())
        //        return StatusTypes.NeedsImprovement;

        //    // Aggregate task performance metrics
        //    var taskMetrics = new List<(int AttemptsToSolve, int HintsChecked, int TotalTime)>();

        //    foreach (var task in tasks)
        //    {
        //        var taskMatrix = skillMatrices
        //            .Where(sm => sm.TaskId == task.TaskId)
        //            .OrderByDescending(sm => sm.ModifyDate)
        //            .FirstOrDefault();

        //        // Skip unvisited or incomplete tasks
        //        if (taskMatrix == null || !taskMatrix.IsCorrect)
        //            return StatusTypes.NeedsImprovement;

        //        taskMetrics.Add((
        //            taskMatrix.AttemptstoSolve,
        //            taskMatrix.HintsChecked,
        //            taskMatrix.TotalTime
        //        ));
        //    }

        //    // Calculate the performance grade for the section
        //    var result = CalculatePerformanceGrade(taskMetrics);

        //    // Update the result in the Activity table
        //    activity.Result = result;
        //    activity.ModifyDate = DateTime.UtcNow;
        //    await _activityRepository.UpdateActivityAsync(activity);

        //    return result;
        //}

        public async Task UpdateActivityAsync(Activity activity)
        {
            await _activityRepository.UpdateAsync(activity);
        }

        public void MoveToNextTask() =>
    CurrentTaskIndex = Math.Min(CurrentTaskIndex + 1, Tasks.Count - 1);

        public void MoveToPreviousTask() =>
            CurrentTaskIndex = Math.Max(CurrentTaskIndex - 1, 0);



        public Task CacheTaskProgressAsync()
        {
            var task = Tasks[CurrentTaskIndex];
            if (!_taskProgressCache.ContainsKey(task.TaskId))
                _taskProgressCache[task.TaskId] = CloneProgress(SectionProgress.TaskProgresses[CurrentTaskIndex]);
            // Update time in cache
            _taskProgressCache[task.TaskId].TimeSpent = CurrentTaskElapsedTime;
            // Optionally update other fields (answer, hints, etc.)
            return Task.CompletedTask;
        }
        private TaskProgress CloneProgress(TaskProgress progress)
        {
            return new TaskProgress
            {
                TaskId = progress.TaskId,
                TimeSpent = progress.TimeSpent,
                HintsChecked = progress.HintsChecked,
                AttemptstoSolve = progress.AttemptstoSolve,
                Status = progress.Status,
                HasCheckedAnswer = progress.HasCheckedAnswer,
                TaskAttempt = progress.TaskAttempt
            };
        }
        public Task CacheTaskAnswerAsync()
        {
            var task = Tasks[CurrentTaskIndex];

            if (!_taskProgressCache.ContainsKey(task.TaskId))
                _taskProgressCache[task.TaskId] = CloneProgress(SectionProgress.TaskProgresses[CurrentTaskIndex]);

            var cached = _taskProgressCache[task.TaskId];
            cached.Status = StatusTypes.Completed;
            cached.TimeSpent = CurrentTaskElapsedTime;

            return Task.CompletedTask;
        }
        public void UpdateActualProgressFromCacheForCurrentTask()
        {
            var currentTaskId = Tasks[CurrentTaskIndex].TaskId;
            if (_taskProgressCache.TryGetValue(currentTaskId, out var cachedProgress))
            {
                var actualProgress = SectionProgress.TaskProgresses[CurrentTaskIndex];
                actualProgress.Status = cachedProgress.Status;
                actualProgress.HintsChecked = cachedProgress.HintsChecked;
                actualProgress.AttemptstoSolve = cachedProgress.AttemptstoSolve;
                actualProgress.TaskAttempt = cachedProgress.TaskAttempt;
            }
        }
        //public Task CacheTaskAnswerAsync()
        //{
        //    var task = Tasks[CurrentTaskIndex];
        //    var progress = SectionProgress.TaskProgresses[CurrentTaskIndex]; // Actual progress

        //    if (!_taskProgressCache.ContainsKey(task.TaskId))
        //        _taskProgressCache[task.TaskId] = CloneProgress(progress);

        //    var cached = _taskProgressCache[task.TaskId];

        //    // Update both cache and actual progress
        //    cached.Status = StatusTypes.Completed;
        //    progress.Status = StatusTypes.Completed; // CRITICAL: Update actual progress

        //    cached.TimeSpent = CurrentTaskElapsedTime;
        //    progress.TimeSpent = CurrentTaskElapsedTime; // Sync time

        //    return Task.CompletedTask;
        //}
        public async Task SaveSectionProgressAsync()
        {
            // Save only current section's tasks
            await SaveTasksToDatabase(
                _taskProgressCache.Values
                    .Where(p => Tasks.Any(t => t.TaskId == p.TaskId))
                    .ToList()
            );
            _taskProgressCache.Clear();
        }
        public async Task SaveSimulationProgressAsync()
        {
            // Save all cached progress
            await SaveAllTasksToDatabase(_taskProgressCache.Values.ToList());
            _taskProgressCache.Clear();
            await SaveStudentFileAsync();

            // Update activity status
            await UpdateActivityStatusAsync();
        }
        private async Task SaveTasksToDatabase(List<TaskProgress> progresses)
        {
            foreach (var progress in progresses)
            {
                // Map TaskProgress to SkillMatrix and upsert
                var skillMatrix = new SkillMatrix
                {
                    ActivityId = ActivityId,
                    TaskId = progress.TaskId,
                    TotalTime = progress.TimeSpent,
                    HintsChecked = progress.HintsChecked,
                    AttemptstoSolve = progress.AttemptstoSolve,
                    TaskAttempt = progress.TaskAttempt,
                    Status = progress.Status,
                    ModifyBy = UserId,
                    ModifyDate = DateTime.Now
                };
                await _skillMatrixRepository.UpsertSkillMatrixAsync(skillMatrix, UserId);
                await SaveStudentFileAsync();

            }
        }
        private async Task SaveAllTasksToDatabase(List<TaskProgress> progresses)
        {
            foreach (var progress in progresses)
            {
                // Map TaskProgress to SkillMatrix as above
                var skillMatrix = new SkillMatrix
                {
                    ActivityId = ActivityId,
                    TaskId = progress.TaskId,
                    TotalTime = progress.TimeSpent,
                    HintsChecked = progress.HintsChecked,
                    AttemptstoSolve = progress.AttemptstoSolve,
                    TaskAttempt = progress.TaskAttempt,
                    Status = progress.Status,
                    ModifyBy = UserId,
                    ModifyDate = DateTime.Now
                };
                await _skillMatrixRepository.UpsertSkillMatrixAsync(skillMatrix, UserId);
            }
        }



      

        public bool IsLastTaskInSection()
        => CurrentTaskIndex >= Tasks.Count - 1;
        // Helper to check if this is the last section


        public bool IsFirstTaskInSection()
            => CurrentTaskIndex <= 0;


        private Section _currentSection;
        //public Section CurrentSection
        //{
        //    get => _currentSection;
        //    private set
        //    {
        //        _currentSection = value;
        //        // Clear cached values when section changes
        //        _isLastSection = null;
        //        _isFirstSection = null;
        //    }
        //}

        public async Task<bool> IsLastSectionAsync()
               => await _sectionRepository.IsLastSectionAsync(CurrentSection.SectionId, SimulationId);



        public async Task<bool> IsFirstSectionAsync()
        {
            // Always reset cache when section changes
            _isFirstSection = null;

            if (_isFirstSection == null && CurrentSection != null)
            {
                _isFirstSection = await _sectionRepository.IsFirstSectionAsync(
                    CurrentSection.SectionId,
                    SimulationId
                );
            }
            return _isFirstSection ?? false;
        }
        public async Task UpdateSectionInfoAsync()
        {
            if (CurrentSection == null) return;

            _isLastSection = null;
            _isFirstSection = null;
            await IsLastSectionAsync();
            await IsFirstSectionAsync();
        }
        public async Task<bool> IsLastTaskInLastSectionAsync()
            => IsLastTaskInSection() && await IsLastSectionAsync();


        public bool IsFirstSection()
        {
            return _sectionRepository.IsFirstSectionAsync(CurrentSection.SectionId, SimulationId).Result;
        }

        // 4. GetTotalTaskCount
        public int GetTotalTaskCount()
        {
            return Tasks?.Count ?? 0;
        }
        public int GetCompletedTaskCount()
        {
            return SectionProgress.TaskProgresses.Count(tp => tp.Status == StatusTypes.Completed);
        }

        // 6. GetTotalSectionCount
        public int GetTotalSectionCount()
        {
            // Implement as per your section management.
            // For example, if you have a List<Section> AllSections, return AllSections.Count;
            return 1; // Placeholder; replace with your logic
        }

        // 7. GetCompletedSectionCount
        public int GetCompletedSectionCount()
        {
            // Implement as per your section management.
            // For example, count sections where all tasks are completed.
            // If you only have 1 section, just return 1 if all tasks completed, else 0.
            return GetCompletedTaskCount() == GetTotalTaskCount() ? 1 : 0;
        }
        public async Task SetActivityInProgressAsync()
        {
            var activity = await _activityRepository.GetByIdAsync(ActivityId);
            if (activity != null && activity.Status != StatusTypes.InProgress)
            {
                activity.Status = StatusTypes.InProgress;
                activity.ModifyBy = UserId;
                activity.ModifyDate = DateTime.Now;
                await _activityRepository.UpdateAsync(activity);
            }
        }

        public async Task UpdateAllSectionResultsAsync()
        {
            var allActivities = await _activityRepository.GetActivitiesForSimulationAsync(SimulationId, UserId);

            foreach (var activity in allActivities)
            {
                string result;

                // Get section details
                var section = await _sectionRepository.GetSectionByIdAsync(activity.SectionId, SimulationId);
                var totalTasks = (await _taskRepository.GetTasksForSectionAsync(activity.SectionId)).Count;

                // Get attempted tasks
                var skillMatrices = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activity.ActivityId);
                var attemptedTasks = skillMatrices.Count(sm => sm.Status != StatusTypes.NotStarted);

                // Calculate result based on completion
                if (attemptedTasks == 0)
                {
                    result = StatusTypes.NeedsImprovement;
                }
                else if (attemptedTasks < totalTasks)
                {
                    result = StatusTypes.InComplete;
                }
                else
                {
                    result = await CalculatePerformanceGradeForActivityAsync(activity.ActivityId);
                }

                // Update activity
                var existingActivity = await _activityRepository.GetByIdAsync(activity.ActivityId);
                existingActivity.Result = result;
                existingActivity.Status = StatusTypes.Completed;
                existingActivity.ModifyBy = UserId;
                existingActivity.ModifyDate = DateTime.Now;

                await _activityRepository.UpdateActivityAsync(existingActivity);
            }
        }
        public async Task<string> CalculatePerformanceGradeForActivityAsync(string activityId)
        {
            var activity = await _activityRepository.GetByIdAsync(activityId);
            var skillMatrices = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activityId);
            var tasks = await _taskRepository.GetTasksForSectionAsync(activity.SectionId);

            if (!tasks.Any())
                return StatusTypes.NeedsImprovement;

            // Only consider tasks that were actually attempted
            var attemptedTasks = tasks
                .Where(task => skillMatrices.Any(sm => sm.TaskId == task.TaskId))
                .ToList();

            if (!attemptedTasks.Any())
                return StatusTypes.NeedsImprovement;

            var taskMetrics = new List<(int AttemptsToSolve, int HintsChecked, int TotalTime)>();

            foreach (var task in attemptedTasks)
            {
                var taskMatrix = skillMatrices
                    .Where(sm => sm.TaskId == task.TaskId)
                    .OrderByDescending(sm => sm.ModifyDate)
                    .First();

                taskMetrics.Add((
                    taskMatrix.AttemptstoSolve,
                    taskMatrix.HintsChecked,
                    taskMatrix.TotalTime
                ));
            }

            return CalculatePerformanceGrade(taskMetrics);
        }
        private string CalculatePerformanceGrade(List<(int AttemptsToSolve, int HintsChecked, int TotalTime)> metrics)
        {
            // Calculate weighted scores for each metric
            var weightedScores = metrics.Select(m =>
            {
                float attemptScore = m.AttemptsToSolve > 0
                    ? 1 / (float)m.AttemptsToSolve * 40
                    : 0.4f * 40;

                float hintScore = (1 - Math.Min(m.HintsChecked / 5f, 1)) * 30;
                float timeScore = (1 - Math.Min(m.TotalTime / 600f, 1)) * 30;

                return attemptScore + hintScore + timeScore;
            }).ToList();

            // Calculate average score for attempted tasks
            float averageScore = weightedScores.Average();

            // Calculate completion ratio
            float completionRatio = metrics.Count / (float)metrics.Count;

            // Apply completion penalty for unattempted tasks
            float finalScore = averageScore * completionRatio;

            return finalScore switch
            {
                >= 90 => StatusTypes.Mastered,
                >= 75 => StatusTypes.Proficient,
                >= 50 => StatusTypes.Developing,
                _ => StatusTypes.NeedsImprovement
            };
        }
        public async Task<int> GetTotalTaskCountAllSectionsAsync()
        {
            int total = 0;
            var allActivities = await _activityRepository.GetActivitiesForSimulationAsync(SimulationId, UserId);
            foreach (var activity in allActivities)
            {
                var tasks = await _taskRepository.GetTasksBySimulationAndSectionIdAsync(SimulationId, activity.SectionId, activityId: null);
                total += tasks?.Count ?? 0;
            }
            return total;
        }
        public int GetCurrentSectionCompletedCount()
        {
            // Combine cache and actual progress
            var completedInCache = _taskProgressCache.Values
                .Count(v => v.Status == StatusTypes.Completed);

            var completedInProgress = SectionProgress.TaskProgresses
                .Count(tp =>
                    tp.Status == StatusTypes.Completed &&
                    !_taskProgressCache.ContainsKey(tp.TaskId));

            return completedInCache + completedInProgress;
        }
        //public async Task<int> GetCompletedTaskCountAllSectionsAsync()
        //{
        //    int completed = 0;
        //    var allActivities = await _activityRepository.GetActivitiesForSimulationAsync(SimulationId, UserId);
        //    foreach (var activity in allActivities)
        //    {
        //        if (activity.ActivityId == this.ActivityId && this.SectionProgress != null)
        //        {
        //            // Use in-memory progress for current section
        //            completed += this.SectionProgress.TaskProgresses.Count(tp => tp.Status == StatusTypes.Completed);
        //        }
        //        else
        //        {
        //            // Use DB for other sections
        //            var skillMatrices = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activity.ActivityId);
        //            completed += skillMatrices.Count(sm => sm.Status == StatusTypes.Completed);
        //        }
        //    }
        //    return completed;
        //}   
        //public async Task MarkAllActivitiesAsCompleteAsync()
        //{
        //    var allActivities = await _activityRepository.GetActivitiesForSimulationAsync(SimulationId, UserId);

        //    foreach (var activity in allActivities)
        //    {
        //        activity.Status = StatusTypes.Completed;
        //        activity.ModifyDate = DateTime.Now;
        //        await _activityRepository.UpdateActivityAsync(activity);
        //    }
        //}
        public async Task<int> GetCompletedTaskCountAllSectionsAsync()
        {
            int completed = 0;
            var allActivities = await _activityRepository.GetActivitiesForSimulationAsync(SimulationId, UserId);

            foreach (var activity in allActivities)
            {
                if (activity.ActivityId == this.ActivityId)
                {
                    // Use both cache AND current progress
                    var inMemoryCompleted = this.SectionProgress.TaskProgresses
                        .Where(tp => tp.Status == StatusTypes.Completed)
                        .Count();

                    var cachedCompleted = _taskProgressCache.Values
                        .Where(v => v.Status == StatusTypes.Completed)
                        .Count();

                    completed += Math.Max(inMemoryCompleted, cachedCompleted);
                }
                else
                {
                    var skillMatrices = await _skillMatrixRepository
                        .GetSkillMatrixEntriesForActivityAsync(activity.ActivityId);

                    completed += skillMatrices.Count(sm => sm.Status == StatusTypes.Completed);
                }
            }
            return completed;
        }
    }
}


