
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

            // 🆕 Load the master data for the newly updated section
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

        public async Task IncrementTimeElapsedAsync()
        {
            if (CurrentTaskIndex < 0 || CurrentTaskIndex >= Tasks.Count) return;

            if (_taskElapsedTimes.ContainsKey(CurrentTaskIndex))
                _taskElapsedTimes[CurrentTaskIndex]++;
            else
                _taskElapsedTimes[CurrentTaskIndex] = 1;

            await UpdateSkillMatrixAsync(Tasks[CurrentTaskIndex]);
        }

        private async Task UpdateSkillMatrixAsync(JobTask currentTask)
        {
            var existingEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, currentTask.TaskId);
            if (existingEntry != null)
            {
                existingEntry.TotalTime = _taskElapsedTimes[CurrentTaskIndex];
                await _skillMatrixRepository.SaveSkillMatrixAsync(existingEntry, UserId);
            }
        }

        public void LoadMasterData()
        {
            var validationForm = ValidationFormFactory.CreateValidationForm(new TaskSubmission
            {
                SectionId = _currentSection.SectionId,
                SoftwareId = _currentSection.SoftwareId // Assuming softwareId is the filePath
            }, _fileService); // Pass the _fileService instance here
            _masterJson = validationForm.GetMasterJsonForSection(_currentSection.SectionId);
        }

        public async Task SaveCurrentStateAsync()
        {
            await SaveProgressAsync();
        }

        public async Task<string> CheckAnswerAsync(int taskIndex)
        {
            var currentTask = Tasks[taskIndex];
            bool isCorrect = ValidateTask(currentTask);

            var existingEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, currentTask.TaskId);
            int taskAttempt = (existingEntry?.TaskAttempt ?? 0) + 1;
            int attempts = existingEntry?.AttemptstoSolve ?? 0;

            // Only update AttemptsToSolve if this is the first correct answer
            if (isCorrect && existingEntry?.Status != StatusTypes.Completed)
            {
                attempts = taskAttempt; // Store the current attempt number
            }

            var skillMatrix = new SkillMatrix
            {
                ActivityId = ActivityId,
                TaskId = currentTask.TaskId,
                Status = isCorrect ? StatusTypes.Completed : StatusTypes.InComplete,
                AttemptstoSolve = attempts, // This now stores the attempt number when first correct
                TaskAttempt = taskAttempt,  // This increments with every check
                ModifyBy = UserId,
                ModifyDate = DateTime.UtcNow,
                HintsChecked = existingEntry?.HintsChecked ?? 0,
                TotalTime = _taskElapsedTimes[taskIndex]
            };

            await _skillMatrixRepository.SaveSkillMatrixAsync(skillMatrix, UserId);
            await UpdateActivityStatusAsync();

            return isCorrect ? "Correct!" : "Incorrect, please try again.";
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
            var latestStatuses = skillMatrixEntries
                .GroupBy(e => e.TaskId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.ModifyDate).First().Status);

            int completedCount = latestStatuses.Count(kv => kv.Value == StatusTypes.Completed);
            int totalTasks = Tasks.Count;
            double completionRatio = (double)completedCount / totalTasks;

            // Determine Status (Section Progress)
            string newStatus;
            if (completedCount == totalTasks)
            {
                newStatus = StatusTypes.Completed;
            }
            else if (completionRatio >= 0.7)
            {
                newStatus = StatusTypes.PartiallyCompleted;
            }
            else if (completedCount > 0)
            {
                newStatus = StatusTypes.InProgress;
            }
            else
            {
                newStatus = StatusTypes.NotStarted;
            }

            // Calculate Result (Performance)
            string result = await _activityRepository.CalculateResultAsync(ActivityId);

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

        public async Task UpdateSkillMatrixStatusAndAttempt(int taskIndex)
        {
            var currentTask = Tasks[taskIndex];
            var existingEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, currentTask.TaskId);
            int taskAttempt = (existingEntry?.TaskAttempt ?? 0) + 1;

            var skillMatrix = new SkillMatrix
            {
                ActivityId = ActivityId,
                TaskId = currentTask.TaskId,
                Status = StatusTypes.Completed, // Mark status as completed regardless of correctness
                TaskAttempt = taskAttempt,  // Increment TaskAttempt
                ModifyBy = UserId,
                ModifyDate = DateTime.UtcNow,
                HintsChecked = existingEntry?.HintsChecked ?? 0,
                TotalTime = _taskElapsedTimes[taskIndex]
            };

            await _skillMatrixRepository.SaveSkillMatrixAsync(skillMatrix, UserId);
        }
        public async Task SaveProgressAsync()
        {
            var existingRow = _progressTable.Rows.Find(new object[] { _currentSection.SectionId, UserId });

            if (existingRow != null)
            {
                existingRow["TaskIndex"] = CurrentTaskIndex;
                existingRow["TimeElapsed"] = _taskElapsedTimes[CurrentTaskIndex];
                existingRow["FilePath"] = FilePath;
            }
            else
            {
                var newRow = _progressTable.NewRow();
                newRow["SectionId"] = _currentSection.SectionId;
                newRow["UserId"] = UserId;
                newRow["TaskIndex"] = CurrentTaskIndex;
                newRow["TimeElapsed"] = _taskElapsedTimes[CurrentTaskIndex];
                newRow["IsCompleted"] = false;
                newRow["FilePath"] = FilePath;
                _progressTable.Rows.Add(newRow);
            }

            if (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
            {
                try
                {
                    byte[] fileBytes;

                    // ✅ Properly read and flush the file
                    using (var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var ms = new MemoryStream())
                    {
                        await stream.CopyToAsync(ms);
                        await stream.FlushAsync(); // 💡 Ensures read completes before file gets released
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
            else
            {
                MessageBox.Show("File path is invalid or file not found while saving progress.", "File Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private async Task MarkTaskAsVisited(int taskIndex)
        {
            var task = Tasks[taskIndex];
            var existing = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, task.TaskId);

            if (existing == null)
            {
                await _skillMatrixRepository.SaveSkillMatrixAsync(new SkillMatrix
                {
                    ActivityId = ActivityId,
                    TaskId = task.TaskId,
                    Status = StatusTypes.Visited,
                    HintsChecked = 0,
                    TotalTime = 0,
                    CreateBy = UserId,
                    ModifyBy = UserId
                }, UserId);
            }
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
                CreateDate = DateTime.UtcNow,
                ModifyBy = UserId,
                ModifyDate = DateTime.UtcNow,
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
                CreateDate = DateTime.UtcNow,
                ModifyDate = DateTime.UtcNow,
                CreateBy = UserId,
                ModifyBy = UserId,
                Result = calculatedResult
            }, calculatedResult);

            await _skillMatrixRepository.SaveSkillMatrixAsync(skillMatrix, UserId);
        }

        public async Task SaveTaskProgressAsync(bool isTaskCompleted)
        {
            var currentTask = Tasks[CurrentTaskIndex];
            var existingSkillMatrix = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, currentTask.TaskId);

            var skillMatrix = new SkillMatrix
            {
                ActivityId = ActivityId,
                TaskId = currentTask.TaskId,
                HintsChecked = existingSkillMatrix?.HintsChecked ?? 0,
                TotalTime = _taskElapsedTimes[CurrentTaskIndex],
                AttemptstoSolve = isTaskCompleted ? 1 : existingSkillMatrix?.AttemptstoSolve + 1 ?? 1,
                Status = isTaskCompleted ? StatusTypes.Completed : StatusTypes.InComplete,
                CreateBy = UserId,
                CreateDate = DateTime.UtcNow,
                ModifyBy = UserId,
                ModifyDate = DateTime.UtcNow,
                TaskAttempt = existingSkillMatrix?.TaskAttempt + 1 ?? 1
            };

            await _skillMatrixRepository.SaveSkillMatrixAsync(skillMatrix, UserId);
            MessageBox.Show("Progress saved successfully.");
        }

        private string CalculateResultStatus(List<JobTask> tasks, Dictionary<string, string> taskStatuses)
        {
            int completedCount = tasks.Count(t => taskStatuses.ContainsKey(t.TaskId) && taskStatuses[t.TaskId] == StatusTypes.Completed);
            double ratio = (double)completedCount / tasks.Count;
            return ratio switch
            {
                >= 0.9 => "Mastered",
                >= 0.7 => "Proficient",
                >= 0.4 => "Developing",
                _ => "Needs Improvement"
            };
        }

        private string DetermineSectionStatus(List<JobTask> tasks, Dictionary<string, SkillMatrix> taskStatuses)
        {
            int completedCount = tasks.Count(t => taskStatuses.ContainsKey(t.TaskId) && taskStatuses[t.TaskId].Status == StatusTypes.Completed);
            double ratio = (double)completedCount / tasks.Count;
            return ratio switch
            {
                1 => StatusTypes.Completed,
                >= 0.7 => StatusTypes.PartiallyCompleted,
                > 0 => StatusTypes.InProgress,
                _ => StatusTypes.NotStarted
            };
        }

        public async Task<bool> AreAllTasksCompleted()
        {
            var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(ActivityId);
            return Tasks.All(task =>
                skillMatrixEntries.Any(e =>
                    e.TaskId == task.TaskId &&
                    e.Status == StatusTypes.Completed));
        }

        public async Task CompleteAllTasks()
        {
            foreach (var task in Tasks)
            {
                var existingEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, task.TaskId);

                var skillMatrix = new SkillMatrix
                {
                    ActivityId = ActivityId,
                    TaskId = task.TaskId,
                    Status = StatusTypes.Completed,
                    AttemptstoSolve = existingEntry?.AttemptstoSolve ?? 1,
                    TaskAttempt = existingEntry?.TaskAttempt ?? 1,
                    TotalTime = _taskElapsedTimes[Tasks.IndexOf(task)],
                    ModifyDate = DateTime.UtcNow
                };

                await _skillMatrixRepository.SaveSkillMatrixAsync(skillMatrix, UserId);
            }

            await UpdateActivityStatusAsync();
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


        //public async Task<int> GetElapsedTimeForTaskAsync(string simulationId, int taskIndex)
        //{
        //    // Fetch the elapsed time for the task from the _taskElapsedTimes dictionary.
        //    // If no time exists, return 0.
        //    return await Task.FromResult(
        //        _taskElapsedTimes.TryGetValue(taskIndex, out var elapsedTime) ? elapsedTime : 0
        //    );
        //}

        //public async Task SaveElapsedTimeForTaskAsync(string simulationId, int taskIndex, int elapsedTime)
        //{
        //    // Save the elapsed time for the task to the _taskElapsedTimes dictionary.
        //    if (_taskElapsedTimes.ContainsKey(taskIndex))
        //    {
        //        _taskElapsedTimes[taskIndex] = elapsedTime;
        //    }
        //    else
        //    {
        //        _taskElapsedTimes.Add(taskIndex, elapsedTime);
        //    }

        //    // Optionally, update the SkillMatrix or persistent storage here.
        //    if (taskIndex >= 0 && taskIndex < Tasks.Count)
        //    {
        //        await UpdateSkillMatrixAsync(Tasks[taskIndex]);
        //    }
        //}
    }
}





