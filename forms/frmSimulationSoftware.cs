using System;
using Microsoft.Data.SqlClient;

using System.Data;

using System.Collections.Generic;
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
        private System.Windows.Forms.Timer activityTimer;
        public event EventHandler SectionCompleted;
        private bool _allTasksCompleted = false;
        private bool _isLastSection = false;
        private int currentTaskIndex;
        private int timeElapsed;
        private List<JobTask> tasks;
        private Section _currentSection;

        private readonly SectionRepository _sectionRepository;
        private readonly string _simulationId;
        private readonly string _sectionId;
        private readonly string _userId;

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
            SectionService sectionService,
            UserRepository userRepository,
            string softwareId,
            string activityId,
            DataSet progressDataSet,
            int attempt,
            Section currentSection, bool isLastSection)
        {
            this.tasks = tasks;
            this._sectionRepository = sectionRepository;
            this._simulationId = simulationId;
            this._sectionId = sectionId;
            this._userId = userId;
            this._currentSection = currentSection;
            _isLastSection = isLastSection;
            _simulationManager = new SimulationManager(
                tasks,
                filePath,
                sectionId,
                simulationId,
                userId,
                sectionRepository,
                fileService,
                skillMatrixRepository,
                activityRepository,
                taskRepository,
                sectionService,
                userRepository,
                softwareId,
                activityId,
                progressDataSet,
                attempt,
                currentSection);
          
            InitializeComponent();
            InitializeComponents();
            InitializeDynamicUI();
            LoadMasterData();
        }
        private void InitializeDynamicUI()
        {
            btnCompleteSimulation.Visible = false;
            btnSaveandNextSession.Visible = true;
        }
        private async Task RetryCurrentSection()
        {
            await _simulationManager.SaveProgressAsync();
            NavigateToSection?.Invoke(this, SectionNavigationAction.Retry);
            this.Close();
        }

        private async Task CheckTaskCompletion()
        {
            _allTasksCompleted = await _simulationManager.AreAllTasksCompleted();

            if (_allTasksCompleted)
            {
                // Show retry option if not last section
                if (!_isLastSection)
                {
                    var result = MessageBox.Show(
                        "All tasks completed! Would you like to retry this section?",
                        "Section Complete",
                        MessageBoxButtons.YesNo);

                    if (result == DialogResult.Yes)
                    {
                        await RetryCurrentSection();
                        return;
                    }
                }

                // Enable complete button if last section
                if (_isLastSection)
                {
                    btnCompleteSimulation.Visible = true;
                    btnSaveandNextSession.Visible = false;
                }
            }
        }
        private async void frmSimulationSoftware_Load(object sender, EventArgs e)
        {
            var nextSection = await _sectionRepository.GetNextSectionAsync(_simulationId, _sectionId);
            if (nextSection == null)
            {
                btnSaveandNextSession.Enabled = false;
            }
        }

        public void SetCurrentTaskIndex(int taskIndex)
        {
            if (taskIndex >= 0 && taskIndex < tasks.Count)
            {
                currentTaskIndex = taskIndex;
                // LoadTask(currentTaskIndex);
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
            lblTimer.Visible = false; // Hide timer initially
            btnPrevious.Enabled = _simulationManager.CurrentTaskIndex > 0;
            btnNext.Enabled = _simulationManager.CurrentTaskIndex < _simulationManager.Tasks.Count - 1;
        }

        private void DisableControls()
        {
            btnCheckAnswer.Enabled = false;
            btnPrevious.Enabled = false;
            btnNext.Enabled = false;
            btnHint.Enabled = false;
        }

        private void StartTimers()
        {
            activityTimer.Start();
        }

        private void LoadMasterData()
        {
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

            // Show timer only after task is loaded
            lblTimer.Visible = true;
            lblTimer.Text = $"Time: {_simulationManager.CurrentTaskElapsedTime} sec";
        }

        private async void btnCheckAnswer_Click(object sender, EventArgs e)
        {
            try
            {
                var resultMessage = await _simulationManager.CheckAnswerAsync(_simulationManager.CurrentTaskIndex);
                MessageBox.Show(resultMessage, "Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await CheckTaskCompletion();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error validating task: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            await _simulationManager.IncrementTimeElapsedAsync();
            lblTimer.Text = $"Time: {_simulationManager.CurrentTaskElapsedTime} sec"; // Use CurrentTaskElapsedTime
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

        private void btnSaveAndExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                // Ensure that tasks have been loaded in the SimulationManager
                if (_simulationManager.Tasks.Count == 0)
                {
                    MessageBox.Show("No tasks to load. Please check the section.");
                    return;
                }

                // Load the current task based on the manager's current task index
                await LoadTask(_simulationManager.CurrentTaskIndex);

                // Set elapsed time based on the simulation manager's current task elapsed time
                var elapsedTime = _simulationManager.CurrentTaskElapsedTime;
                SetElapsedTime(elapsedTime);

                // Update the label to indicate the task number
                label2.Text = $"Task {currentTaskIndex + 1} of {_simulationManager.Tasks.Count}";

                // Notify the user that the process has started
                MessageBox.Show("Process started!");

                // Start the timer for tracking elapsed time
                StartTimers();

                // Enable controls for user interaction
                EnableControls();

                // Disable the start button to prevent multiple starts
                btnStart.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void EnableControls()
        {
            btnCheckAnswer.Enabled = true;
            btnPrevious.Enabled = true;
            btnNext.Enabled = true;
            btnHint.Enabled = true;
        }

        private void UpdateUI()
        {
            InitializeUI();
            // LoadTask(_simulationManager.CurrentTaskIndex);
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

        private async Task OpenSectionFile(Section section)
        {
            var sectionFilePath = Path.Combine("SectionFolder", $"{section.SectionId}_{section.SoftwareId}.file");
            if (File.Exists(sectionFilePath))
            {
                Process.Start(new ProcessStartInfo(sectionFilePath)
                {
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Maximized
                })?.WaitForInputIdle();
            }
            else
            {
                MessageBox.Show($"Section file for {section.SectionId} not found.");
            }

            var newActivity = new Activity
            {
                ActivityId = _simulationManager.ActivityId,
                UserId = _simulationManager.UserId,
                SimulationId = _simulationManager.SimulationId,
                SectionId = section.SectionId,
                Status = StatusTypes.New,
                SectionAttempt = _simulationManager.Attempt,
                StudentFile = Convert.ToBase64String(File.ReadAllBytes(sectionFilePath)),
                CreateDate = DateTime.UtcNow,
                ModifyDate = DateTime.UtcNow,
                CreateBy = _simulationManager.UserId,
                ModifyBy = _simulationManager.UserId,
                Result = string.Empty
            };

            await _simulationManager.ActivityRepository.SaveActivityAsync(newActivity);
        }

        private async void btnNext_Click(object sender, EventArgs e)
        {
            try
            {
                activityTimer.Stop(); // Pause the timer
                await _simulationManager.SaveProgressAsync(); // Save current progress

                if (_simulationManager.CurrentTaskIndex < _simulationManager.Tasks.Count - 1) // If not at the last task
                {
                    await _simulationManager.MoveToNextTask(); // Move to the next task
                    await LoadCurrentTask(); // Load the task related to the updated index
                }
                else
                {
                    await _simulationManager.SaveAndLoadNextSectionAsync();
                }

                UpdateNavigationButtons(); // Enable/Disable navigation buttons accordingly
                activityTimer.Start(); // Resume timer after task loading
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
                activityTimer.Stop(); // Pause the timer
                await _simulationManager.SaveProgressAsync(); // Save current progress

                await _simulationManager.MoveToPreviousTask(); // Move to the previous task
                await LoadCurrentTask(); // Load the task after moving
                UpdateNavigationButtons(); // Enable/Disable navigation buttons accordingly

                activityTimer.Start(); // Resume timer
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private async void btnSaveandNextSession_Click(object sender, EventArgs e)
        {
            await _simulationManager.SaveProgressAsync();

            if (_allTasksCompleted || await CheckForceMoveToNextSection())
            {
                NavigateToSection?.Invoke(this, SectionNavigationAction.Next);
                this.Close();
            }
        }
        private async void btnSaveandPreviousSession_Click(object sender, EventArgs e)
        {
            await _simulationManager.SaveProgressAsync();
            NavigateToSection?.Invoke(this, SectionNavigationAction.Previous);
            this.Close();
        }

        private async Task<bool> CheckForceMoveToNextSection()
        {
            var result = MessageBox.Show(
                "You haven't completed all tasks. Move to next section anyway?",
                "Confirm Navigation",
                MessageBoxButtons.YesNo);

            return result == DialogResult.Yes;
        }        // A new method for loading the current task in the simulation manager
        private async Task LoadCurrentTask()
        {
            await LoadTask(_simulationManager.CurrentTaskIndex); // Load task based on current task index
        }

        private async void btnCompleteSimulation_Click(object sender, EventArgs e)
        {
            if (!_allTasksCompleted)
            {
                MessageBox.Show("Please complete all tasks before finishing the simulation.");
                return;
            }

            await _simulationManager.SaveProgressAsync();
            MessageBox.Show("Congratulations! You've completed the entire simulation.");
            Application.Exit();
        }
    }
}