
using System;
using Microsoft.Data.SqlClient;

using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using JobSimulation.BLL;
using JobSimulation.DAL;
using JobSimulation.Models;
using Activity = JobSimulation.Models.Activity;

namespace JobSimulation.Forms
{
    public partial class frmSectionLauncher : Form
    {
        private List<Section> _sections;
        private int currentSectionIndex;

        private readonly SectionService _sectionService;
        private readonly string _simulationId;
        private readonly SectionRepository _sectionRepository;
        private readonly FileService _fileService;
        private readonly TaskRepository _taskRepository;
        private readonly SkillMatrixRepository _skillMatrixRepository;
        private readonly ActivityRepository _activityRepository;
        private string _userDirectoryPath;
        private string _tempFilePath;
        private readonly string _userId;
        private string _activityId;
        private int _attempt;
        private int currentTaskIndex = 0;
        private DataSet _progressDataSet;
        private DataTable _progressTable;
        private readonly UserRepository _userRepository;
        private Section _currentSection;

        public frmSectionLauncher(
            SectionRepository sectionRepository,
            FileService fileService,
            SectionService sectionService,
            TaskRepository taskRepository,
            SkillMatrixRepository skillMatrixRepository,
            ActivityRepository activityRepository,
            UserRepository userRepository,
            string simulationId,
            string userId,
            Section currentSection,
            string activityId)
        {
            InitializeComponent();
            _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
            _skillMatrixRepository = skillMatrixRepository ?? throw new ArgumentNullException(nameof(skillMatrixRepository));
            _activityRepository = activityRepository ?? throw new ArgumentNullException(nameof(activityRepository));
            _sectionService = sectionService ?? throw new ArgumentNullException(nameof(sectionService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

            _simulationId = simulationId;
            _userId = userId;
            _currentSection = currentSection;
            _activityId = activityId;
            _userDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                            "JobSimulationFiles", _userId);
            Directory.CreateDirectory(_userDirectoryPath);
            InitializeProgressDataSet();
            LoadSectionAsync();
        }

        private void InitializeProgressDataSet()
        {
            _progressDataSet = new DataSet("ProgressDataSet");
            _progressTable = new DataTable("SectionProgress");

            _progressTable.Columns.Add("SectionId", typeof(string));
            _progressTable.Columns.Add("UserId", typeof(string));
            _progressTable.Columns.Add("TaskIndex", typeof(int));
            _progressTable.Columns.Add("TimeElapsed", typeof(int));
            _progressTable.Columns.Add("IsCompleted", typeof(bool));
            _progressTable.Columns.Add("FilePath", typeof(string));

            _progressTable.PrimaryKey = new[]
            {
        _progressTable.Columns["SectionId"],
        _progressTable.Columns["UserId"]
    };

            _progressDataSet.Tables.Add(_progressTable);
        }

        private void UpdateSectionProgress(string sectionId, string userId, int taskIndex, int timeElapsed, string filePath, bool isCompleted = false)
        {
            DataRow row = _progressTable.Rows.Find(new object[] { sectionId, userId });

            if (row == null)
            {
                row = _progressTable.NewRow();
                row["SectionId"] = sectionId;
                row["UserId"] = userId;
                row["TaskIndex"] = taskIndex;
                row["TimeElapsed"] = timeElapsed;
                row["IsCompleted"] = isCompleted;
                row["FilePath"] = filePath;
                _progressTable.Rows.Add(row);
            }
            else
            {
                row["TaskIndex"] = taskIndex;
                row["TimeElapsed"] = timeElapsed;
                row["IsCompleted"] = isCompleted;
                row["FilePath"] = filePath;
            }
        }

        private (int taskIndex, int timeElapsed, string filePath) GetSectionProgress(string sectionId, string userId)
        {
            DataRow row = _progressTable.Rows.Find(new object[] { sectionId, userId });

            if (row != null)
            {
                int taskIndex = Convert.ToInt32(row["TaskIndex"]);
                int timeElapsed = Convert.ToInt32(row["TimeElapsed"]);
                string filePath = row["FilePath"].ToString();

                return (taskIndex, timeElapsed, filePath);
            }

            return (0, 0, string.Empty); // Default if no progress exists
        }

        private bool IsSectionCompleted(string sectionId, string userId)
        {
            DataRow row = _progressTable.Rows.Find(new object[] { sectionId, userId });

            return row != null && Convert.ToBoolean(row["IsCompleted"]);
        }

        private async void LoadSectionAsync()
        {
            if (_currentSection == null)
                await LoadAndLaunchInitialSection();
            else
                await LoadAndLaunchNextSection(_currentSection);
        }
        private async Task<(List<JobTask> tasks, int currentTaskIndex, int timeElapsed)> LoadTaskDetailsForSectionAsync(string sectionId, string activityId)
        {
            var tasks = await _sectionService.GetAllTasksForSectionAsync(sectionId, _userId);
            if (tasks == null || tasks.Count == 0)
            {
                return (new List<JobTask>(), 0, 0);
            }

            var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activityId);
            var incompleteTasks = tasks
                .Where(t => skillMatrixEntries.Any(sm => sm.TaskId == t.TaskId && sm.Status != StatusTypes.Completed))
                .ToList();

            int currentTaskIndex = await GetCurrentTaskIndexAsync(incompleteTasks, tasks);
            int timeElapsed = await _taskRepository.GetElapsedTimeForTaskAsync(activityId, tasks[currentTaskIndex].TaskId);

            return (tasks, currentTaskIndex, timeElapsed);
        }

        private async Task LoadAndLaunchInitialSection()
        {
            try
            {
                Debug.WriteLine("Starting LoadAndLaunchInitialSection...");
                var lastActivity = await _activityRepository.GetLastSessionForUserAsync(_userId);
                Section nextSection = null;
                int lastAttempt = 1;

                if (lastActivity != null && lastActivity.SimulationId == _simulationId)
                {
                    _activityId = lastActivity.ActivityId;
                    nextSection = await _sectionRepository.GetSectionByIdAsync(lastActivity.SectionId);

                    if (nextSection == null)
                    {
                        MessageBox.Show("Section not found for the last activity.");
                        return;
                    }

                    if (!string.IsNullOrEmpty(lastActivity.StudentFile))
                    {
                        _tempFilePath = await Task.Run(() => SaveFileToUserDirectory(
                            Convert.FromBase64String(lastActivity.StudentFile),
                            GetFileExtension(nextSection.SoftwareId)));
                        await Task.Run(() => OpenFileMaximized(_tempFilePath));
                    }
                }
                else
                {
                    var firstSection = await _sectionService.LoadNextSectionAsync(_userId, _simulationId);
                    if (firstSection == null)
                    {
                        MessageBox.Show("No sections found.");
                        return;
                    }

                    _activityId = await _activityRepository.GenerateNewActivityIdAsync(_userId, _simulationId, firstSection.SectionId);
                    nextSection = firstSection;

                    var newActivity = new Activity
                    {
                        ActivityId = _activityId,
                        UserId = _userId,
                        SimulationId = _simulationId,
                        SectionId = nextSection.SectionId,
                        Status = StatusTypes.NotStarted,
                        SectionAttempt = lastAttempt,
                        StudentFile = nextSection.StudentFile,
                        CreateDate = DateTime.UtcNow,
                        ModifyDate = DateTime.UtcNow,
                        CreateBy = _userId,
                        ModifyBy = _userId,
                        Result = string.Empty
                    };

                    await _activityRepository.SaveActivityAsync(newActivity);

                    _tempFilePath = await Task.Run(() => SaveFileToUserDirectory(
                        Convert.FromBase64String(nextSection.StudentFile),
                        GetFileExtension(nextSection.SoftwareId)));
                    await Task.Run(() => OpenFileMaximized(_tempFilePath));
                }

                if (nextSection == null)
                {
                    MessageBox.Show("All sections completed. Simulation over.");
                    LogoutAndClose();
                    return;
                }

                var (tasks, currentTaskIndex, timeElapsed) = await LoadTaskDetailsForSectionAsync(nextSection.SectionId, _activityId);

                if (tasks == null || tasks.Count == 0)
                {
                    LaunchSimulationForm(nextSection, new List<JobTask>(), 0, timeElapsed, lastAttempt);
                    MessageBox.Show("No tasks found for this section.");
                    return;
                }

                this.currentTaskIndex = currentTaskIndex;
                LaunchSimulationForm(nextSection, tasks, currentTaskIndex, timeElapsed, lastAttempt);
                this.Hide();
            }
            catch (Exception ex)
            {
                HandleError("Error in LoadAndLaunchInitialSection", ex);
            }
        }

        private async Task<int> GetCurrentTaskIndexAsync(List<JobTask> incompleteTasks, List<JobTask> allTasks)
        {
            var inProgressTasks = new List<JobTask>();
            foreach (var task in incompleteTasks)
            {
                var skillMatrixEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(_activityId, task.TaskId);
                if (skillMatrixEntry?.Status == StatusTypes.InProgress)
                {
                    inProgressTasks.Add(task);
                }
            }

            if (inProgressTasks.Count > 0)
                return allTasks.IndexOf(inProgressTasks.First());
            if (incompleteTasks.Count > 0)
                return allTasks.IndexOf(incompleteTasks.First());

            return 0;
        }

        private async Task LaunchSimulationForm(Section section, List<JobTask> tasks, int currentTaskIndex, int timeElapsed, int attempt)
        {
            // Fetch and determine if current section is the last
            _sections = await _sectionRepository.GetAllSectionsBySimulationIdAsync(_simulationId);
            bool isLastSection = _sections?.LastOrDefault()?.SectionId == section.SectionId;

            var simulationForm = new frmSimulationSoftware(
                tasks: tasks,
                filePath: _tempFilePath,
                sectionId: section.SectionId,
                simulationId: _simulationId,
                userId: _userId,
                sectionRepository: _sectionRepository,
                fileService: _fileService,
                skillMatrixRepository: _skillMatrixRepository,
                activityRepository: _activityRepository,
                taskRepository: _taskRepository,
                sectionService: _sectionService,
                userRepository: _userRepository,
                softwareId: section.SoftwareId,
                activityId: _activityId,
                progressDataSet: _progressDataSet,
                attempt: attempt,
                currentSection: section,
                isLastSection: isLastSection
            );

            simulationForm.SetCurrentTaskIndex(currentTaskIndex);
            simulationForm.SetElapsedTime(timeElapsed);
            simulationForm.NavigateToSection += async (sender, action) =>
            {
                this.Show();
                await HandleSectionNavigation(action);
            };
            simulationForm.FormClosed += (s, e) => this.Show();
            simulationForm.Show();
        }

        private void UpdateSimulationForm(Section section, List<JobTask> tasks, int timeElapsed, int attempt)
        {
            bool isLastSection = _sections.LastOrDefault()?.SectionId == section.SectionId;

            var simulationForm = new frmSimulationSoftware(
                tasks: tasks,
                filePath: _tempFilePath,
                sectionId: section.SectionId,
                simulationId: _simulationId,
                userId: _userId,
                sectionRepository: _sectionRepository,
                fileService: _fileService,
                skillMatrixRepository: _skillMatrixRepository,
                activityRepository: _activityRepository,
                taskRepository: _taskRepository,
                sectionService: _sectionService,
                userRepository: _userRepository,
                softwareId: section.SoftwareId,
                activityId: _activityId,
                progressDataSet: _progressDataSet,
                attempt: attempt,
                currentSection: section,
                isLastSection: isLastSection // <-- ✅ Add this
            );

            simulationForm.SetCurrentTaskIndex(currentTaskIndex); // Make sure currentTaskIndex is defined in scope
            simulationForm.SetElapsedTime(timeElapsed);
            simulationForm.SectionCompleted += (s, e) => LoadNextSection();
            simulationForm.FormClosed += (s, e) => this.Show();
            simulationForm.Show();
        }

        public async Task LoadNextSection()
        {
            try
            {
                // Close the current student file
                CloseFile(_tempFilePath);

                var currentActivity = await _activityRepository.GetActivityByIdAsync(_activityId);
                if (currentActivity == null)
                {
                    MessageBox.Show("Current activity not found.");
                    return;
                }

                var currentSection = await _sectionRepository.GetSectionByIdAsync(currentActivity.SectionId);
                var nextSection = await _sectionService.LoadNextSectionAsync(_userId, _simulationId, currentSection.SectionId);

                if (nextSection == null)
                {
                    MessageBox.Show("No more sections available.");
                    LogoutAndClose();
                    return;
                }

                // Open the new student file for the next section
                _tempFilePath = SaveFileToUserDirectory(Convert.FromBase64String(nextSection.StudentFile), GetFileExtension(nextSection.SoftwareId));
                OpenFileMaximized(_tempFilePath);

                await LoadAndLaunchSection(nextSection);
            }
            catch (Exception ex)
            {
                HandleError("Error loading next section", ex);
            }
        }


        private string SaveFileToUserDirectory(byte[] fileBytes, string fileExtension)
        {
            string filePath = Path.Combine(_userDirectoryPath, $"SimulationFile{fileExtension}");
            File.WriteAllBytes(filePath, fileBytes);
            return filePath;
        }

        private void OpenFileMaximized(string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo(filePath)
                {
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Maximized
                })?.WaitForInputIdle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file: {ex.Message}");
            }
        }

        private string GetFileExtension(string softwareId) => softwareId switch
        {
            "S1" => ".xlsx",
            "S2" => ".docx",
            "S3" => ".pptx",
            "S4" => ".gsheet",
            "S5" => ".gdoc",
            "S6" => ".gslides",
            _ => throw new ArgumentException("Unknown software ID")
        };

        private void LogoutAndClose()
        {
            MessageBox.Show("Simulation completed. Logging out...");
            this.Close();
        }

        private void HandleError(string context, Exception ex)
        {
            MessageBox.Show($"{context}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Debug.WriteLine($"{context}: {ex.Message}");
        }

        // Event handlers
        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();
            new frmSimulationLibrary(_userId, _userRepository).Show();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                CloseFile(_tempFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error closing file: {ex.Message}");
            }
            base.OnFormClosed(e);
        }

        private void CloseFile(string filePath)
        {
            try
            {
                var processName = GetProcessNameForFileType(filePath);
                if (!string.IsNullOrEmpty(processName))
                {
                    foreach (var process in Process.GetProcessesByName(processName))
                        process.Kill();
                }
                if (File.Exists(filePath)) File.Delete(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error closing file: {ex.Message}");
            }
        }

        private string GetProcessNameForFileType(string filePath) =>
            Path.GetExtension(filePath)?.ToLower() switch
            {
                ".xlsx" or ".xls" => "EXCEL",
                ".pdf" => "Acrobat",
                ".docx" or ".doc" => "WINWORD",
                ".pptx" or ".ppt" => "POWERPNT",
                ".txt" => "notepad",
                _ => null
            };


        private async Task LoadAndLaunchPreviousSection(Section currentSection)
        {
            try
            {
                var previousSection = await _sectionService.LoadPreviousSectionAsync(_userId, _simulationId, currentSection.SectionId);
                if (previousSection == null)
                {
                    MessageBox.Show("This is the first section.");
                    return;
                }

                await LoadAndLaunchSection(previousSection);
            }
            catch (Exception ex)
            {
                HandleError("Error loading previous section", ex);
            }
        }

        private async Task LoadAndLaunchNextSection(Section currentSection)
        {
            try
            {
                var nextSection = await _sectionService.LoadNextSectionAsync(_userId, _simulationId, currentSection.SectionId);
                if (nextSection == null)
                {
                    MessageBox.Show("No more sections available.");
                    LogoutAndClose();
                    return;
                }

                await LoadAndLaunchSection(nextSection);
            }
            catch (Exception ex)
            {
                HandleError("Error loading next section", ex);
            }
        }

        private async Task RetryCurrentSection(Section currentSection)
        {
            try
            {
                var lastActivity = await _activityRepository.GetLatestActivityAsync(_userId, _simulationId, currentSection.SectionId);
                if (lastActivity == null)
                {
                    MessageBox.Show("No previous activity found to retry.");
                    return;
                }

                if (lastActivity.SectionAttempt >= 3)
                {
                    MessageBox.Show("Maximum retry limit reached for this section.");
                    return;
                }

                int newAttempt = lastActivity.SectionAttempt + 1;

                // Get fresh blank file from Section table
                var freshStudentFile = currentSection.StudentFile;

                if (string.IsNullOrEmpty(freshStudentFile))
                {
                    MessageBox.Show("No base student file available for this section.");
                    return;
                }

                _activityId = await _activityRepository.GenerateNewActivityIdAsync(_userId, _simulationId, currentSection.SectionId);

                var newActivity = new Activity
                {
                    ActivityId = _activityId,
                    UserId = _userId,
                    SectionId = currentSection.SectionId,
                    SimulationId = _simulationId,
                    StudentFile = freshStudentFile,
                    SectionAttempt = newAttempt,
                    CreateDate = DateTime.UtcNow,
                    ModifyDate = DateTime.UtcNow,
                    CreateBy = _userId,
                    ModifyBy = _userId,
                    Status = StatusTypes.New,
                    Result = ""
                };

                await _activityRepository.SaveActivityAsync(newActivity);

                _tempFilePath = SaveFileToUserDirectory(
                    Convert.FromBase64String(freshStudentFile),
                    GetFileExtension(currentSection.SoftwareId));

                OpenFileMaximized(_tempFilePath);

                var tasks = await _sectionService.GetAllTasksForSectionAsync(currentSection.SectionId, _userId);
                if (tasks == null || tasks.Count == 0)
                {
                    MessageBox.Show("No tasks found for this section.");
                    return;
                }

                currentTaskIndex = 0;
                LaunchSimulationForm(currentSection, tasks, 0, 0, newAttempt); // Fixed line
                this.Hide();
            }
            catch (Exception ex)
            {
                HandleError("Error retrying section", ex);
            }
        }

        private async Task LoadAndLaunchSection(Section section)
        {
            // Create a new Activity for the section
            _activityId = await _activityRepository.GenerateNewActivityIdAsync(_userId, _simulationId, section.SectionId);
            var newActivity = new Activity
            {
                ActivityId = _activityId,
                UserId = _userId,
                SimulationId = _simulationId,
                SectionId = section.SectionId,
                Status = StatusTypes.New,
                SectionAttempt = 1,
                StudentFile = section.StudentFile,
                CreateDate = DateTime.UtcNow,
                ModifyDate = DateTime.UtcNow,
                CreateBy = _userId,
                ModifyBy = _userId,
                Result = string.Empty
            };

            await _activityRepository.SaveActivityAsync(newActivity);
            _tempFilePath = SaveFileToUserDirectory(
                Convert.FromBase64String(section.StudentFile),
                GetFileExtension(section.SoftwareId));
            OpenFileMaximized(_tempFilePath);

            // Load tasks for the section
            var tasks = await _sectionService.GetAllTasksForSectionAsync(section.SectionId, _userId);
            if (tasks == null || tasks.Count == 0)
            {
                MessageBox.Show("No tasks found for this section.");
                return;
            }

            // Determine tasks status
            var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(_activityId);
            var incompleteTasks = tasks.Where(t =>
                skillMatrixEntries.Any(sm => sm.TaskId == t.TaskId && sm.Status != StatusTypes.Completed)).ToList();

            // Get the current task index
            currentTaskIndex = await GetCurrentTaskIndexAsync(incompleteTasks, tasks);

            // Load current task elapsed time
            var task = tasks[currentTaskIndex];
            int timeElapsed = await _taskRepository.GetElapsedTimeForTaskAsync(_activityId, task.TaskId);

            // Launch the Simulation Form
            LaunchSimulationForm(section, tasks, currentTaskIndex, timeElapsed, 1); // Fixed line
            this.Hide();
        }


        private async Task LaunchSectionAsync(Section section, string activityId)
        {
            var studentFile = section.StudentFile;
            if (!string.IsNullOrEmpty(studentFile))
            {
                _tempFilePath = SaveFileToUserDirectory(Convert.FromBase64String(studentFile), GetFileExtension(section.SoftwareId));
                OpenFileMaximized(_tempFilePath);
            }

            var tasks = await _sectionService.GetAllTasksForSectionAsync(section.SectionId, _userId);
            if (tasks == null || tasks.Count == 0)
            {
                MessageBox.Show("No tasks found for this section.");
                return;
            }

            var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activityId);
            var incompleteTasks = tasks.Where(t => skillMatrixEntries.Any(sm => sm.TaskId == t.TaskId && sm.Status != StatusTypes.Completed)).ToList();

            currentTaskIndex = await GetCurrentTaskIndexAsync(incompleteTasks, tasks);
            var currentTask = tasks[currentTaskIndex];
            int timeElapsed = await _taskRepository.GetElapsedTimeForTaskAsync(activityId, currentTask.TaskId);

            LaunchSimulationForm(section, tasks, currentTaskIndex, timeElapsed, 1); // Fixed line
        }

        private async Task LoadAndLaunchNextSection()
        {
            currentSectionIndex++;

            if (currentSectionIndex >= _sections.Count)
            {
                MessageBox.Show("No more sections available.");
                return;
            }

            var section = _sections[currentSectionIndex];
            var activity = await _activityRepository.GetActivityBySimulationAndSection(_simulationId, section.SectionId, _userId);
            await LaunchSectionAsync(section, activity.ActivityId);
        }
        private async Task LoadPreviousSection()
        {
            currentSectionIndex--;

            if (currentSectionIndex < 0)
            {
                MessageBox.Show("No previous section available.");
                return;
            }

            var section = _sections[currentSectionIndex];
            var activity = await _activityRepository.GetActivityBySimulationAndSection(_simulationId, section.SectionId, _userId);
            await LaunchSectionAsync(section, activity.ActivityId);
        }


        public async Task HandleSectionNavigation(SectionNavigationAction action)
        {
            try
            {
                CloseFile(_tempFilePath);

                Section nextSection = null;
                int attempt = 1;

                switch (action)
                {
                    case SectionNavigationAction.Next:
                        nextSection = await _sectionService.LoadNextSectionAsync(_userId, _simulationId, _currentSection.SectionId);
                        if (nextSection == null)
                        {
                            MessageBox.Show("Congratulations! You've completed all sections.");
                            LogoutAndClose();
                            return;
                        }
                        break;

                    case SectionNavigationAction.Previous:
                        nextSection = await _sectionService.LoadPreviousSectionAsync(_userId, _simulationId, _currentSection.SectionId);
                        if (nextSection == null)
                        {
                            MessageBox.Show("This is the first section.");
                            return;
                        }
                        break;

                    case SectionNavigationAction.Retry:
                        nextSection = _currentSection;
                        var lastActivity = await _activityRepository.GetLatestActivityAsync(_userId, _simulationId, _currentSection.SectionId);
                         attempt = (lastActivity?.SectionAttempt ?? 0) + 1;

                        if (attempt > 3)
                        {
                            MessageBox.Show("Maximum retry attempts reached for this section.");
                            await HandleSectionNavigation(SectionNavigationAction.Next);
                            return;
                        }
                        break;
                }

                // Create new activity for the section
                _activityId = await _activityRepository.GenerateNewActivityIdAsync(_userId, _simulationId, nextSection.SectionId);

                var newActivity = new Activity
                {
                    ActivityId = _activityId,
                    UserId = _userId,
                    SimulationId = _simulationId,
                    SectionId = nextSection.SectionId,
                    Status = StatusTypes.New,
                    SectionAttempt = attempt,
                    StudentFile = nextSection.StudentFile,
                    CreateDate = DateTime.UtcNow,
                    ModifyDate = DateTime.UtcNow,
                    CreateBy = _userId,
                    ModifyBy = _userId,
                    Result = string.Empty
                };

                await _activityRepository.SaveActivityAsync(newActivity);

                // Update current section reference
                _currentSection = nextSection;

                // Prepare student file
                _tempFilePath = SaveFileToUserDirectory(
                    Convert.FromBase64String(nextSection.StudentFile),
                    GetFileExtension(nextSection.SoftwareId));

                OpenFileMaximized(_tempFilePath);

                // Load tasks and launch simulation form
                var tasks = await _sectionService.GetAllTasksForSectionAsync(nextSection.SectionId, _userId);
                if (tasks == null || tasks.Count == 0)
                {
                    MessageBox.Show("No tasks found for this section.");
                    return;
                }

                // Get current task progress (reset for retry)
                int taskIndex = 0;
                int elapsedTime = 0;

                if (action != SectionNavigationAction.Retry)
                {
                    var progress = GetSectionProgress(nextSection.SectionId, _userId);
                    taskIndex = progress.taskIndex;
                    elapsedTime = progress.timeElapsed;
                }

                LaunchSimulationForm(nextSection, tasks, taskIndex, elapsedTime, attempt);
                this.Hide();
            }
            catch (Exception ex)
            {
                HandleError("Error handling section navigation", ex);
            }
        }


    }
}
