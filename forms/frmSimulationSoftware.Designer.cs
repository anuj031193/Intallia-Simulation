using System.Drawing.Drawing2D;

namespace JobSimulation.Forms
{
    partial class frmSimulationSoftware
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmSimulationSoftware));
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            btnPrevious = new Button();
            btnNext = new Button();
            progressBar1 = new ProgressBar();
            button4 = new Button();
            ddlTestSheet = new ComboBox();
            txtTimer = new TextBox();
            label8 = new Label();
            label9 = new Label();
            txtFileName = new TextBox();
            lblTimer = new Label();
            dataGridView1 = new DataGridView();
            dataGridView2 = new DataGridView();
            dataGridView3 = new DataGridView();
            button5 = new Button();
            button6 = new Button();
            button7 = new Button();
            btnCheckAnswer = new Button();
            btnClose = new Button();
            button9 = new Button();
            lblArrow = new Label();
            btnHint = new Button();
            btnStart = new Button();
            StartCell = new TextBox();
            EndCell = new TextBox();
            lblHint = new Label();
            btnSaveandNextSession = new Button();
            btnSaveandPreviousSession = new Button();
            btnSaveandExit = new Button();
            btnCompleteSimulation = new Button();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView3).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.BackColor = Color.FromArgb(224, 224, 224);
            resources.ApplyResources(label1, "label1");
            label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(label2, "label2");
            label2.Name = "label2";
            // 
            // label3
            // 
            resources.ApplyResources(label3, "label3");
            label3.Name = "label3";
            // 
            // btnPrevious
            // 
            btnPrevious.BackColor = Color.Silver;
            resources.ApplyResources(btnPrevious, "btnPrevious");
            btnPrevious.Name = "btnPrevious";
            btnPrevious.UseVisualStyleBackColor = false;
            btnPrevious.Click += btnPrevious_Click;
            // 
            // btnNext
            // 
            btnNext.BackColor = Color.Silver;
            resources.ApplyResources(btnNext, "btnNext");
            btnNext.Name = "btnNext";
            btnNext.UseVisualStyleBackColor = false;
            btnNext.Click += btnNext_Click;
            // 
            // progressBar1
            // 
            resources.ApplyResources(progressBar1, "progressBar1");
            progressBar1.Name = "progressBar1";
            progressBar1.Style = ProgressBarStyle.Continuous;
            // 
            // button4
            // 
            button4.BackColor = Color.Silver;
            resources.ApplyResources(button4, "button4");
            button4.Name = "button4";
            button4.UseVisualStyleBackColor = false;
            // 
            // ddlTestSheet
            // 
            ddlTestSheet.FormattingEnabled = true;
            resources.ApplyResources(ddlTestSheet, "ddlTestSheet");
            ddlTestSheet.Name = "ddlTestSheet";
            // 
            // txtTimer
            // 
            resources.ApplyResources(txtTimer, "txtTimer");
            txtTimer.Name = "txtTimer";
            // 
            // label8
            // 
            resources.ApplyResources(label8, "label8");
            label8.Name = "label8";
            // 
            // label9
            // 
            resources.ApplyResources(label9, "label9");
            label9.Name = "label9";
            // 
            // txtFileName
            // 
            resources.ApplyResources(txtFileName, "txtFileName");
            txtFileName.Name = "txtFileName";
            // 
            // lblTimer
            // 
            resources.ApplyResources(lblTimer, "lblTimer");
            lblTimer.Name = "lblTimer";
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resources.ApplyResources(dataGridView1, "dataGridView1");
            dataGridView1.Name = "dataGridView1";
            // 
            // dataGridView2
            // 
            dataGridView2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resources.ApplyResources(dataGridView2, "dataGridView2");
            dataGridView2.Name = "dataGridView2";
            // 
            // dataGridView3
            // 
            dataGridView3.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resources.ApplyResources(dataGridView3, "dataGridView3");
            dataGridView3.Name = "dataGridView3";
            // 
            // button5
            // 
            button5.BackColor = Color.SteelBlue;
            resources.ApplyResources(button5, "button5");
            button5.Name = "button5";
            button5.UseVisualStyleBackColor = false;
            // 
            // button6
            // 
            button6.BackColor = Color.SteelBlue;
            resources.ApplyResources(button6, "button6");
            button6.Name = "button6";
            button6.UseVisualStyleBackColor = false;
            // 
            // button7
            // 
            button7.BackColor = Color.SteelBlue;
            resources.ApplyResources(button7, "button7");
            button7.Name = "button7";
            button7.UseVisualStyleBackColor = false;
            // 
            // btnCheckAnswer
            // 
            btnCheckAnswer.BackColor = Color.Silver;
            resources.ApplyResources(btnCheckAnswer, "btnCheckAnswer");
            btnCheckAnswer.Name = "btnCheckAnswer";
            btnCheckAnswer.UseVisualStyleBackColor = false;
            btnCheckAnswer.Click += btnCheckAnswer_Click;
            // 
            // btnClose
            // 
            btnClose.BackColor = Color.Silver;
            resources.ApplyResources(btnClose, "btnClose");
            btnClose.Name = "btnClose";
            btnClose.UseVisualStyleBackColor = false;
            btnClose.Click += btnSaveAndExit_Click;
            // 
            // button9
            // 
            button9.BackColor = Color.SteelBlue;
            resources.ApplyResources(button9, "button9");
            button9.Name = "button9";
            button9.UseVisualStyleBackColor = false;
            // 
            // lblArrow
            // 
            resources.ApplyResources(lblArrow, "lblArrow");
            lblArrow.Cursor = Cursors.Hand;
            lblArrow.Name = "lblArrow";
            lblArrow.Click += lblArrow_Click;
            // 
            // btnHint
            // 
            btnHint.BackColor = Color.Silver;
            resources.ApplyResources(btnHint, "btnHint");
            btnHint.Name = "btnHint";
            btnHint.UseVisualStyleBackColor = false;
            btnHint.Click += btnHint_Click;
            // 
            // btnStart
            // 
            btnStart.BackColor = Color.Silver;
            resources.ApplyResources(btnStart, "btnStart");
            btnStart.Name = "btnStart";
            btnStart.UseVisualStyleBackColor = false;
            btnStart.Click += btnStart_Click;
            // 
            // StartCell
            // 
            resources.ApplyResources(StartCell, "StartCell");
            StartCell.Name = "StartCell";
            // 
            // EndCell
            // 
            resources.ApplyResources(EndCell, "EndCell");
            EndCell.Name = "EndCell";
            // 
            // lblHint
            // 
            resources.ApplyResources(lblHint, "lblHint");
            lblHint.Name = "lblHint";
            // 
            // btnSaveandNextSession
            // 
            btnSaveandNextSession.BackColor = Color.Silver;
            resources.ApplyResources(btnSaveandNextSession, "btnSaveandNextSession");
            btnSaveandNextSession.Name = "btnSaveandNextSession";
            btnSaveandNextSession.UseVisualStyleBackColor = false;
            btnSaveandNextSession.Click += btnSaveandNextSession_Click;
            // 
            // btnSaveandPreviousSession
            // 
            btnSaveandPreviousSession.BackColor = Color.Silver;
            resources.ApplyResources(btnSaveandPreviousSession, "btnSaveandPreviousSession");
            btnSaveandPreviousSession.Name = "btnSaveandPreviousSession";
            btnSaveandPreviousSession.UseVisualStyleBackColor = false;
            btnSaveandPreviousSession.Click += btnSaveandPreviousSession_Click;
            // 
            // btnSaveandExit
            // 
            btnSaveandExit.BackColor = Color.Silver;
            resources.ApplyResources(btnSaveandExit, "btnSaveandExit");
            btnSaveandExit.Name = "btnSaveandExit";
            btnSaveandExit.UseVisualStyleBackColor = false;
            btnSaveandExit.Click += btnSaveAndExit_Click;


            //btnCompleteSimulation
            btnCompleteSimulation.BackColor = Color.Silver;
            resources.ApplyResources(btnCompleteSimulation, "btnCompleteSimulation");
            btnCompleteSimulation.Name = "btnCompleteSimulation";
            btnCompleteSimulation.UseVisualStyleBackColor = false;
            btnCompleteSimulation.Click += btnCompleteSimulation_Click;
            // 
            // frmSimulationSoftware
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(224, 224, 224);
            Controls.Add(btnSaveandExit);
            Controls.Add(btnSaveandNextSession);
            Controls.Add(btnSaveandPreviousSession);
            Controls.Add(btnCompleteSimulation);
            Controls.Add(lblHint);
            Controls.Add(EndCell);
            Controls.Add(StartCell);
            Controls.Add(btnStart);
            Controls.Add(lblArrow);
            Controls.Add(button9);
            Controls.Add(btnClose);
            Controls.Add(btnCheckAnswer);
            Controls.Add(button5);
            Controls.Add(button6);
            Controls.Add(button7);
            Controls.Add(dataGridView3);
            Controls.Add(dataGridView2);
            Controls.Add(dataGridView1);
            Controls.Add(lblTimer);
            Controls.Add(txtFileName);
            Controls.Add(label9);
            Controls.Add(label8);
            Controls.Add(txtTimer);
            Controls.Add(ddlTestSheet);
            Controls.Add(button4);
            Controls.Add(progressBar1);
            Controls.Add(btnNext);
            Controls.Add(btnPrevious);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(btnHint);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Name = "frmSimulationSoftware";
            TopMost = true;
            Load += frmSimulationSoftware_Load;
            MouseDown += frmSimulationSoftware_MouseDown;
            MouseMove += frmSimulationSoftware_MouseMove;
            MouseUp += frmSimulationSoftware_MouseUp;
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView2).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView3).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnHint;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.ComboBox ddlTestSheet;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.TextBox txtTimer;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtFileName;
        private System.Windows.Forms.Label lblTimer;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridView dataGridView2;
        private System.Windows.Forms.DataGridView dataGridView3;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Button button7;

       
        private System.Windows.Forms.Button btnCheckAnswer;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button button9;
        private System.Windows.Forms.Label lblArrow;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.TextBox StartCell;
        private System.Windows.Forms.TextBox EndCell;
        private System.Windows.Forms.Label lblHint;
        private System.Windows.Forms.Button btnPrevious;
        private System.Windows.Forms.Button btnSaveandNextSession;
        private System.Windows.Forms.Button btnSaveandPreviousSession;
        private System.Windows.Forms.Button btnSaveandExit;
        private System.Windows.Forms.Button btnCompleteSimulation;


        private bool isDragging = false;
        private Point clickOffset;
        private System.Windows.Forms.Timer slideTimer;
        private bool isAtRightEdge = false;
        private int originalX;
        private int originalY;

        private void SlideTimer_Tick(object sender, EventArgs e)
        {
            if (isAtRightEdge)
            {
                // Move the form back to its original position
                if (this.Left > originalX)
                {
                    this.Left -= 10; // Adjust speed of the slide here
                }
                else
                {
                    slideTimer.Stop(); // Stop the timer once the form reaches the original position
                }
            }
            else
            {
                // Move the form towards the right edge of the screen
                if (this.Left < Screen.PrimaryScreen.WorkingArea.Width - this.Width)
                {
                    this.Left += 10; // Adjust speed of the slide here
                }
                else
                {
                    slideTimer.Stop(); // Stop the timer once the form reaches the right edge
                }
            }
        }
        private void lblArrow_Click(object sender, EventArgs e)
        {
            // Toggle the form's position (right edge or original position)
            isAtRightEdge = !isAtRightEdge;

            // Change the arrow direction
            if (isAtRightEdge)
            {
                lblArrow.Text = ">"; // Change to '>' when at the right edge
            }
            else
            {
                lblArrow.Text = "<"; // Change to '<' when back to original position
            }

            //slideTimer.Start(); // Start the timer to animate the sliding
        }

        private void frmSimulationSoftware_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                clickOffset = e.Location; // Store the location of the mouse click within the form
            }
        }

        private void frmSimulationSoftware_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                this.Location = new Point(this.Location.X + e.X - clickOffset.X, this.Location.Y + e.Y - clickOffset.Y);
            }
        }

        private void frmSimulationSoftware_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false; // Stop dragging when the mouse button is released
        }

        // Hover effects for buttons
        private void InitializeButtonHoverEffects()
        {
            btnCompleteSimulation.MouseEnter += (sender, e) => btnCompleteSimulation.BackColor = Color.DarkGray;
            btnCompleteSimulation.MouseLeave += (sender, e) => btnCompleteSimulation.BackColor = Color.Silver;


            btnSaveandExit.MouseEnter += (sender, e) => btnSaveandExit.BackColor = Color.DarkGray;
            btnSaveandExit.MouseLeave += (sender, e) => btnSaveandExit.BackColor = Color.Silver;

            btnSaveandNextSession.MouseEnter += (sender, e) => btnSaveandNextSession.BackColor = Color.DarkGray;
            btnSaveandNextSession.MouseLeave += (sender, e) => btnSaveandNextSession.BackColor = Color.Silver;


            btnSaveandPreviousSession.MouseEnter += (sender, e) => btnSaveandPreviousSession.BackColor = Color.DarkGray;
            btnSaveandPreviousSession.MouseLeave += (sender, e) => btnSaveandPreviousSession.BackColor = Color.Silver;

            btnStart.MouseEnter += (sender, e) => btnStart.BackColor = Color.DarkGray;
            btnStart.MouseLeave += (sender, e) => btnStart.BackColor = Color.Silver;

            btnPrevious.MouseEnter += (sender, e) => btnPrevious.BackColor = Color.DarkGray;
            btnPrevious.MouseLeave += (sender, e) => btnPrevious.BackColor = Color.Silver;

            btnNext.MouseEnter += (sender, e) => btnNext.BackColor = Color.DarkGray;
            btnNext.MouseLeave += (sender, e) => btnNext.BackColor = Color.Silver;

            btnCheckAnswer.MouseEnter += (sender, e) => btnCheckAnswer.BackColor = Color.DarkGray;
            btnCheckAnswer.MouseLeave += (sender, e) => btnCheckAnswer.BackColor = Color.Silver;
        }

        private void SetDarkMode()
        {
            this.BackColor = Color.FromArgb(30, 30, 30);
            foreach (Control control in this.Controls)
            {
                if (control is Button)
                {
                    control.BackColor = Color.FromArgb(45, 45, 45);
                    control.ForeColor = Color.White;
                }
                else if (control is Label)
                {
                    control.ForeColor = Color.White;
                }
            }
        }

        private void SetLightMode()
        {
            this.BackColor = Color.White;
            foreach (Control control in this.Controls)
            {
                if (control is Button)
                {
                    control.BackColor = Color.Silver;
                    control.ForeColor = Color.Black;
                }
                else if (control is Label)
                {
                    control.ForeColor = Color.Black;
                }
            }
        }

        private void AddTooltips()
        {
            ToolTip toolTip = new ToolTip();

            toolTip.SetToolTip(btnSaveandExit, "Click to save and exit.");
            toolTip.SetToolTip(btnSaveandNextSession, "Click to save and move to the next session.");
            toolTip.SetToolTip(btnSaveandPreviousSession, "Click to save and move to the Previous session.");
            toolTip.SetToolTip(btnCompleteSimulation, "Click to complete the simulation."); 
            toolTip.SetToolTip(btnStart, "Click to start the simulation.");
            toolTip.SetToolTip(btnPrevious, "Click to go to the previous section.");
            toolTip.SetToolTip(btnNext, "Click to go to the next section.");
            toolTip.SetToolTip(btnCheckAnswer, "Click to check the answer.");
            toolTip.SetToolTip(lblArrow, "Click to toggle form position.");
        }


        private void DisableAllButtonsExceptStartAndClose()
        {
            foreach (Control control in this.Controls)
            {
                if (control is Button && control != btnStart && control != btnClose)
                {
                    control.Enabled = false; // Disable all buttons except Start and Close
                }
            }
        }



    }
}