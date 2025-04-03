namespace JobSimulation.excelApp
{
    partial class ExcelValidationForm
    {
        private System.ComponentModel.IContainer components = null;

        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing && (components != null))
        //    {
        //        components.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}

        private void InitializeComponent()
        {
            this.validateButton = new System.Windows.Forms.Button();
            this.resultTextBox = new System.Windows.Forms.TextBox();
            this.lblCellFrom = new System.Windows.Forms.Label();
            this.lblCellTo = new System.Windows.Forms.Label();
            this.txtStartCell = new System.Windows.Forms.TextBox();
            this.txtEndCell = new System.Windows.Forms.TextBox();
            this.cmbStudentSheetName = new System.Windows.Forms.ComboBox();
            this.lblStudentSheetName = new System.Windows.Forms.Label();
            //this.SuspendLayout();
            // 
            // validateButton
            // 
            this.validateButton.Location = new System.Drawing.Point(16, 50);
            this.validateButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.validateButton.Name = "validateButton";
            this.validateButton.Size = new System.Drawing.Size(100, 28);
            this.validateButton.TabIndex = 1;
            this.validateButton.Text = "Validate";
            this.validateButton.UseVisualStyleBackColor = true;
            //this.validateButton.Click += new System.EventHandler(this.validateButton_Click);
            // 
            // resultTextBox
            // 
            this.resultTextBox.Location = new System.Drawing.Point(16, 86);
            this.resultTextBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.resultTextBox.Multiline = true;
            this.resultTextBox.Name = "resultTextBox";
            this.resultTextBox.Size = new System.Drawing.Size(345, 219);
            this.resultTextBox.TabIndex = 2;
            // 
            // lblCellFrom
            // 
            this.lblCellFrom.AutoSize = true;
            this.lblCellFrom.Location = new System.Drawing.Point(16, 321);
            this.lblCellFrom.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCellFrom.Name = "lblCellFrom";
            this.lblCellFrom.Size = new System.Drawing.Size(67, 16);
            this.lblCellFrom.TabIndex = 3;
            this.lblCellFrom.Text = "Cell From:";
            // 
            // lblCellTo
            // 
            this.lblCellTo.AutoSize = true;
            this.lblCellTo.Location = new System.Drawing.Point(193, 321);
            this.lblCellTo.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCellTo.Name = "lblCellTo";
            this.lblCellTo.Size = new System.Drawing.Size(53, 16);
            this.lblCellTo.TabIndex = 4;
            this.lblCellTo.Text = "Cell To:";
            // 
            // txtStartCell
            // 
            this.txtStartCell.Location = new System.Drawing.Point(113, 318);
            this.txtStartCell.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtStartCell.Name = "txtStartCell";
            this.txtStartCell.Size = new System.Drawing.Size(71, 22);
            this.txtStartCell.TabIndex = 5;
            // 
            // txtEndCell
            // 
            this.txtEndCell.Location = new System.Drawing.Point(265, 318);
            this.txtEndCell.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtEndCell.Name = "txtEndCell";
            this.txtEndCell.Size = new System.Drawing.Size(71, 22);
            this.txtEndCell.TabIndex = 6;
            // 
            // cmbStudentSheetName
            // 
            this.cmbStudentSheetName.FormattingEnabled = true;
            this.cmbStudentSheetName.Location = new System.Drawing.Point(113, 350);
            this.cmbStudentSheetName.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cmbStudentSheetName.Name = "cmbStudentSheetName";
            this.cmbStudentSheetName.Size = new System.Drawing.Size(223, 24);
            this.cmbStudentSheetName.TabIndex = 7;
            // 
            // lblStudentSheetName
            // 
            this.lblStudentSheetName.AutoSize = true;
            this.lblStudentSheetName.Location = new System.Drawing.Point(16, 353);
            this.lblStudentSheetName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStudentSheetName.Name = "lblStudentSheetName";
            this.lblStudentSheetName.Size = new System.Drawing.Size(85, 16);
            this.lblStudentSheetName.TabIndex = 8;
            this.lblStudentSheetName.Text = "Sheet Name:";
            // 
            // ExcelValidationForm
            // 
            //this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            //this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            //this.ClientSize = new System.Drawing.Size(1111, 695);
            //this.Controls.Add(this.lblStudentSheetName);
            //this.Controls.Add(this.cmbStudentSheetName);
            //this.Controls.Add(this.txtEndCell);
            //this.Controls.Add(this.txtStartCell);
            //this.Controls.Add(this.lblCellTo);
            //this.Controls.Add(this.lblCellFrom);
            //this.Controls.Add(this.resultTextBox);
            //this.Controls.Add(this.validateButton);
            //this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            //this.Name = "ExcelValidationForm";
            //this.Text = "Excel Validation";
            //this.ResumeLayout(false);
            //this.PerformLayout();

        }

        private System.Windows.Forms.Button validateButton;
        private System.Windows.Forms.TextBox resultTextBox;
        private System.Windows.Forms.Label lblCellFrom;
        private System.Windows.Forms.Label lblCellTo;
        private System.Windows.Forms.TextBox txtStartCell;
        private System.Windows.Forms.TextBox txtEndCell;
        private System.Windows.Forms.ComboBox cmbStudentSheetName;
        private System.Windows.Forms.Label lblStudentSheetName;
    }
}