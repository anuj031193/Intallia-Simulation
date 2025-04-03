using JobSimulation.DAL;
using JobSimulation.Models;
using JobSimulation.BLL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Drawing;
using Timer = System.Windows.Forms.Timer; // Add this line
using System.ComponentModel;

namespace JobSimulation.Forms
{
    public partial class frmSimulationSoftware : Form
    {
        private int _expandedWidth;
        private const int MinimizedWidth = 150;
        private bool _isMinimized;
        private Point _originalLocation; // Match your design
        private FileStream _fileLock;
        private int currentTaskIndex = 0;
        private Timer timer;
        private int timeElapsed = 0;
        private readonly List<JobTask> tasks;
        private readonly FileService fileService;
        private readonly string softwareId;
        private readonly string _tempFilePath; // Temporary file path
        private readonly string _sectionId;    // Current section ID
        private readonly SectionRepository _sectionRepository;
        private readonly FileService _fileService;
  
        public event EventHandler SectionCompleted;

        // Text properties for UI labels and messages
        private readonly string TaskDescriptionLabelText = "Task Description";
        private readonly string TimerInitialText = "Time: {0} sec";
        private readonly string TaskCounterFormat = "Task {0} of {1}";
        private readonly string ErrorInvalidTaskIndex = "Invalid task index.";
        private readonly string ErrorNoMoreTasks = "No more tasks available.";
        private readonly string ErrorFirstTask = "This is the first task.";
        private readonly string MsgTaskCompleted = "Task completed successfully.";
        private readonly string MsgTaskFailedFormat = "Task validation failed: {0}";
      
        private readonly string MsgProgressSaved = "Progress saved successfully.";

        public frmSimulationSoftware(List<JobTask> tasks, string tempFilePath, string sectionId, SectionRepository sectionRepository, FileService fileService, string softwareId)
        {
            InitializeComponent();
            _expandedWidth = this.Width;
            _originalLocation = this.Location;

            PositionFormRightEdge();
            this.btnMin.BringToFront();
            this.DoubleBuffered = true;


            btnMin.BringToFront();
            this.DoubleBuffered = true;
            this.tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
            this._tempFilePath = tempFilePath ?? throw new ArgumentNullException(nameof(tempFilePath));
            this.softwareId = softwareId ?? throw new ArgumentNullException(nameof(softwareId)); // Ensure softwareId is not null
            this._sectionId = sectionId ?? throw new ArgumentNullException(nameof(sectionId));
            this._sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
            this._fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            InitializeAutosaveTimer();
            fileService = new FileService();
            InitializeUI();
            InitializeTimer();
            InitializeAutosaveTimer();
            LoadTask(currentTaskIndex);
        }

        private void InitializeUI()
        {
            label1.Text = TaskDescriptionLabelText;
            label2.Text = string.Format(TaskCounterFormat, currentTaskIndex + 1, tasks.Count);
            lblHint.Text = tasks[currentTaskIndex].Hint;
            txtStartCell.Text = tasks[currentTaskIndex].From;
            txtEndCell.Text = tasks[currentTaskIndex].To;
            ddlTestSheet.SelectedItem = tasks[currentTaskIndex].SheetName;
            lblTimer.Text = string.Format(TimerInitialText, timeElapsed);
        }
        private Point dragStartPoint;


        private void btnMin_Click(object sender, EventArgs e)
        {
            var screenWorkingArea = Screen.PrimaryScreen.WorkingArea;

            if (_isMinimized)
            {
                // Expand the form: Restore the original width and location
                this.Width = _expandedWidth;
                this.Location = new Point(screenWorkingArea.Right - _expandedWidth, this.Location.Y);
                btnMin.Text = "⏩";

            }
            else
            {
                // Minimize the form: Stick it to the right of the screen with minimized width
                this.Width = MinimizedWidth;
                this.Location = new Point(screenWorkingArea.Right - MinimizedWidth, this.Location.Y);
                btnMin.Text = "⏪";

            }

            _isMinimized = !_isMinimized;
            ToggleControlsVisibility(!_isMinimized);
        }
        private void PositionFormRightEdge()
        {
            int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
            this.Location = new Point(
                screenWidth - _expandedWidth - 10,
                (Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 2
            );
        }
        //private void btnMin_MouseDown(object sender, MouseEventArgs e)
        //{
        //    if (isMinimized)
        //    {
        //        dragStartPoint = e.Location;
        //    }
        //}

        //private void btnMin_MouseMove(object sender, MouseEventArgs e)
        //{
        //    if (isMinimized && e.Button == MouseButtons.Left)
        //    {
        //        Point difference = Point.Subtract(Cursor.Position, new Size(dragStartPoint));
        //        this.Location = difference;
        //    }
        //}
        private void ToggleControlsVisibility(bool visible)
        {
            // Toggle visibility of main content controls
            dataGridView1.Visible = visible;
            dataGridView2.Visible = visible;
            dataGridView3.Visible = visible;
            label1.Visible = visible;
            // Add other controls that should hide when minimized
        }
        private void InitializeTimer()
        {
            timer = new Timer();
            timer.Interval = 1000; // 1 second
            timer.Tick += Timer_Tick;
        }
        private void InitializeAutosaveTimer()
        {
            autosaveTimer = new Timer();
            autosaveTimer.Interval = 60000; // 1 minute
            autosaveTimer.Tick += AutosaveTimer_Tick;
        }
        private void AutosaveTimer_Tick(object sender, EventArgs e)
        {
            AutoSaveProgress();
        }
        private void AutoSaveProgress()
        {
            try
            {
                // Save the current progress
                _sectionRepository.SaveProgress(_sectionId, currentTaskIndex, timeElapsed);
                _fileService.SaveFile(_tempFilePath, softwareId);
                Console.WriteLine("Progress auto-saved at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during auto-save: " + ex.Message);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timeElapsed++;
            lblTimer.Text = string.Format(TimerInitialText, timeElapsed);
        }

        private void LoadTask(int taskIndex)
        {
            if (taskIndex < 0 || taskIndex >= tasks.Count)
            {
                MessageBox.Show(ErrorInvalidTaskIndex);
                return;
            }

            var task = tasks[taskIndex];
            label1.Text = task.TaskDescription;
            label2.Text = string.Format(TaskCounterFormat, taskIndex + 1, tasks.Count);
            lblHint.Text = task.Hint;
            txtStartCell.Text = task.From;
            txtEndCell.Text = task.To;
            ddlTestSheet.SelectedItem = task.SheetName;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                _sectionRepository.SaveProgress(_sectionId, currentTaskIndex, timeElapsed);
                MessageBox.Show(MsgProgressSaved, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving progress: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnSubmit_Click(object sender, EventArgs e)
        {
            try
            {
                // Convert the temporary file to Base64
                string studentFileBase64 = _fileService.ConvertFileToBase64(_tempFilePath);

                // Generate the Student JSON file (placeholder logic)
                string studentJsonBase64 = GenerateStudentJson(studentFileBase64);

                // Save both files to the database
                _sectionRepository.SaveStudentFiles(_sectionId, studentFileBase64, studentJsonBase64);

                // Delete the temporary file
                _fileService.DeleteFile(_tempFilePath);

                MessageBox.Show("Files saved successfully.");

                // Notify that the section is completed
                SectionCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GenerateStudentJson(string studentFileBase64)
        {
            // Placeholder logic to generate JSON
            var studentData = new
            {
                FileSize = studentFileBase64.Length,
                FileType = "Student Submission"
            };

            // Serialize to JSON and convert to Base64
            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(studentData);
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonString));
        }


        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private void lblArrow_Click(object sender, System.EventArgs e)
        {
            // Handle lblArrow click event
        }

        private void timer1_Tick(object sender, System.EventArgs e)
        {
            // Handle timer tick event
        }

        private void pictureBox1_Click(object sender, System.EventArgs e)
        {
            // Handle pictureBox1 click event
        }

        private void label1_Click(object sender, System.EventArgs e)
        {
            // Handle label1 click event
        }

        private void groupBox1_Enter(object sender, System.EventArgs e)
        {
            // Handle groupBox1 enter event
        }

        private void Form2_Load(object sender, System.EventArgs e)
        {
            // Handle form load event
        }

        private void ddlTestSheet_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            // Handle ddlTestSheet selected index changed event
        }

        private void button9_Click_1(object sender, System.EventArgs e)
        {
            // Handle button9 click event
        }

        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                // Save the student's progress
                //_sectionRepository.SaveProgress(_sectionId, currentTaskIndex, timeElapsed);
                //var fileService = new FileService();
                //// Save the student's work to the file (optional, if applicable)
                //fileService.SaveFile(tempFilePath, softwareId);
                // Lock the answer (e.g., mark the file as read-only or perform other locking logic)
                //LockAnswer(tempFilePath);

                // Notify the student that the task is finished, answer is locked, and progress is saved
                MessageBox.Show("Your answer has been saved and locked. You can now move to the next question.", "Task Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Optionally, stop any further operations such as auto-save (if implemented)

                // Optionally, close the form or proceed to the next task
                this.Close(); // Close the form after finishing the task (optional)
            }
            catch (Exception ex)
            {
                // Handle errors during the save or lock process
                MessageBox.Show($"Error finishing the task: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        //private void LockAnswer(string filePath)
        //{
        //    try
        //    {
        //        // Open the file with exclusive access
        //        _fileLock = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);

        //        // Notify the user
        //        MessageBox.Show($"The file '{Path.GetFileName(filePath)}' has been locked.", "File Locked", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"An error occurred while locking the file: {ex.Message}");
        //    }
        //}

        //private void UnlockAnswer()
        //{
        //    try
        //    {
        //        // Release the file lock
        //        _fileLock?.Close();
        //        _fileLock = null;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"An error occurred while unlocking the file: {ex.Message}");
        //    }
        //}
        private void button7_Click(object sender, System.EventArgs e)
        {
            // Handle button7 click event
        }

        private void button6_Click(object sender, System.EventArgs e)
        {
            // Handle button6 click event
        }

        private void button5_Click(object sender, System.EventArgs e)
        {
            // Handle button5 click event
        }

        private void button4_Click(object sender, System.EventArgs e)
        {
            // Handle button4 click event
        }

        private void button3_Click(object sender, System.EventArgs e)
        {
            if (currentTaskIndex < tasks.Count - 1)
            {
                currentTaskIndex++;
                LoadTask(currentTaskIndex);
            }
            else
            {
                MessageBox.Show(ErrorNoMoreTasks);
            }
        }

        private void button2_Click(object sender, System.EventArgs e)
        {
            if (currentTaskIndex > 0)
            {
                currentTaskIndex--;
                LoadTask(currentTaskIndex);
            }
            else
            {
                MessageBox.Show(ErrorFirstTask);
            }
        }

        private void button1_Click(object sender, EventArgs e) // Reveal Hint
        {
            MessageBox.Show(tasks[currentTaskIndex].Hint, "Hint");
        }

        private void button11_Click_1(object sender, System.EventArgs e)
        {
            // Handle button11 click event
        }

        private void button10_Click(object sender, System.EventArgs e)
        {
            // Handle button10 click event
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnCheckAnswer.Enabled = true;
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            btnFinish.Enabled = true;
            // Initialize timer for the form
            timer.Start();
            btnStart.Enabled = false;
        }



        private void btnExportImage_Click(object sender, System.EventArgs e)
        {
            // Handle btnExportImage click event
        }

        private void btnCheckAnswer_Click(object sender, EventArgs e)
        {
            try
            {
                // Save the file before validation
                _fileService.SaveFile(_tempFilePath, softwareId);

                // Collect all necessary information from the current task
                var taskSubmission = new TaskSubmission
                {
                    FilePath = _tempFilePath, // Use the existing temporary file path
                    SectionId = tasks[currentTaskIndex].SectionId, // Section ID
                    Task = tasks[currentTaskIndex] // Current task details
                };

                // Create the appropriate validation form based on the software ID
                var validationForm = ValidationFormFactory.CreateValidationForm(taskSubmission);

                // Validate the task
                bool isCorrect = validationForm.ValidateTask(taskSubmission);

                // Provide feedback to the student
                if (isCorrect)
                {
                    MessageBox.Show(MsgTaskCompleted, "Correct Answer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(string.Format(MsgTaskFailedFormat, "Please review your work."), "Incorrect Answer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error validating the answer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}