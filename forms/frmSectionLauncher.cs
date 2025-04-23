

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


        private async Task<int> GetLastTaskIndexAsync(string sectionId, string activityId, List<JobTask> tasks)
        {
            var skillMatrix = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(activityId);
            var lastTask = skillMatrix.OrderByDescending(sm => sm.ModifyDate).FirstOrDefault();
            return lastTask != null ? tasks.FindIndex(t => t.TaskId == lastTask.TaskId) : 0;
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
            // Show login form
            var loginForm = new frmUserLogin(_userRepository);
            loginForm.Show();
        }
    }
}