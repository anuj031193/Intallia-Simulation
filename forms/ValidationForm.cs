//using System;
//using System.Windows.Forms;
//using JobSimulation.excelApp;
//using JobSimulation.Models;

//namespace JobSimulation.Forms
//{
//    public partial class ValidationForm : Form
//    {
//        private readonly TaskSubmission _taskSubmission;
//        private readonly ExcelValidationService _excelValidationService;

//        // Parameterless constructor (required by the designer)
//        public ValidationForm(TaskSubmission taskSubmission, ExcelValidationService excelValidationService)
//        {
//            InitializeComponent();
//            _taskSubmission = taskSubmission ?? throw new ArgumentNullException(nameof(taskSubmission));
//            _excelValidationService = excelValidationService ?? throw new ArgumentNullException(nameof(excelValidationService));
//        }

//        private void ValidateButton_Click(object sender, EventArgs e)
//        {
//            bool isValid = _excelValidationService.ValidateExcelTask(_taskSubmission);

//            if (isValid)
//            {
//                MessageBox.Show("The task is valid.", "Validation Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
//            }
//            else
//            {
//                MessageBox.Show("The task is invalid.", "Validation Result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//            }
//        }

//        private void ValidationForm_Load(object sender, EventArgs e)
//        {
//            // You can load any necessary details here if needed
//        }
//    }
//}