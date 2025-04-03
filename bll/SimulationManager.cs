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
        public ActivityRepository ActivityRepository { get; }
        private readonly SectionRepository _sectionRepository;
        private readonly FileService _fileService;
        private readonly SkillMatrixRepository _skillMatrixRepository;
        private readonly TaskRepository _taskRepository;
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

        public async Task<Section> GetPreviousSectionAsync()
        {
            return await _sectionService.GetPreviousSectionAsync(UserId, SimulationId, _currentSection.SectionId);
        }

        public async Task<string> CheckAnswerAsync(int taskIndex)
        {
            var currentTask = Tasks[taskIndex];
            bool isCorrect = ValidateTask(currentTask);

            var existingEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(ActivityId, currentTask.TaskId);
            int taskAttempt = (existingEntry?.TaskAttempt ?? 0) + 1;
            int attempts = existingEntry?.AttemptstoSolve ?? 0;

            if (isCorrect && existingEntry?.Status != StatusTypes.Completed)
                attempts = taskAttempt;
            else if (!isCorrect)
                attempts++;

            var skillMatrix = new SkillMatrix
            {
                ActivityId = ActivityId,
                TaskId = currentTask.TaskId,
                Status = StatusTypes.Completed, // Always set to Completed
                AttemptstoSolve = attempts,
                TaskAttempt = taskAttempt,
                ModifyBy = UserId,
                ModifyDate = DateTime.UtcNow,
                HintsChecked = existingEntry?.HintsChecked ?? 0,
                TotalTime = _taskElapsedTimes[taskIndex]
            };

            await _skillMatrixRepository.SaveSkillMatrixAsync(skillMatrix, UserId);
            await UpdateActivityStatusAsync();

            return isCorrect ? "Correct!" : "Incorrect, but task marked as completed.";
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

            double completionRatio = (double)latestStatuses.Count(kv => kv.Value == StatusTypes.Completed) / Tasks.Count;

            string newStatus = completionRatio switch
            {
                1 => StatusTypes.Completed,
                >= 0.9 => "Mastered",
                >= 0.7 => "Proficient",
                >= 0.4 => "Developing",
                _ => "Needs Improvement"
            };

            string result = await ActivityRepository.CalculateActivityResult(ActivityId);

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

            string calculatedResult = await ActivityRepository.CalculateActivityResult(ActivityId);
            var skillMatrix = new SkillMatrix
            {
                ActivityId = ActivityId,
                TaskId = taskSubmission.Task.TaskId,
                HintsChecked = taskSubmission.HintsChecked,
                TotalTime = _taskElapsedTimes[CurrentTaskIndex],
                AttemptstoSolve = isCorrect ? taskAttempt : 0,
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

        public async Task SaveAndNextSectionAsync()
        {
            // Check if any hints were used in this section
            var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(ActivityId);
            bool hintsUsed = skillMatrixEntries.Any(e => e.HintsChecked > 0);

            if (await AreAllTasksCompleted(_currentSection.SectionId))
            {
                string message = hintsUsed
                    ? "Retry section to improve your score?"
                    : "Proceed to next section?";

                var result = MessageBox.Show(message, "Section Complete", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes && hintsUsed)
                {
                    // Reset task statuses for retry
                    foreach (var entry in skillMatrixEntries)
                        entry.Status = StatusTypes.Visited;
                    await _skillMatrixRepository.BatchUpdateSkillMatrixAsync(skillMatrixEntries);

                    // Reload current section
                    await LoadSectionAsync(_currentSection);
                }
                else
                {
                    await LoadNextSectionAsync();
                }
            }
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

        private async Task<bool> AreAllTasksCompleted(string sectionId)
        {
            var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(ActivityId);
            return Tasks.All(task =>
                skillMatrixEntries.Any(e =>
                    e.TaskId == task.TaskId &&
                    e.Status == StatusTypes.Completed));
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

        public async Task MoveToNextTask()
        {
            if (CurrentTaskIndex < Tasks.Count - 1)
            {
                CurrentTaskIndex++;
                await SaveCurrentStateAsync();
            }
            else
            {
                await HandleSectionCompletion();
            }
        }

        public async Task MoveToPreviousTask()
        {
            if (CurrentTaskIndex > 0)
            {
                CurrentTaskIndex--;
                await SaveCurrentStateAsync();
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

      

        public async Task SaveAndPreviousSectionAsync()
        {
            var previousSection = await GetPreviousSectionAsync();
            if (previousSection != null)
            {
                await SaveProgressAsync();
                await LoadSectionAsync(previousSection);
            }
            else
            {
                MessageBox.Show("This is the first section.");
            }
        }
    }
}