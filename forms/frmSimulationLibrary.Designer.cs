namespace JobSimulation.Forms
{
    partial class frmSimulationLibrary
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DataGridView dgvSimulations;
        private System.Windows.Forms.Button btnSelectSimulation;
        private System.Windows.Forms.Button btnLogout;

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
            dgvSimulations = new DataGridView();
            indexColumn = new DataGridViewTextBoxColumn();
            simulationColumn = new DataGridViewTextBoxColumn();
            btnSelectSimulation = new Button();
            btnLogout = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvSimulations).BeginInit();
            SuspendLayout();
            // 
            // dgvSimulations
            // 
            dgvSimulations.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSimulations.Columns.AddRange(new DataGridViewColumn[] { indexColumn, simulationColumn });
            dgvSimulations.Location = new Point(29, 32);
            dgvSimulations.Margin = new Padding(4, 5, 4, 5);
            dgvSimulations.MultiSelect = false;
            dgvSimulations.Name = "dgvSimulations";
            dgvSimulations.RowHeadersWidth = 51;
            dgvSimulations.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSimulations.Size = new Size(871, 453);
            dgvSimulations.TabIndex = 0;
            // 
            // indexColumn
            // 
            indexColumn.MinimumWidth = 6;
            indexColumn.Name = "indexColumn";
            indexColumn.Width = 125;
            // 
            // simulationColumn
            // 
            simulationColumn.MinimumWidth = 6;
            simulationColumn.Name = "simulationColumn";
            simulationColumn.Width = 125;
            // 
            // btnSelectSimulation
            // 
            btnSelectSimulation.BackColor = Color.FromArgb(72, 133, 237);
            btnSelectSimulation.FlatAppearance.BorderSize = 0;
            btnSelectSimulation.FlatAppearance.MouseOverBackColor = Color.FromArgb(66, 122, 218);
            btnSelectSimulation.FlatStyle = FlatStyle.Flat;
            btnSelectSimulation.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSelectSimulation.ForeColor = Color.White;
            btnSelectSimulation.Location = new Point(29, 520);
            btnSelectSimulation.Margin = new Padding(4, 5, 4, 5);
            btnSelectSimulation.Name = "btnSelectSimulation";
            btnSelectSimulation.Size = new Size(293, 62);
            btnSelectSimulation.TabIndex = 1;
            btnSelectSimulation.Text = "Select Simulation";
            btnSelectSimulation.UseVisualStyleBackColor = false;
            btnSelectSimulation.Click += btnSelectSimulation_Click;
            // 
            // btnLogout
            // 
            btnLogout.BackColor = Color.FromArgb(219, 68, 55);
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.FlatAppearance.MouseOverBackColor = Color.FromArgb(197, 61, 50);
            btnLogout.FlatStyle = FlatStyle.Flat;
            btnLogout.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnLogout.ForeColor = Color.White;
            btnLogout.Location = new Point(396, 520);
            btnLogout.Margin = new Padding(4, 5, 4, 5);
            btnLogout.Name = "btnLogout";
            btnLogout.Size = new Size(293, 62);
            btnLogout.TabIndex = 2;
            btnLogout.Text = "Logout";
            btnLogout.UseVisualStyleBackColor = false;
            btnLogout.Click += btnLogout_Click;
            // 
            // frmSimulationLibrary
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1719, 1003);
            Controls.Add(btnLogout);
            Controls.Add(btnSelectSimulation);
            Controls.Add(dgvSimulations);
            Margin = new Padding(4, 5, 4, 5);
            Name = "frmSimulationLibrary";
            Text = "Simulation Library";
            ((System.ComponentModel.ISupportInitialize)dgvSimulations).EndInit();
            ResumeLayout(false);
        }
        private DataGridViewTextBoxColumn indexColumn;
        private DataGridViewTextBoxColumn simulationColumn;
    }
}