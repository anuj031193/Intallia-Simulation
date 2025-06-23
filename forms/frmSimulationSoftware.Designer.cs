using System.Diagnostics;
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
            label8 = new Label();
            label9 = new Label();
        
            lblTimer = new Label();
            btnCheckAnswer = new Button();
            btnClose = new Button();
            btnHint = new Button();
            btnStart = new Button();
            lblHint = new Label();
            btnSaveandExit = new Button();
            btnSubmit = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.BackColor = SystemColors.Control;
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
            btnPrevious.BackColor = SystemColors.Control;
            resources.ApplyResources(btnPrevious, "btnPrevious");
            btnPrevious.Name = "btnPrevious";
            btnPrevious.UseVisualStyleBackColor = false;
            btnPrevious.Click += btnPrevious_Click;
            // 
            // btnNext
            // 
            btnNext.BackColor = SystemColors.Control;
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
            //progressBar1.Click += progressBar1_Click;
            // 
            // txtTimer
        
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
           
            // lblTimer
            // 
            resources.ApplyResources(lblTimer, "lblTimer");
            lblTimer.Name = "lblTimer";


            // 
            // btnCheckAnswer
            // 
            btnCheckAnswer.BackColor = SystemColors.Control;
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
            // btnHint
            // 
            btnHint.BackColor = SystemColors.Control;
            resources.ApplyResources(btnHint, "btnHint");
            btnHint.Name = "btnHint";
            btnHint.UseVisualStyleBackColor = false;
            btnHint.Click += btnHint_Click;
            // 
            // btnStart
            // 
            btnStart.BackColor = SystemColors.Control;
            resources.ApplyResources(btnStart, "btnStart");
            btnStart.Name = "btnStart";
            btnStart.UseVisualStyleBackColor = false;
            btnStart.Click += btnStart_Click;
            // 
            // lblHint
            // 
            resources.ApplyResources(lblHint, "lblHint");
            lblHint.Name = "lblHint";
            // 
            // btnSaveandExit
            // 
            btnSaveandExit.BackColor = SystemColors.Control;
            resources.ApplyResources(btnSaveandExit, "btnSaveandExit");
            btnSaveandExit.Name = "btnSaveandExit";
            btnSaveandExit.UseVisualStyleBackColor = false;
            btnSaveandExit.Click += btnSaveAndExit_Click;
            // 
            // btnSubmit
            // 
            btnSubmit.BackColor = SystemColors.Control;
            resources.ApplyResources(btnSubmit, "btnSubmit");
            btnSubmit.Name = "btnSubmit";
            btnSubmit.UseVisualStyleBackColor = false;
            btnSubmit.Click += btnSubmit_Click;
            // 
            // frmSimulationSoftware
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            Controls.Add(btnSubmit);
            Controls.Add(btnSaveandExit);
            Controls.Add(lblHint);
            Controls.Add(btnStart);
            Controls.Add(btnClose);
            Controls.Add(btnCheckAnswer);
            Controls.Add(lblTimer);

            Controls.Add(label9);
            Controls.Add(label8);
    
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
            //MouseDown += frmSimulationSoftware_MouseDown;
            //MouseMove += frmSimulationSoftware_MouseMove;
            //MouseUp += frmSimulationSoftware_MouseUp;
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
        //private System.Windows.Forms.Button button4;
  
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
   
        private System.Windows.Forms.Label lblTimer;
 


        private System.Windows.Forms.Button btnCheckAnswer;
        private System.Windows.Forms.Button btnClose;
 
        private System.Windows.Forms.Button btnStart;

        private System.Windows.Forms.Label lblHint;
        private System.Windows.Forms.Button btnPrevious;
        private System.Windows.Forms.Button btnSaveandExit;
        private System.Windows.Forms.Button btnSubmit;



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
                if (this.Left < Screen.PrimaryScreen.WorkingArea.Width - this.Width)
                {
                    this.Left += 10;
                }
                else
                {
                    this.Left = Screen.PrimaryScreen.WorkingArea.Width - this.Width;
                    slideTimer.Stop();
                }
            }
            else
            {
                if (this.Left > originalX)
                {
                    this.Left -= 10;
                }
                else
                {
                    this.Left = originalX;
                    slideTimer.Stop();
                }
            }
        }



        //private void frmSimulationSoftware_MouseDown(object sender, MouseEventArgs e)
        //{
        //    if (e.Button == MouseButtons.Left)
        //    {
        //        isDragging = true;
        //        clickOffset = e.Location; // Store the location of the mouse click within the form
        //    }
        //}

        //private void frmSimulationSoftware_MouseMove(object sender, MouseEventArgs e)
        //{
        //    if (isDragging)
        //    {
        //        this.Location = new Point(this.Location.X + e.X - clickOffset.X, this.Location.Y + e.Y - clickOffset.Y);
        //    }
        //}

        //private void frmSimulationSoftware_MouseUp(object sender, MouseEventArgs e)
        //{
        //    isDragging = false; // Stop dragging when the mouse button is released
        //}

        // Hover effects for buttons
        private void InitializeButtonHoverEffects()
        {
            //btnCompleteSimulation.MouseEnter += (sender, e) => btnCompleteSimulation.BackColor = Color.DarkGray;
            //btnCompleteSimulation.MouseLeave += (sender, e) => btnCompleteSimulation.BackColor = Color.Silver;


            btnSaveandExit.MouseEnter += (sender, e) => btnSaveandExit.BackColor = Color.DarkGray;
            btnSaveandExit.MouseLeave += (sender, e) => btnSaveandExit.BackColor = Color.Silver;

            //btnSaveandNextSession.MouseEnter += (sender, e) => btnSaveandNextSession.BackColor = Color.DarkGray;
            //btnSaveandNextSession.MouseLeave += (sender, e) => btnSaveandNextSession.BackColor = Color.Silver;


            //btnSaveandPreviousSession.MouseEnter += (sender, e) => btnSaveandPreviousSession.BackColor = Color.DarkGray;
            //btnSaveandPreviousSession.MouseLeave += (sender, e) => btnSaveandPreviousSession.BackColor = Color.Silver;

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
            //toolTip.SetToolTip(btnSaveandNextSession, "Click to save and move to the next session.");
            //toolTip.SetToolTip(btnSaveandPreviousSession, "Click to save and move to the Previous session.");
            //toolTip.SetToolTip(btnCompleteSimulation, "Click to complete the simulation."); 
            toolTip.SetToolTip(btnStart, "Click to start the simulation.");
            toolTip.SetToolTip(btnPrevious, "Click to go to the previous section.");
            toolTip.SetToolTip(btnNext, "Click to go to the next section.");
            toolTip.SetToolTip(btnCheckAnswer, "Click to check the answer.");
            //toolTip.SetToolTip(lblArrow, "Click to toggle form position.");
        }




        // --- Shape Generation Methods (Copied from ToggleDemo) ---
        private void UpdateFormRegionBasedOnState()
        {
            if (this.ClientRectangle.Width <= 0 || this.ClientRectangle.Height <= 0 || this.WindowState == FormWindowState.Minimized)
            {
                try { this.Region?.Dispose(); this.Region = null; } catch { }
                return;
            }

            Region oldRegion = this.Region;
            Region newRegion = null;

            try
            {
                // Recalculate button Y in case of resize events in the future
                int buttonY = (this.ClientRectangle.Height / 2) - (toggleButtonHeight / 2);
                // Use the *current* width for the toggle area calculation during transitions
                Rectangle currentToggleButtonArea = new Rectangle(0, buttonY, formWidthCollapsed, toggleButtonHeight);

                if (isFormCollapsed && this.Width <= formWidthCollapsed) // Strictly check collapsed state AND width
                {
                    // Collapsed state: Only the left-rounded toggle button area visible
                    if (currentToggleButtonArea.Width > 0 && currentToggleButtonArea.Height > 0)
                    {
                        using (GraphicsPath togglePath = GetLeftRoundedRectPath(currentToggleButtonArea, formCornerRadius))
                        {
                            newRegion = new Region(togglePath);
                        }
                    }
                }
                else // Expanded State or Transitioning to Expanded
                {
                    using (GraphicsPath combinedPath = new GraphicsPath())
                    {
                        // 1. Always include the Toggle Button Area (Left Rounded)
                        if (currentToggleButtonArea.Width > 0 && currentToggleButtonArea.Height > 0)
                        {
                            using (GraphicsPath togglePath = GetLeftRoundedRectPath(currentToggleButtonArea, formCornerRadius))
                            {
                                combinedPath.AddPath(togglePath, false);
                            }
                        }

                        // 2. Main Form Body Area (Left Rounded where it meets toggle, Sharp Right)
                        // Calculate based on the *current* form width during animation
                        Rectangle mainFormRect = new Rectangle(
                            formWidthCollapsed, // Starts after the toggle button's width
                            0,
                            this.ClientRectangle.Width - formWidthCollapsed, // Width is the remainder
                            this.ClientRectangle.Height);

                        // 3. Add the main body path if it has valid dimensions
                        if (mainFormRect.Width > 0 && mainFormRect.Height > 0)
                        {
                            using (GraphicsPath mainBodyPath = GetMainBodyLeftRoundedPath(mainFormRect, formCornerRadius))
                            {
                                combinedPath.AddPath(mainBodyPath, false);
                            }
                        }

                        // Create region from the combined path if it contains points
                        if (combinedPath.PointCount > 0)
                        {
                            newRegion = new Region(combinedPath);
                        }
                        else if (newRegion == null) // Fallback if combined path is empty somehow
                        {
                            // If only toggle path was added previously, use that
                            // This case is less likely with the current logic but safe to have
                            using (GraphicsPath togglePath = GetLeftRoundedRectPath(currentToggleButtonArea, formCornerRadius))
                            {
                                if (togglePath.PointCount > 0) newRegion = new Region(togglePath);
                            }
                        }
                    } // Dispose combinedPath
                }

                // Assign the final region to the form
                this.Region = newRegion;
                newRegion = null; // Prevent accidental disposal below
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating form region: {ex.Message}");
                try { this.Region = null; } catch { } // Fallback to default rectangle region
            }
            finally
            {
                // Dispose the old region ONLY if it's different from the new one
                if (oldRegion != this.Region)
                {
                    oldRegion?.Dispose();
                }
                newRegion?.Dispose(); // Should be null, but dispose just in case
            }
            this.Invalidate(); // Force the form to repaint with the new shape
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0 || rect.Width <= 0 || rect.Height <= 0) { path.AddRectangle(rect); return path; }
            int diameter = radius * 2;
            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;
            if (diameter <= 0) { path.AddRectangle(rect); return path; }

            Rectangle arcRect = new Rectangle(rect.Location, new Size(diameter, diameter));
            path.AddArc(arcRect, 180, 90); // Top-Left
            arcRect.X = rect.Right - diameter;
            path.AddArc(arcRect, 270, 90); // Top-Right
            arcRect.Y = rect.Bottom - diameter;
            path.AddArc(arcRect, 0, 90);   // Bottom-Right
            arcRect.X = rect.Left;
            path.AddArc(arcRect, 90, 90);  // Bottom-Left
            path.CloseFigure();
            return path;
        }

        private GraphicsPath GetLeftRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0 || rect.Width <= 0 || rect.Height <= 0) { path.AddRectangle(rect); return path; }
            int diameter = radius * 2;
            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;
            if (diameter <= 0) { path.AddRectangle(rect); return path; }

            Rectangle arcRect = new Rectangle(rect.Location, new Size(diameter, diameter));
            path.AddArc(arcRect, 180, 90); // Top-Left Arc
            // Draw lines for sharp right side
            path.AddLine(rect.X + radius, rect.Y, rect.Right, rect.Y); // Top Line (Sharp Right)
            path.AddLine(rect.Right, rect.Y, rect.Right, rect.Bottom); // Right Line (Sharp Down)
            arcRect.Y = rect.Bottom - diameter; // Position for bottom-left arc
            path.AddLine(rect.Right, rect.Bottom, rect.X + radius, rect.Bottom); // Bottom Line (Sharp Left Start)
            path.AddArc(arcRect, 90, 90); // Bottom-Left Arc
            path.CloseFigure(); // Connects bottom-left arc end to top-left arc start (Left Line)
            return path;
        }

        // Generates path for the main body with ONLY the LEFT corners rounded
        private GraphicsPath GetMainBodyLeftRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0 || rect.Width <= 0 || rect.Height <= 0) { path.AddRectangle(rect); return path; }
            int diameter = radius * 2;
            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;
            if (diameter <= 0) { path.AddRectangle(rect); return path; }

            Rectangle arcRect = new Rectangle(rect.Location, new Size(diameter, diameter));

            // Define the Path (Sharp right, rounded left)
            path.AddArc(arcRect, 180, 90); // Top-Left Arc
            path.AddLine(rect.X + radius, rect.Y, rect.Right, rect.Y); // Top Line (Sharp Right)
            path.AddLine(rect.Right, rect.Y, rect.Right, rect.Bottom); // Right Line (Sharp Down)
            arcRect.Y = rect.Bottom - diameter;
            path.AddLine(rect.Right, rect.Bottom, rect.X + radius, rect.Bottom); // Bottom Line (Sharp Left Start)
            path.AddArc(arcRect, 90, 90);  // Bottom-Left Arc
            path.CloseFigure(); // Closes the path (Left Line)

            return path;
        }

        // --- Control Styling Method ---
        private void ApplyRoundedCorners(Control ctl, int cornerRadius)
        {
            if (ctl == null || ctl.IsDisposed || ctl.Width <= 0 || ctl.Height <= 0) return;

            if (ctl is Label lbl && lbl.AutoSize)
            {
                // Debug.WriteLine($"Warning: AutoSize for Label '{lbl.Name}' is true. Setting to false for Region rounding.");
                lbl.AutoSize = false; // Required for Region to work reliably on Labels
            }

            // Make Buttons Flat for better look with Region
            if (ctl is Button btn)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
            }

            try
            {
                Region oldRegion = ctl.Region;
                using (GraphicsPath path = GetRoundedRectPath(ctl.ClientRectangle, cornerRadius))
                {
                    ctl.Region = new Region(path);
                }
                oldRegion?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying region to {ctl?.Name ?? "Unknown Control"}: {ex.Message}");
                try { ctl?.Region?.Dispose(); ctl.Region = null; } catch { }
            }
            ctl.Invalidate();
        }

        // Helper to apply rounding to common controls
        private void ApplyRoundedCornersToControls()
        {
            int controlCornerRadius = 8; // Adjust as needed for buttons etc.

            foreach (Control control in this.Controls)
            {
                // Apply to Buttons (except maybe the toggle button if it were a real button)
                if (control is Button btn)
                {
                    ApplyRoundedCorners(btn, controlCornerRadius);
                }
                // Apply to Labels if desired (make sure AutoSize is false)
                else if (control is Label lbl)
                {
                    // Only round specific labels if needed, ensure AutoSize=false
                    if (lbl == label1 || lbl == label2 || lbl == lblTimer)
                    {
                        ApplyRoundedCorners(lbl, controlCornerRadius);
                    }
                }
                // Apply to ProgressBar if you have one
                else if (control is ProgressBar pBar && pBar.Name == "progressBar1")
                {
                    ApplyRoundedCorners(pBar, controlCornerRadius);
                }
                // Add other control types as needed
            }
        }


        // --- Form Shape Update (Region Logic) ---


        // --- Custom Form Painting ---
        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized || this.ClientRectangle.Width <= 0 || this.ClientRectangle.Height <= 0)
            {
                base.OnPaint(e); // Let base handle minimized state
                return;
            }
            if (lblTimer != null && lblTimer.Visible)
            {
                lblTimer.BringToFront();
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // --- Step 0: Draw Background Image FIRST ---
            if (this.BackgroundImage != null)
            {
                // Option A: Tile the image (if BackgroundImageLayout is Tile)
                using (TextureBrush tBrush = new TextureBrush(this.BackgroundImage, WrapMode.Tile))
                {
                    // Fill the entire client rectangle area
                    e.Graphics.FillRectangle(tBrush, this.ClientRectangle);

                    // OR: Fill only within the calculated form region if needed (more complex)
                    // if (this.Region != null) {
                    //    e.Graphics.FillRegion(tBrush, this.Region);
                    // } else {
                    //    e.Graphics.FillRectangle(tBrush, this.ClientRectangle);
                    // }
                }

                // Option B: Draw the image according to BackgroundImageLayout (Stretch, Center, Zoom, None)
                // This requires more complex logic to replicate the layout styles.
                // Simple example for stretching (ignores actual layout property):
                // e.Graphics.DrawImage(this.BackgroundImage, this.ClientRectangle);

                // Option C: Just draw it at 0,0 without scaling/tiling (if layout is None/TopLeft)
                // e.Graphics.DrawImageUnscaled(this.BackgroundImage, Point.Empty);
            }
            else
            {
                // Fallback if no background image: Clear with BackColor
                e.Graphics.Clear(this.BackColor);
            }
            // --- End Background Image ---


            // --- Define toggle button area ---
            int buttonY = (this.ClientRectangle.Height / 2) - (toggleButtonHeight / 2);
            Rectangle currentToggleButtonArea = new Rectangle(0, buttonY, formWidthCollapsed, toggleButtonHeight);
            int toggleRadius = formCornerRadius;

            // --- Step 1: Draw Toggle Button Visuals (Always Visible Part) ---
            // This draws ON TOP of the background image now
            if (currentToggleButtonArea.Width > 0 && currentToggleButtonArea.Height > 0)
            {
                using (GraphicsPath toggleVisualPath = GetLeftRoundedRectPath(currentToggleButtonArea, toggleRadius))
                {
                    // Background Gradient for the toggle button
                    Color color1 = ColorTranslator.FromHtml("#0DAFDC");
                    Color color2 = ColorTranslator.FromHtml("#22E9A2");
                    using (LinearGradientBrush gradBrush = new LinearGradientBrush(currentToggleButtonArea, color1, color2, LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillPath(gradBrush, toggleVisualPath);
                    }

                    // Arrow Symbol (< or >)
                    using (SolidBrush arrowBrush = new SolidBrush(Color.White))
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    using (Font arrowFont = new Font("Segoe UI Symbol", 14, FontStyle.Bold))
                    {
                        string arrow = isFormCollapsed ? ">" : "<";
                        e.Graphics.DrawString(arrow, arrowFont, arrowBrush, currentToggleButtonArea, sf);
                    }
                }
            }
        }



        //    // *** DO NOT CALL base.OnPaint(e) ***
        //    // We are fully handling the painting.
        //}


            // --- Form State and Animation ---
        private void ToggleFormState()
        {
            var screen = Screen.PrimaryScreen.WorkingArea;
            bool collapsing = !isFormCollapsed; // Are we starting the collapse animation?

            // Calculate target X based on the *intended* final state
            targetX = isFormCollapsed
                ? screen.Right - this.formWidthExpanded  // Target is expanded position
                : screen.Right - this.formWidthCollapsed; // Target is collapsed position

            // --- Control Visibility Handling ---
            if (collapsing)
            {
                // Hide controls *immediately* when starting to collapse for a cleaner look
                //SetSimulationControlsVisibility(false);
            }
            // Controls will be shown again in MoveTimer_Tick when expansion finishes.

            // Start the animation timer if not already running and not already at target
            if (this.Location.X != targetX && !moveTimer.Enabled)
            {
                moveTimer.Start();
            }
            else if (this.Location.X == targetX) // If already at the target (e.g., rapid clicks)
            {
                // Ensure state and visibility are correct even if timer doesn't run
                bool intendedCollapseState = (targetX == screen.Right - formWidthCollapsed);
                if (isFormCollapsed != intendedCollapseState)
                {
                    isFormCollapsed = intendedCollapseState;
                    UpdateFormRegionBasedOnState(); // Update shape
                    // Set visibility based on the final state
                    //SetSimulationControlsVisibility(!isFormCollapsed);
                    Debug.WriteLine($"Form already at target. State forced to: {isFormCollapsed}");
                    this.Invalidate(); // Redraw
                }
            }
        }

        private void MoveTimer_Tick(object sender, EventArgs e)
        {
            int stepSize = 45; // Animation step size (pixels per interval) - adjust for speed
            var screen = Screen.PrimaryScreen.WorkingArea;
            int collapsedTargetX = screen.Right - this.formWidthCollapsed;
            int expandedTargetX = screen.Right - this.formWidthExpanded;

            bool targetReached = false;
            int currentX = this.Location.X;
            int newX = currentX;

            // Calculate movement
            if (currentX > targetX) // Moving left (Expanding or Correcting)
            {
                newX = Math.Max(targetX, currentX - stepSize);
            }
            else if (currentX < targetX) // Moving right (Collapsing or Correcting)
            {
                newX = Math.Min(targetX, currentX + stepSize);
            }

            // Apply movement and update Width
            if (newX != currentX)
            {
                // Calculate new width based on how far we are from the right edge
                int newWidth = screen.Right - newX;
                // Ensure width doesn't go below collapsed width during animation
                this.Width = Math.Max(formWidthCollapsed, newWidth);
                this.Location = new Point(newX, this.Location.Y);

                // Update region *during* animation for smooth visual change
                UpdateFormRegionBasedOnState();

                if (newX == targetX)
                {
                    targetReached = true;
                }
            }
            else // Already at target X
            {
                targetReached = true;
            }

            // Actions when timer stops (target reached)
            if (targetReached && moveTimer.Enabled)
            {
                moveTimer.Stop();

                // Set final state based on the location we arrived at
                isFormCollapsed = (this.Location.X == collapsedTargetX);

                // Ensure final Width and Region are correct
                this.Width = isFormCollapsed ? formWidthCollapsed : formWidthExpanded;
                UpdateFormRegionBasedOnState();

                // Set final visibility of controls
                //SetSimulationControlsVisibility(!isFormCollapsed);

                Debug.WriteLine($"Timer stopped. Arrived at X: {this.Location.X}. isFormCollapsed set to: {isFormCollapsed}");

                // Final redraw
                this.Invalidate();
            }
        }

        // --- Mouse Interaction for Toggle ---
        protected override void OnMouseDown(MouseEventArgs e)
        {
            // Recalculate toggle area bounds based on current state (important!)
            int buttonY = (this.ClientRectangle.Height / 2) - (toggleButtonHeight / 2);
            // The clickable area is always the leftmost part, with collapsedWidth
            Rectangle currentClickableToggleArea = new Rectangle(0, buttonY, formWidthCollapsed, toggleButtonHeight);

            // Check if click is within the physical toggle button area
            if (currentClickableToggleArea.Contains(e.Location))
            {
                // Use BeginInvoke to ensure any pending UI updates complete before starting animation
                this.BeginInvoke(new Action(() =>
                {
                    ToggleFormState(); // Initiate slide/toggle
                }));
            }
            else
            {
                // If the click is outside the toggle area AND the form is expanded,
                // allow standard interactions (form drag, control clicks).
                if (!isFormCollapsed)
                {
                    base.OnMouseDown(e);
                }
                // If collapsed, clicks outside the toggle area do nothing.
            }
        }
    }
}

