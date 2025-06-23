

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
using WinFormsTimer = System.Windows.Forms.Timer;


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
        //private int currentTaskIndex;
        private int timeElapsed;
        private List<JobTask> tasks;
        private Section _currentSection;
        private readonly UserRepository _userRepository;
        private readonly SectionRepository _sectionRepository;
        private readonly string _simulationId;
        private string _sectionId;
        private readonly string _userId;
        private string _masterJson;
        private readonly SkillMatrixRepository _skillMatrixRepository;




        //--- State and Animation Variables for Collapse/Expand ---
        private bool isFormCollapsed = true;
        private WinFormsTimer moveTimer; // Timer for the sliding animation
        private int targetX;             // Target X coordinate for animation
        private int formWidthExpanded = 400; // Expanded width (ADJUST AS NEEDED based on your content)
        private int formWidthCollapsed = 35; // Width to show when collapsed (adjust to fit arrow comfortably)
        private int toggleButtonHeight = 80; // Height of the clickable toggle area (adjust as needed)
        private Rectangle toggleButtonArea;  // Defined relative to form's client area (0, Y)
        private int formCornerRadius = 20; // Radius for the form corners

        private bool _exiting = false; // Add this class-level flag

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
            //this.currentTaskIndex = currentTaskIndex;
            this._isLastSection = isLastSection;
            this.tasks = tasks;
            this._sectionId = sectionId;
            this._simulationId = simulationId;
            this._userId = userId;
            this._sectionRepository = sectionRepository;
            this._currentSection = currentSection;
            this._userRepository = userRepository;
            this._skillMatrixRepository = skillMatrixRepository;
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
                sectionRepository,
                progressDataSet,
                attempt,
                currentSection,
                activityId,
                currentTaskIndex
            );

            InitializeComponent();
            // --- Form Styling for Collapse/Expand ---
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;
            this.ShowInTaskbar = false; // Optional: Hide from taskbar
            this.TopMost = true;       // Keep on top
            this.DoubleBuffered = true; // For smoother drawing

            // --- Initialize Simulation Components & UI (Original) ---
            InitializeComponents(); // Initializes activityTimer etc.
            InitializeDynamicUI();
            InitializeFormToCollapsedState();// Sets initial button visibility
        /*    LoadMasterData();  */     // Loads simulation master data

            // --- Initialize Collapse/Expand Animation ---
            InitializeCollapseExpand();

            // Wire up Load event AFTER InitializeComponent
            this.Load += frmSimulationSoftware_Load;
            //InitializeComponents();
            //InitializeDynamicUI();
            //LoadMasterData();
        }
        private void InitializeFormToCollapsedState()
        {
            var screen = Screen.PrimaryScreen.WorkingArea;

            isFormCollapsed = true;
            this.Width = formWidthCollapsed;
            this.Location = new Point(screen.Right - formWidthCollapsed, screen.Top);
            UpdateFormRegionBasedOnState();
            this.Invalidate(); // Optional: Forces a repaint
        }


        private void InitializeDynamicUI()
        {

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


        private async void frmSimulationSoftware_Load(object sender, EventArgs e)
        {
            DisableAllButtonsExceptStartAndClose(); // Ensure only btnStart and btnClose are enabled
            
            _isLastSection = await _sectionRepository.IsLastSectionAsync(_sectionId, _simulationId);
            await UpdateProgressBarAsync();
            Debug.WriteLine($"[DEBUG] frmSimulationSoftware_Load - _isLastSection: {_isLastSection}");

            if (_isLastSection)
            {

            }

            await UpdateSectionNavigationButtons(_sectionId, enableNavigation: false); // Skip enabling navigation buttons on load
        }

        private async Task UpdateSectionNavigationButtons(string sectionId, bool enableNavigation = true)
        {
            btnPrevious.Enabled = false;
            btnNext.Enabled = false;

            if (enableNavigation)
            {
                bool isFirstTask = _simulationManager.IsFirstTaskInSection();
                bool isLastTask = _simulationManager.IsLastTaskInSection();

                btnPrevious.Enabled = !isFirstTask;
                btnNext.Enabled = !isLastTask || !_isLastSection;
            }
        }

        //public void SetCurrentTaskIndex(int taskIndex)
        //{
        //    if (taskIndex >= 0 && taskIndex < tasks.Count)
        //    {
        //        currentTaskIndex = taskIndex;
        //    }
        //}

        public void SetElapsedTime(int elapsedTime)
        {
            timeElapsed = elapsedTime;
            lblTimer.Text = $"Time: {FormatTime(timeElapsed)}";
        }
        private string FormatTime(int totalSeconds)
        {
            TimeSpan ts = TimeSpan.FromSeconds(totalSeconds);
            return ts.ToString(@"hh\:mm\:ss");
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
            btnSubmit.Enabled = false;


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
            btnSubmit.Enabled = true;


        
            btnSaveandExit.Enabled = true;

            btnStart.Enabled = false; // Disable btnStart after it's clicked
        }

        private void StartTimers()
        {
            activityTimer.Start();
        }

        //private void LoadMasterData()
        //{
        //    Debug.WriteLine($"[DEBUG] LoadMasterData called. Current Section: {_currentSection.SectionId}");
        //    _simulationManager.LoadMasterData();
        //}

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
            lblTimer.Text = $"Time: {FormatTime(_simulationManager.CurrentTaskElapsedTime)}"; await UpdateProgressBarAsync();
        }
        //private async void btnSubmit_Click(object sender, EventArgs e)
        //{
        //    activityTimer.Stop();
        //    try
        //    {
        //        await _simulationManager.CacheTaskAnswerAsync();

        //        if (_simulationManager.IsLastTaskInLastSection())
        //        {
        //            // Save entire simulation to DB
        //            await _simulationManager.SaveSimulationProgressAsync();
        //            MessageBox.Show("Simulation Complete! Thank you.");
        //            await CloseFileAndExitAsync();
        //        }
        //        else
        //        {
        //            // Just cache locally (DB save happens on section change)
        //            MessageBox.Show("Task submitted (locally). Progress will be saved to database on section change.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Error submitting: {ex.Message}");
        //    }
        //    finally
        //    {
        //        activityTimer.Start();
        //    }
        //}


        private async void btnSubmit_Click(object sender, EventArgs e)
        {
            activityTimer.Stop();
            bool shouldCloseForm = false; // Local flag to determine if we should close

            try
            {
                await _simulationManager.CacheTaskAnswerAsync();
                await UpdateProgressBarAsync();

                if (_simulationManager.IsLastTaskInSection())
                {
                    await _simulationManager.SaveSectionProgressAsync();

                    if (await _simulationManager.IsLastTaskInLastSectionAsync())
                    {
                        await _simulationManager.SaveSimulationProgressAsync();
                        await _simulationManager.UpdateAllSectionResultsAsync();
                       
                        string overallResult = await _simulationManager.CalculateSimulationResultAsync(_simulationManager.SimulationId);
                       
                        MessageBox.Show(
                            $"Simulation Complete! Thank you.\n\nOverall Result: {overallResult}",
                            "Simulation Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );

                        // Set flags to trigger form closure
                        _exiting = true;
                        shouldCloseForm = true;
                        await CloseFileAndExitAsync();
                    }
                    else
                    {
                        MessageBox.Show("Section complete! Progress saved to database.");
                    }
                }
                else
                {
                    MessageBox.Show("Task submitted (locally). Progress will be saved to database on section change.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error submitting: {ex.Message}");
            }
            finally
            {
                // Close form if needed
                if (shouldCloseForm)
                {
                    this.Close();
                }
                // Restart timer only if not exiting
                else if (!_exiting)
                {
                    activityTimer.Start();
                }
            }
        }
        private async void btnCheckAnswer_Click(object sender, EventArgs e)
        {
            try
            {
                // Only update cache, not DB
                var resultMessage = await _simulationManager.CheckAnswerAsync(_simulationManager.CurrentTaskIndex);
                MessageBox.Show(resultMessage, "Result", MessageBoxButtons.OK, MessageBoxIcon.Information);

                await UpdateProgressBarAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error validating task: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void InitializeCollapseExpand()
        {
            moveTimer = new WinFormsTimer();
            moveTimer.Interval = 15; // Animation speed (milliseconds)
            moveTimer.Tick += MoveTimer_Tick;
        }
        private async void Timer_Tick(object sender, EventArgs e)
        {
            await _simulationManager.IncrementTimeElapsedAsync();
            lblTimer.Text = $"Time: {FormatTime(_simulationManager.CurrentTaskElapsedTime)}";
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

       



        private async void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                if (_simulationManager.Tasks.Count == 0)
                {
                    MessageBox.Show("No tasks to load. Please check the section.");
                    return;
                }
                await _simulationManager.SetActivityInProgressAsync();

                await LoadTask(_simulationManager.CurrentTaskIndex);
            
                SetElapsedTime(_simulationManager.CurrentTaskElapsedTime);
                //label2.Text = $"Task {currentTaskIndex + 1} of {_simulationManager.Tasks.Count}";
              
                MessageBox.Show("Process started!");

                StartTimers();

                EnableControls(); // Enable all relevant buttons
                await UpdateNavigationButtonsAsync();
                if (_isLastSection)
                {
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

        private async Task<bool> CanNavigateToNextSection()
        {
            try
            {
                // Check if all tasks are completed in the current section
                bool allCompleted = await _simulationManager.AreAllTasksCompleted();

                if (!allCompleted)
                {
                    var result = MessageBox.Show(
                        "Not all tasks are completed. Do you still want to move to the next section?",
                        "Incomplete Tasks",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );

                    return result == DialogResult.Yes;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Validation error: {ex.Message}");
                return false;
            }
        }
        private async void btnNext_Click(object sender, EventArgs e)
        {
            activityTimer.Stop();
            bool navigatingSection = false;
            try
            {
                _simulationManager.UpdateActualProgressFromCacheForCurrentTask();
                //await _simulationManager.SaveProgressAsync();

                if (_simulationManager.IsLastTaskInSection())
                {  // Validate before section navigation
                    if (!await CanNavigateToNextSection())
                    {
                        activityTimer.Start();
                        return;
                    }
             
                    //await _simulationManager.SaveSectionProgressAsync();

                    bool isLastSection = await _simulationManager.IsLastSectionAsync();

                    if (isLastSection)
                    {
                        await _simulationManager.SaveSimulationProgressAsync();
                        MessageBox.Show("Simulation Complete! Thank you.");
                        await CloseFileAndExitAsync();
                        // Do NOT restart timer
                        return;
                    }
                    else
                    {
                        // Set flag so timer is NOT restarted
                        navigatingSection = true;
                        NavigateToSection?.Invoke(this, SectionNavigationAction.Next);
                        return; // exit so timer does not restart
                    }
                }
                else
                {
                    _simulationManager.MoveToNextTask();
                    await LoadCurrentTask();
                    await UpdateProgressBarAsync();
                }
                await UpdateNavigationButtonsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
            finally
            {
                // Only restart timer if not navigating to section
                if (!navigatingSection)
                {
                    activityTimer.Start();
                }
            }
        }
        private async Task UpdateNavigationButtonsAsync()
        {
            bool isFirstSection = await _simulationManager.IsFirstSectionAsync();
            bool isLastSection = await _simulationManager.IsLastSectionAsync();
            bool isFirstTask = _simulationManager.IsFirstTaskInSection();
            bool isLastTask = _simulationManager.IsLastTaskInSection();

            btnPrevious.Enabled = !(isFirstTask && isFirstSection);
            btnNext.Enabled = !(isLastTask && isLastSection);  // disable Next if at last task of last section
        }
        private async void btnPrevious_Click(object sender, EventArgs e)
        {
            activityTimer.Stop();
            bool navigatingSection = false;
            try
            {
                _simulationManager.UpdateActualProgressFromCacheForCurrentTask();
                //await _simulationManager.SaveProgressAsync();

                if (_simulationManager.IsFirstTaskInSection())
                {
                    // Save section progress before navigating
                    await _simulationManager.SaveSectionProgressAsync();

                    // Move to previous section
                    navigatingSection = true;
                    NavigateToSection?.Invoke(this, SectionNavigationAction.Previous);
                    return; // Exit so timer does not restart
                }
                else
                {
                    // Move to previous task in current section
                    _simulationManager.MoveToPreviousTask();
                    await LoadCurrentTask();
                    await UpdateProgressBarAsync();
                }

                await UpdateNavigationButtonsAsync(); // ✅ Keep navigation buttons up to date
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
            finally
            {
                // Only restart timer if not navigating to section
                if (!navigatingSection)
                {
                    activityTimer.Start();
                }
            }
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
            // Only update in-memory SimulationManager state (no DB writes!)
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

            // UI resets (no DB writes here!)
            _isLastSection = isLastSection;
            _currentSection = newCurrentSection;
            _sectionId = newSectionId;

            await UpdateSectionNavigationButtons(newSectionId);

            await LoadCurrentTask();
            await UpdateProgressBarAsync();
            //label2.Text = $"Task {_simulationManager.CurrentTaskIndex + 1} of {_simulationManager.Tasks.Count}";
            lblTimer.Text = "Time: 00:00:00";
            label1.Text = string.Empty;
            label1.Visible = false;
            lblTimer.Visible = false;

            btnStart.Enabled = true;
            btnStart.Visible = true;

            DisableAllButtonsExceptStartAndClose();
        }
        private async Task UpdateProgressBarAsync()
        {
            // Use the new method for current section
            int currentCompleted = _simulationManager.GetCurrentSectionCompletedCount();
            int totalTasks = _simulationManager.Tasks.Count;

            // Calculate current section percentage
            int sectionPercent = totalTasks == 0 ? 0 :
                (int)Math.Round((currentCompleted / (double)totalTasks) * 100);

            // Now get overall progress
            int totalTasksAll = await _simulationManager.GetTotalTaskCountAllSectionsAsync();
            int completedAll = await _simulationManager.GetCompletedTaskCountAllSectionsAsync();

            int overallPercent = totalTasksAll == 0 ? 0 :
                (int)Math.Round((completedAll / (double)totalTasksAll) * 100);

            // Update UI - use overall or section progress as needed
            if (progressBar1 != null)
            {
                progressBar1.Value = overallPercent;
                progressBar1.Refresh();
            }
        }
        
        //private void UpdateProgressBar()
        //{
        //    // Get total and completed counts from your simulation manager
        //    int totalTasks = _simulationManager.GetTotalTaskCount();
        //    int completedTasks = _simulationManager.GetCompletedTaskCount();
        //    int totalSections = _simulationManager.GetTotalSectionCount();
        //    int completedSections = _simulationManager.GetCompletedSectionCount();

        //    // Calculate total items and completed items for progress calculation
        //    int total = totalTasks + totalSections;
        //    int done = completedTasks + completedSections;

        //    // Calculate percentage safely (avoid division by zero)
        //    int percent = total == 0 ? 0 : (int)((done / (double)total) * 100);

        //    if (progressBar1 != null)
        //    {
        //        progressBar1.Minimum = 0;
        //        progressBar1.Maximum = 100;
        //        // Ensure value is within bounds (in case)
        //        progressBar1.Value = Math.Clamp(percent, progressBar1.Minimum, progressBar1.Maximum);
        //    }
        //}

        private async void btnSaveAndExit_Click(object sender, EventArgs e)
        {
            try
            {
                // Set exiting flag
                _exiting = true;

                // Cache and save progress
                await _simulationManager.CacheTaskAnswerAsync();
                await _simulationManager.SaveSectionProgressAsync();

                // Close file and form
                await CloseFileAndExitAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving and exiting: {ex.Message}");
                _exiting = false; // Reset flag on error
            }
            finally
            {
                // Close the form after cleanup is complete
                this.Close();
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
        private async Task<bool> CheckForceMoveToNextSection()
        {
            var result = MessageBox.Show(
                "You haven't completed all tasks. Move to next section anyway?",
                "Confirm Navigation",
                MessageBoxButtons.YesNo);

            return result == DialogResult.Yes;
        }



        private async Task CloseFileAndExitAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(_simulationManager?.FilePath))
                {
                    var fileService = new FileService();

                    // Close the file using software-specific method
                    fileService.CloseFile(_simulationManager.FilePath);

                    // Wait for file to close
                    await Task.Delay(500);

                    // Delete the file
                    if (File.Exists(_simulationManager.FilePath))
                    {
                        fileService.DeleteFile(_simulationManager.FilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"File cleanup error: {ex.Message}");
                // Consider logging instead of showing UI in cleanup
            }
        }
        private async Task RetryCurrentSection()
        {
            //await _simulationManager.SaveProgressAsync();
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

        private void progressBar1_Click(object sender, EventArgs e)
        {
            int totalTasks = _simulationManager.GetTotalTaskCount();
            int completedTasks = _simulationManager.GetCompletedTaskCount();
            int totalSections = _simulationManager.GetTotalSectionCount();
            int completedSections = _simulationManager.GetCompletedSectionCount();

            int total = totalTasks + totalSections;
            int done = completedTasks + completedSections;
            int percent = total == 0 ? 0 : (int)((done / (double)total) * 100);

            MessageBox.Show(
                $"Tasks Completed: {completedTasks}/{totalTasks}\n" +
                $"Sections Completed: {completedSections}/{totalSections}\n" +
                $"Overall Progress: {percent}%",
                "Progress Details",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // Ensure form closes even if something fails
            if (!e.Cancel)
            {
                this.Dispose();
                Application.Exit();
            }
        }        //protected override void OnFormClosing(FormClosingEventArgs e)
    }
}




