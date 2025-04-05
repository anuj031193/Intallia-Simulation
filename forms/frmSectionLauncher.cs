

//using System;
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
//            LoadSectionAsync();
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
//            _progressTable.PrimaryKey = new[] { _progressTable.Columns["SectionId"], _progressTable.Columns["UserId"] };
//            _progressDataSet.Tables.Add(_progressTable);
//        }

//        private async void LoadSectionAsync()
//        {
//            if (_currentSection == null)
//                await LoadAndLaunchInitialSection();
//            else
//                await LoadAndLaunchNextSection(_currentSection);
//        }

//        private async Task LoadAndLaunchInitialSection()
//        {
//            try
//            {
//                Debug.WriteLine("Starting LoadAndLaunchInitialSection...");
//                var lastActivity = await _activityRepository.GetLastSessionForUserAsync(_userId);
//                Section nextSection = null;
//                List<JobTask> tasks = null;
//                int lastAttempt = 1;

//                if (lastActivity != null && lastActivity.SimulationId == _simulationId)
//                {
//                    _activityId = lastActivity.ActivityId;
//                    currentTaskIndex = await _taskRepository.GetCurrentTaskIndexAsync(_activityId);
//                    nextSection = await _sectionRepository.GetSectionByIdAsync(lastActivity.SectionId);

//                    if (nextSection == null)
//                    {
//                        MessageBox.Show("Section not found for the last activity.");
//                        return;
//                    }

//                    if (!string.IsNullOrEmpty(lastActivity.StudentFile))
//                    {
//                        _tempFilePath = await Task.Run(() => SaveFileToUserDirectory(
//                            Convert.FromBase64String(lastActivity.StudentFile),
//                            GetFileExtension(nextSection.SoftwareId)));
//                        await Task.Run(() => OpenFileMaximized(_tempFilePath));
//                    }
//                }
//                else
//                {
//                    var firstSection = await _sectionService.LoadNextSectionAsync(_userId, _simulationId);
//                    if (firstSection == null)
//                    {
//                        MessageBox.Show("No sections found.");
//                        return;
//                    }

//                    _activityId = await _activityRepository.GenerateNewActivityIdAsync(_userId, _simulationId, firstSection.SectionId);
//                    nextSection = firstSection;
//                    lastAttempt = 1;

//                    var newActivity = new Activity
//                    {
//                        ActivityId = _activityId,
//                        UserId = _userId,
//                        SimulationId = _simulationId,
//                        SectionId = nextSection.SectionId,
//                        Status = StatusTypes.New,
//                        SectionAttempt = lastAttempt,
//                        StudentFile = nextSection.StudentFile,
//                        CreateDate = DateTime.UtcNow,
//                        ModifyDate = DateTime.UtcNow,
//                        CreateBy = _userId,
//                        ModifyBy = _userId,
//                        Result = string.Empty
//                    };

//                    await _activityRepository.SaveActivityAsync(newActivity);
//                    _tempFilePath = await Task.Run(() => SaveFileToUserDirectory(
//                        Convert.FromBase64String(nextSection.StudentFile),
//                        GetFileExtension(nextSection.SoftwareId)));
//                    await Task.Run(() => OpenFileMaximized(_tempFilePath));
//                }

//                if (nextSection == null)
//                {
//                    MessageBox.Show("All sections completed. Simulation over.");
//                    LogoutAndClose();
//                    return;
//                }

//                tasks = await _sectionService.GetAllTasksForSectionAsync(nextSection.SectionId, _userId);
//                if (tasks == null || tasks.Count == 0)
//                {
//                    LaunchSimulationForm(nextSection, new List<JobTask>(), 0, lastAttempt);
//                    MessageBox.Show("No tasks found for this section.");
//                    return;
//                }

//                var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(_activityId);
//                var incompleteTasks = tasks.Where(t =>
//                    skillMatrixEntries.Any(sm => sm.TaskId == t.TaskId && sm.Status != StatusTypes.Completed)).ToList();

//                currentTaskIndex = await GetCurrentTaskIndexAsync(incompleteTasks, tasks);
//                var task = tasks[currentTaskIndex];
//                int timeElapsed = await _taskRepository.GetElapsedTimeForTaskAsync(_activityId, task.TaskId);

//                LaunchSimulationForm(nextSection, tasks, timeElapsed, lastAttempt);
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

//        private void LaunchSimulationForm(Section section, List<JobTask> tasks, int timeElapsed, int attempt)
//        {
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
//                currentSection: section
//            );

//            simulationForm.SetCurrentTaskIndex(currentTaskIndex);
//            simulationForm.SetElapsedTime(timeElapsed);
//            simulationForm.SectionCompleted += (s, e) => LoadNextSection();
//            simulationForm.FormClosed += (s, e) => this.Show();
//            simulationForm.Show();
//        }

//        private async Task LoadAndLaunchNextSection(Section currentSection)
//        {
//            try
//            {
//                List<JobTask> tasks = null;
//                int lastAttempt = 1;

//                var newActivity = new Activity
//                {
//                    ActivityId = _activityId,
//                    UserId = _userId,
//                    SimulationId = _simulationId,
//                    SectionId = currentSection.SectionId,
//                    Status = StatusTypes.New,
//                    SectionAttempt = lastAttempt,
//                    StudentFile = currentSection.StudentFile,
//                    CreateDate = DateTime.UtcNow,
//                    ModifyDate = DateTime.UtcNow,
//                    CreateBy = _userId,
//                    ModifyBy = _userId,
//                    Result = string.Empty
//                };

//                await _activityRepository.SaveActivityAsync(newActivity);
//                _tempFilePath = SaveFileToUserDirectory(
//                    Convert.FromBase64String(currentSection.StudentFile),
//                    GetFileExtension(currentSection.SoftwareId));
//                OpenFileMaximized(_tempFilePath);

//                // Launch the simulation form first
//                LaunchSimulationForm(currentSection, new List<JobTask>(), 0, lastAttempt);

//                tasks = await _sectionService.GetAllTasksForSectionAsync(currentSection.SectionId, _userId);
//                Debug.WriteLine($"Loaded {tasks?.Count ?? 0} tasks for section {currentSection.SectionId}.");

//                if (tasks == null || tasks.Count == 0)
//                {
//                    MessageBox.Show("No tasks found for this section.");
//                    return;
//                }

//                var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(_activityId);
//                var incompleteTasks = tasks.Where(t =>
//                    skillMatrixEntries.Any(sm => sm.TaskId == t.TaskId && sm.Status != StatusTypes.Completed)).ToList();

//                Debug.WriteLine($"Found {incompleteTasks.Count} incomplete tasks.");

//                currentTaskIndex = await GetCurrentTaskIndexAsync(incompleteTasks, tasks);

//                var task = tasks[currentTaskIndex];
//                int timeElapsed = await _taskRepository.GetElapsedTimeForTaskAsync(_activityId, task.TaskId);

//                Debug.WriteLine("Updating SimulationForm...");
//                UpdateSimulationForm(currentSection, tasks, timeElapsed, lastAttempt);
//                Debug.WriteLine("SimulationForm updated.");
//                this.Hide();
//            }
//            catch (Exception ex)
//            {
//                HandleError("Error in LoadAndLaunchNextSection", ex);
//            }
//        }

//        private void UpdateSimulationForm(Section section, List<JobTask> tasks, int timeElapsed, int attempt)
//        {
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
//                currentSection: section
//            );

//            simulationForm.SetCurrentTaskIndex(currentTaskIndex);
//            simulationForm.SetElapsedTime(timeElapsed);
//            simulationForm.SectionCompleted += (s, e) => LoadNextSection();
//            simulationForm.FormClosed += (s, e) => this.Show();
//            simulationForm.Show();
//        }

//        public async Task LoadNextSection()
//        {
//            try
//            {
//                var currentSection = await _sectionRepository.GetSectionByIdAsync(_activityId);
//                if (currentSection == null)
//                {
//                    MessageBox.Show("Current section not found.");
//                    return;
//                }

//                var nextSection = await _sectionService.LoadNextSectionAsync(_userId, _simulationId, currentSection.SectionId);
//                if (nextSection == null)
//                {
//                    MessageBox.Show("No more sections available.");
//                    return;
//                }

//                var sectionLauncher = new frmSectionLauncher(
//                    _sectionRepository,
//                    _fileService,
//                    _sectionService,
//                    _taskRepository,
//                    _skillMatrixRepository,
//                    _activityRepository,
//                    _userRepository,
//                    _simulationId,
//                    _userId,
//                    nextSection,
//                    _activityId
//                );
//                sectionLauncher.Show();
//                this.Close();
//            }
//            catch (Exception ex)
//            {
//                HandleError("Error loading next section", ex);
//            }
//        }

//        public async Task LoadPreviousSection()
//        {
//            try
//            {
//                var currentSection = await _sectionRepository.GetSectionByIdAsync(_activityId);
//                if (currentSection == null)
//                {
//                    MessageBox.Show("Current section not found.");
//                    return;
//                }

//                var previousSection = await _sectionService.LoadPreviousSectionAsync(_userId, _simulationId, currentSection.SectionId);
//                if (previousSection == null)
//                {
//                    MessageBox.Show("This is the first section.");
//                    return;
//                }

//                var lastActivity = await _activityRepository.GetLastActivityForSectionAsync(_userId, _simulationId, previousSection.SectionId);
//                if (lastActivity != null)
//                {
//                    _activityId = lastActivity.ActivityId;
//                    currentTaskIndex = await _taskRepository.GetCurrentTaskIndexAsync(_activityId);

//                    if (!string.IsNullOrEmpty(lastActivity.StudentFile))
//                    {
//                        _tempFilePath = await Task.Run(() => SaveFileToUserDirectory(
//                            Convert.FromBase64String(lastActivity.StudentFile),
//                            GetFileExtension(previousSection.SoftwareId)));
//                        await Task.Run(() => OpenFileMaximized(_tempFilePath));
//                    }
//                }
//                else
//                {
//                    _activityId = await _activityRepository.GenerateNewActivityIdAsync(_userId, _simulationId, previousSection.SectionId);
//                    var newActivity = new Activity
//                    {
//                        ActivityId = _activityId,
//                        UserId = _userId,
//                        SimulationId = _simulationId,
//                        SectionId = previousSection.SectionId,
//                        Status = StatusTypes.New,
//                        SectionAttempt = 1,
//                        StudentFile = previousSection.StudentFile,
//                        CreateDate = DateTime.UtcNow,
//                        ModifyDate = DateTime.UtcNow,
//                        CreateBy = _userId,
//                        ModifyBy = _userId,
//                        Result = string.Empty
//                    };

//                    await _activityRepository.SaveActivityAsync(newActivity);
//                    _tempFilePath = await Task.Run(() => SaveFileToUserDirectory(
//                        Convert.FromBase64String(previousSection.StudentFile),
//                        GetFileExtension(previousSection.SoftwareId)));
//                    await Task.Run(() => OpenFileMaximized(_tempFilePath));
//                }

//                var tasks = await _sectionService.GetAllTasksForSectionAsync(previousSection.SectionId, _userId);
//                if (tasks == null || tasks.Count == 0)
//                {
//                    LaunchSimulationForm(previousSection, new List<JobTask>(), 0, 1);
//                    MessageBox.Show("No tasks found for this section.");
//                    return;
//                }

//                var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(_activityId);
//                var incompleteTasks = tasks.Where(t =>
//                    skillMatrixEntries.Any(sm => sm.TaskId == t.TaskId && sm.Status != StatusTypes.Completed)).ToList();

//                currentTaskIndex = await GetCurrentTaskIndexAsync(incompleteTasks, tasks);

//                var task = tasks[currentTaskIndex];
//                int timeElapsed = await _taskRepository.GetElapsedTimeForTaskAsync(_activityId, task.TaskId);

//                LaunchSimulationForm(previousSection, tasks, timeElapsed, 1);
//                this.Hide();
//            }
//            catch (Exception ex)
//            {
//                HandleError("Error loading previous section", ex);
//            }
//        }

//        // Helper methods
//        private string SaveFileToUserDirectory(byte[] fileBytes, string fileExtension)
//        {
//            string filePath = Path.Combine(_userDirectoryPath, $"SimulationFile{fileExtension}");
//            File.WriteAllBytes(filePath, fileBytes);
//            return filePath;
//        }

//        private void OpenFileMaximized(string filePath)
//        {
//            try
//            {
//                Process.Start(new ProcessStartInfo(filePath)
//                {
//                    UseShellExecute = true,
//                    WindowStyle = ProcessWindowStyle.Maximized
//                })?.WaitForInputIdle();
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error opening file: {ex.Message}");
//            }
//        }

//        private string GetFileExtension(string softwareId) => softwareId switch
//        {
//            "S1" => ".xlsx",
//            "S2" => ".docx",
//            "S3" => ".pptx",
//            "S4" => ".gsheet",
//            "S5" => ".gdoc",
//            "S6" => ".gslides",
//            _ => throw new ArgumentException("Unknown software ID")
//        };

//        private void LogoutAndClose()
//        {
//            MessageBox.Show("Simulation completed. Logging out...");
//            this.Close();
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
//                CloseFile(_tempFilePath);
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error closing file: {ex.Message}");
//            }
//            base.OnFormClosed(e);
//        }

//        private void CloseFile(string filePath)
//        {
//            try
//            {
//                var processName = GetProcessNameForFileType(filePath);
//                if (!string.IsNullOrEmpty(processName))
//                {
//                    foreach (var process in Process.GetProcessesByName(processName))
//                        process.Kill();
//                }
//                if (File.Exists(filePath)) File.Delete(filePath);
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error closing file: {ex.Message}");
//            }
//        }


//        private string GetProcessNameForFileType(string filePath) =>
//            Path.GetExtension(filePath)?.ToLower() switch
//            {
//                ".xlsx" or ".xls" => "EXCEL",
//                ".pdf" => "Acrobat",
//                ".docx" or ".doc" => "WINWORD",
//                ".pptx" or ".ppt" => "POWERPNT",
//                ".txt" => "notepad",
//                _ => null
//            };
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
using Activity = JobSimulation.Models.Activity;

namespace JobSimulation.Forms
{
    public partial class frmSectionLauncher : Form
    {
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
            _progressTable.PrimaryKey = new[] { _progressTable.Columns["SectionId"], _progressTable.Columns["UserId"] };
            _progressDataSet.Tables.Add(_progressTable);
        }

        private async void LoadSectionAsync()
        {
            if (_currentSection == null)
                await LoadAndLaunchInitialSection();
            else
                await LoadAndLaunchNextSection(_currentSection);
        }

        private async Task LoadAndLaunchInitialSection()
        {
            try
            {
                Debug.WriteLine("Starting LoadAndLaunchInitialSection...");
                var lastActivity = await _activityRepository.GetLastSessionForUserAsync(_userId);
                Section nextSection = null;
                List<JobTask> tasks = null;
                int lastAttempt = 1;

                if (lastActivity != null && lastActivity.SimulationId == _simulationId)
                {
                    _activityId = lastActivity.ActivityId;
                    currentTaskIndex = await _taskRepository.GetCurrentTaskIndexAsync(_activityId);
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
                    lastAttempt = 1;

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

                tasks = await _sectionService.GetAllTasksForSectionAsync(nextSection.SectionId, _userId);
                if (tasks == null || tasks.Count == 0)
                {
                    LaunchSimulationForm(nextSection, new List<JobTask>(), 0, lastAttempt);
                    MessageBox.Show("No tasks found for this section.");
                    return;
                }

                var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(_activityId);
                var incompleteTasks = tasks.Where(t =>
                    skillMatrixEntries.Any(sm => sm.TaskId == t.TaskId && sm.Status != StatusTypes.Completed)).ToList();

                currentTaskIndex = await GetCurrentTaskIndexAsync(incompleteTasks, tasks);
                var task = tasks[currentTaskIndex];
                int timeElapsed = await _taskRepository.GetElapsedTimeForTaskAsync(_activityId, task.TaskId);

                LaunchSimulationForm(nextSection, tasks, timeElapsed, lastAttempt);
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

        private void LaunchSimulationForm(Section section, List<JobTask> tasks, int timeElapsed, int attempt)
        {
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
                currentSection: section
            );

            simulationForm.SetCurrentTaskIndex(currentTaskIndex);
            simulationForm.SetElapsedTime(timeElapsed);
            simulationForm.SectionCompleted += (s, e) => LoadNextSection();
            simulationForm.FormClosed += (s, e) => this.Show();
            simulationForm.Show();
        }

        //private async Task LoadAndLaunchNextSection(Section currentSection)
        //{
        //    try
        //    {
        //        List<JobTask> tasks = null;
        //        int lastAttempt = 1;

        //        var newActivity = new Activity
        //        {
        //            ActivityId = _activityId,
        //            UserId = _userId,
        //            SimulationId = _simulationId,
        //            SectionId = currentSection.SectionId,
        //            Status = StatusTypes.New,
        //            SectionAttempt = lastAttempt,
        //            StudentFile = currentSection.StudentFile,
        //            CreateDate = DateTime.UtcNow,
        //            ModifyDate = DateTime.UtcNow,
        //            CreateBy = _userId,
        //            ModifyBy = _userId,
        //            Result = string.Empty
        //        };

        //        await _activityRepository.SaveActivityAsync(newActivity);
        //        _tempFilePath = SaveFileToUserDirectory(
        //            Convert.FromBase64String(currentSection.StudentFile),
        //            GetFileExtension(currentSection.SoftwareId));
        //        OpenFileMaximized(_tempFilePath);

        //        // Launch the simulation form first
        //        LaunchSimulationForm(currentSection, new List<JobTask>(), 0, lastAttempt);

        //        tasks = await _sectionService.GetAllTasksForSectionAsync(currentSection.SectionId, _userId);
        //        Debug.WriteLine($"Loaded {tasks?.Count ?? 0} tasks for section {currentSection.SectionId}.");

        //        if (tasks == null || tasks.Count == 0)
        //        {
        //            MessageBox.Show("No tasks found for this section.");
        //            return;
        //        }

        //        var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(_activityId);
        //        var incompleteTasks = tasks.Where(t =>
        //            skillMatrixEntries.Any(sm => sm.TaskId == t.TaskId && sm.Status != StatusTypes.Completed)).ToList();

        //        Debug.WriteLine($"Found {incompleteTasks.Count} incomplete tasks.");

        //        currentTaskIndex = await GetCurrentTaskIndexAsync(incompleteTasks, tasks);

        //        var task = tasks[currentTaskIndex];
        //        int timeElapsed = await _taskRepository.GetElapsedTimeForTaskAsync(_activityId, task.TaskId);

        //        Debug.WriteLine("Updating SimulationForm...");
        //        UpdateSimulationForm(currentSection, tasks, timeElapsed, lastAttempt);
        //        Debug.WriteLine("SimulationForm updated.");
        //        this.Hide();
        //    }
        //    catch (Exception ex)
        //    {
        //        HandleError("Error in LoadAndLaunchNextSection", ex);
        //    }
        //}

        private void UpdateSimulationForm(Section section, List<JobTask> tasks, int timeElapsed, int attempt)
        {
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
                currentSection: section
            );

            simulationForm.SetCurrentTaskIndex(currentTaskIndex);
            simulationForm.SetElapsedTime(timeElapsed);
            simulationForm.SectionCompleted += (s, e) => LoadNextSection();
            simulationForm.FormClosed += (s, e) => this.Show();
            simulationForm.Show();
        }

        public async Task LoadNextSection()
        {
            try
            {
                var currentSection = await _sectionRepository.GetSectionByIdAsync(_activityId);
                if (currentSection == null)
                {
                    MessageBox.Show("Current section not found.");
                    return;
                }

                var nextSection = await _sectionService.LoadNextSectionAsync(_userId, _simulationId, currentSection.SectionId);
                if (nextSection == null)
                {
                    MessageBox.Show("No more sections available.");
                    return;
                }

                await LoadAndLaunchNextSection(nextSection);
            }
            catch (Exception ex)
            {
                HandleError("Error loading next section", ex);
            }
        }

        public async Task LoadPreviousSection()
        {
            try
            {
                var currentSection = await _sectionRepository.GetSectionByIdAsync(_activityId);
                if (currentSection == null)
                {
                    MessageBox.Show("Current section not found.");
                    return;
                }

                var previousSection = await _sectionService.LoadPreviousSectionAsync(_userId, _simulationId, currentSection.SectionId);
                if (previousSection == null)
                {
                    MessageBox.Show("This is the first section.");
                    return;
                }

                var lastActivity = await _activityRepository.GetLastActivityForSectionAsync(_userId, _simulationId, previousSection.SectionId);
                if (lastActivity != null)
                {
                    _activityId = lastActivity.ActivityId;
                    currentTaskIndex = await _taskRepository.GetCurrentTaskIndexAsync(_activityId);

                    if (!string.IsNullOrEmpty(lastActivity.StudentFile))
                    {
                        _tempFilePath = await Task.Run(() => SaveFileToUserDirectory(
                            Convert.FromBase64String(lastActivity.StudentFile),
                            GetFileExtension(previousSection.SoftwareId)));
                        await Task.Run(() => OpenFileMaximized(_tempFilePath));
                    }
                }
                else
                {
                    _activityId = await _activityRepository.GenerateNewActivityIdAsync(_userId, _simulationId, previousSection.SectionId);
                    var newActivity = new Activity
                    {
                        ActivityId = _activityId,
                        UserId = _userId,
                        SimulationId = _simulationId,
                        SectionId = previousSection.SectionId,
                        Status = StatusTypes.New,
                        SectionAttempt = 1,
                        StudentFile = previousSection.StudentFile,
                        CreateDate = DateTime.UtcNow,
                        ModifyDate = DateTime.UtcNow,
                        CreateBy = _userId,
                        ModifyBy = _userId,
                        Result = string.Empty
                    };

                    await _activityRepository.SaveActivityAsync(newActivity);
                    _tempFilePath = await Task.Run(() => SaveFileToUserDirectory(
                        Convert.FromBase64String(previousSection.StudentFile),
                        GetFileExtension(previousSection.SoftwareId)));
                    await Task.Run(() => OpenFileMaximized(_tempFilePath));
                }

                var tasks = await _sectionService.GetAllTasksForSectionAsync(previousSection.SectionId, _userId);
                if (tasks == null || tasks.Count == 0)
                {
                    LaunchSimulationForm(previousSection, new List<JobTask>(), 0, 1);
                    MessageBox.Show("No tasks found for this section.");
                    return;
                }

                var skillMatrixEntries = await _skillMatrixRepository.GetSkillMatrixEntriesForActivityAsync(_activityId);
                var incompleteTasks = tasks.Where(t =>
                    skillMatrixEntries.Any(sm => sm.TaskId == t.TaskId && sm.Status != StatusTypes.Completed)).ToList();

                currentTaskIndex = await GetCurrentTaskIndexAsync(incompleteTasks, tasks);

                var task = tasks[currentTaskIndex];
                int timeElapsed = await _taskRepository.GetElapsedTimeForTaskAsync(_activityId, task.TaskId);

                LaunchSimulationForm(previousSection, tasks, timeElapsed, 1);
                this.Hide();
            }
            catch (Exception ex)
            {
                HandleError("Error loading previous section", ex);
            }
        }

        // Helper methods
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
                // Retrieve the previous section
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
                // Retrieve the next section
                var nextSection = await _sectionService.LoadNextSectionAsync(_userId, _simulationId, currentSection.SectionId);
                if (nextSection == null)
                {
                    MessageBox.Show("No more sections available.");
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
                // Reload and retry the section by simply invoking LoadAndLaunchSection
                await LoadAndLaunchSection(currentSection);
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
            LaunchSimulationForm(section, tasks, timeElapsed, 1);
            this.Hide();
        }

    }
}
