namespace JobSimulation.Forms
{
    partial class frmSimulationLibrary
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DataGridView dgvSimulations;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.Button btnLogout;
        private System.Windows.Forms.DataGridViewCheckBoxColumn selectionColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn indexColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn simulationColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn nameColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn descriptionColumn;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            dgvSimulations = new DataGridView();
            selectionColumn = new DataGridViewCheckBoxColumn();
            indexColumn = new DataGridViewTextBoxColumn();
            simulationColumn = new DataGridViewTextBoxColumn();
            nameColumn = new DataGridViewTextBoxColumn();
            descriptionColumn = new DataGridViewTextBoxColumn();
            btnNext = new Button();
            btnLogout = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvSimulations).BeginInit();
            SuspendLayout();
            // dgvSimulations
            dgvSimulations.AllowUserToAddRows = false;
            dgvSimulations.AllowUserToDeleteRows = false;
            dgvSimulations.AllowUserToResizeRows = false;
            dgvSimulations.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvSimulations.BackgroundColor = Color.White;
            dgvSimulations.BorderStyle = BorderStyle.None;
            dgvSimulations.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSimulations.EnableHeadersVisualStyles = false;
            dgvSimulations.GridColor = Color.LightGray;
            dgvSimulations.Location = new Point(60, 60);
            dgvSimulations.MultiSelect = false;
            dgvSimulations.Name = "dgvSimulations";
            dgvSimulations.RowHeadersVisible = false;
            dgvSimulations.RowTemplate.Height = 35;
            dgvSimulations.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSimulations.Size = new Size(800, 400);
            dgvSimulations.TabIndex = 0;
            // Set header style
            DataGridViewCellStyle headerStyle = new DataGridViewCellStyle();
            dgvSimulations.ColumnHeadersDefaultCellStyle = headerStyle;
            // Columns
            selectionColumn.HeaderText = "";
            selectionColumn.Name = "selectionColumn";
            selectionColumn.Width = 30;

            indexColumn.HeaderText = "No";
            indexColumn.Name = "indexColumn";
            indexColumn.ReadOnly = true;
            this.indexColumn.Width = 40;

            simulationColumn.HeaderText = "Simulation ID";
            simulationColumn.Name = "simulationColumn";
            simulationColumn.ReadOnly = true;
            simulationColumn.Visible = false;

            nameColumn.HeaderText = "Name";
            nameColumn.Name = "nameColumn";
            nameColumn.ReadOnly = true;

            descriptionColumn.HeaderText = "Description";
            descriptionColumn.Name = "descriptionColumn";
            descriptionColumn.ReadOnly = true;

            dgvSimulations.Columns.AddRange(new DataGridViewColumn[] { selectionColumn, indexColumn, simulationColumn, nameColumn, descriptionColumn });
            dgvSimulations.CellClick += dgvSimulations_CellClick;
            // btnNext
            btnNext.BackColor = Color.FromArgb(72, 133, 237);
            btnNext.FlatStyle = FlatStyle.Flat;
            btnNext.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnNext.ForeColor = Color.White;
            btnNext.Location = new Point(60, 490);
            btnNext.Name = "btnNext";
            btnNext.Size = new Size(240, 50);
            btnNext.TabIndex = 1;
            btnNext.Text = "Next";
            btnNext.UseVisualStyleBackColor = false;
            btnNext.Click += btnNext_Click;
            // btnLogout
            btnLogout.BackColor = Color.FromArgb(219, 68, 55);
            btnLogout.FlatStyle = FlatStyle.Flat;
            btnLogout.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnLogout.ForeColor = Color.White;
            btnLogout.Location = new Point(320, 490);
            btnLogout.Name = "btnLogout";
            btnLogout.Size = new Size(240, 50);
            btnLogout.TabIndex = 2;
            btnLogout.Text = "Logout";
            btnLogout.UseVisualStyleBackColor = false;
            btnLogout.Click += btnLogout_Click;
            // frmSimulationLibrary
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.WhiteSmoke;
            ClientSize = new Size(920, 600);
            Controls.Add(btnLogout);
            Controls.Add(btnNext);
            Controls.Add(dgvSimulations);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmSimulationLibrary";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Simulation Library";
            ((System.ComponentModel.ISupportInitialize)dgvSimulations).EndInit();
            ResumeLayout(false);
            Load += frmSimulationLibrary_Load;
            // Adjust size dynamically
            Resize += frmSimulationLibrary_Resize;
        }

        // Resize event to adjust DataGridView and buttons
        private void frmSimulationLibrary_Resize(object sender, EventArgs e)
        {
            dgvSimulations.Width = this.ClientSize.Width - 120;  // 60 padding on each side
            dgvSimulations.Height = this.ClientSize.Height - 200; // Adjust as per other UI elements

            btnNext.Width = this.ClientSize.Width / 3 - 20;
            btnLogout.Width = this.ClientSize.Width / 3 - 20;
            btnLogout.Location = new Point(btnNext.Width + 40, btnLogout.Location.Y);

            // Force layout update
            this.PerformLayout();
        }



        // Automatically number rows
        private void DgvSimulations_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Check if the click was on the checkbox column
            if (e.ColumnIndex == dgvSimulations.Columns["selectionColumn"].Index && e.RowIndex >= 0)
            {
                // Uncheck all checkboxes first
                foreach (DataGridViewRow row in dgvSimulations.Rows)
                {
                    row.Cells["selectionColumn"].Value = false;
                }

                // Check the clicked checkbox
                dgvSimulations.Rows[e.RowIndex].Cells["selectionColumn"].Value = true;
            }
        }

        // Add this method to your frmSimulationLibrary class
        private void DgvSimulations_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // Example formatting logic (if needed)
            if (e.RowIndex >= 0)
            {
                if (dgvSimulations.Columns[e.ColumnIndex].Name == "indexColumn")
                {
                    // For example: make index column text bold
                    e.CellStyle.Font = new Font(dgvSimulations.DefaultCellStyle.Font, FontStyle.Bold);
                }
            }
        }

    }
}
