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
using Activity = JobSimulation.Models.Activity; // Import the new SimulationManager

namespace JobSimulation.Forms
{
    public partial class frmSimulationSoftware : Form
    {
        private readonly SimulationManager _simulationManager;
        private System.Windows.Forms.Timer activityTimer;
        public event EventHandler SectionCompleted;

        private int currentTaskIndex;
        private int timeElapsed;
        private List<JobTask> tasks;

        private readonly SectionRepository _sectionRepository;
        private readonly string _simulationId;
        private readonly string _sectionId;

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
            Section currentSection)
        {
            this.tasks = tasks;
            this._sectionRepository = sectionRepository;
            this._simulationId = simulationId;
            this._sectionId = sectionId;

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
            // InitializeNavigationButtons();
            InitializeComponents();
            LoadMasterData();
        }

        private async void frmSimulationSoftware_Load(object sender, EventArgs e)
        {
            var nextSection = await _sectionRepository.GetNextSectionAsync(_simulationId, _sectionId);
            if (nextSection == null)
            {
                btnSaveandNextSession.Enabled = false;
            }
        }

        // private void InitializeNavigationButtons()
        // {
        //     btnNext.Click += btnNext_Click;
        //     btnPrevious.Click += btnPrevious_Click;
        // }

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

        protected virtual void OnSectionCompleted(EventArgs e)
        {
            SectionCompleted?.Invoke(this, e);
        }

        private void btnCompleteSection_Click(object sender, EventArgs e)
        {
            OnSectionCompleted(EventArgs.Empty);
        }

        private async void btnNext_Click(object sender, EventArgs e)
        {
            try
            {
                activityTimer.Stop(); // Pause timer
                await _simulationManager.SaveProgressAsync();
                await _simulationManager.MoveToNextTask();
                await LoadTask(_simulationManager.CurrentTaskIndex);
                UpdateNavigationButtons();
                activityTimer.Start(); // Resume only after new task is loaded
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void btnPrevious_Click(object sender, EventArgs e)
        {
            try
            {
                activityTimer.Stop(); // Pause timer
                await _simulationManager.SaveProgressAsync();
                await _simulationManager.MoveToPreviousTask();
                await LoadTask(_simulationManager.CurrentTaskIndex);
                UpdateNavigationButtons();
                activityTimer.Start(); // Resume only after new task is loaded
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateNavigationButtons()
        {
            btnPrevious.Enabled = _simulationManager.CurrentTaskIndex > 0;
            btnNext.Enabled = _simulationManager.CurrentTaskIndex < _simulationManager.Tasks.Count - 1;
        }

        private async Task LoadNextSection()
        {
            var nextSection = await _simulationManager.GetNextSectionAsync();
            if (nextSection != null)
            {
                await _simulationManager.LoadSectionAsync(nextSection);
                UpdateUI();
                CloseFile(_simulationManager.FilePath);
                await OpenSectionFile(nextSection);
            }
            else
            {
                MessageBox.Show("Congratulations! You've completed all sections.");
                Close();
            }
        }

        private async Task LoadPreviousSection()
        {
            var prevSection = await _simulationManager.GetPreviousSectionAsync();
            if (prevSection != null)
            {
                await _simulationManager.LoadSectionAsync(prevSection);
                UpdateUI();
                CloseFile(_simulationManager.FilePath);
                await OpenSectionFile(prevSection);
            }
            else
            {
                MessageBox.Show("This is the first section.");
            }
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

        private async void btnSaveandNextSession_Click(object sender, EventArgs e)
        {
            try
            {
                await _simulationManager.SaveAndNextSectionAsync();
                activityTimer.Start(); // Restart the FORM'S timer here if needed
                await LoadNextSection();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving session: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnSaveandPreviousSession_Click(object sender, EventArgs e)
        {
            try
            {
                await _simulationManager.SaveAndPreviousSectionAsync();
                activityTimer.Start(); // Restart the FORM'S timer here if needed
                await LoadPreviousSection();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving session: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                // Load the latest modified task first
                currentTaskIndex = _simulationManager.CurrentTaskIndex; // Use the CurrentTaskIndex set by the SimulationManager
                await LoadTask(currentTaskIndex);
                var elapsedTime = _simulationManager.CurrentTaskElapsedTime;
                SetElapsedTime(elapsedTime);
                label2.Text = $"Task {currentTaskIndex + 1} of {_simulationManager.Tasks.Count}";

                MessageBox.Show("Process started!");
                StartTimers(); // Ensure this starts the timer
                EnableControls();
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
    }
}