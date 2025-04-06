﻿//using System;
//using Microsoft.Data.SqlClient;
//using System.Data;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Windows.Forms;
//using JobSimulation.BLL;
//using JobSimulation.DAL;
//using JobSimulation.Models;
//using JobSimulation.Managers;
//using Timer = System.Windows.Forms.Timer;
//using Activity = JobSimulation.Models.Activity;

//namespace JobSimulation.Forms
//{
//    public partial class frmSimulationSoftware : Form
//    {
//        public event EventHandler<SectionNavigationAction> NavigateToSection;
//        private readonly SimulationManager _simulationManager;
//        private Timer activityTimer;
//        public event EventHandler SectionCompleted;
//        private bool _allTasksCompleted = false;
//        private bool _isLastSection = false;
//        private int currentTaskIndex;
//        private int timeElapsed;
//        private List<JobTask> tasks;
//        private Section _currentSection;

//        private readonly SectionRepository _sectionRepository;
//        private readonly string _simulationId;
//        private  string _sectionId;
//        private readonly string _userId;

//        public frmSimulationSoftware(
//      List<JobTask> tasks,
//      string filePath,
//      string sectionId,
//      string simulationId,
//      string userId,
//      SectionRepository sectionRepository,
//      FileService fileService,
//      SkillMatrixRepository skillMatrixRepository,
//      ActivityRepository activityRepository,
//      TaskRepository taskRepository,
//      SectionService sectionService,
//      UserRepository userRepository,
//      string softwareId,
//      string activityId,
//      DataSet progressDataSet,
//      int attempt,
//      Section currentSection,
//      bool isLastSection,
//      int currentTaskIndex)
//        {
//            this.currentTaskIndex = currentTaskIndex;
//            this._isLastSection = isLastSection;
//            this.tasks = tasks;
//            this._sectionId = sectionId;
//            this._simulationId = simulationId;
//            this._userId = userId;
//            this._sectionRepository = sectionRepository;
//            this._currentSection = currentSection;

//            _simulationManager = new SimulationManager(
//                tasks,
//                filePath,
//                sectionId,
//                simulationId,
//                userId,
//                sectionRepository,
//                fileService,
//                skillMatrixRepository,
//                activityRepository,
//                taskRepository,
//                sectionService,
//                userRepository,
//                softwareId,
//                activityId,
//                progressDataSet,
//                attempt,
//                currentSection,
//                currentTaskIndex);

//            InitializeComponent();
//            InitializeComponents();
//            InitializeDynamicUI();
//            LoadMasterData();
//        }

//        private void InitializeDynamicUI()
//        {
//            btnCompleteSimulation.Visible = false;
//            btnSaveandNextSession.Visible = true;
//            btnStart.Enabled = true;
//            btnStart.Visible = true;
//        }

//        private async Task RetryCurrentSection()
//        {
//            await _simulationManager.SaveProgressAsync();
//            NavigateToSection?.Invoke(this, SectionNavigationAction.Retry);
//            Close();
//        }

//        private async Task CheckTaskCompletion()
//        {
//            _allTasksCompleted = await _simulationManager.AreAllTasksCompleted();

//            if (_allTasksCompleted)
//            {
//                if (!_isLastSection)
//                {
//                    var result = MessageBox.Show(
//                        "All tasks completed! Would you like to retry this section?",
//                        "Section Complete",
//                        MessageBoxButtons.YesNo);

//                    if (result == DialogResult.Yes)
//                    {
//                        await RetryCurrentSection();
//                        return;
//                    }
//                }

//                if (_isLastSection)
//                {
//                    btnCompleteSimulation.Visible = true;
//                    btnSaveandNextSession.Visible = false;
//                }
//            }
//        }

//        private async void frmSimulationSoftware_Load(object sender, EventArgs e)
//        {
//            var nextSection = await _sectionRepository.GetNextSectionAsync(_simulationId, _sectionId);
//            if (nextSection == null)
//            {
//                btnSaveandNextSession.Enabled = false;
//            }
//        }

//        public void SetCurrentTaskIndex(int taskIndex)
//        {
//            if (taskIndex >= 0 && taskIndex < tasks.Count)
//            {
//                currentTaskIndex = taskIndex;
//            }
//        }

//        public void SetElapsedTime(int elapsedTime)
//        {
//            timeElapsed = elapsedTime;
//            lblTimer.Text = $"Time: {timeElapsed} sec";
//        }

//        private void UpdateNavigationButtons()
//        {
//            btnPrevious.Enabled = _simulationManager.CurrentTaskIndex > 0;
//            btnNext.Enabled = _simulationManager.CurrentTaskIndex < _simulationManager.Tasks.Count - 1;
//        }

//        private void InitializeComponents()
//        {
//            activityTimer = new Timer { Interval = 1000 };
//            activityTimer.Tick += Timer_Tick;

//            InitializeUI();
//            DisableControls();
//        }

//        private void InitializeUI()
//        {
//            lblTimer.Visible = false;
//            btnPrevious.Enabled = _simulationManager.CurrentTaskIndex > 0;
//            btnNext.Enabled = _simulationManager.CurrentTaskIndex < _simulationManager.Tasks.Count - 1;
//        }

//        private void DisableControls()
//        {
//            btnCheckAnswer.Enabled = false;
//            btnPrevious.Enabled = false;
//            btnNext.Enabled = false;
//            btnHint.Enabled = false;
//        }

//        private void StartTimers()
//        {
//            activityTimer.Start();
//        }

//        private void LoadMasterData()
//        {
//            _simulationManager.LoadMasterData();
//        }

//        private async Task LoadTask(int taskIndex)
//        {
//            var task = await _simulationManager.LoadTaskAsync(taskIndex);
//            if (task == null)
//            {
//                MessageBox.Show("Invalid task index.");
//                return;
//            }

//            dynamic details = task.Details;
//            label1.Text = details?.TaskDescription ?? task.Description;
//            label1.Visible = true;
//            label2.Text = $"Task {taskIndex + 1} of {_simulationManager.Tasks.Count}";

//            lblTimer.Visible = true;
//            lblTimer.Text = $"Time: {_simulationManager.CurrentTaskElapsedTime} sec";
//        }

//        private async void btnCheckAnswer_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                var resultMessage = await _simulationManager.CheckAnswerAsync(_simulationManager.CurrentTaskIndex);
//                MessageBox.Show(resultMessage, "Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
//                await CheckTaskCompletion();
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error validating task: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }
//        }

//        private async void Timer_Tick(object sender, EventArgs e)
//        {
//            await _simulationManager.IncrementTimeElapsedAsync();
//            lblTimer.Text = $"Time: {_simulationManager.CurrentTaskElapsedTime} sec";
//        }

//        private async void AutosaveTimer_Tick(object sender, EventArgs e)
//        {
//            try
//            {
//                await _simulationManager.SaveProgressAsync();
//                Console.WriteLine($"Auto-saved at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Auto-save error: {ex.Message}");
//            }
//        }

//        private async void btnHint_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                var hint = await _simulationManager.GetHintAsync(_simulationManager.CurrentTaskIndex);
//                MessageBox.Show(hint, "Hint");
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error fetching hint: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }
//        }

//        private void btnSaveAndExit_Click(object sender, EventArgs e)
//        {
//            Application.Exit();
//        }

//        private async void btnStart_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                if (_simulationManager.Tasks.Count == 0)
//                {
//                    MessageBox.Show("No tasks to load. Please check the section.");
//                    return;
//                }

//                await LoadTask(_simulationManager.CurrentTaskIndex);
//                SetElapsedTime(_simulationManager.CurrentTaskElapsedTime);
//                label2.Text = $"Task {currentTaskIndex + 1} of {_simulationManager.Tasks.Count}";

//                MessageBox.Show("Process started!");

//                StartTimers();
//                EnableControls();

//                btnStart.Enabled = false;
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"An error occurred: {ex.Message}");
//            }
//        }

//        private void EnableControls()
//        {
//            btnCheckAnswer.Enabled = true;
//            btnPrevious.Enabled = true;
//            btnNext.Enabled = true;
//            btnHint.Enabled = true;
//        }

//        private async Task LoadCurrentTask()
//        {
//            await LoadTask(_simulationManager.CurrentTaskIndex);
//        }

//        private void UpdateUI()
//        {
//            InitializeUI();
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

//        private async Task OpenSectionFile(Section section)
//        {
//            var sectionFilePath = Path.Combine("SectionFolder", $"{section.SectionId}_{section.SoftwareId}.file");
//            if (File.Exists(sectionFilePath))
//            {
//                Process.Start(new ProcessStartInfo(sectionFilePath)
//                {
//                    UseShellExecute = true,
//                    WindowStyle = ProcessWindowStyle.Maximized
//                })?.WaitForInputIdle();
//            }
//            else
//            {
//                MessageBox.Show($"Section file for {section.SectionId} not found.");
//            }

//            var newActivity = new Activity
//            {
//                ActivityId = _simulationManager.ActivityId,
//                UserId = _simulationManager.UserId,
//                SimulationId = _simulationManager.SimulationId,
//                SectionId = section.SectionId,
//                Status = StatusTypes.New,
//                SectionAttempt = _simulationManager.Attempt,
//                StudentFile = Convert.ToBase64String(File.ReadAllBytes(sectionFilePath)),
//                CreateDate = DateTime.UtcNow,
//                ModifyDate = DateTime.UtcNow,
//                CreateBy = _simulationManager.UserId,
//                ModifyBy = _simulationManager.UserId,
//                Result = string.Empty
//            };

//            await _simulationManager.ActivityRepository.SaveActivityAsync(newActivity);
//        }

//        private async void btnNext_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                activityTimer.Stop();
//                await _simulationManager.SaveProgressAsync();

//                if (_simulationManager.CurrentTaskIndex < _simulationManager.Tasks.Count - 1)
//                {
//                    await _simulationManager.MoveToNextTask();
//                    await LoadCurrentTask();
//                }
//                else
//                {
//                    //await _simulationManager.SaveAndLoadNextSectionAsync();
//                }

//                UpdateNavigationButtons();
//                activityTimer.Start();
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error: {ex.Message}");
//            }
//        }

//        private async void btnPrevious_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                activityTimer.Stop();
//                await _simulationManager.SaveProgressAsync();

//                await _simulationManager.MoveToPreviousTask();
//                await LoadCurrentTask();
//                UpdateNavigationButtons();

//                activityTimer.Start();
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error: {ex.Message}");
//            }
//        }

//        private async void btnSaveandNextSession_Click(object sender, EventArgs e)
//        {
//            await _simulationManager.SaveProgressAsync();

//            if (_allTasksCompleted || await CheckForceMoveToNextSection())
//            {
//                NavigateToSection?.Invoke(this, SectionNavigationAction.Next);

//            }
//        }

//        private async void btnSaveandPreviousSession_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                activityTimer.Stop();
//                await _simulationManager.SaveProgressAsync();



//                NavigateToSection?.Invoke(this, SectionNavigationAction.Previous);

//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error saving progress: {ex.Message}");
//            }
//        }
//        private async Task<bool> CheckForceMoveToNextSection()
//        {
//            var result = MessageBox.Show(
//                "You haven't completed all tasks. Move to next section anyway?",
//                "Confirm Navigation",
//                MessageBoxButtons.YesNo);

//            return result == DialogResult.Yes;
//        }

//        private async void btnCompleteSimulation_Click(object sender, EventArgs e)
//        {
//            if (!_allTasksCompleted)
//            {
//                MessageBox.Show("Please complete all tasks before finishing the simulation.");
//                return;
//            }

//            await _simulationManager.SaveProgressAsync();
//            MessageBox.Show("Congratulations! You've completed the entire simulation.");
//            Application.Exit();
//        }
//        public void UpdateSectionData(
//    List<JobTask> newTasks,
//    string newFilePath,
//    string newSectionId,
//    string newSoftwareId,
//    string newActivityId,
//    bool newIsLastSection,
//    Section newCurrentSection,
//    int newAttempt,
//    int newTaskIndex)
//        {
//            // Update simulation manager internals
//            _simulationManager.UpdateSectionData(
//                newTasks,
//                newFilePath,
//                newSectionId,
//                newSoftwareId,
//                newActivityId,
//                newAttempt,
//                newCurrentSection,
//                newTaskIndex);

//            // Update internal fields
//            tasks = newTasks;
//            _sectionId = newSectionId;
//            _currentSection = newCurrentSection;
//            _isLastSection = newIsLastSection;

//            // Reset UI
//            btnCompleteSimulation.Visible = false;
//            btnSaveandNextSession.Visible = true;
//            btnStart.Enabled = true;
//            btnStart.Visible = true;

//            label1.Text = string.Empty;
//            label2.Text = string.Empty;
//            lblTimer.Text = "Time: 0 sec";

//            DisableControls();
//        }


//        protected override void OnFormClosing(FormClosingEventArgs e)
//        {
//            // Save current state before closing
//            _ = _simulationManager.SaveProgressAsync();
//            base.OnFormClosing(e);
//        }
//        private void frmSimulationSoftware_FormClosed(object sender, FormClosedEventArgs e)
//        {
//            // Clean up any open files
//            if (!string.IsNullOrEmpty(_simulationManager?.FilePath))
//            {
//                CloseFile(_simulationManager.FilePath);
//            }

//            // Stop and dispose timers
//            activityTimer?.Stop();
//            activityTimer?.Dispose();
//        }


//    }
//}


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
        private Timer activityTimer;
        public event EventHandler SectionCompleted;
        private bool _allTasksCompleted = false;
        private bool _isLastSection = false;
        private int currentTaskIndex;
        private int timeElapsed;
        private List<JobTask> tasks;
        private Section _currentSection;

        private readonly SectionRepository _sectionRepository;
        private readonly string _simulationId;
        private string _sectionId;
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
                currentSection,
                currentTaskIndex);

            InitializeComponent();
            InitializeComponents();
            InitializeDynamicUI();
            LoadMasterData();
        }

        private void InitializeDynamicUI()
        {
            btnCompleteSimulation.Visible = false;
            btnSaveandNextSession.Visible = true;
            btnStart.Enabled = true;
            btnStart.Visible = true;
        }

        private async Task RetryCurrentSection()
        {
            await _simulationManager.SaveProgressAsync();
            NavigateToSection?.Invoke(this, SectionNavigationAction.Retry);
        }

        private async Task CheckTaskCompletion()
        {
            _allTasksCompleted = await _simulationManager.AreAllTasksCompleted();

            if (_allTasksCompleted)
            {
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
            lblTimer.Visible = false;
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

        private void btnSaveAndExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
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
                    await _simulationManager.MoveToNextTask();
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

                await _simulationManager.MoveToPreviousTask();
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
            if (!_allTasksCompleted)
            {
                MessageBox.Show("Please complete all tasks before finishing the simulation.");
                return;
            }

            await _simulationManager.SaveProgressAsync();
            MessageBox.Show("Congratulations! You've completed the entire simulation.");
            Application.Exit();
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

            // Optionally reset form UI elements here:
            btnStart.Enabled = true;
            btnStart.Visible = true;
            btnSaveandNextSession.Visible = true;
            btnCompleteSimulation.Visible = isLastSection;
            lblTimer.Text = "Time: 0 sec";
            DisableControls();
        }




        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _ = _simulationManager.SaveProgressAsync();
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
