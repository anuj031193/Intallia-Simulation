
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JobSimulation.BLL;
using JobSimulation.DAL;
using JobSimulation.Models;
using System.Diagnostics;
using Activity = JobSimulation.Models.Activity;
using JobSimulation.Forms;

namespace JobSimulation.Managers
{
    public class SimulationManager
    {
        public int CurrentTaskIndex { get; set; }
        public List<JobTask> Tasks { get; set; }
        public string FilePath { get; set; }
        public string ActivityId { get; set; }
        public string UserId { get; }
        public string SimulationId { get; }
        public int Attempt { get; set; }

        private readonly FileService _fileService;
        private readonly SkillMatrixRepository _skillMatrixRepository;
        private readonly TaskRepository _taskRepository;
        private readonly ActivityRepository _activityRepository;
        private readonly DataTable _progressTable;
        private readonly TaskService _taskService;
        public Section _currentSection;

        private Dictionary<int, int> _taskElapsedTimes;
        private string _masterJson;

        public int CurrentTaskElapsedTime => _taskElapsedTimes.TryGetValue(CurrentTaskIndex, out var time) ? time : 0;

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
            DataSet progressDataSet,
            int attempt,
            Section currentSection,
            string activityId,
            int initialTaskIndex = 0)
        {
            Tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _fileService = fileService;
            _skillMatrixRepository = skillMatrixRepository;
            _taskRepository = taskRepository;
            _activityRepository = activityRepository;
            _taskService = taskService;
            _progressTable = progressDataSet.Tables["SectionProgress"];
            Attempt = attempt;
            _currentSection = currentSection;
            UserId = userId;
            SimulationId = simulationId;
            ActivityId = activityId;
            CurrentTaskIndex = initialTaskIndex;

            InitializeTaskElapsedTimes();
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
            Tasks = newTasks;
            FilePath = newFilePath;
            ActivityId = newActivityId;
            Attempt = newAttempt;
            _currentSection = newCurrentSection;
            CurrentTaskIndex = newTaskIndex;

            _taskElapsedTimes.Clear();
            for (int i = 0; i < Tasks.Count; i++)
                _taskElapsedTimes[i] = 0;

            if (CurrentTaskIndex >= 0 && CurrentTaskIndex < Tasks.Count)
            {
                var taskId = Tasks[CurrentTaskIndex].TaskId;
                var existingEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, taskId);
                if (existingEntry != null)
                    _taskElapsedTimes[CurrentTaskIndex] = existingEntry.TotalTime;
            }

            //// 🆕 Load the master data for the newly updated section
            LoadMasterData();
        }

        public async Task<(List<JobTask> tasks, int currentTaskIndex, int timeElapsed)> LoadTaskDetailsForSectionAsync(string sectionId, string activityId)
        {
            return await _taskService.LoadTaskDetailsForSectionAsync(sectionId, UserId, activityId);
        }

        private void InitializeTaskElapsedTimes()
        {
            _taskElapsedTimes = new Dictionary<int, int>();
            for (int i = 0; i < Tasks.Count; i++)
            {
                _taskElapsedTimes[i] = 0; // Initialize with task index as key
            }
        }


        private async Task UpdateSkillMatrixAsync(JobTask currentTask)
        {
            var existingEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, currentTask.TaskId);
            if (existingEntry != null)
            {
                existingEntry.TotalTime = _taskElapsedTimes[CurrentTaskIndex];
             await _skillMatrixRepository.UpsertSkillMatrixAsync(existingEntry, UserId);
            }
        }

        public void LoadMasterData()
        {
            try
            {
                // Create a TaskSubmission object for the current section
                var taskSubmission = new TaskSubmission
                {
                    SectionId = _currentSection.SectionId,
                    SimulationId = _currentSection.SimulationId, // Include the SimulationId
                    SoftwareId = _currentSection.SoftwareId // Use the SoftwareId from the current section
                };

                // Create the validation form using the factory
                var validationForm = ValidationFormFactory.CreateValidationForm(taskSubmission, _fileService);

                // Fetch the master JSON for the current section
                _masterJson = validationForm.GetMasterJsonForSection(_currentSection.SectionId);

                if (string.IsNullOrEmpty(_masterJson))
                {
                    MessageBox.Show("Master data could not be loaded. Please verify the configuration.",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions gracefully and provide feedback
                MessageBox.Show($"Error loading master data: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public async Task<string> CheckAnswerAsync(int taskIndex)
        {
            // Retrieve the current task
            var currentTask = Tasks[taskIndex];

            // Validate the task to check if the answer is correct
            bool isCorrect = ValidateTask(currentTask);

            // Get the existing entry from the SkillMatrix or initialize default values
            var existingEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, currentTask.TaskId);
            int taskAttempt = existingEntry?.TaskAttempt ?? 0; // Default to 0 if no entry exists
            int attemptsToSolve = existingEntry?.AttemptstoSolve ?? 0; // Default to 0 if no entry exists

            // Increment TaskAttempt for every click of btnCheckAnswer
            taskAttempt++;

            // Update AttemptsToSolve only for the first correct answer
            if (isCorrect && (existingEntry?.AttemptstoSolve ?? 0) == 0)
            {
                attemptsToSolve = taskAttempt; // Set AttemptsToSolve to the current TaskAttempt
            }

            // Prepare the updated SkillMatrix entry
            var skillMatrix = new SkillMatrix
            {
                ActivityId = ActivityId,
                TaskId = currentTask.TaskId,
                Status = StatusTypes.Completed, // Always mark the status as Completed
                AttemptstoSolve = attemptsToSolve, // Retain the first correct attempt value
                TaskAttempt = taskAttempt,         // Increment with each click of btnCheckAnswer
                ModifyBy = UserId,
                ModifyDate = DateTime.Now, // Use local time
                CreateBy = existingEntry?.CreateBy ?? UserId, // Preserve existing CreateBy or set for new entries
                CreateDate = existingEntry?.CreateDate ?? DateTime.Now, // Preserve existing CreateDate or set for new entries
                HintsChecked = existingEntry?.HintsChecked ?? 0,
                TotalTime = _taskElapsedTimes[taskIndex]
            };

            // Save the updated SkillMatrix entry
            await _skillMatrixRepository.UpsertSkillMatrixAsync(skillMatrix, UserId);

            // Update the activity status to reflect the progress
            await UpdateActivityStatusAsync();

            // Return feedback to the user
            return isCorrect ? "Correct!" : "Incorrect!";
        }
        private bool ValidateTask(JobTask task)
        {
            var taskSubmission = new TaskSubmission
            {
                FilePath = FilePath,
                SoftwareId = _currentSection.SoftwareId, // Assuming softwareId is the filePath
                SimulationId = SimulationId,
                SectionId = _currentSection.SectionId,
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

            string result = CalculateResult(skillMatrixEntries);

            await _activityRepository.UpdateActivityAsync(new Activity
            {
                ActivityId = ActivityId,
                Status = newStatus,
                Result = result,
                ModifyBy = UserId,
                ModifyDate = DateTime.UtcNow
            });
        }
        public async Task<Activity> GetActivityAsync()
        {
            return await _activityRepository.GetByIdAsync(ActivityId);
        }
        public async Task UpdateActivityAsync(Activity activity)
        {
            await _activityRepository.UpdateAsync(activity);
        }

        public async Task SaveProgressAsync()
        {
            var existingRow = _progressTable.Rows.Find(new object[] { _currentSection.SectionId, UserId });

            if (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
            {
                try
                {
                    byte[] fileBytes;
                    using (var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var ms = new MemoryStream())
                    {
                        await stream.CopyToAsync(ms);
                        fileBytes = ms.ToArray();
                    }
                    string base64File = Convert.ToBase64String(fileBytes);
                    await _activityRepository.UpdateActivityStudentFileAsync(ActivityId, base64File, UserId);
                }
                catch (IOException ioEx)
                {
                    MessageBox.Show($"Failed to save student file: {ioEx.Message}", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            await _taskRepository.SaveCurrentTaskIndexAsync(
                ActivityId,
                Tasks[CurrentTaskIndex].TaskId,
                CurrentTaskIndex,
                _currentSection.SectionId,
                UserId);
        }
        public async Task<JobTask> LoadTaskAsync(int taskIndex)
        {
            if (taskIndex < 0 || taskIndex >= Tasks.Count) return null;

            var task = Tasks[taskIndex];
            var existingEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, task.TaskId);
            _taskElapsedTimes[taskIndex] = existingEntry?.TotalTime ?? 0;

            return task;
        }


        public async Task<string> GetHintAsync(int taskIndex)
        {
            var currentTask = Tasks[taskIndex];
            var existingEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, currentTask.TaskId);

            // Do not update hints or status if task is already completed
            if (existingEntry?.Status == StatusTypes.Completed)
                return ((dynamic)currentTask.Details).Hint;

            // Increment hints checked
            int newHints = (existingEntry?.HintsChecked ?? 0) + 1;

            var taskSubmission = new TaskSubmission
            {
                FilePath = FilePath,
                SoftwareId = _currentSection.SoftwareId,
                SimulationId = SimulationId,
                SectionId = _currentSection.SectionId,
                Task = currentTask,
                HintsChecked = newHints
            };

            // Do not increment task attempt here
            await SaveActivityDataAsync(taskSubmission, isCorrect: false, taskAttempt: existingEntry?.TaskAttempt ?? 0);

            return ((dynamic)currentTask.Details).Hint;
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

            // Only update AttemptsToSolve if this is the first correct answer
            if (isCorrect && existingEntry?.Status != StatusTypes.Completed)
            {
                attempts = taskAttempt;
            }

            var skillMatrix = new SkillMatrix
            {
                ActivityId = ActivityId,
                TaskId = taskSubmission.Task.TaskId,
                HintsChecked = taskSubmission.HintsChecked,
                TotalTime = _taskElapsedTimes[CurrentTaskIndex],
                AttemptstoSolve = attempts,
                Status = isCorrect ? StatusTypes.Completed :
                    (taskSubmission.HintsChecked > 0 || taskAttempt > 0) ?
                        StatusTypes.InComplete :
                        StatusTypes.Visited,
                CreateBy = UserId,
                CreateDate = DateTime.Now,
                ModifyBy = UserId,
                ModifyDate = DateTime.Now,
                TaskAttempt = taskAttempt
            };

            string calculatedResult = await _activityRepository.CalculateActivityResult(ActivityId);

            await _activityRepository.SaveActivityAsync(new Activity
            {
                ActivityId = ActivityId,
                UserId = UserId,
                SimulationId = SimulationId,
                SectionId = _currentSection.SectionId,
                Status = skillMatrix.Status,
                SectionAttempt = Attempt,
                StudentFile = Convert.ToBase64String(fileBytes),
                CreateDate = DateTime.Now,
                ModifyDate = DateTime.Now,
                CreateBy = UserId,
                ModifyBy = UserId,
                Result = calculatedResult
            }, calculatedResult);

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


        public void MoveToNextTask()
        {
            if (CurrentTaskIndex < Tasks.Count - 1)
            {
                CurrentTaskIndex++;
            }
        }

        public void MoveToPreviousTask()
        {
            if (CurrentTaskIndex > 0)
            {
                CurrentTaskIndex--;
            }
        }


        public async Task MarkUnvisitedTasksAsInCompleted()
        {
            // Fetch all activities (sections) for the simulation
            var allActivities = await GetAllActivitiesForSimulationAsync();

            foreach (var activity in allActivities)
            {
                // Fetch all tasks for this activity's section
                var tasks = await _taskRepository.GetTasksForSectionAsync(activity.SectionId);

                // Fetch all skill matrix entries for this activity
                var skillMatrices = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activity.ActivityId);

                foreach (var task in tasks)
                {
                    var existingEntry = skillMatrices.FirstOrDefault(sm => sm.TaskId == task.TaskId);

                    if (existingEntry == null)
                    {
                        // Task was never visited, mark as Incomplete
                        var newEntry = new SkillMatrix
                        {
                            ActivityId = activity.ActivityId,
                            TaskId = task.TaskId,
                            Status = StatusTypes.InComplete,
                            AttemptstoSolve = 0,
                            TaskAttempt = 0,
                            HintsChecked = 0,
                            TotalTime = 0,
                            CreateBy = UserId,
                            CreateDate = DateTime.Now,
                            ModifyBy = UserId,
                            ModifyDate = DateTime.Now
                        };
                        await _skillMatrixRepository.UpsertSkillMatrixAsync(newEntry, UserId);
                    }
                    else if (existingEntry.Status == StatusTypes.Visited)
                    {
                        // Retain the status for visited tasks
                        existingEntry.ModifyBy = UserId;
                        existingEntry.ModifyDate = DateTime.Now;
                        await _skillMatrixRepository.UpsertSkillMatrixAsync(existingEntry, UserId);
                    }
                }
            }
        }
        public async Task<string> CalculateSectionResultAsync(string activityId)
        {
            // Fetch the activity and related data
            var activity = await _activityRepository.GetByIdAsync(activityId);
            var skillMatrices = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activityId);
            var tasks = await _taskRepository.GetTasksForSectionAsync(activity.SectionId);

            // If no tasks exist or no skill matrix entries are found, return "Needs Improvement"
            if (!tasks.Any() || !skillMatrices.Any())
                return StatusTypes.NeedsImprovement;

            // Aggregate task performance metrics
            var taskMetrics = new List<(int AttemptsToSolve, int HintsChecked, int TotalTime)>();

            foreach (var task in tasks)
            {
                var taskMatrix = skillMatrices
                    .Where(sm => sm.TaskId == task.TaskId)
                    .OrderByDescending(sm => sm.ModifyDate)
                    .FirstOrDefault();

                // Skip unvisited or incomplete tasks
                if (taskMatrix == null || !taskMatrix.IsCorrect)
                    return StatusTypes.NeedsImprovement;

                taskMetrics.Add((
                    taskMatrix.AttemptstoSolve,
                    taskMatrix.HintsChecked,
                    taskMatrix.TotalTime
                ));
            }

            // Calculate the performance grade for the section
            var result = CalculatePerformanceGrade(taskMetrics);

            // Update the result in the Activity table
            activity.Result = result;
            activity.ModifyDate = DateTime.UtcNow;
            await _activityRepository.UpdateActivityAsync(activity);

            return result;
        }

        private string CalculatePerformanceGrade(List<(int AttemptsToSolve, int HintsChecked, int TotalTime)> metrics)
        {
            var performanceScore = metrics.Average(m =>
                (m.AttemptsToSolve == 0 ? 0.4 : 1 / (float)m.AttemptsToSolve) * 40 + // Score for attempts
                (1 - (float)m.HintsChecked / 5) * 30 + // Score for hints
                (1 - (float)m.TotalTime / 600) * 30   // Score for time
            );

            return performanceScore switch
            {
                >= 90 => StatusTypes.Mastered,
                >= 75 => StatusTypes.Proficient,
                >= 50 => StatusTypes.Developing,
                _ => StatusTypes.NeedsImprovement
            };
        }
        public async Task SaveActivityAsync(Activity activity)
        {
            if (activity == null) throw new ArgumentNullException(nameof(activity));
            await _activityRepository.SaveActivityAsync(activity);
        }

        private string CalculateProgressStatus(int completedCount, int totalTasks)
        {
            double completionRatio = (double)completedCount / totalTasks;

            return completedCount switch
            {
                _ when completedCount == totalTasks => StatusTypes.Completed,
                _ when completionRatio >= 0.7 => StatusTypes.PartiallyCompleted,
                _ when completedCount > 0 => StatusTypes.InProgress,
                _ => StatusTypes.NotStarted
            };
        }

        private void UpsertProgressRow(string sectionId, int taskIndex, int timeElapsed, string filePath)
        {
            var existingRow = _progressTable.Rows.Find(new object[] { sectionId, UserId });

            if (existingRow != null)
            {
                existingRow["TaskIndex"] = taskIndex;
                existingRow["TimeElapsed"] = timeElapsed;
                existingRow["FilePath"] = filePath;
            }
            else
            {
                var newRow = _progressTable.NewRow();
                newRow["SectionId"] = sectionId;
                newRow["UserId"] = UserId;
                newRow["TaskIndex"] = taskIndex;
                newRow["TimeElapsed"] = timeElapsed;
                newRow["IsCompleted"] = false;
                newRow["FilePath"] = filePath;
                _progressTable.Rows.Add(newRow);
            }
        }

        private string CalculateResult(IEnumerable<SkillMatrix> skillMatrixEntries)
        {
            // Ensure skill matrix entries are not null or empty
            if (skillMatrixEntries == null || !skillMatrixEntries.Any())
            {
                return StatusTypes.NeedsImprovement;
            }

            // Count total tasks and correctly completed tasks
            int totalTasks = skillMatrixEntries.Count();
            int correctTasks = skillMatrixEntries.Count(e => e.Status == StatusTypes.Completed);

            // Calculate performance ratio
            double performanceRatio = (double)correctTasks / totalTasks;

            // Determine the result based on performance ratio
            return performanceRatio switch
            {
                >= 0.9 => StatusTypes.Mastered,
                >= 0.7 => StatusTypes.Proficient,
                >= 0.4 => StatusTypes.Developing,
                _ => StatusTypes.NeedsImprovement
            };
        }



        public async Task<string> UpdateSectionResultAsync(string activityId)
        {
            var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activityId);
            var sectionResult = CalculateSectionResult(skillMatrixEntries);

            var activity = await _activityRepository.GetByIdAsync(activityId);
            activity.Result = sectionResult;
            activity.ModifyDate = DateTime.Now;

            await _activityRepository.UpdateActivityAsync(activity);
            return sectionResult;
        }

        private string CalculateSectionResult(IEnumerable<SkillMatrix> skillMatrixEntries)
        {
            if (skillMatrixEntries == null || !skillMatrixEntries.Any())
                return StatusTypes.NeedsImprovement;

            // Aggregate task performance metrics
            var taskMetrics = skillMatrixEntries.Select(e => new
            {
                IsCorrect = e.AttemptstoSolve > 0, // Correct if AttemptstoSolve > 0
                AttemptstoSolve = e.AttemptstoSolve,
                HintsChecked = e.HintsChecked,
                TotalTime = e.TotalTime
            }).ToList();

            // Calculate weighted score for the section
            var sectionScore = taskMetrics.Average(task =>
                (task.IsCorrect ? 1 : 0) * 0.5 + // Correctness contributes 50%
                (1 / (1 + (float)task.AttemptstoSolve)) * 0.2 + // Fewer attempts contribute positively
                (1 - (float)task.HintsChecked / 5) * 0.2 + // Fewer hints contribute positively
                (1 - (float)task.TotalTime / 600) * 0.1 // Less time contributes positively
            ) * 100;

            // Map the score to a performance level
            return sectionScore switch
            {
                >= 90 => StatusTypes.Mastered,
                >= 75 => StatusTypes.Proficient,
                >= 50 => StatusTypes.Developing,
                _ => StatusTypes.NeedsImprovement
            };
        }
        // SIMULATION-LEVEL OPERATIONS

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


        public async Task IncrementTimeElapsedAsync()
        {
            if (CurrentTaskIndex < 0 || CurrentTaskIndex >= Tasks.Count) return;

            if (_taskElapsedTimes.ContainsKey(CurrentTaskIndex))
                _taskElapsedTimes[CurrentTaskIndex]++;
            else
                _taskElapsedTimes[CurrentTaskIndex] = 1;

            // Update the SkillMatrix with the new TotalTime
            var currentTask = Tasks[CurrentTaskIndex];
            var existingEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, currentTask.TaskId);

            if (existingEntry != null)
            {
                existingEntry.TotalTime = _taskElapsedTimes[CurrentTaskIndex];
                existingEntry.ModifyBy = UserId;
                existingEntry.ModifyDate = DateTime.UtcNow;

                await _skillMatrixRepository.UpsertSkillMatrixAsync(existingEntry, UserId);
            }
            else
            {
                // If the task is not in SkillMatrix, add it with the current TotalTime
                await _skillMatrixRepository.UpsertSkillMatrixAsync(new SkillMatrix
                {
                    ActivityId = ActivityId,
                    TaskId = currentTask.TaskId,
                    TotalTime = _taskElapsedTimes[CurrentTaskIndex],
                    Status = StatusTypes.Visited,
                    CreateBy = UserId,
                    CreateDate = DateTime.UtcNow,
                    ModifyBy = UserId,
                    ModifyDate = DateTime.UtcNow,
                    TaskAttempt = 1
                }, UserId);
            }
        }


        public async Task<List<Activity>> GetAllActivitiesForSimulationAsync()
        {
            var activities = await _activityRepository.GetActivitiesForSimulationAsync(SimulationId, UserId);
            return activities.ToList(); // Convert IEnumerable<Activity> to List<Activity>
        }

    }
}


//public async Task UpdateSkillMatrixStatusAndAttempt(int taskIndex)
//{
//    var currentTask = Tasks[taskIndex];
//    var existingEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, currentTask.TaskId);
//    int taskAttempt = (existingEntry?.TaskAttempt ?? 0) + 1;

//    var skillMatrix = new SkillMatrix
//    {
//        ActivityId = ActivityId,
//        TaskId = currentTask.TaskId,
//        Status = StatusTypes.Completed, // Mark status as completed regardless of correctness
//        TaskAttempt = taskAttempt,  // Increment TaskAttempt
//        ModifyBy = UserId,
//        ModifyDate = DateTime.UtcNow,
//        HintsChecked = existingEntry?.HintsChecked ?? 0,
//        TotalTime = _taskElapsedTimes[taskIndex]
//    };
//    await _skillMatrixRepository.UpsertSkillMatrixAsync(skillMatrix, UserId);
//}

//public async Task CompleteAllTasks()
//{
//    foreach (var task in Tasks)
//    {
//        var existingEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, task.TaskId);

//        var skillMatrix = new SkillMatrix
//        {
//            ActivityId = ActivityId,
//            TaskId = task.TaskId,
//            Status = StatusTypes.Completed,
//            AttemptstoSolve = existingEntry?.AttemptstoSolve ?? 1,
//            TaskAttempt = existingEntry?.TaskAttempt ?? 1,
//            TotalTime = _taskElapsedTimes[Tasks.IndexOf(task)],
//            ModifyDate = DateTime.Now
//        };

//        await _skillMatrixRepository.UpsertSkillMatrixAsync(skillMatrix, UserId);
//    }

//    await UpdateActivityStatusAsync();
//}
//public async Task SaveCurrentStateAsync()
//{
//    await SaveProgressAsync();
//}
//public async Task SaveTaskProgressAsync(bool isTaskCompleted)
//{
//    var currentTask = Tasks[CurrentTaskIndex];
//    var existingSkillMatrix = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, currentTask.TaskId);

//    var skillMatrix = new SkillMatrix
//    {
//        ActivityId = ActivityId,
//        TaskId = currentTask.TaskId,
//        HintsChecked = existingSkillMatrix?.HintsChecked ?? 0,
//        TotalTime = _taskElapsedTimes[CurrentTaskIndex],
//        AttemptstoSolve = isTaskCompleted ? 1 : existingSkillMatrix?.AttemptstoSolve + 1 ?? 1,
//        Status = isTaskCompleted ? StatusTypes.Completed : StatusTypes.InComplete,
//        CreateBy = UserId,
//        CreateDate = DateTime.Now,
//        ModifyBy = UserId,
//        ModifyDate = DateTime.Now,
//        TaskAttempt = existingSkillMatrix?.TaskAttempt + 1 ?? 1
//    };

//    await _skillMatrixRepository.UpsertSkillMatrixAsync(skillMatrix, UserId);
//    MessageBox.Show("Progress saved successfully.");
//}

//private string CalculateResultStatus(List<JobTask> tasks, Dictionary<string, string> taskStatuses)
//{
//    int completedCount = tasks.Count(t => taskStatuses.ContainsKey(t.TaskId) && taskStatuses[t.TaskId] == StatusTypes.Completed);
//    double ratio = (double)completedCount / tasks.Count;
//    return ratio switch
//    {
//        >= 0.9 => "Mastered",
//        >= 0.7 => "Proficient",
//        >= 0.4 => "Developing",
//        _ => "Needs Improvement"
//    };
//}

//private string DetermineSectionStatus(List<JobTask> tasks, Dictionary<string, SkillMatrix> taskStatuses)
//{
//    int completedCount = tasks.Count(t => taskStatuses.ContainsKey(t.TaskId) && taskStatuses[t.TaskId].Status == StatusTypes.Completed);
//    double ratio = (double)completedCount / tasks.Count;
//    return ratio switch
//    {
//        1 => StatusTypes.Completed,
//        >= 0.7 => StatusTypes.PartiallyCompleted,
//        > 0 => StatusTypes.InProgress,
//        _ => StatusTypes.NotStarted
//    };
//}



