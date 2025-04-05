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
        public int CurrentTaskIndex { get; private set; }
        public List<JobTask> Tasks { get; }
        public string FilePath { get; }
        public string ActivityId { get; }
        public string UserId { get; }
        public string SimulationId { get; }
        public int Attempt { get; }
        private List<Section> _sections;
        private int _currentSectionIndex; 
        public ActivityRepository ActivityRepository { get; }
        private readonly SectionRepository _sectionRepository;
        private readonly FileService _fileService;
        private readonly SkillMatrixRepository _skillMatrixRepository;
        private readonly TaskRepository _taskRepository;
        private readonly ActivityRepository _activityRepository;
        private readonly SectionService _sectionService;
        private readonly DataSet _progressDataSet;
        private readonly DataTable _progressTable;
        private Section _currentSection;
        private string _masterJson;
        private Dictionary<int, int> _taskElapsedTimes; // Tracks time per task index

        public int CurrentTaskElapsedTime => _taskElapsedTimes.TryGetValue(CurrentTaskIndex, out var time) ? time : 0;

        public SimulationManager(
            List<JobTask> tasks,
            string filePath,
            string sectionId,
            string simulationId,
            string userId,
            SectionRepository sectionRepository,
            FileService fileService,
            SkillMatrixRepository skillMatrixRepository,
            ActivityRepository activityRepository,
            TaskRepository taskRepository,
            SectionService sectionService,
            UserRepository userRepository,
            string softwareId,
            string activityId,
            DataSet progressDataSet,
            int attempt,
            Section currentSection)
        {
            Tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _sectionRepository = sectionRepository;
            _fileService = fileService;
            _skillMatrixRepository = skillMatrixRepository;
            ActivityRepository = activityRepository;
            _taskRepository = taskRepository;
            _sectionService = sectionService;
            _progressDataSet = progressDataSet;
            _activityRepository = activityRepository ?? throw new ArgumentNullException(nameof(activityRepository));
            _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
            _progressTable = _progressDataSet.Tables["SectionProgress"];
            CurrentTaskIndex = 0;
            _currentSection = currentSection;
            _masterJson = string.Empty;
            _taskElapsedTimes = new Dictionary<int, int>();
            for (int i = 0; i < Tasks.Count; i++)
            {
                _taskElapsedTimes[i] = 0; // Initialize with task index as key
            }

            ActivityId = activityId;
            UserId = userId;
            SimulationId = simulationId;
            Attempt = attempt;
        }



        public void InitializeSections(List<Section> sections)
        {
            _sections = sections;
            _currentSectionIndex = _sections.FindIndex(s => s.SectionId == _currentSection.SectionId);
        }


        public string GetLastSectionIdForUser()
        {
            var row = _progressTable.Rows
                .Cast<DataRow>()
                .Where(r => r["UserId"].ToString() == UserId && r["SimulationId"].ToString() == SimulationId)
                .OrderByDescending(r => Convert.ToDateTime(r["ModifyDate"]))
                .FirstOrDefault();

            return row?["SectionId"]?.ToString();
        }

        private void SaveSectionProgress(string sectionId, int taskIndex)
        {
            var existingRow = _progressTable.Rows
                .Cast<DataRow>()
                .FirstOrDefault(r => r["UserId"].ToString() == UserId && r["SimulationId"].ToString() == SimulationId);

            if (existingRow != null)
            {
                existingRow["SectionId"] = sectionId;
                existingRow["LastTaskIndex"] = taskIndex;
                existingRow["ModifyDate"] = DateTime.UtcNow;
            }
            else
            {
                var row = _progressTable.NewRow();
                row["UserId"] = UserId;
                row["SimulationId"] = SimulationId;
                row["SectionId"] = sectionId;
                row["LastTaskIndex"] = taskIndex;
                row["ModifyDate"] = DateTime.UtcNow;
                _progressTable.Rows.Add(row);
            }

            _progressTable.AcceptChanges(); // or write to file/db
        }

        public async Task LoadSectionAsync(Section section)
        {
            var tasks = await _sectionService.GetAllTasksForSectionAsync(section.SectionId, section.SoftwareId);

            // Order tasks by ModifyDate (newest first)
            Tasks.Clear();
            Tasks.AddRange(tasks.OrderByDescending(t => t.ModifyDate));

            if (Tasks.Count > 0)
            {
                // Load the last modified task and its time from the database
                var lastModifiedTask = await GetLastModifiedTaskAsync();
                if (lastModifiedTask != null)
                {
                    CurrentTaskIndex = Tasks.FindIndex(t => t.TaskId == lastModifiedTask.TaskId);
                    _taskElapsedTimes[CurrentTaskIndex] = lastModifiedTask.TotalTime;
                }
                else
                {
                    CurrentTaskIndex = 0; // Default to the first task if no previous task is found
                }
            }
            else
            {
                CurrentTaskIndex = -1; // No tasks available
            }

            _currentSection = section;
            InitializeTaskElapsedTimes();
        }

        private async Task<SkillMatrix> GetLastModifiedTaskAsync()
        {
            var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(ActivityId);
            return skillMatrixEntries
                .OrderByDescending(e => e.ModifyDate)
                .FirstOrDefault();
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
            if (CurrentTaskIndex < 0 || CurrentTaskIndex >= Tasks.Count)
            {
                // If CurrentTaskIndex is out of range, do not proceed
                return;
            }

            if (_taskElapsedTimes.ContainsKey(CurrentTaskIndex))
            {
                _taskElapsedTimes[CurrentTaskIndex]++;
            }
            else
            {
                _taskElapsedTimes[CurrentTaskIndex] = 1;
            }

            await UpdateSkillMatrixAsync(Tasks[CurrentTaskIndex]);
        }

        private async Task UpdateSkillMatrixAsync(JobTask currentTask)
        {
            var existingEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, currentTask.TaskId);
            if (existingEntry != null)
            {
                existingEntry.TotalTime = _taskElapsedTimes[CurrentTaskIndex]; // Save current task's time
                await _skillMatrixRepository.SaveSkillMatrixAsync(existingEntry, UserId);
            }
        }

        public void LoadMasterData()
        {
            var validationForm = ValidationFormFactory.CreateValidationForm(new TaskSubmission
            {
                SectionId = _currentSection.SectionId,
                SoftwareId = _currentSection.SoftwareId // Assuming softwareId is the filePath
            });
            _masterJson = validationForm.GetMasterJsonForSection(_currentSection.SectionId);
        }

        public async Task SaveCurrentStateAsync()
        {
            await SaveProgressAsync();
        }

        public async Task<Section> GetNextSectionAsync()
        {
            return await _sectionService.GetNextSectionAsync(UserId, SimulationId, _currentSection.SectionId);
        }

        public async Task<Section> GetPreviousSectionAsync(string userId, string simulationId, string currentSectionId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(simulationId) || string.IsNullOrEmpty(currentSectionId))
            {
                throw new ArgumentException("User ID, simulation ID, and current section ID cannot be null or empty.");
            }

            var lastActivity = await _activityRepository.GetLatestActivityAsync(userId, simulationId, currentSectionId);
            if (lastActivity == null)
            {
                Console.WriteLine("No last activity found, cannot load previous section.");
                return null;
            }

            var prevSection = await _sectionRepository.GetSectionByIdAsync(lastActivity.SectionId);
            if (prevSection == null)
            {
                Console.WriteLine($"No previous section found for Section ID: {lastActivity.SectionId}");
                return null; // Or throw an exception
            }

            return prevSection;
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

            var validationForm = ValidationFormFactory.CreateValidationForm(taskSubmission);
            return validationForm.ValidateTask(taskSubmission, _masterJson);
        }

        private async Task UpdateActivityStatusAsync()
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
            string result = await ActivityRepository.CalculateResultAsync(ActivityId);

            await ActivityRepository.UpdateActivityAsync(new Activity
            {
                ActivityId = ActivityId,
                Status = newStatus,
                Result = result,
                ModifyBy = UserId,
                ModifyDate = DateTime.UtcNow
            });
        }
        public async Task SaveProgressAsync()
        {
            var taskId = Tasks[CurrentTaskIndex].TaskId;
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

            // Use await here to avoid blocking the thread
            await _taskRepository.SaveCurrentTaskIndexAsync(ActivityId, taskId, CurrentTaskIndex, _currentSection.SectionId, UserId);
        }

        public async Task<JobTask> LoadTaskAsync(int taskIndex)
        {
            if (taskIndex < 0 || taskIndex >= Tasks.Count) return null;

            var task = Tasks[taskIndex];
            var existingEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, task.TaskId);

            // Initialize or update the elapsed time for this task
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

            string calculatedResult = await ActivityRepository.CalculateActivityResult(ActivityId);

            await ActivityRepository.SaveActivityAsync(new Activity
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

        private async Task SaveTaskProgressAsync(bool isTaskCompleted)
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

        private async Task LoadNextSectionAsync()
        {
            var nextSection = await GetNextSectionAsync();
            if (nextSection != null)
            {
                await LoadSectionAsync(nextSection);
            }
            else
            {
                MessageBox.Show("No more sections available.");
            }
        }


        private void CloseFile(string filePath)
        {
            try
            {
                var processName = Path.GetExtension(filePath)?.ToLower() switch
                {
                    ".xlsx" or ".xls" => "EXCEL",
                    ".pdf" => "Acrobat",
                    ".docx" or ".doc" => "WINWORD",
                    ".pptx" or ".ppt" => "POWERPNT",
                    _ => null
                };

                if (!string.IsNullOrEmpty(processName))
                {
                    foreach (var process in Process.GetProcessesByName(processName))
                        process.Kill();
                }
                if (File.Exists(filePath)) File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing file: {ex.Message}");
            }
        }

        private async Task HandleSectionCompletion()
        {
            var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(ActivityId);
            var completedCount = skillMatrixEntries.Count(e => e.Status == StatusTypes.Completed);

            if (completedCount == Tasks.Count)
            {
                await ActivityRepository.UpdateActivityAsync(new Activity
                {
                    ActivityId = ActivityId,
                    Status = StatusTypes.Completed,
                    ModifyDate = DateTime.UtcNow
                });
            }
        }



        public async Task MoveToNextTask()
        {
            if (CurrentTaskIndex < Tasks.Count - 1)
            {
                CurrentTaskIndex++;
                await SaveCurrentStateAsync();
                await LoadTaskAsync(CurrentTaskIndex); // Load the current task
            }
            else
            {
                await HandleSectionCompletion(); // Handle if at last task
            }
        }

        public async Task MoveToPreviousTask()
        {
            if (CurrentTaskIndex > 0)
            {
                CurrentTaskIndex--;
                await SaveCurrentStateAsync();
                await LoadTaskAsync(CurrentTaskIndex);// Load the current task
            }
        }

        public async Task SaveAndLoadNextSectionAsync()
        {
            // Your logic to save progress
            await SaveProgressAsync();

            // Then move to the next section
            var nextSection = await GetNextSectionAsync();
            if (nextSection != null)
            {
                await LoadSectionAsync(nextSection);
            }
            else
            {
                MessageBox.Show("No more sections available.");
            }
        }

        public async Task SaveAndPreviousSectionAsync()
        {
            // Always save progress
            await SaveProgressAsync();

            // Get the previous section using the SectionService with the stored userId and simulationId
            var previousSection = await _sectionService.GetPreviousSectionAsync(UserId, SimulationId, _currentSection.SectionId);

            if (previousSection != null)
            {
                await LoadSectionAsync(previousSection); // Load the previous section
            }
            else
            {
                MessageBox.Show("This is the first section.");
            }
        }

    }
}




