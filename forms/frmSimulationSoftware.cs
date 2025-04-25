
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
using Timer = System.Windows.Forms.Timer;
using Activity = JobSimulation.Models.Activity;

namespace JobSimulation.Forms
{
    public partial class frmSimulationSoftware : Form
    {
        public event EventHandler<SectionNavigationAction> NavigateToSection;
        private readonly SimulationManager _simulationManager;
        private Timer activityTimer;
        public event EventHandler SectionCompleted;
        private bool _allTasksCompleted = false;
        private bool _isLastSection = false;
        private int currentTaskIndex;
        private int timeElapsed;
        private List<JobTask> tasks;
        private Section _currentSection;
        private readonly UserRepository _userRepository;
        private readonly SectionRepository _sectionRepository;
        private readonly string _simulationId;
        private string _sectionId;
        private readonly string _userId;
        private string _masterJson;

        public frmSimulationSoftware(
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
            TaskService taskService,
            SectionService sectionService,
            UserRepository userRepository,
            string softwareId,
            string activityId,
            DataSet progressDataSet,
            int attempt,
            Section currentSection,
            bool isLastSection,
            int currentTaskIndex)
        {
            this.currentTaskIndex = currentTaskIndex;
            this._isLastSection = isLastSection;
            this.tasks = tasks;
            this._sectionId = sectionId;
            this._simulationId = simulationId;
            this._userId = userId;
            this._sectionRepository = sectionRepository;
            this._currentSection = currentSection;
            this._userRepository= userRepository;

            _simulationManager = new SimulationManager(
                tasks,
                filePath,
                simulationId,
                userId,
                fileService,
                skillMatrixRepository,
                activityRepository,
                taskRepository,
                taskService,
                progressDataSet,
                attempt,
                currentSection,
                activityId,
                currentTaskIndex
            );

            InitializeComponent();
            InitializeComponents();
            InitializeDynamicUI();
            LoadMasterData();
        }

        private void InitializeDynamicUI()
        {
            btnCompleteSimulation.Visible = false; // Initially hidden
            btnSaveandNextSession.Visible = true;
            btnStart.Enabled = true;
            btnStart.Visible = true;
        }
        private void DisableAllButtonsExceptStartAndClose()
        {
            foreach (Control control in this.Controls)
            {
                // Disable all buttons except Start and Close
                if (control is Button button)
                {
                    button.Enabled = false;
                }
            }

            // Explicitly enable only btnStart and btnClose
            btnStart.Enabled = true;
            btnClose.Enabled = true;
        }
        private async Task RetryCurrentSection()
        {
            await _simulationManager.SaveProgressAsync();
            NavigateToSection?.Invoke(this, SectionNavigationAction.Retry);
        }

        private async Task CheckTaskCompletion(bool isLastTaskInSection)
        {
            _allTasksCompleted = await _simulationManager.AreAllTasksCompleted();

            if (_allTasksCompleted)
            {
                var activity = await _simulationManager.GetActivityAsync();
                if (activity.Status != StatusTypes.Completed)
                {
                    activity.Status = StatusTypes.Completed;
                    await _simulationManager.UpdateActivityAsync(activity);
                }

                if (!_isLastSection)
                {
                    var result = MessageBox.Show(
                        "All tasks completed! Would you like to retry this section?",
                        "Section Complete",
                        MessageBoxButtons.YesNo);

                    if (result == DialogResult.Yes)
                    {
                        await RetryCurrentSection();
                    }
                    else
                    {
                        if (!isLastTaskInSection)
                        {
                            NavigateToSection?.Invoke(this, SectionNavigationAction.Next);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("All tasks completed! You have finished the last section.");
                    NavigateToSection?.Invoke(this, SectionNavigationAction.Complete);
                }
            }
            else
            {
                if (!isLastTaskInSection)
                {
                    NavigateToSection?.Invoke(this, SectionNavigationAction.Next);
                }
            }
        }

        private async void frmSimulationSoftware_Load(object sender, EventArgs e)
        {
            DisableAllButtonsExceptStartAndClose(); // Ensure only btnStart and btnClose are enabled

            // Explicitly check if the current section is the last section
            _isLastSection = await _sectionRepository.IsLastSectionAsync(_sectionId, _simulationId);

            Debug.WriteLine($"[DEBUG] frmSimulationSoftware_Load - _isLastSection: {_isLastSection}");

            // Update btnCompleteSimulation based on _isLastSection
            if (_isLastSection)
            {
                btnCompleteSimulation.Visible = true; // Make it visible
                btnCompleteSimulation.Enabled = false; // Disable it until btnStart is clicked
            }

            await UpdateSectionNavigationButtons(_sectionId, enableNavigation: false); // Skip enabling navigation buttons on load
        }
        private async Task UpdateSectionNavigationButtons(string sectionId, bool enableNavigation = true)
        {
            // Disable all navigation buttons initially
            btnSaveandNextSession.Enabled = false;
            btnSaveandPreviousSession.Enabled = false;

            if (enableNavigation)
            {
                // Update navigation buttons based on section availability
                if (!_isLastSection)
                {
                    btnSaveandNextSession.Enabled = await _sectionRepository.GetNextSectionAsync(_simulationId, sectionId) != null;
                }

                btnSaveandPreviousSession.Enabled = await _sectionRepository.GetPreviousSectionByOrderAsync(_simulationId, _currentSection.Order) != null;
            }
        }
        public void SetCurrentTaskIndex(int taskIndex)
        {
            if (taskIndex >= 0 && taskIndex < tasks.Count)
            {
                currentTaskIndex = taskIndex;
            }
        }

        public void SetElapsedTime(int elapsedTime)
        {
            timeElapsed = elapsedTime;
            lblTimer.Text = $"Time: {timeElapsed} sec";
        }

        private void UpdateNavigationButtons()
        {
            btnPrevious.Enabled = _simulationManager.CurrentTaskIndex > 0;
            btnNext.Enabled = _simulationManager.CurrentTaskIndex < _simulationManager.Tasks.Count - 1;
        }

        private void InitializeComponents()
        {
            activityTimer = new Timer { Interval = 1000 };
            activityTimer.Tick += Timer_Tick;

            InitializeUI();
            DisableControls();
        }

        private void InitializeUI()
        {
            // Ensure the initial UI state when the form is loaded
            lblTimer.Visible = false;
            btnPrevious.Enabled = false;
            btnNext.Enabled = false;

            DisableControls(); // Call DisableControls to set the initial state
        }

        private void DisableControls()
        {
            // Disable all buttons except btnStart and btnClose
            btnCheckAnswer.Enabled = false;
            btnPrevious.Enabled = false;
            btnNext.Enabled = false;
            btnHint.Enabled = false;
            btnSaveandNextSession.Enabled = false; // Ensure this is always disabled initially
            btnSaveandPreviousSession.Enabled = false; // Ensure this is always disabled initially
            btnCompleteSimulation.Enabled = false;
            btnSaveandExit.Enabled = false;

            btnStart.Enabled = true; // Ensure btnStart is enabled
            btnClose.Enabled = true; // Allow closing the application
        }

        private async void EnableControls()
        {
            // Enable basic buttons
            btnCheckAnswer.Enabled = true;
            btnPrevious.Enabled = true;
            btnNext.Enabled = true;
            btnHint.Enabled = true;

            // Enable navigation buttons based on section availability
            btnSaveandNextSession.Enabled = !_isLastSection && (await _sectionRepository.GetNextSectionAsync(_simulationId, _sectionId) != null);
            btnSaveandPreviousSession.Enabled = await _sectionRepository.GetPreviousSectionByOrderAsync(_simulationId, _currentSection.Order) != null;

            // Enable completion and save/exit buttons
            btnCompleteSimulation.Enabled = _isLastSection; // Enable only if it's the last section
            btnSaveandExit.Enabled = true;

            btnStart.Enabled = false; // Disable btnStart after it's clicked
        }

        private void StartTimers()
        {
            activityTimer.Start();
        }

        private void LoadMasterData()
        {
            Debug.WriteLine($"[DEBUG] LoadMasterData called. Current Section: {_currentSection.SectionId}");
            _simulationManager.LoadMasterData();
        }

        private async Task LoadTask(int taskIndex)
        {
            var task = await _simulationManager.LoadTaskAsync(taskIndex);
            if (task == null)
            {
                MessageBox.Show("Invalid task index.");
                return;
            }

            dynamic details = task.Details;
            label1.Text = details?.TaskDescription ?? task.Description;
            label1.Visible = true;
            label2.Text = $"Task {taskIndex + 1} of {_simulationManager.Tasks.Count}";

            lblTimer.Visible = true;
            lblTimer.Text = $"Time: {_simulationManager.CurrentTaskElapsedTime} sec";
        }

        private async void btnCheckAnswer_Click(object sender, EventArgs e)
        {
            try
            {
                var resultMessage = await _simulationManager.CheckAnswerAsync(_simulationManager.CurrentTaskIndex);
                MessageBox.Show(resultMessage, "Result", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Ensure the section does not change when clicking the Check Answer button
                // Update the status of the SkillMatrix table to Completed and increment TaskAttempt
                //await _simulationManager.UpdateSkillMatrixStatusAndAttempt(_simulationManager.CurrentTaskIndex);

                // Check if all tasks are completed and handle accordingly
                //await CheckTaskCompletion(isLastTaskInSection: false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error validating task: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            await _simulationManager.IncrementTimeElapsedAsync();
            lblTimer.Text = $"Time: {_simulationManager.CurrentTaskElapsedTime} sec";
        }

        private async void AutosaveTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                await _simulationManager.SaveProgressAsync();
                Console.WriteLine($"Auto-saved at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Auto-save error: {ex.Message}");
            }
        }

        private async void btnHint_Click(object sender, EventArgs e)
        {
            try
            {
                var hint = await _simulationManager.GetHintAsync(_simulationManager.CurrentTaskIndex);
                MessageBox.Show(hint, "Hint");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching hint: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnSaveAndExit_Click(object sender, EventArgs e)
        {
            await CloseFileAndExitAsync();
        }

        private async Task CloseFileAndExitAsync()
        {
            try
            {
                // 1. Save progress while the file is still available
                if (!string.IsNullOrEmpty(_simulationManager?.FilePath) &&
                    File.Exists(_simulationManager.FilePath))
                {
                    // Force save while the file still exists
                    await _simulationManager.SaveProgressAsync();
                }
                else
                {
                    // Skip save if file is already gone
                    Debug.WriteLine("File path is null or already deleted before SaveProgress.");
                }

                // 2. Close and delete the file safely
                if (!string.IsNullOrEmpty(_simulationManager?.FilePath))
                {
                    FileService fileService = new FileService();

                    // Close the file (kills Notepad, Excel, etc.)
                    fileService.CloseFile(_simulationManager.FilePath);

                    // Wait to ensure it's closed fully (very important)
                    await Task.Delay(300); // Optional: Increase to 500ms if needed

                    // Delete after close
                    if (File.Exists(_simulationManager.FilePath))
                        fileService.DeleteFile(_simulationManager.FilePath);
                }

                // 3. Now exit
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while saving or closing file: {ex.Message}", "Exit Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                if (_simulationManager.Tasks.Count == 0)
                {
                    MessageBox.Show("No tasks to load. Please check the section.");
                    return;
                }

                await LoadTask(_simulationManager.CurrentTaskIndex);
                SetElapsedTime(_simulationManager.CurrentTaskElapsedTime);
                label2.Text = $"Task {currentTaskIndex + 1} of {_simulationManager.Tasks.Count}";

                MessageBox.Show("Process started!");

                StartTimers();

                EnableControls(); // Enable all relevant buttons
                if (_isLastSection)
                {
                    btnCompleteSimulation.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }
        private async Task LoadCurrentTask()
        {
            await LoadTask(_simulationManager.CurrentTaskIndex);
        }

        private void UpdateUI()
        {
            InitializeUI();
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

        private async void btnNext_Click(object sender, EventArgs e)
        {
            try
            {
                activityTimer.Stop();
                await _simulationManager.SaveProgressAsync();

                if (_simulationManager.CurrentTaskIndex < _simulationManager.Tasks.Count - 1)
                {
                    _simulationManager.MoveToNextTask();
                    await LoadCurrentTask();
                }

                UpdateNavigationButtons();
                activityTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private async void btnPrevious_Click(object sender, EventArgs e)
        {
            try
            {
                activityTimer.Stop();
                await _simulationManager.SaveProgressAsync();

                _simulationManager.MoveToPreviousTask();
                await LoadCurrentTask();
                UpdateNavigationButtons();

                activityTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private async void btnSaveandNextSession_Click(object sender, EventArgs e)
        {
            activityTimer.Stop();
            await _simulationManager.SaveProgressAsync();

            if (_allTasksCompleted || await CheckForceMoveToNextSection())
            {
                NavigateToSection?.Invoke(this, SectionNavigationAction.Next);
            }
        }

        private async void btnSaveandPreviousSession_Click(object sender, EventArgs e)
        {
            try
            {
                activityTimer.Stop();
                await _simulationManager.SaveProgressAsync();
                NavigateToSection?.Invoke(this, SectionNavigationAction.Previous);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving progress: {ex.Message}");
            }
        }

        private async Task<bool> CheckForceMoveToNextSection()
        {
            var result = MessageBox.Show(
                "You haven't completed all tasks. Move to next section anyway?",
                "Confirm Navigation",
                MessageBoxButtons.YesNo);

            return result == DialogResult.Yes;
        }

        private async void btnCompleteSimulation_Click(object sender, EventArgs e)
        {
            try
            {
                // Check if all tasks are completed
                bool allTasksCompleted = await _simulationManager.AreAllTasksCompleted();

                if (!allTasksCompleted)
                {
                    var result = MessageBox.Show(
                        "Not all tasks are completed. Are you sure you want to submit?",
                        "Confirm Submission",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result != DialogResult.Yes)
                        return;

                    // Mark unvisited tasks as Incomplete
                    await _simulationManager.MarkUnvisitedTasksAsInCompleted();
                }

                // Fetch all activities (sections) for the simulation
                var allActivities = await _simulationManager.GetAllActivitiesForSimulationAsync();

                // Calculate and save results for all sections
                foreach (var activity in allActivities)
                {
                    var sectionResult = await _simulationManager.UpdateSectionResultAsync(activity.ActivityId); // Reuse UpdateSectionResultAsync
                    activity.Result = sectionResult;
                    activity.Status = StatusTypes.Completed;
                    activity.ModifyBy = _userId;
                    activity.ModifyDate = DateTime.Now;

                    // Save the updated activity for the section
                    await _simulationManager.SaveActivityAsync(activity);
                }

                // Calculate overall result across all sections
                var simulationResult = await _simulationManager.CalculateSimulationResultAsync(_simulationId);
                MessageBox.Show($"Simulation completed. Overall Result: {simulationResult}",
                    "Simulation Completed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Ensure both forms close and navigate to login
                CloseAllAndOpenLogin();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while completing the simulation: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void CloseAllAndOpenLogin()
        {
            // Close frmSectionLauncher if it is open
            //var sectionLauncherForm = Application.OpenForms.OfType<frmSectionLauncher>().FirstOrDefault();
            //if (sectionLauncherForm != null)
            //{
            //    sectionLauncherForm.Close();
            //}

            // Subscribe to FormClosed event to ensure navigation happens after forms are closed
            this.FormClosed += (s, e) =>
            {
                // Show the login form after closing all forms
                var loginForm = new frmUserLogin(_userRepository);
                loginForm.Show();
            };

            // Close this form
            this.Close();
        }
        public async Task UpdateSectionDataAsync(
       List<JobTask> newTasks,
       string newFilePath,
       string newSectionId,
       string newSoftwareId,
       string newActivityId,
       bool isLastSection,
       Section newCurrentSection,
       int newAttempt,
       int newTaskIndex)
        {
            await _simulationManager.UpdateSectionDataAsync(
                newTasks,
                newFilePath,
                newSectionId,
                newSoftwareId,
                newActivityId,
                newAttempt,
                newCurrentSection,
                newTaskIndex
            );

            // Update whether this is the last section
            _isLastSection = isLastSection;
            _currentSection = newCurrentSection;
            _sectionId = newSectionId;

            // Update navigation buttons
            await UpdateSectionNavigationButtons(newSectionId);

            // Reset UI elements
            await LoadCurrentTask();
            label2.Text = $"Task {_simulationManager.CurrentTaskIndex + 1} of {_simulationManager.Tasks.Count}";
            lblTimer.Text = "Time: 0 sec";

            label1.Text = string.Empty;
            label1.Visible = false;
            lblTimer.Visible = false;

            btnStart.Enabled = true;
            btnStart.Visible = true;

            btnSaveandNextSession.Visible = true;

            // Explicitly set visibility and state of btnCompleteSimulation
            btnCompleteSimulation.Visible = _isLastSection;
            //btnCompleteSimulation.Enabled = _isLastSection;

            DisableAllButtonsExceptStartAndClose();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CloseFileAndExitAsync();
            base.OnFormClosing(e);
        }

        private void frmSimulationSoftware_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_simulationManager?.FilePath))
            {
                CloseFile(_simulationManager.FilePath);
            }

            activityTimer?.Stop();
            activityTimer?.Dispose();
        }
    }
}



//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Windows.Forms;
//using JobSimulation.BLL;
//using JobSimulation.DAL;
//using JobSimulation.Models;
//using JobSimulation.Managers;
//using System.Data;

//namespace JobSimulation.Forms
//{
//    public partial class frmSimulationSoftware : Form
//    {
//        public event EventHandler<SectionNavigationAction> NavigateToSection;
//        public event EventHandler SectionCompleted;

//        private readonly SimulationManager _simulationManager;
//        private readonly SectionRepository _sectionRepository;
//        private readonly string _simulationId;
//        private readonly string _userId;

//        private System.Windows.Forms.Timer _activityTimer;
//        private bool _isLastSection;
//        private int _currentTaskIndex;
//        private Section _currentSection;
//        private string _sectionId;
//        private bool _allTasksCompleted;
//        private int timeElapsed = 0; // Tracks the elapsed time for the current task

//        public frmSimulationSoftware(
//            List<JobTask> tasks,
//            string filePath,
//            string sectionId,
//            string simulationId,
//            string userId,
//            SectionRepository sectionRepository,
//            FileService fileService,
//            SkillMatrixRepository skillMatrixRepository,
//            ActivityRepository activityRepository,
//            TaskRepository taskRepository,
//            TaskService taskService,
//            SectionService sectionService,
//            UserRepository userRepository,
//            string softwareId,
//            string activityId,
//            DataSet progressDataSet,
//            int attempt,
//            Section currentSection,
//            bool isLastSection,
//            int currentTaskIndex)
//        {
//            InitializeComponent();
//            InitializeDynamicUI();

//            _simulationManager = new SimulationManager(
//                tasks,
//                filePath,
//                simulationId,
//                userId,
//                fileService,
//                skillMatrixRepository,
//                activityRepository,
//                taskRepository,
//                taskService,
//                progressDataSet,
//                attempt,
//                currentSection,
//                activityId,
//                currentTaskIndex
//            );

//            _sectionRepository = sectionRepository;
//            _simulationId = simulationId;
//            _userId = userId;
//            _sectionId = sectionId;
//            _isLastSection = isLastSection;
//            _currentTaskIndex = currentTaskIndex;
//            _currentSection = currentSection;

//            InitializeComponents();
//        }
//        private void InitializeUI()
//        {
//            lblTimer.Visible = false;
//            btnPrevious.Enabled = false;
//            btnNext.Enabled = false;

//            DisableControls(); // Ensure the initial state of buttons
//        }
//        private void DisableControls()
//        {
//            btnCheckAnswer.Enabled = false;
//            btnPrevious.Enabled = false;
//            btnNext.Enabled = false;
//            btnHint.Enabled = false;
//            btnSaveandNextSession.Enabled = false;
//            btnSaveandPreviousSession.Enabled = false;
//            btnCompleteSimulation.Enabled = false;
//            btnSaveandExit.Enabled = false;

//            btnStart.Enabled = true;
//            btnClose.Enabled = true;
//        }
//        private void InitializeDynamicUI()
//        {
//            btnCompleteSimulation.Visible = false;
//            btnSaveandNextSession.Visible = true;
//            btnStart.Enabled = true;
//        }
//        private async void Timer_Tick(object sender, EventArgs e)
//        {
//            try
//            {
//                timeElapsed++;
//                lblTimer.Text = $"Time: {timeElapsed} sec";

//                // Save elapsed time every 10 seconds
//                if (timeElapsed % 10 == 0)
//                {
//                    await _simulationManager.SaveElapsedTimeForTaskAsync(_simulationId, _currentTaskIndex, timeElapsed);
//                }
//            }
//            catch (Exception ex)
//            {
//                HandleError("Error during timer tick", ex);
//            }
//        }
//        public void SetElapsedTime(int elapsedTime)
//        {
//            timeElapsed = elapsedTime;
//            lblTimer.Text = $"Time: {timeElapsed} sec";
//        }
//        private void InitializeComponents()
//        {
//            _activityTimer = new System.Windows.Forms.Timer { Interval = 1000 };
//            _activityTimer.Tick += Timer_Tick;

//            InitializeUI();
//            DisableControls();
//        }
//        private void StartTimers()
//        {
//            _activityTimer.Start();
//        }

//        private void StopTimers()
//        {
//            _activityTimer.Stop();
//        }
//        private void DisableAllButtonsExceptStartAndClose()
//        {
//            foreach (var button in Controls.OfType<Button>())
//            {
//                button.Enabled = false;
//            }
//            btnStart.Enabled = true;
//            btnClose.Enabled = true;
//        }

//        private async void frmSimulationSoftware_Load(object sender, EventArgs e)
//        {
//            DisableAllButtonsExceptStartAndClose();
//            _isLastSection = await _sectionRepository.IsLastSectionAsync(_sectionId, _simulationId);
//            UpdateCompleteSimulationButton();
//            await UpdateSectionNavigationButtons(_sectionId, enableNavigation: false);
//        }

//        private async Task UpdateSectionNavigationButtons(string sectionId, bool enableNavigation)
//        {
//            btnSaveandNextSession.Enabled = enableNavigation && !_isLastSection &&
//                                             await _sectionRepository.GetNextSectionAsync(_simulationId, sectionId) != null;

//            btnSaveandPreviousSession.Enabled = enableNavigation &&
//                                                 await _sectionRepository.GetPreviousSectionByOrderAsync(_simulationId, _currentSection.Order) != null;
//        }

//        private void UpdateCompleteSimulationButton()
//        {
//            btnCompleteSimulation.Visible = _isLastSection;
//            btnCompleteSimulation.Enabled = false;
//        }

//        private async void btnHint_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                var hint = await _simulationManager.GetHintAsync(_currentTaskIndex);
//                MessageBox.Show(hint, "Hint");
//            }
//            catch (Exception ex)
//            {
//                HandleError("Error fetching hint", ex);
//            }
//        }

//        private async void btnSaveandNextSession_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                _activityTimer.Stop(); // Stop the timer
//                await _simulationManager.SaveProgressAsync(); // Save progress

//                if (_allTasksCompleted || await CheckForceMoveToNextSection())
//                {
//                    UnloadTask(); // Clear the current task
//                    NavigateToSection?.Invoke(this, SectionNavigationAction.Next);
//                }
//            }
//            catch (Exception ex)
//            {
//                HandleError("Error saving and navigating to the next section", ex);
//            }
//        }

//        private async void btnSaveandPreviousSession_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                _activityTimer.Stop(); // Stop the timer
//                await _simulationManager.SaveProgressAsync(); // Save progress

//                UnloadTask(); // Clear the current task
//                NavigateToSection?.Invoke(this, SectionNavigationAction.Previous);
//            }
//            catch (Exception ex)
//            {
//                HandleError("Error saving and navigating to the previous section", ex);
//            }
//        }
//        private void SetElapsedTime(int elapsedTime)
//        {
//            timeElapsed = elapsedTime;
//            lblTimer.Text = $"Time: {timeElapsed} sec";
//        }
//        public async Task UpdateSectionDataAsync(
//            List<JobTask> newTasks,
//            string newFilePath,
//            string newSectionId,
//            string newSoftwareId,
//            string newActivityId,
//            bool isLastSection,
//            Section newCurrentSection,
//            int newAttempt,
//            int newTaskIndex)
//        {
//            await _simulationManager.UpdateSectionDataAsync(
//                newTasks,
//                newFilePath,
//                newSectionId,
//                newSoftwareId,
//                newActivityId,
//                newAttempt,
//                newCurrentSection,
//                newTaskIndex
//            );

//            _isLastSection = isLastSection;
//            _currentSection = newCurrentSection;
//            _sectionId = newSectionId;
//            _currentTaskIndex = newTaskIndex;

//            UpdateCompleteSimulationButton();
//            await LoadTask(_currentTaskIndex);
//        }

//        private async void btnStart_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                if (!_simulationManager.Tasks.Any())
//                {
//                    MessageBox.Show("No tasks to load. Please check the section.");
//                    return;
//                }

//                // Load the current task and fetch the stored elapsed time from the database
//                await LoadTask(_currentTaskIndex);
//                int elapsedTime = await _simulationManager.GetElapsedTimeForTaskAsync(_simulationId, _currentTaskIndex);
//                SetElapsedTime(elapsedTime); // Display the elapsed time in the UI

//                StartTimers(); // Start the timer from the stored elapsed time
//                EnableControls(); // Enable buttons for navigation and actions

//                btnStart.Enabled = false; // Disable the Start button to avoid multiple clicks
//                btnCompleteSimulation.Enabled = _isLastSection; // Enable the Complete Simulation button if it's the last section

//                MessageBox.Show("Process started!");
//            }
//            catch (Exception ex)
//            {
//                HandleError("Error starting the simulation", ex);
//            }
//        }

//        private async void btnNext_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                _activityTimer.Stop(); // Stop the timer
//                await _simulationManager.SaveElapsedTimeForTaskAsync(_simulationId, _currentTaskIndex, timeElapsed); // Save elapsed time

//                if (_currentTaskIndex < _simulationManager.Tasks.Count - 1)
//                {
//                    _currentTaskIndex++;
//                    UnloadTask(); // Clear the current task
//                    await LoadTask(_currentTaskIndex); // Load the next task

//                    // Fetch and set the elapsed time for the new task
//                    int elapsedTime = await _simulationManager.GetElapsedTimeForTaskAsync(_simulationId, _currentTaskIndex);
//                    SetElapsedTime(elapsedTime);

//                    StartTimers(); // Restart the timer
//                }
//            }
//            catch (Exception ex)
//            {
//                HandleError("Error navigating to the next task", ex);
//            }
//        }
//        private async void btnPrevious_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                _activityTimer.Stop(); // Stop the timer
//                await _simulationManager.SaveElapsedTimeForTaskAsync(_simulationId, _currentTaskIndex, timeElapsed); // Save elapsed time

//                if (_currentTaskIndex > 0)
//                {
//                    _currentTaskIndex--;
//                    UnloadTask(); // Clear the current task
//                    await LoadTask(_currentTaskIndex); // Load the previous task

//                    // Fetch and set the elapsed time for the new task
//                    int elapsedTime = await _simulationManager.GetElapsedTimeForTaskAsync(_simulationId, _currentTaskIndex);
//                    SetElapsedTime(elapsedTime);

//                    StartTimers(); // Restart the timer
//                }
//            }
//            catch (Exception ex)
//            {
//                HandleError("Error navigating to the previous task", ex);
//            }
//        }

//        private async Task LoadTask(int taskIndex)
//        {
//            if (taskIndex < 0 || taskIndex >= _simulationManager.Tasks.Count)
//            {
//                MessageBox.Show("Invalid task index.");
//                return;
//            }

//            var task = await _simulationManager.LoadTaskAsync(taskIndex);
//            if (task == null)
//            {
//                MessageBox.Show("Task could not be loaded.");
//                return;
//            }

//            label1.Text = task.Details?.TaskDescription ?? task.Description;
//            label2.Text = $"Task {taskIndex + 1} of {_simulationManager.Tasks.Count}";

//            lblTimer.Visible = true;

//            // Fetch and set the elapsed time for the task
//            int elapsedTime = await _simulationManager.GetElapsedTimeForTaskAsync(_simulationId, taskIndex);
//            SetElapsedTime(elapsedTime);
//        }
//        private void UnloadTask()
//        {
//            label1.Text = string.Empty;
//            label2.Text = string.Empty;
//            lblTimer.Visible = false;

//            _activityTimer.Stop(); // Stop the timer
//            timeElapsed = 0; // Reset the elapsed time
//            lblTimer.Text = "Time: 0 sec";
//        }
//        private void EnableControls()
//        {
//            btnCheckAnswer.Enabled = true;
//            btnPrevious.Enabled = true;
//            btnNext.Enabled = true;
//            btnHint.Enabled = true;
//            btnSaveandNextSession.Enabled = true;
//            btnSaveandPreviousSession.Enabled = true;
//            btnSaveandExit.Enabled = true;
//            btnStart.Enabled = false;
//        }

//        private async void btnCheckAnswer_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                var result = await _simulationManager.CheckAnswerAsync(_currentTaskIndex);
//                MessageBox.Show(result, "Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
//            }
//            catch (Exception ex)
//            {
//                HandleError("Error checking the answer", ex);
//            }
//        }

//        private async Task<bool> CheckForceMoveToNextSection()
//        {
//            var result = MessageBox.Show("You haven't completed all tasks. Move to next section anyway?", "Confirm Navigation", MessageBoxButtons.YesNo);
//            return result == DialogResult.Yes;
//        }
//        private async void btnSaveAndExit_Click(object sender, EventArgs e)
//        {
//            await CloseFileAndExitAsync();
//        }
//        public void SetCurrentTaskIndex(int taskIndex)
//        {
//            if (taskIndex >= 0 && taskIndex < _simulationManager.Tasks.Count)
//            {
//                _currentTaskIndex = taskIndex;
//            }
//        }
//        private async void btnCompleteSimulation_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                if (!_allTasksCompleted)
//                {
//                    MessageBox.Show("Please complete all tasks before finishing the simulation.");
//                    return;
//                }

//                await _simulationManager.SaveProgressAsync();
//                MessageBox.Show("Congratulations! You've completed the entire simulation.");
//                Application.Exit();
//            }
//            catch (Exception ex)
//            {
//                HandleError("Error completing the simulation", ex);
//            }
//        }
//        private async Task CloseFileAndExitAsync()
//        {
//            try
//            {
//                await _simulationManager.SaveProgressAsync();
//                MessageBox.Show("Progress saved successfully.");
//                Application.Exit();
//            }
//            catch (Exception ex)
//            {
//                HandleError("Error saving and exiting", ex);
//            }
//        }
//        private void HandleError(string context, Exception ex)
//        {
//            MessageBox.Show($"{context}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//        }
//    }
//}
