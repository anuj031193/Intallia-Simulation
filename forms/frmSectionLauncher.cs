
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
using DocumentFormat.OpenXml.Office2021.DocumentTasks;

namespace JobSimulation.Forms
{
    public partial class frmSectionLauncher : Form
    {
        private frmSimulationSoftware _simulationForm;
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
            LoadAndLaunchInitialSection();
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
        private bool IsSectionInProgress(Activity activity)
        {
            return activity != null &&
                   activity.Status != StatusTypes.Completed &&
                   activity.SimulationId == _simulationId;
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

        private async System.Threading.Tasks.Task LoadAndLaunchInitialSection()
        {
            try
            {
                Debug.WriteLine("Starting LoadAndLaunchInitialSection...");
                var lastActivity = await _activityRepository.GetLastSessionForUserAsync(_userId);
                Section nextSection = null;
                int lastAttempt = 1;

                if (IsSectionInProgress(lastActivity))
                {
                    // Resume in-progress section
                    _activityId = lastActivity.ActivityId;
                    nextSection = await _sectionRepository.GetSectionByIdAsync(lastActivity.SectionId);

                    if (nextSection == null)
                    {
                        MessageBox.Show("Section not found for the last activity.");
                        return;
                    }

                    _tempFilePath = await System.Threading.Tasks.Task.Run(() => SaveFileToUserDirectory(
                        Convert.FromBase64String(lastActivity.StudentFile),
                        GetFileExtension(nextSection.SoftwareId)));
                    await System.Threading.Tasks.Task.Run(() => OpenFileMaximized(_tempFilePath));
                }
                else
                {
                    // Load next new section
                    var firstSection = await _sectionService.LoadNextSectionAsync(_userId, _simulationId);
                    if (firstSection == null)
                    {
                        MessageBox.Show("All sections completed. Simulation over.");
                        LogoutAndClose();
                        return;
                    }

                    nextSection = firstSection;
                    _activityId = await _activityRepository.GenerateNewActivityIdAsync(
                        _userId, _simulationId, nextSection.SectionId);

                    var newActivity = new Activity
                    {
                        ActivityId = _activityId,
                        UserId = _userId,
                        SimulationId = _simulationId,
                        SectionId = nextSection.SectionId,
                        Status = StatusTypes.NotStarted,
                        SectionAttempt = 1,
                        StudentFile = nextSection.StudentFile,
                        CreateDate = DateTime.UtcNow,
                        ModifyDate = DateTime.UtcNow,
                        CreateBy = _userId,
                        ModifyBy = _userId,
                        Result = string.Empty
                    };

                    // Ensure the activity is saved before continuing
                    await _activityRepository.SaveActivityAsync(newActivity);

                    // Ensure the file path is set and the file is saved before opening
                    _tempFilePath = await System.Threading.Tasks.Task.Run(() => SaveFileToUserDirectory(
                        Convert.FromBase64String(nextSection.StudentFile),
                        GetFileExtension(nextSection.SoftwareId)));

                    await System.Threading.Tasks.Task.Run(() => OpenFileMaximized(_tempFilePath));
                }

                // Fetch tasks for the next section, if available
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

        private async void LaunchSimulationForm(Section section, List<JobTask> tasks, int currentTaskIndex, int timeElapsed, int attempt)
        {
            bool isLastSection = await _sectionRepository.IsLastSectionAsync(section.SectionId, _simulationId);

            _currentSection = section;
            // Close existing form if it exists
            if (_simulationForm != null && !_simulationForm.IsDisposed)
            {
                _simulationForm.FormClosed -= SimulationForm_FormClosed;
                _simulationForm.Close();
            }

            _simulationForm = new frmSimulationSoftware(
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
          isLastSection: isLastSection,
        currentTaskIndex: currentTaskIndex);

            _simulationForm.NavigateToSection += async (sender, action) =>
            {
                this.Show();
                await HandleSectionNavigation(action);
            };

            _simulationForm.FormClosed += SimulationForm_FormClosed;
            _simulationForm.Show();
            this.Hide();
        }

        private void SimulationForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _simulationForm = null;
            this.Show();
        }
        private void UpdateSimulationForm(Section section, List<JobTask> tasks, int timeElapsed, int attempt)
        {
            _currentSection = section;
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
                isLastSection: isLastSection,
                 currentTaskIndex: currentTaskIndex);// <-- ✅ Add this
           

            simulationForm.SetCurrentTaskIndex(currentTaskIndex); // Make sure currentTaskIndex is defined in scope
            simulationForm.SetElapsedTime(timeElapsed);
            simulationForm.SectionCompleted += async (s, e) =>
            {
                this.Show();
                await HandleSectionNavigation(SectionNavigationAction.Next);
            }; simulationForm.FormClosed += (s, e) => this.Show();
            simulationForm.Show();
        }

        public async Task<bool> HandleSectionNavigation(SectionNavigationAction action)
        {
            try
            {
                if (_currentSection == null && action != SectionNavigationAction.Next)
                {
                    MessageBox.Show("Current section not initialized");
                    return false;
                }

                // Get section + activity ID + task index
                SectionNavigationResult navResult = await GetTargetSectionAsync(action);
                if (navResult?.Section == null)
                {
                    if (action == SectionNavigationAction.Previous)
                        MessageBox.Show("No previous section available");
                    else if (action == SectionNavigationAction.Next)
                    {
                        MessageBox.Show("No more sections available");
                        LogoutAndClose();
                    }
                    return false;
                }

                int attempt = await GetAttemptCountAsync(action, navResult.Section);
                if (attempt == -1) return false;

                // Use existing activityId if available (from GetTargetSectionAsync), otherwise create new one
                string activityId = navResult.ActivityId;
                if (string.IsNullOrEmpty(activityId))
                {
                    activityId = await GetOrCreateActivityAsync(_userId, _simulationId, navResult.Section.SectionId);
                    if (string.IsNullOrEmpty(activityId))
                    {
                        MessageBox.Show("Failed to create activity");
                        return false;
                    }
                }

                // Load the student file from that activity
                var activity = await _activityRepository.GetActivityByIdAsync(activityId);
                CloseFile(_tempFilePath); // Ensure previous file is closed

                _tempFilePath = SaveFileToUserDirectory(
                    Convert.FromBase64String(activity.StudentFile),
                    GetFileExtension(navResult.Section.SoftwareId));

                // Get task list and progress
                var (tasks, taskIndex, elapsedTime) = await LoadTaskDetailsForSectionAsync(navResult.Section.SectionId, activityId);
                if (tasks == null || tasks.Count == 0)
                {
                    MessageBox.Show("No tasks found for this section");
                    return false;
                }

                // Override taskIndex only if it was passed from navigation
                if (action == SectionNavigationAction.Previous && navResult.TaskIndex >= 0)
                {
                    taskIndex = navResult.TaskIndex;
                }

                currentTaskIndex = taskIndex;
                _activityId = activityId;
                _currentSection = navResult.Section;

                // Launch or update form
                if (_simulationForm == null || _simulationForm.IsDisposed)
                {
                    LaunchNewSimulationForm(navResult.Section, tasks, attempt);
                }
                else
                {
                    UpdateExistingSimulationForm(navResult.Section, tasks, attempt);
                }

                this.Hide();
                return true;
            }
            catch (Exception ex)
            {
                HandleError("Navigation error", ex);
                return false;
            }
        }

        private async void LaunchNewSimulationForm(Section section, List<JobTask> tasks, int attempt)
        {
            bool isLastSection = await _sectionRepository.IsLastSectionAsync(section.SectionId, _simulationId);
            _simulationForm = new frmSimulationSoftware(
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
                           isLastSection: isLastSection,
                            currentTaskIndex: currentTaskIndex);
                       

            _simulationForm.NavigateToSection += async (sender, action) =>
            {
                this.Show();
                await HandleSectionNavigation(action);
            };

            _simulationForm.FormClosed += (s, e) =>
            {
                _simulationForm = null;
                this.Show();
            };

            _simulationForm.Show();
        }

        private async void UpdateExistingSimulationForm(Section section, List<JobTask> tasks, int attempt)
        {
            bool isLastSection = await _sectionRepository.IsLastSectionAsync(section.SectionId, _simulationId);

            await _simulationForm.UpdateSectionDataAsync(
     tasks,
     _tempFilePath,
     section.SectionId,
     section.SoftwareId,
     _activityId,
     isLastSection,
     section,
     attempt,
     currentTaskIndex
 );
            _simulationForm.Show();
        }

        private async Task<bool> LaunchSectionFromActivity(Section section, string activityId, int attempt, SectionNavigationAction action)
        {
            // Close old file
            CloseFile(_tempFilePath);

            // Save new file
            _tempFilePath = SaveFileToUserDirectory(
                Convert.FromBase64String(section.StudentFile),
                GetFileExtension(section.SoftwareId));

            OpenFileMaximized(_tempFilePath);

            // Get tasks
            var tasks = await _taskRepository.GetTasksBySectionIdAsync(section.SectionId, _userId);
            if (tasks == null || tasks.Count == 0)
            {
                MessageBox.Show("No tasks found for this section.");
                return false;
            }

            // Determine task index based on navigation action
            int taskIndex = 0;
            if (action == SectionNavigationAction.Previous)
            {
                // Get last task progress for this section
                var lastActivity = await _activityRepository.GetLatestActivityAsync(_userId, _simulationId, section.SectionId);
                if (lastActivity != null)
                {
                    var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(lastActivity.ActivityId);
                    var lastTask = skillMatrixEntries.OrderByDescending(e => e.ModifyDate).FirstOrDefault();
                    if (lastTask != null)
                    {
                        taskIndex = tasks.FindIndex(t => t.TaskId == lastTask.TaskId);
                        if (taskIndex < 0) taskIndex = 0;
                    }
                }
            }

            // Get elapsed time for the task
            int elapsedTime = 0;
            if (taskIndex >= 0 && taskIndex < tasks.Count)
            {
                elapsedTime = await _taskRepository.GetElapsedTimeForTaskAsync(activityId, tasks[taskIndex].TaskId);
            }

            // Launch form with proper task index
            LaunchSimulationForm(section, tasks, taskIndex, elapsedTime, attempt);
            return true;
        }

        //private async Task<Section> GetTargetSectionAsync(SectionNavigationAction action)
        //{
        //    switch (action)
        //    {
        //        case SectionNavigationAction.Next:
        //            var next = await _sectionService.GetNextSectionAsync(_userId, _simulationId, _currentSection?.SectionId);
        //            if (next == null)
        //            {
        //                MessageBox.Show("All sections completed!");
        //                return null;
        //            }
        //            return next;

        //        case SectionNavigationAction.Previous:
        //            if (_currentSection == null) return null;
        //            var prev = await _sectionService.GetPreviousSectionAsync(_userId, _simulationId, _currentSection.SectionId);
        //            if (prev == null)
        //            {
        //                MessageBox.Show("This is the first section");
        //            }
        //            return prev;

        //        case SectionNavigationAction.Retry:
        //            return _currentSection;

        //        default:
        //            return null;
        //    }
        //}

        private async Task<SectionNavigationResult> GetTargetSectionAsync(SectionNavigationAction action)
        {
            switch (action)
            {
                case SectionNavigationAction.Next:
                    var nextSection = await _sectionService.GetNextSectionAsync(_userId, _simulationId, _currentSection?.SectionId);
                    return new SectionNavigationResult
                    {
                        Section = nextSection,
                        ActivityId = null,
                        TaskIndex = 0
                    };

                case SectionNavigationAction.Previous:
                    if (_currentSection == null) return null;

                    var prevSection = await _sectionService.GetPreviousSectionAsync(_userId, _simulationId, _currentSection.SectionId);
                    if (prevSection != null)
                    {
                        var lastActivity = await _activityRepository.GetLatestActivityAsync(_userId, _simulationId, prevSection.SectionId);
                        int taskIndex = 0;
                        if (lastActivity != null)
                        {
                            var skillMatrix = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(lastActivity.ActivityId);
                            var lastTask = skillMatrix.OrderByDescending(sm => sm.ModifyDate).FirstOrDefault();
                            if (lastTask != null)
                            {
                                var tasks = await _taskRepository.GetTasksBySectionIdAsync(prevSection.SectionId, _userId);
                                taskIndex = tasks.FindIndex(t => t.TaskId == lastTask.TaskId);
                            }

                            return new SectionNavigationResult
                            {
                                Section = prevSection,
                                ActivityId = lastActivity.ActivityId,
                                TaskIndex = taskIndex
                            };
                        }
                    }
                    return new SectionNavigationResult { Section = prevSection };

                case SectionNavigationAction.Retry:
                    return new SectionNavigationResult
                    {
                        Section = _currentSection,
                        ActivityId = _activityId,
                        TaskIndex = currentTaskIndex
                    };

                default:
                    return null;
            }
        }
        private async Task<int> GetAttemptCountAsync(SectionNavigationAction action, Section section)
        {
            if (action != SectionNavigationAction.Retry)
                return 1;

            var lastActivity = await _activityRepository.GetLatestActivityAsync(_userId, _simulationId, section.SectionId);
            int attempt = (lastActivity?.SectionAttempt ?? 0) + 1;

            if (attempt > 3)
            {
                MessageBox.Show("Maximum retry attempts reached.");
                await HandleSectionNavigation(SectionNavigationAction.Next);
                return -1;
            }

            return attempt;
        }

        private async Task<string> GetOrCreateActivityAsync(string userId, string simulationId, string sectionId)
        {
            var existing = await _activityRepository.GetLatestActivityAsync(userId, simulationId, sectionId);
            if (existing != null)
            {
                return existing.ActivityId;
            }

            // Generate a new ActivityId if no existing activity found
            string newActivityId = await _activityRepository.GenerateNewActivityIdAsync(userId, simulationId, sectionId);

            var section = await _sectionRepository.GetSectionByIdAsync(sectionId); // Fetch the section details

            var newActivity = new Activity
            {
                ActivityId = newActivityId,
                UserId = userId,
                SimulationId = simulationId,
                SectionId = sectionId,
                Status = StatusTypes.NotStarted,  // or another initial status
                SectionAttempt = 1,
                StudentFile = section.StudentFile,  // Set this as required
                CreateDate = DateTime.UtcNow,
                ModifyDate = DateTime.UtcNow,
                CreateBy = userId,
                ModifyBy = userId,
                Result = string.Empty  // or any other initial value
            };

            // Save the new activity to the database
            await _activityRepository.SaveActivityAsync(newActivity);

            return newActivityId;
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

        private string SaveFileToUserDirectory(byte[] fileBytes, string fileExtension)
        {
            string filePath = Path.Combine(_userDirectoryPath, $"SimulationFile{fileExtension}");
            File.WriteAllBytes(filePath, fileBytes);
            return filePath;
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
                //MessageBox.Show($"Error closing file: {ex.Message}");
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
        private void LogoutAndClose()
        {
            MessageBox.Show("Simulation completed. Logging out...");
            this.Close();
        }



    }
}
