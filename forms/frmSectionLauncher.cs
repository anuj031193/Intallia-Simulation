
//using System;
//using Microsoft.Data.SqlClient;

//using System.Collections.Generic;
//using System.Data;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Windows.Forms;
//using JobSimulation.BLL;
//using JobSimulation.DAL;
//using JobSimulation.Models;
//using Activity = JobSimulation.Models.Activity;

//namespace JobSimulation.Forms
//{
//    public partial class frmSectionLauncher : Form
//    {
//        private frmSimulationSoftware _simulationForm;
//        private List<Section> _sections;
//        private int currentSectionIndex;

//        private readonly SectionService _sectionService;
//        private readonly string _simulationId;
//        private readonly SectionRepository _sectionRepository;
//        private readonly FileService _fileService;
//        private readonly TaskRepository _taskRepository;
//        private readonly SkillMatrixRepository _skillMatrixRepository;
//        private readonly ActivityRepository _activityRepository;
//        private string _userDirectoryPath;
//        private string _tempFilePath;
//        private readonly string _userId;
//        private string _activityId;
//        private int _attempt;
//        private int currentTaskIndex = 0;
//        private DataSet _progressDataSet;
//        private DataTable _progressTable;
//        private readonly UserRepository _userRepository;
//        private Section _currentSection;

//        public frmSectionLauncher(
//            SectionRepository sectionRepository,
//            FileService fileService,
//            SectionService sectionService,
//            TaskRepository taskRepository,
//            SkillMatrixRepository skillMatrixRepository,
//            ActivityRepository activityRepository,
//            UserRepository userRepository,
//            string simulationId,
//            string userId,
//            Section currentSection,
//            string activityId)
//        {
//            InitializeComponent();
//            _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
//            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
//            _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
//            _skillMatrixRepository = skillMatrixRepository ?? throw new ArgumentNullException(nameof(skillMatrixRepository));
//            _activityRepository = activityRepository ?? throw new ArgumentNullException(nameof(activityRepository));
//            _sectionService = sectionService ?? throw new ArgumentNullException(nameof(sectionService));
//            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

//            _simulationId = simulationId;
//            _userId = userId;
//            _currentSection = currentSection;
//            _activityId = activityId;
//            _userDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
//                                            "JobSimulationFiles", _userId);
//            Directory.CreateDirectory(_userDirectoryPath);
//            InitializeProgressDataSet();
//            LoadAndLaunchInitialSection();
//        }

//        private void InitializeProgressDataSet()
//        {
//            _progressDataSet = new DataSet("ProgressDataSet");
//            _progressTable = new DataTable("SectionProgress");

//            _progressTable.Columns.Add("SectionId", typeof(string));
//            _progressTable.Columns.Add("UserId", typeof(string));
//            _progressTable.Columns.Add("TaskIndex", typeof(int));
//            _progressTable.Columns.Add("TimeElapsed", typeof(int));
//            _progressTable.Columns.Add("IsCompleted", typeof(bool));
//            _progressTable.Columns.Add("FilePath", typeof(string));

//            _progressTable.PrimaryKey = new[]
//            {
//        _progressTable.Columns["SectionId"],
//        _progressTable.Columns["UserId"]
//    };

//            _progressDataSet.Tables.Add(_progressTable);
//        }
//        private bool IsSectionInProgress(Activity activity)
//        {
//            return activity != null &&
//                   activity.Status != StatusTypes.Completed &&
//                   activity.SimulationId == _simulationId;
//        }

//        private void UpdateSectionProgress(string sectionId, string userId, int taskIndex, int timeElapsed, string filePath, bool isCompleted = false)
//        {
//            DataRow row = _progressTable.Rows.Find(new object[] { sectionId, userId });

//            if (row == null)
//            {
//                row = _progressTable.NewRow();
//                row["SectionId"] = sectionId;
//                row["UserId"] = userId;
//                row["TaskIndex"] = taskIndex;
//                row["TimeElapsed"] = timeElapsed;
//                row["IsCompleted"] = isCompleted;
//                row["FilePath"] = filePath;
//                _progressTable.Rows.Add(row);
//            }
//            else
//            {
//                row["TaskIndex"] = taskIndex;
//                row["TimeElapsed"] = timeElapsed;
//                row["IsCompleted"] = isCompleted;
//                row["FilePath"] = filePath;
//            }
//        }

//        private (int taskIndex, int timeElapsed, string filePath) GetSectionProgress(string sectionId, string userId)
//        {
//            DataRow row = _progressTable.Rows.Find(new object[] { sectionId, userId });

//            if (row != null)
//            {
//                int taskIndex = Convert.ToInt32(row["TaskIndex"]);
//                int timeElapsed = Convert.ToInt32(row["TimeElapsed"]);
//                string filePath = row["FilePath"].ToString();

//                return (taskIndex, timeElapsed, filePath);
//            }

//            return (0, 0, string.Empty); // Default if no progress exists
//        }

//        private bool IsSectionCompleted(string sectionId, string userId)
//        {
//            DataRow row = _progressTable.Rows.Find(new object[] { sectionId, userId });

//            return row != null && Convert.ToBoolean(row["IsCompleted"]);
//        }

//        private async Task<(List<JobTask> tasks, int currentTaskIndex, int timeElapsed)> LoadTaskDetailsForSectionAsync(string sectionId, string activityId)
//        {
//            var tasks = await _sectionService.GetAllTasksForSectionAsync(sectionId, _userId);
//            if (tasks == null || tasks.Count == 0)
//            {
//                return (new List<JobTask>(), 0, 0);
//            }

//            var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activityId);
//            var incompleteTasks = tasks
//                .Where(t => skillMatrixEntries.Any(sm => sm.TaskId == t.TaskId && sm.Status != StatusTypes.Completed))
//                .ToList();

//            int currentTaskIndex = await GetCurrentTaskIndexAsync(incompleteTasks, tasks);
//            int timeElapsed = await _taskRepository.GetElapsedTimeForTaskAsync(activityId, tasks[currentTaskIndex].TaskId);

//            return (tasks, currentTaskIndex, timeElapsed);
//        }

//        private async System.Threading.Tasks.Task LoadAndLaunchInitialSection()
//        {
//            try
//            {
//                Debug.WriteLine("Starting LoadAndLaunchInitialSection...");
//                var lastActivity = await _activityRepository.GetLastSessionForUserAsync(_userId);
//                Section nextSection = null;
//                int lastAttempt = 1;

//                var fileService = new FileService();
//                // Case 1: Resume in-progress section
//                // Case 1: Resume in-progress section
//                if (IsSectionInProgress(lastActivity))
//                {
//                    _activityId = lastActivity.ActivityId;
//                    nextSection = await _sectionRepository.GetSectionByIdAsync(lastActivity.SectionId);

//                    if (nextSection == null)
//                    {
//                        MessageBox.Show("Section not found for the last activity.");
//                        return;
//                    }
//                    _currentSection = nextSection; // ✅ Add this line

//                    // ✅ Assign to _tempFilePath after opening file
//                    await Task.Run(() =>
//                    {
//                        var fileBytes = fileService.ConvertBase64ToFile(lastActivity.StudentFile);
//                        _tempFilePath = fileService.OpenStudentFileFromBytes( // 🔥 Fix here
//                            fileBytes,
//                            nextSection.SectionId,
//                            nextSection.SoftwareId,
//                            _userId,
//                            lastActivity.ActivityId
//                        );
//                    });
//                }
//                else
//                {
//                    // Case 2: Load next section (new activity)
//                    nextSection = await _sectionService.LoadNextSectionAsync(_userId, _simulationId);
//                    if (nextSection == null)
//                    {
//                        MessageBox.Show("All sections completed. Simulation over.");
//                        LogoutAndClose();
//                        return;
//                    }
//                    _currentSection = nextSection;
//                    bool isRetry = false;
//                    _activityId = await GetOrCreateActivityAsync(_userId, _simulationId, nextSection.SectionId, isRetry);
//                    var activity = await _activityRepository.GetActivityByIdAsync(_activityId);

//                    if (activity == null)
//                    {
//                        MessageBox.Show("Failed to load activity for the section.");
//                        return;
//                    }

//                    // ✅ Decode from Base64 and open
//                    _tempFilePath = await Task.Run(() =>
//                    {
//                        var fileBytes = fileService.ConvertBase64ToFile(activity.StudentFile);
//                        return fileService.OpenStudentFileFromBytes(fileBytes, nextSection.SectionId, nextSection.SoftwareId, _userId, _activityId);
//                    });
//                }

//                // Load tasks for the section
//                var (tasks, currentTaskIndex, timeElapsed) = await LoadTaskDetailsForSectionAsync(nextSection.SectionId, _activityId);

//                if (tasks == null || tasks.Count == 0)
//                {
//                    LaunchSimulationForm(nextSection, new List<JobTask>(), 0, timeElapsed, lastAttempt, _tempFilePath); // Pass _tempFilePath
//                    MessageBox.Show("No tasks found for this section.");
//                    return;
//                }

//                this.currentTaskIndex = currentTaskIndex;
//                LaunchSimulationForm(nextSection, tasks, currentTaskIndex, timeElapsed, lastAttempt, _tempFilePath); // Pass _tempFilePath
//                this.Hide();
//            }
//            catch (Exception ex)
//            {
//                HandleError("Error in LoadAndLaunchInitialSection", ex);
//            }
//        }
//        private async Task<int> GetCurrentTaskIndexAsync(List<JobTask> incompleteTasks, List<JobTask> allTasks)
//        {
//            var inProgressTasks = new List<JobTask>();
//            foreach (var task in incompleteTasks)
//            {
//                var skillMatrixEntry = await _skillMatrixRepository.GetSkillMatrixByTaskId(_activityId, task.TaskId);
//                if (skillMatrixEntry?.Status == StatusTypes.InProgress)
//                {
//                    inProgressTasks.Add(task);
//                }
//            }

//            if (inProgressTasks.Count > 0)
//                return allTasks.IndexOf(inProgressTasks.First());
//            if (incompleteTasks.Count > 0)
//                return allTasks.IndexOf(incompleteTasks.First());

//            return 0;
//        }
//        private async Task InitializeAndShowSimulationForm(
//        Section section,
//        List<JobTask> tasks,
//        int attempt,
//        int taskIndex,
//        int elapsedTime,
//        string filePath,
//        bool isUpdate = false)

//        {
//            bool isLastSection = await _sectionRepository.IsLastSectionAsync(section.SectionId, _simulationId);

//            _simulationForm = new frmSimulationSoftware(
//          tasks,
//          _tempFilePath,
//          section.SectionId,
//          _simulationId,
//          _userId,
//          _sectionRepository,
//          _fileService,
//          _skillMatrixRepository,
//          _activityRepository,
//          _taskRepository,
//          _sectionService,
//          _userRepository,
//          section.SoftwareId,
//          _activityId,
//          _progressDataSet,
//          attempt,
//          section,
//          await _sectionRepository.IsLastSectionAsync(section.SectionId, _simulationId),
//          taskIndex);

//            _simulationForm.SetCurrentTaskIndex(taskIndex);
//            _simulationForm.SetElapsedTime(elapsedTime);


//            if (tasks.Count > currentTaskIndex)
//            {
//                _simulationForm.SetElapsedTime(
//                    await _taskRepository.GetElapsedTimeForTaskAsync(
//                        _activityId, tasks[currentTaskIndex].TaskId));
//            }

//            _simulationForm.NavigateToSection += async (sender, action) =>
//            {
//                this.Show();
//                await HandleSectionNavigation(action);
//            };

//            _simulationForm.SectionCompleted += async (sender, args) =>
//            {
//                this.Show();
//                await HandleSectionNavigation(SectionNavigationAction.Next);
//            };

//            _simulationForm.FormClosed += (sender, e) =>
//            {
//                _simulationForm = null;
//                this.Show();
//            };

//            if (!isUpdate)
//            {
//                _simulationForm.Show();
//                this.Hide();
//            }
//        }

//        private async void LaunchSimulationForm(Section section, List<JobTask> tasks, int taskIndex, int elapsedTime, int attempt, string filePath)
//        {
//            await InitializeAndShowSimulationForm(section, tasks, attempt, taskIndex, elapsedTime, filePath);
//        }


//        private void SimulationForm_FormClosed(object sender, FormClosedEventArgs e)
//        {
//            _simulationForm = null;
//            this.Show();
//        }
//        private void UpdateSimulationForm(Section section, List<JobTask> tasks, int timeElapsed, int attempt,string filepath)
//        {
//            _currentSection = section;
//            bool isLastSection = _sections.LastOrDefault()?.SectionId == section.SectionId;

//            var simulationForm = new frmSimulationSoftware(
//                tasks: tasks,
//                filePath: _tempFilePath,
//                sectionId: section.SectionId,
//                simulationId: _simulationId,
//                userId: _userId,
//                sectionRepository: _sectionRepository,
//                fileService: _fileService,
//                skillMatrixRepository: _skillMatrixRepository,
//                activityRepository: _activityRepository,
//                taskRepository: _taskRepository,
//                sectionService: _sectionService,
//                userRepository: _userRepository,
//                softwareId: section.SoftwareId,
//                activityId: _activityId,
//                progressDataSet: _progressDataSet,
//                attempt: attempt,
//                currentSection: section,
//                isLastSection: isLastSection,
//                 currentTaskIndex: currentTaskIndex);// <-- ✅ Add this


//            simulationForm.SetCurrentTaskIndex(currentTaskIndex); // Make sure currentTaskIndex is defined in scope
//            simulationForm.SetElapsedTime(timeElapsed);
//            simulationForm.SectionCompleted += async (s, e) =>
//            {
//                this.Show();
//                await HandleSectionNavigation(SectionNavigationAction.Next);
//            }; simulationForm.FormClosed += (s, e) => this.Show();
//            simulationForm.Show();
//        }

//        public async Task<bool> HandleSectionNavigation(SectionNavigationAction action)
//        {
//            try
//            {
//                if (_currentSection == null && action != SectionNavigationAction.Next)
//                {
//                    MessageBox.Show("Current section not initialized");
//                    return false;
//                }

//                // Get section + activity ID + task index
//                SectionNavigationResult navResult = await GetTargetSectionAsync(action);
//                if (navResult?.Section == null)
//                {
//                    if (action == SectionNavigationAction.Previous)
//                        MessageBox.Show("No previous section available");
//                    else if (action == SectionNavigationAction.Next)
//                    {
//                        MessageBox.Show("No more sections available");
//                        LogoutAndClose();
//                    }
//                    return false;
//                }

//                int attempt = await GetAttemptCountAsync(action, navResult.Section);
//                if (attempt == -1) return false;

//                // Use existing activityId if available, otherwise create new one
//                string activityId = navResult.ActivityId;
//                if (string.IsNullOrEmpty(activityId))
//                {
//                    bool isRetry = action == SectionNavigationAction.Retry;
//                    activityId = await GetOrCreateActivityAsync(_userId, _simulationId, navResult.Section.SectionId, isRetry);

//                    if (string.IsNullOrEmpty(activityId))
//                    {
//                        MessageBox.Show("Failed to create activity");
//                        return false;
//                    }
//                }

//                var activity = await _activityRepository.GetActivityByIdAsync(activityId);
//                if (activity == null)
//                {
//                    MessageBox.Show("Activity not found");
//                    return false;
//                }

//                // ✅ Use FileService to handle file decoding and saving
//                var fileService = new FileService();

//                // Close old file if needed
//                fileService.CloseFile(_tempFilePath);

//                // Convert base64 and save new file
//                _tempFilePath = fileService.SaveFileToUserDirectory(
//    fileService.ConvertBase64ToFile(activity.StudentFile),
//    fileService.GetFileExtension(navResult.Section.SoftwareId),
//    navResult.Section.SectionId,
//    _userId,
//    activity.ActivityId
//);
//                fileService.OpenFileMaximized(_tempFilePath);

//                // Load task progress
//                var (tasks, taskIndex, elapsedTime) = await LoadTaskDetailsForSectionAsync(navResult.Section.SectionId, activityId);
//                if (tasks == null || tasks.Count == 0)
//                {
//                    MessageBox.Show("No tasks found for this section");
//                    return false;
//                }

//                // Override task index if coming from Previous section
//                if (action == SectionNavigationAction.Previous && navResult.TaskIndex >= 0)
//                {
//                    taskIndex = navResult.TaskIndex;
//                }

//                currentTaskIndex = taskIndex;
//                _activityId = activityId;
//                _currentSection = navResult.Section;

//                // Launch or update form
//                // Launch or update form
//                if (_simulationForm == null || _simulationForm.IsDisposed)
//                {
//                    LaunchNewSimulationForm(navResult.Section, tasks, attempt, taskIndex, elapsedTime, _tempFilePath);
//                }
//                else
//                {
//                    UpdateExistingSimulationForm(navResult.Section, tasks, attempt, taskIndex, elapsedTime, _tempFilePath);
//                }


//                this.Hide();
//                return true;
//            }
//            catch (Exception ex)
//            {
//                HandleError("Navigation error", ex);
//                return false;
//            }
//        }

//        private async void LaunchNewSimulationForm(Section section, List<JobTask> tasks, int attempt, int taskIndex, int elapsedTime, string filePath)
//        {
//            await InitializeAndShowSimulationForm(section, tasks, attempt, taskIndex, elapsedTime, filePath);
//        }


//        private async void UpdateExistingSimulationForm(Section section, List<JobTask> tasks, int attempt, int taskIndex, int elapsedTime, string filePath)
//        {
//            await _simulationForm.UpdateSectionDataAsync(
//                tasks,
//                filePath,
//                section.SectionId,
//                section.SoftwareId,
//                _activityId,
//                await _sectionRepository.IsLastSectionAsync(section.SectionId, _simulationId),
//                section,
//                attempt,
//                taskIndex);

//            _simulationForm.SetElapsedTime(elapsedTime);
//            _simulationForm.Show();
//        }

//        private async Task<bool> LaunchSectionFromActivity(Section section, string activityId, int attempt, SectionNavigationAction action)
//        {
//            var fileService = new FileService();
//            fileService.CloseFile(_tempFilePath);

//            var activity = await _activityRepository.GetActivityByIdAsync(activityId);
//            if (activity == null)
//            {
//                MessageBox.Show("Activity not found.");
//                return false;
//            }

//            // Fetch student file from the activity (not section)
//            var studentFileBase64 = activity.StudentFile;
//            if (string.IsNullOrEmpty(studentFileBase64))
//            {
//                MessageBox.Show("No student file found in activity.");
//                return false;
//            }

//            // Save the activity file locally
//            _tempFilePath = fileService.SaveFileToUserDirectory(
//       fileService.ConvertBase64ToFile(studentFileBase64),
//       fileService.GetFileExtension(section.SoftwareId),
//       section.SectionId,
//       _userId,
//       activityId); // ✅ ADD THIS


//            fileService.OpenFileMaximized(_tempFilePath);


//            // Get tasks
//            var tasks = await _taskRepository.GetTasksBySectionIdAsync(section.SectionId, _userId);
//            if (tasks == null || tasks.Count == 0)
//            {
//                MessageBox.Show("No tasks found for this section.");
//                return false;
//            }

//            // Determine task index based on navigation action
//            int taskIndex = 0;
//            if (action == SectionNavigationAction.Previous)
//            {
//                var lastActivity = await _activityRepository.GetLatestActivityAsync(_userId, _simulationId, section.SectionId);
//                if (lastActivity != null)
//                {
//                    taskIndex = await GetLastTaskIndexAsync(section.SectionId, lastActivity.ActivityId, tasks);
//                }
//            }


//            // Get elapsed time for the task
//            int elapsedTime = 0;
//            if (taskIndex >= 0 && taskIndex < tasks.Count)
//            {
//                elapsedTime = await _taskRepository.GetElapsedTimeForTaskAsync(activityId, tasks[taskIndex].TaskId);
//            }

//            // Launch form with proper task index
//            LaunchSimulationForm(section, tasks, taskIndex, elapsedTime, attempt, _tempFilePath);
//            return true;
//        }

//        private async Task<SectionNavigationResult> GetTargetSectionAsync(SectionNavigationAction action)
//        {
//            switch (action)
//            {
//                case SectionNavigationAction.Next:
//                    var nextSection = await _sectionService.GetNextSectionAsync(_userId, _simulationId, _currentSection?.SectionId);
//                    return new SectionNavigationResult
//                    {
//                        Section = nextSection,
//                        ActivityId = null,
//                        TaskIndex = 0
//                    };



//                case SectionNavigationAction.Previous:
//                    if (_currentSection == null) return null;

//                    var prevSection = await _sectionService.GetPreviousSectionAsync(_userId, _simulationId, _currentSection.SectionId);
//                    if (prevSection != null)
//                    {
//                        var lastActivity = await _activityRepository.GetLatestActivityAsync(_userId, _simulationId, prevSection.SectionId);
//                        if (lastActivity != null)
//                        {
//                            var tasks = await _taskRepository.GetTasksBySectionIdAsync(prevSection.SectionId, _userId);
//                            // ✅ Get the actual latest task index from skill matrix
//                            int taskIndex = await GetLastTaskIndexAsync(prevSection.SectionId, lastActivity.ActivityId, tasks);

//                            return new SectionNavigationResult
//                            {
//                                Section = prevSection,
//                                ActivityId = lastActivity.ActivityId,
//                                TaskIndex = taskIndex
//                            };
//                        }
//                    }
//                    return new SectionNavigationResult { Section = prevSection };

//                case SectionNavigationAction.Retry:
//                    return new SectionNavigationResult
//                    {
//                        Section = _currentSection,
//                        ActivityId = _activityId,
//                        TaskIndex = currentTaskIndex
//                    };

//                default:
//                    return null;
//            }
//        }
//        private async Task<int> GetAttemptCountAsync(SectionNavigationAction action, Section section)
//        {
//            if (action != SectionNavigationAction.Retry)
//                return 1;

//            var lastActivity = await _activityRepository.GetLatestActivityAsync(_userId, _simulationId, section.SectionId);
//            int attempt = (lastActivity?.SectionAttempt ?? 0) + 1;

//            if (attempt > 3)
//            {
//                MessageBox.Show("Maximum retry attempts reached.");
//                await HandleSectionNavigation(SectionNavigationAction.Next);
//                return -1;
//            }

//            return attempt;
//        }
//        private async Task<int> GetLastTaskIndexAsync(string sectionId, string activityId, List<JobTask> tasks)
//        {
//            var skillMatrix = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activityId);
//            var lastTask = skillMatrix.OrderByDescending(sm => sm.ModifyDate).FirstOrDefault();
//            return lastTask != null ? tasks.FindIndex(t => t.TaskId == lastTask.TaskId) : 0;
//        }

//        private async Task<string> GetOrCreateActivityAsync(string userId, string simulationId, string sectionId, bool isRetry)
//        {
//            if (!isRetry)
//            {
//                var existing = await _activityRepository.GetLatestActivityAsync(userId, simulationId, sectionId);
//                if (existing != null)
//                {
//                    return existing.ActivityId;
//                }
//            }

//            // Generate a new ActivityId
//            var section = await _sectionRepository.GetSectionByIdAsync(sectionId);
//            var newActivityId = await _activityRepository.GenerateNewActivityIdAsync(userId, simulationId, sectionId);


//            var newActivity = new Activity
//            {
//                ActivityId = newActivityId,
//                UserId = userId,
//                SimulationId = simulationId,
//                SectionId = sectionId,
//                Status = StatusTypes.NotStarted,
//                SectionAttempt = 1,
//                StudentFile = section.StudentFile,
//                CreateDate = DateTime.UtcNow,
//                ModifyDate = DateTime.UtcNow,
//                CreateBy = userId,
//                ModifyBy = userId,
//                Result = string.Empty
//            };

//            await _activityRepository.SaveActivityAsync(newActivity);

//            return newActivityId;
//        }



//        private void HandleError(string context, Exception ex)
//        {
//            MessageBox.Show($"{context}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            Debug.WriteLine($"{context}: {ex.Message}");
//        }

//        // Event handlers
//        private void btnBack_Click(object sender, EventArgs e)
//        {
//            this.Close();
//            new frmSimulationLibrary(_userId, _userRepository).Show();
//        }

//        protected override void OnFormClosed(FormClosedEventArgs e)
//        {
//            try
//            {
//                // ✅ Close any open file handles before deletion
//                _fileService.CloseFile(_tempFilePath);
//                _fileService.DeleteFile(_tempFilePath);
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error closing file: {ex.Message}");
//            }
//            base.OnFormClosed(e);
//        }

//        private void LogoutAndClose()
//        {
//            MessageBox.Show("Simulation completed. Logging out...");
//            this.Close();
//        }



//    }
//}

using System;
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
using JobSimulation.Managers;
using Activity = JobSimulation.Models.Activity;

namespace JobSimulation.Forms
{
    public partial class frmSectionLauncher : Form
    {
        private frmSimulationSoftware _simulationForm;
        private int currentTaskIndex = 0;
        private readonly string _simulationId;
        private readonly string _userId;
        private string _activityId;
        private Section _currentSection;
        private readonly SectionRepository _sectionRepository;
        private readonly FileService _fileService;
        private readonly TaskRepository _taskRepository;
        private readonly SkillMatrixRepository _skillMatrixRepository;
        private readonly ActivityRepository _activityRepository;
        private readonly SectionService _sectionService;
        private readonly UserRepository _userRepository;
        private readonly TaskService _taskService;
        private string _tempFilePath;
        private DataSet _progressDataSet;

        public frmSectionLauncher(
            SectionRepository sectionRepository,
            FileService fileService,
            SectionService sectionService,
            TaskRepository taskRepository,
            SkillMatrixRepository skillMatrixRepository,
            ActivityRepository activityRepository,
            UserRepository userRepository,
            TaskService taskService,
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
            _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));

            _simulationId = simulationId;
            _userId = userId;
            _currentSection = currentSection;
            _activityId = activityId;
            InitializeProgressDataSet();
            _ = LoadAndLaunchInitialSection();
        }

        private void InitializeProgressDataSet()
        {
            _progressDataSet = new DataSet("ProgressDataSet");
            var progressTable = new DataTable("SectionProgress");

            progressTable.Columns.Add("SectionId", typeof(string));
            progressTable.Columns.Add("UserId", typeof(string));
            progressTable.Columns.Add("TaskIndex", typeof(int));
            progressTable.Columns.Add("TimeElapsed", typeof(int));
            progressTable.Columns.Add("IsCompleted", typeof(bool));
            progressTable.Columns.Add("FilePath", typeof(string));

            progressTable.PrimaryKey = new[]
            {
                progressTable.Columns["SectionId"],
                progressTable.Columns["UserId"]
            };

            _progressDataSet.Tables.Add(progressTable);
        }

        private async Task LoadAndLaunchInitialSection()
        {
            try
            {
                Debug.WriteLine("Starting LoadAndLaunchInitialSection...");
                var lastActivity = await _activityRepository.GetLastSessionForUserAsync(_userId);
                Section nextSection = null;
                int lastAttempt = 1;

                var fileService = new FileService();

                if (IsSectionInProgress(lastActivity))
                {
                    _activityId = lastActivity.ActivityId;
                    nextSection = await _sectionRepository.GetSectionByIdAsync(lastActivity.SectionId);

                    if (nextSection == null)
                    {
                        MessageBox.Show("Section not found for the last activity.");
                        return;
                    }
                    _currentSection = nextSection;

                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        var fileBytes = fileService.ConvertBase64ToFile(lastActivity.StudentFile);
                        _tempFilePath = fileService.OpenStudentFileFromBytes(
                            fileBytes,
                            nextSection.SectionId,
                            nextSection.SoftwareId,
                            _userId,
                            lastActivity.ActivityId
                        );
                    });
                }
                else
                {
                    nextSection = await _sectionService.LoadNextSectionAsync(_userId, _simulationId);
                    if (nextSection == null)
                    {
                        MessageBox.Show("All sections completed. Simulation over.");
                        LogoutAndClose();
                        return;
                    }
                    _currentSection = nextSection;
                    bool isRetry = false;
                    _activityId = await GetOrCreateActivityAsync(_userId, _simulationId, nextSection.SectionId, isRetry);
                    var activity = await _activityRepository.GetActivityByIdAsync(_activityId);

                    if (activity == null)
                    {
                        MessageBox.Show("Failed to load activity for the section.");
                        return;
                    }

                    _tempFilePath = await System.Threading.Tasks.Task.Run(() =>
                    {
                        var fileBytes = fileService.ConvertBase64ToFile(activity.StudentFile);
                        return fileService.OpenStudentFileFromBytes(fileBytes, nextSection.SectionId, nextSection.SoftwareId, _userId, _activityId);
                    });
                }

                var simulationManager = new SimulationManager(
                    new List<JobTask>(),
                    _tempFilePath,
                    _simulationId,
                    _userId,
                    _fileService,
                    _skillMatrixRepository,
                    _activityRepository,
                    _taskRepository,
                    _taskService,
                    _progressDataSet,
                    lastAttempt,
                    _currentSection,
                    _activityId,
                    0
                );

                var result = await simulationManager.LoadTaskDetailsForSectionAsync(nextSection.SectionId, _activityId);
                var tasks = result.tasks;
                var currentTaskIndex = result.currentTaskIndex;
                var timeElapsed = result.timeElapsed;

                if (tasks == null || tasks.Count == 0)
                {
                    LaunchSimulationForm(nextSection, new List<JobTask>(), 0, timeElapsed, lastAttempt, _tempFilePath);
                    MessageBox.Show("No tasks found for this section.");
                    return;
                }

                this.currentTaskIndex = currentTaskIndex;
                LaunchSimulationForm(nextSection, tasks, currentTaskIndex, timeElapsed, lastAttempt, _tempFilePath);
                this.Hide();
            }
            catch (Exception ex)
            {
                HandleError("Error in LoadAndLaunchInitialSection", ex);
            }
        }

        private bool IsSectionInProgress(Activity activity)
        {
            return activity != null &&
                   activity.Status != StatusTypes.Completed &&
                   activity.SimulationId == _simulationId;
        }

        private async Task<string> GetOrCreateActivityAsync(string userId, string simulationId, string sectionId, bool isRetry)
        {
            if (isRetry)
            {
                // Check if retry is allowed (e.g., max 3 attempts)
                bool canRetry = await _activityRepository.CanRetrySectionAsync(userId, simulationId, sectionId);
                if (!canRetry)
                {
                    MessageBox.Show("Retry limit reached for this section.");
                    return null;
                }

                // Always create a retry activity with incremented attempt
                return await CreateNewActivityAsync(userId, simulationId, sectionId, true);
            }
            else
            {
                // Try fetching the latest activity if not a retry
                var existing = await _activityRepository.GetLatestActivityAsync(userId, simulationId, sectionId);
                if (existing != null)
                {
                    return existing.ActivityId;
                }

                // If no existing found, create a new activity
                return await CreateNewActivityAsync(userId, simulationId, sectionId, false);
            }
        }
        private async Task<string> CreateNewActivityAsync(string userId, string simulationId, string sectionId, bool increaseAttempt)
        {
            var section = await _sectionRepository.GetSectionByIdAsync(sectionId);
            if (section == null)
            {
                MessageBox.Show("Section not found while creating new activity.");
                return null;
            }

            var newActivityId = await _activityRepository.GenerateNewActivityIdAsync(userId, simulationId, sectionId);

            // Determine current attempt count
            var currentAttemptCount = await _activityRepository.GetAttemptCountAsync(userId, simulationId, sectionId);
            int attempt = increaseAttempt ? currentAttemptCount + 1 : 1;

            var newActivity = new Activity
            {
                ActivityId = newActivityId,
                UserId = userId,
                SimulationId = simulationId,
                SectionId = sectionId,
                Status = StatusTypes.NotStarted,
                SectionAttempt = attempt,
                StudentFile = section.StudentFile,
                CreateDate = DateTime.UtcNow,
                ModifyDate = DateTime.UtcNow,
                CreateBy = userId,
                ModifyBy = userId,
                Result = string.Empty
            };

            await _activityRepository.SaveActivityAsync(newActivity);
            return newActivityId;
        }

        private void LaunchSimulationForm(Section section, List<JobTask> tasks, int taskIndex, int elapsedTime, int attempt, string filePath)
        {
            _simulationForm = new frmSimulationSoftware(
                tasks,
                filePath,
                section.SectionId,
                _simulationId,
                _userId,
                _sectionRepository,
                _fileService,
                _skillMatrixRepository,
                _activityRepository,
                _taskRepository,
                _taskService,
                _sectionService,
                _userRepository,
                section.SoftwareId,
                _activityId,
                _progressDataSet,
                attempt,
                section,
                false,
                taskIndex);   // Ensure currentTaskIndex is passed as the last parameter

            _simulationForm.SetCurrentTaskIndex(taskIndex);
            _simulationForm.SetElapsedTime(elapsedTime);
            _simulationForm.NavigateToSection += async (sender, action) =>
            {
                this.Show();
                await HandleSectionNavigation(action);
            };

            _simulationForm.SectionCompleted += async (sender, args) =>
            {
                this.Show();
                await HandleSectionNavigation(SectionNavigationAction.Next);
            };

            _simulationForm.FormClosed += (sender, e) =>
            {
                _simulationForm = null;
                this.Show();
            };

            _simulationForm.Show();
            this.Hide();
        }
        private async Task HandleSectionNavigation(SectionNavigationAction action)
        {
            try
            {
                // Prevent navigation if current section is null (except for first-time "Next")
                if (_currentSection == null && action != SectionNavigationAction.Next)
                {
                    MessageBox.Show("Current section not initialized");
                    return;
                }

                // Fetch the target section and related activity/task metadata
                var navResult = await GetTargetSectionAsync(action);
                if (navResult?.Section == null)
                {
                    if (action == SectionNavigationAction.Previous)
                        MessageBox.Show("No previous section available");
                    else if (action == SectionNavigationAction.Next)
                        LogoutAndClose(); // Last section reached
                    return;
                }

                // Get or create the activity ID
                string activityId;
                if (action == SectionNavigationAction.Retry)
                {
                    activityId = await GetOrCreateActivityAsync(_userId, _simulationId, navResult.Section.SectionId, true);

                    if (string.IsNullOrEmpty(activityId))
                    {
                        // Retry limit reached, move to next section
                        await HandleSectionNavigation(SectionNavigationAction.Next);
                        return;
                    }
                }
                else
                {
                    activityId = navResult.ActivityId;
                    if (string.IsNullOrEmpty(activityId))
                    {
                        activityId = await GetOrCreateActivityAsync(_userId, _simulationId, navResult.Section.SectionId, false);
                    }
                }
                var activity = await _activityRepository.GetActivityByIdAsync(activityId);
                if (activity == null)
                {
                    MessageBox.Show("Activity not found");
                    return;
                }

                // Get current attempt count
                int attempt = activity.SectionAttempt;


                // 🗂 File Handling
                var fileService = new FileService();
                fileService.CloseFile(_tempFilePath);

                _tempFilePath = fileService.SaveFileToUserDirectory(
                    fileService.ConvertBase64ToFile(activity.StudentFile),
                    fileService.GetFileExtension(navResult.Section.SoftwareId),
                    navResult.Section.SectionId,
                    _userId,
                    activity.ActivityId
                );

                fileService.OpenFileMaximized(_tempFilePath);

                // 🧠 Simulation setup
                var simulationManager = new SimulationManager(
                    new List<JobTask>(),
                    _tempFilePath,
                    _simulationId,
                    _userId,
                    _fileService,
                    _skillMatrixRepository,
                    _activityRepository,
                    _taskRepository,
                    _taskService,
                    _progressDataSet,
                    attempt,
                    navResult.Section,
                    activityId,
                    0
                );

                var result = await simulationManager.LoadTaskDetailsForSectionAsync(navResult.Section.SectionId, activityId);
                var tasks = result.tasks;
                var taskIndex = result.currentTaskIndex;
                var elapsedTime = result.timeElapsed;

                if (tasks == null || tasks.Count == 0)
                {
                    MessageBox.Show("No tasks found for this section");
                    return;
                }

                // If navigating back, resume from saved task index
                if (action == SectionNavigationAction.Previous && navResult.TaskIndex >= 0)
                {
                    taskIndex = navResult.TaskIndex;
                }

                // Save current session state
                currentTaskIndex = taskIndex;
                _activityId = activityId;
                _currentSection = navResult.Section;

                // 🚀 Launch or update UI
                if (_simulationForm == null || _simulationForm.IsDisposed)
                {
                    LaunchSimulationForm(navResult.Section, tasks, taskIndex, elapsedTime, attempt, _tempFilePath);
                }
                else
                {
                    UpdateSimulationForm(navResult.Section, tasks, elapsedTime, attempt, _tempFilePath);
                }

                this.Hide();
            }
            catch (Exception ex)
            {
                HandleError("Navigation error", ex);
            }
        }
        private async Task UpdateSimulationForm(Section section, List<JobTask> tasks, int elapsedTime, int attempt, string filePath)
        {
            _currentSection = section;

            await _simulationForm.UpdateSectionDataAsync(
                tasks,
                filePath,
                section.SectionId,
                section.SoftwareId,
                _activityId,
                await _sectionRepository.IsLastSectionAsync(section.SectionId, _simulationId),
                section,
                attempt,
                currentTaskIndex
            );

            _simulationForm.SetElapsedTime(elapsedTime);
            _simulationForm.Show();
        }

        // Example of btnBack_Click event handler
        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();
            new frmSimulationLibrary(_userId, _userRepository).Show();
        }

        // Example of LoadTaskDetailsForSectionAsync method
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
                        if (lastActivity != null)
                        {
                            var tasks = await _taskRepository.GetTasksBySectionIdAsync(prevSection.SectionId, _userId);
                            int taskIndex = await GetLastTaskIndexAsync(prevSection.SectionId, lastActivity.ActivityId, tasks);

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

        //private async Task<int> GetAttemptCountAsync(SectionNavigationAction action, Section section)
        //{
        //    if (action != SectionNavigationAction.Retry)
        //        return 1;

        //    var lastActivity = await _activityRepository.GetLatestActivityAsync(_userId, _simulationId, section.SectionId);
        //    int attempt = (lastActivity?.SectionAttempt ?? 0) + 1;

        //    if (attempt > 3)
        //    {
        //        MessageBox.Show("Maximum retry attempts reached.");
        //        await HandleSectionNavigation(SectionNavigationAction.Next);
        //        return -1;
        //    }

        //    return attempt;
        //}

        private async Task<int> GetAttemptCountAsync(Section section)
        {
            var lastActivity = await _activityRepository.GetLatestActivityAsync(_userId, _simulationId, section.SectionId);
            return (lastActivity?.SectionAttempt ?? 0) + 1;
        }

        private async Task<int> GetLastTaskIndexAsync(string sectionId, string activityId, List<JobTask> tasks)
        {
            var skillMatrix = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activityId);
            var lastTask = skillMatrix.OrderByDescending(sm => sm.ModifyDate).FirstOrDefault();
            return lastTask != null ? tasks.FindIndex(t => t.TaskId == lastTask.TaskId) : 0;
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

        private void HandleError(string context, Exception ex)
        {
            MessageBox.Show($"{context}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Debug.WriteLine($"{context}: {ex.Message}");
        }

        private void LogoutAndClose()
        {
            MessageBox.Show("Simulation completed. Logging out...");
            this.Close();
        }
    }
}