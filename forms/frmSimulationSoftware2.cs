//using JobSimulation.DAL;
//using JobSimulation.Models;
//using JobSimulation.BLL;
//using System;
//using System.Collections.Generic;
//using System.Windows.Forms;
//using System.Drawing;
//using Timer = System.Windows.Forms.Timer;

//namespace JobSimulation
//{
//    public partial class frmSimulationSoftware : Form
//    {
//        private int _expandedWidth;
//        private const int MinimizedWidth = 150;
//        private bool _isMinimized;
//        private Point _originalLocation;
//        private int currentTaskIndex = 0;
//        private Timer timer;
//        private int timeElapsed = 0;
//        private readonly List<JobTask> tasks;
//        private readonly string softwareId;
//        private readonly string _filePath;
//        private readonly string _sectionId;
//        private readonly string _simulationId;
//        private readonly string _userId;
//        private readonly SectionRepository _sectionRepository;
//        private readonly FileService _fileService;
//        private readonly SkillMatrixRepository _skillMatrixRepository;
//        private readonly ActivityRepository _activityRepository;
//        private Timer autosaveTimer;
//        public event EventHandler SectionCompleted;
//        private Dictionary<int, int> taskTimeDictionary = new Dictionary<int, int>();

//        private readonly string TaskDescriptionLabelText = "Task Description";
//        private readonly string TimerInitialText = "Time: {0} sec";
//        private readonly string TaskCounterFormat = "Task {0} of {1}";
//        private readonly string ErrorInvalidTaskIndex = "Invalid task index.";
//        private readonly string ErrorNoMoreTasks = "No more tasks available.";
//        private readonly string ErrorFirstTask = "This is the first task.";
//        private readonly string MsgTaskCompleted = "Task completed successfully.";
//        private readonly string MsgTaskFailedFormat = "Task validation failed: {0}";
//        private readonly string MsgProgressSaved = "Progress saved successfully.";

//        public frmSimulationSoftware(List<JobTask> tasks, string filePath, string sectionId, string simulationId, string userId,
//                                     SectionRepository sectionRepository, FileService fileService, SkillMatrixRepository skillMatrixRepository,
//                                     ActivityRepository activityRepository, string softwareId)
//        {
//            InitializeComponent();
//            _expandedWidth = this.Width;
//            _originalLocation = this.Location;

//            PositionFormRightEdge();
//            this.buttonMinimize.BringToFront();
//            this.DoubleBuffered = true;

//            this.tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
//            this._filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
//            this.softwareId = softwareId ?? throw new ArgumentNullException(nameof(softwareId));
//            this._sectionId = sectionId ?? throw new ArgumentNullException(nameof(sectionId));
//            this._simulationId = simulationId ?? throw new ArgumentNullException(nameof(simulationId));
//            this._userId = userId ?? throw new ArgumentNullException(nameof(userId));
//            this._sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
//            this._fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
//            this._skillMatrixRepository = skillMatrixRepository ?? throw new ArgumentNullException(nameof(skillMatrixRepository));
//            this._activityRepository = activityRepository ?? throw new ArgumentNullException(nameof(activityRepository));
//            InitializeAutosaveTimer();
//            InitializeUI();
//            InitializeTimer();
//            LoadTask(currentTaskIndex);
//        }

//        private void InitializeUI()
//        {
//            labelTaskDescription.Text = TaskDescriptionLabelText;
//            labelTaskCounter.Text = string.Format(TaskCounterFormat, currentTaskIndex + 1, tasks.Count);
//            var task = tasks[currentTaskIndex];
//            dynamic details = task.Details;
//            labelHintText.Text = details?.Hint ?? task.Hint;
//            textBoxStartCell.Text = details?.From ?? task.From;
//            textBoxEndCell.Text = details?.To ?? task.To;
//            comboBoxSheet.SelectedItem = details?.SheetName ?? task.SheetName;
//            labelTimer.Text = string.Format(TimerInitialText, timeElapsed);
//        }

//        private Point dragStartPoint;

//        private void buttonMinimize_Click(object sender, EventArgs e)
//        {
//            var screenWorkingArea = Screen.PrimaryScreen.WorkingArea;

//            if (_isMinimized)
//            {
//                this.Width = _expandedWidth;
//                this.Location = new Point(screenWorkingArea.Right - _expandedWidth, this.Location.Y);
//                buttonMinimize.Text = "⏩";
//            }
//            else
//            {
//                this.Width = MinimizedWidth;
//                this.Location = new Point(screenWorkingArea.Right - MinimizedWidth, this.Location.Y);
//                buttonMinimize.Text = "⏪";
//            }

//            _isMinimized = !_isMinimized;
//            ToggleControlsVisibility(!_isMinimized);
//        }

//        private void PositionFormRightEdge()
//        {
//            int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
//            this.Location = new Point(
//                screenWidth - _expandedWidth - 10,
//                (Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 2
//            );
//        }

//        private void ToggleControlsVisibility(bool visible)
//        {
//            dataGridView1.Visible = visible;
//            dataGridView2.Visible = visible;
//            dataGridView3.Visible = visible;
//            labelTaskDescription.Visible = visible;
//        }

//        private void InitializeTimer()
//        {
//            timer = new Timer();
//            timer.Interval = 1000;
//            timer.Tick += Timer_Tick;
//        }

//        private void InitializeAutosaveTimer()
//        {
//            autosaveTimer = new Timer();
//            autosaveTimer.Interval = 60000;
//            autosaveTimer.Tick += AutosaveTimer_Tick;
//        }

//        private void AutosaveTimer_Tick(object sender, EventArgs e)
//        {
//            AutoSaveProgress();
//        }

//        private void AutoSaveProgress()
//        {
//            try
//            {
//                _sectionRepository.SaveProgress(_sectionId, currentTaskIndex, timeElapsed);
//                _fileService.SaveFile(_filePath, softwareId);
//                Console.WriteLine("Progress auto-saved at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Error during auto-save: " + ex.Message);
//            }
//        }

//        private void Timer_Tick(object sender, EventArgs e)
//        {
//            timeElapsed++;
//            labelTimer.Text = string.Format(TimerInitialText, timeElapsed);
//            if (taskTimeDictionary.ContainsKey(currentTaskIndex))
//            {
//                taskTimeDictionary[currentTaskIndex] = taskTimeDictionary[currentTaskIndex] + 1;
//            }
//            else
//            {
//                taskTimeDictionary[currentTaskIndex] = 1;
//            }
//        }

//        private void LoadTask(int taskIndex)
//        {
//            if (taskIndex < 0 || taskIndex >= tasks.Count)
//            {
//                MessageBox.Show(ErrorInvalidTaskIndex);
//                return;
//            }

//            var task = tasks[taskIndex];
//            dynamic details = task.Details;
//            labelTaskDescription.Text = details?.TaskDescription ?? task.Description;
//            labelTaskCounter.Text = string.Format(TaskCounterFormat, taskIndex + 1, tasks.Count);
//            labelHintText.Text = details?.Hint ?? task.Hint;
//            textBoxStartCell.Text = details?.From ?? task.From;
//            textBoxEndCell.Text = details?.To ?? task.To;
//            comboBoxSheet.SelectedItem = details?.SheetName ?? task.SheetName;
//        }

//        private void buttonSave_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                SaveProgress(currentTaskIndex);
//                MessageBox.Show(MsgProgressSaved, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error saving progress: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }
//        }

//        private void buttonSubmit_Click(object sender, EventArgs e)
//        {
//            SaveAndFinishSection();
//        }

//        private void buttonClose_Click(object sender, EventArgs e)
//        {
//            this.Close();
//        }

//        private void labelArrow_Click(object sender, EventArgs e)
//        {
//        }

//        private void timer1_Tick(object sender, EventArgs e)
//        {
//        }

//        private void pictureBoxInfo_Click(object sender, EventArgs e)
//        {
//        }

//        private void labelTaskDescription_Click(object sender, EventArgs e)
//        {
//        }

//        private void groupBoxInfo_Enter(object sender, EventArgs e)
//        {
//        }

//        private void Form2_Load(object sender, EventArgs e)
//        {
//        }

//        private void comboBoxSheet_SelectedIndexChanged(object sender, EventArgs e)
//        {
//        }

//        private void buttonShowInfo_Click(object sender, EventArgs e)
//        {
//        }

//        private void button8_Click(object sender, EventArgs e)
//        {
//            SaveAndFinishSection();
//        }

//        private void SaveAndFinishSection()
//        {
//            try
//            {
//                _fileService.SaveFile(_filePath, softwareId);
//                string base64File = _fileService.ConvertFileToBase64(_filePath);
//                _fileService.SaveStudentFileToDatabase(_sectionId, _userId, base64File);

//                SaveToSkillMatrix();

//                MessageBox.Show("Section completed and file saved successfully.");

//                SectionCompleted?.Invoke(this, EventArgs.Empty);

//                autosaveTimer.Stop();
//                this.Close();
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error finishing the section: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }
//        }

//        private void SaveToSkillMatrix()
//        {
//            try
//            {
//                foreach (var task in tasks)
//                {
//                    dynamic details = task.Details;
//                    var skillMatrix = new SkillMatrix
//                    {
//                        ActivityId = Guid.NewGuid().ToString(),
//                        TaskId = task.TaskId,
//                        HintsChecked = details?.HintsChecked ?? 0,
//                        TotalTime = taskTimeDictionary.ContainsKey(currentTaskIndex) ? taskTimeDictionary[currentTaskIndex] : 0,
//                        AttemptstoSolve = details?.AttemptstoSolve ?? 0,
//                        Status = "Completed",
//                        CreateBy = _userId,
//                        CreateDate = DateTime.UtcNow,
//                        ModifyBy = _userId,
//                        ModifyDate = DateTime.UtcNow,
//                        Attempt = details?.Attempt ?? 0
//                    };

//                    _sectionRepository.SaveSkillMatrix(skillMatrix);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error saving to SkillMatrix: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }
//        }
//        private void SaveProgress(int taskIndex)
//        {
//            try
//            {
//                _sectionRepository.SaveProgress(_sectionId, taskIndex, timeElapsed);
//                _fileService.SaveFile(_filePath, softwareId);
//                SaveToSkillMatrix();
//                Console.WriteLine("Progress saved at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Error during save: " + ex.Message);
//            }
//        }

//        private void button5_Click(object sender, EventArgs e)
//        {
//        }

//        private void button6_Click(object sender, EventArgs e)
//        {
//        }

//        private void button7_Click(object sender, EventArgs e)
//        {
//        }

//        private void button4_Click(object sender, EventArgs e)
//        {
//        }

//        private void buttonNext_Click(object sender, EventArgs e)
//        {
//            if (currentTaskIndex < tasks.Count - 1)
//            {
//                currentTaskIndex++;
//                LoadTask(currentTaskIndex);
//            }
//            else
//            {
//                MessageBox.Show(ErrorNoMoreTasks);
//            }
//        }

//        private void buttonPrevious_Click(object sender, EventArgs e)
//        {
//            if (currentTaskIndex > 0)
//            {
//                currentTaskIndex--;
//                LoadTask(currentTaskIndex);
//            }
//            else
//            {
//                MessageBox.Show(ErrorFirstTask);
//            }
//        }

//        private void buttonHint_Click(object sender, EventArgs e)
//        {
//            MessageBox.Show(tasks[currentTaskIndex].Hint, "Hint");
//        }

//        private void button10_Click(object sender, EventArgs e)
//        {
//        }

//        private void buttonStart_Click(object sender, EventArgs e)
//        {
//            buttonCheckAnswer.Enabled = true;
//            buttonHint.Enabled = true;
//            buttonPrevious.Enabled = true;
//            buttonNext.Enabled = true;
//            buttonSubmit.Enabled = true;
//            autosaveTimer.Start();
//            timer.Start();
//            buttonStart.Enabled = false;
//        }

//        private void buttonExportImage_Click(object sender, EventArgs e)
//        {
//        }

//        private void buttonCheckAnswer_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                _fileService.SaveFile(_filePath, softwareId);

//                var taskSubmission = new TaskSubmission
//                {
//                    FilePath = _filePath,
//                    SoftwareId = softwareId,
//                    SimulationId = _simulationId,
//                    SectionId = _sectionId,
//                    Task = tasks[currentTaskIndex]
//                };

//                var validationForm = ValidationFormFactory.CreateValidationForm(taskSubmission);

//                bool isCorrect = validationForm.ValidateTask(taskSubmission);

//                if (isCorrect)
//                {
//                    MessageBox.Show(MsgTaskCompleted, "Correct Answer", MessageBoxButtons.OK, MessageBoxIcon.Information);
//                }
//                else
//                {
//                    MessageBox.Show(string.Format(MsgTaskFailedFormat, "Please review your work."), "Incorrect Answer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error validating the answer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }
//        }

//        private void buttonNextTask_Click(object sender, EventArgs e)
//        {
//            buttonNext_Click(sender, e);
//        }

//        private void buttonPreviousTask_Click(object sender, EventArgs e)
//        {
//            buttonPrevious_Click(sender, e);
//        }

//        private void lblArrow_Click(object sender, EventArgs e)
//        {
//            if (lblArrow.Text == ">")
//            {
//                this.SetDesktopLocation(1300, 0);
//                lblArrow.Text = "<";

//            }
//            else
//            {
//                this.SetDesktopLocation(1100, 0);
//                lblArrow.Text = ">";

//            }


//        }
//        private void btnStart_Click(object sender, EventArgs e)
//        {
//            btnCheckAnswer.Enabled = true;
//            button1.Enabled = true;
//            button2.Enabled = true;
//            button3.Enabled = true;
//            button8.Enabled = true;
 
//            //  GetData(intCaseStudyId);
//            //loadFiles();
         
//            val = 0;
//            a = val++;
//            lblTimer.Text = a.ToString();
//            timer1.Start();
//            btnStart.Enabled = false;
//        }

//        private void button1_Click(object sender, EventArgs e)
//        {
//            if (GlobalVar.intOpen == 1)
//            {
//                conn.Open();
//                string selectquery = "SELECT * FROM Task where QNo = " + GlobalVar.QNo + " and FK_CaseStudyId=" + GlobalVar.CaseStudyId + "";
//                SqlCommand cmd = new SqlCommand(selectquery, conn);
//                SqlDataReader reader1;
//                reader1 = cmd.ExecuteReader();

//                if (reader1.Read())
//                {

//                    //    Form3 frm3 = new Form3();

//                    label3.Visible = false;
//                    label3.Text = reader1.GetValue(6).ToString();
//                    GlobalVar.QLink = reader1.GetValue(12).ToString();
//                    GlobalVar.Qhint = reader1.GetValue(7).ToString();
//                    GlobalVar.frm3 = new Form3();
//                    GlobalVar.frm3.Show();//   frm3.Show();
//                }
//                else
//                {
//                    MessageBox.Show("NO DATA FOUND");
//                }
//                conn.Close();
//                GlobalVar.intOpen = GlobalVar.intOpen + 1;
//            }
//        }


//        private void button9_Click_1(object sender, EventArgs e)
//        {
//            MsgBox msg = new MsgBox();
//            msg.ShowDialog();
//        }
//    }
//}