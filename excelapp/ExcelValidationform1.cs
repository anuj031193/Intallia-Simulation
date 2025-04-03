//using System;
//using System.Data;
//using System.IO;
//using System.Linq;
//using System.Text.RegularExpressions;
//using System.Windows.Forms;
//using Newtonsoft.Json;
//using DocumentFormat.OpenXml.Packaging;
//using DocumentFormat.OpenXml.Spreadsheet;
//using JobSimulation.shared.services;
//using DocumentFormat.OpenXml;
//using JobSimulation.shared.model;
//using System.Collections.Generic;

//using shared.model.data;

//namespace JobSimulation.excelApp
//{
//    public partial class ExcelValidationForm : Form
//    {
//        private readonly string filePath;
//        private readonly string sectionId;
//        private readonly DatabaseService _databaseService;
//        private readonly WorkbookManager _workbookManager;
//        public event EventHandler SectionCompleted;
//        public ExcelValidationForm(string filePath, string sectionId, WorkbookManager workbookManager)
//        {
//            InitializeComponent();
//            this.filePath = filePath;
//            this.sectionId = sectionId;
//            _databaseService = new DatabaseService();
//            _workbookManager = workbookManager;
//            LoadStudentSheetNames();
//        }

//        private void validateButton_Click(object sender, EventArgs e)
//        {
//            if (string.IsNullOrEmpty(filePath))
//            {
//                MessageBox.Show("File path is empty.");
//                return;
//            }

//            // Get user inputs
//            string sheetName = cmbStudentSheetName.SelectedItem?.ToString();
//            string startCell = txtStartCell.Text.Trim();
//            string endCell = txtEndCell.Text.Trim();

//            if (string.IsNullOrEmpty(sheetName) || string.IsNullOrEmpty(startCell) || string.IsNullOrEmpty(endCell))
//            {
//                MessageBox.Show("Please provide valid values for Sheet Name, Cell From, and Cell To.");
//                return;
//            }

//            // Validate the Excel file
//            ValidateExcel(filePath, sectionId, sheetName, startCell, endCell);
//        }

//        private void ValidateExcel(string filePath, string sectionId, string sheetName, string startCell, string endCell)
//        {
//            try
//            {
//                using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(filePath, true))
//                {
//                    // Fetch task details for the given cell range
//                    var taskDetails = _databaseService.GetTasksByCellRangeAsDataTable(sectionId, sheetName, startCell, endCell);

//                    if (taskDetails.Rows.Count == 0)
//                    {
//                        MessageBox.Show("No task found for the given cell range.");
//                        return;
//                    }

//                    var sectionResults = new List<TaskResult>();

//                    // Process each task
//                    foreach (DataRow row in taskDetails.Rows)
//                    {
//                        string taskId = row["TaskId"].ToString();
//                        string selectTask = row["SelectTask"].ToString().ToLower();
//                        string fromCell = row["From"].ToString();
//                        string toCell = row["To"].ToString();
//                        string resultCell = row["ResultCellLocation"].ToString();
//                        string simulationId = row["SimulationId"].ToString(); // Fetch SimulationId
//                        string softwareId = row["SoftwareId"].ToString();     // Fetch SoftwareId

//                        WorksheetPart worksheetPart = GetWorksheetPartByName(spreadsheetDocument, sheetName);
//                        if (worksheetPart == null)
//                        {
//                            MessageBox.Show($"Sheet '{sheetName}' not found.");
//                            continue;
//                        }

//                        // Process the task based on its type
//                        dynamic studentResult = ProcessTask(spreadsheetDocument, selectTask, sheetName, fromCell, toCell, sectionId);

//                        // Compare with master JSON
//                        string studentJsonString = JsonConvert.SerializeObject(studentResult);
//                        string masterJsonString = _databaseService.FetchMasterJsonFromResources(simulationId, softwareId);

//                        // Deserialize master JSON into a strongly-typed model
//                        var masterJson = JsonConvert.DeserializeObject<MasterJsonModel>(masterJsonString);

//                        if (masterJson == null || masterJson.Sections == null)
//                        {
//                            MessageBox.Show("Invalid master JSON format.");
//                            continue;
//                        }

//                        // Find the master section for the current sectionId
//                        var masterSection = masterJson.Sections.FirstOrDefault(s => s.SectionName == sectionId);
//                        if (masterSection == null)
//                        {
//                            MessageBox.Show($"No master section found for section ID: {sectionId}");
//                            continue;
//                        }
//                        var comparisonData = new ComparisonData
//                        {
//                            MasterCells = masterSection.Cells,            // Ensure these properties exist and are lists of appropriate types
//                            StudentCells = studentResult.Cells,
//                            MasterCharts = masterSection.Charts,
//                            StudentCharts = studentResult.Charts,
//                            MasterPivotTables = masterSection.PivotTables,
//                            StudentPivotTables = studentResult.Pivots
//                        };
//                        // Compare student result with master JSON
//                        var comparisonResult = ValidationService.CompareJson(comparisonData);

//                        // Update results
//                        UpdateResults(worksheetPart, resultCell, studentResult, comparisonResult, sectionResults);
//                    }

//                    spreadsheetDocument.Save();

//                    // Save the student file and JSON results
//                    byte[] fileBytes = File.ReadAllBytes(filePath);
//                    string base64Excel = Convert.ToBase64String(fileBytes);
//                    _databaseService.SaveStudentFile(sectionId, base64Excel);
//                    _databaseService.SaveJsonFile(sectionId, JsonConvert.SerializeObject(sectionResults));

//                    // Display results in the text box
//                    DisplayResults(sectionResults);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error: {ex.Message}");
//                // Log the error for debugging
//                Console.WriteLine($"Error in ValidateExcel: {ex}");
//            }
//        }

//        private StudentTask ProcessTask(SpreadsheetDocument spreadsheetDocument, string selectTask, string sheetName, string fromCell, string toCell, string sectionId)
//        {
//            var result = new StudentTask();

//            switch (selectTask)
//            {
//                case "cell":
//                    var cellManager = new CellManager(spreadsheetDocument.WorkbookPart, null, _databaseService);
//                    result.Cells = cellManager.DetectCells(sheetName, fromCell, toCell, sectionId);
//                    break;
//                case "chart":
//                    var chartManager = new ChartManager(spreadsheetDocument.WorkbookPart, _databaseService);
//                    result.Charts = chartManager.DetectCharts(sheetName, fromCell, toCell, sectionId);
//                    break;
//                case "extended chart":
//                    var extChartManager = new ChartManager(spreadsheetDocument.WorkbookPart, _databaseService);
//                    result.ExtendedCharts = extChartManager.DetectCharts(sheetName, fromCell, toCell, sectionId);
//                    break;
//                case "pivot":
//                    var pivotManager = new PivotTableManager(spreadsheetDocument.WorkbookPart, _databaseService);
//                    result.Pivots = pivotManager.DetectPivotTables(GetWorksheetPartByName(spreadsheetDocument, sheetName), fromCell, toCell, sectionId, sheetName);
//                    break;
//                default:
//                    throw new InvalidOperationException($"Unknown task type: {selectTask}");
//            }

//            return result;
//        }

//        private void UpdateResults(WorksheetPart worksheetPart, string resultCell, dynamic studentResult, dynamic comparisonResultDynamic, List<TaskResult> sectionResults)
//        {
//            // Cast comparisonResult to a List<TaskResult>
//            List<TaskResult> comparisonResult = comparisonResultDynamic as List<TaskResult>;
//            if (comparisonResult == null)
//            {
//                throw new InvalidOperationException("Comparison result is not in the expected format.");
//            }

//            // Process Cells
//            if (studentResult.Cells != null)
//            {
//                foreach (var cell in studentResult.Cells)
//                {
//                    // Look for a matching task result using the TaskId (cell.ID)
//                    var match = comparisonResult.FirstOrDefault(r => r.TaskId == cell.ID);
//                    string resultText = (match != null && match.Result.ToString() == "Correct") ? "Correct" : "Incorrect";

//                    var taskResult = new TaskResult
//                    {
//                        TaskId = cell.ID,
//                        Result = resultText
//                    };

//                    sectionResults.Add(taskResult);
//                    WriteResultToCell(worksheetPart, resultCell, taskResult.Result);
//                }
//            }

//            // Process Charts
//            if (studentResult.Charts != null)
//            {
//                foreach (var chart in studentResult.Charts)
//                {
//                    var match = comparisonResult.FirstOrDefault(r => r.TaskId == chart.ID);
//                    string resultText = (match != null && match.Result.ToString() == "Correct") ? "Correct" : "Incorrect";

//                    var taskResult = new TaskResult
//                    {
//                        TaskId = chart.ID,
//                        Result = resultText
//                    };

//                    sectionResults.Add(taskResult);
//                    WriteResultToCell(worksheetPart, resultCell, taskResult.Result);
//                }
//            }

//            // Process Pivot Tables
//            if (studentResult.Pivots != null)
//            {
//                foreach (var pivot in studentResult.Pivots)
//                {
//                    var match = comparisonResult.FirstOrDefault(r => r.TaskId == pivot.ID);
//                    string resultText = (match != null && match.Result.ToString() == "Correct") ? "Correct" : "Incorrect";

//                    var taskResult = new TaskResult
//                    {
//                        TaskId = pivot.ID,
//                        Result = resultText
//                    };

//                    sectionResults.Add(taskResult);
//                    WriteResultToCell(worksheetPart, resultCell, taskResult.Result);
//                }
//            }
//        }

//        private void WriteResultToCell(WorksheetPart worksheetPart, string resultCell, string result)
//        {
//            var cell = GetCell(worksheetPart, resultCell) ?? CreateCell(worksheetPart, resultCell);
//            cell.CellValue = new CellValue(result);
//            cell.DataType = new EnumValue<CellValues>(CellValues.String);
//            worksheetPart.Worksheet.Save();
//        }

//        private Cell GetCell(WorksheetPart worksheetPart, string cellReference)
//        {
//            return worksheetPart.Worksheet.Descendants<Cell>().FirstOrDefault(c => c.CellReference == cellReference);
//        }

//        private Cell CreateCell(WorksheetPart worksheetPart, string cellReference)
//        {
//            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
//            uint rowIndex = uint.Parse(Regex.Match(cellReference, @"\d+").Value);

//            var row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex == rowIndex) ?? sheetData.AppendChild(new Row { RowIndex = rowIndex });
//            var cell = new Cell { CellReference = cellReference };
//            row.AppendChild(cell);

//            return cell;
//        }

//        private WorksheetPart GetWorksheetPartByName(SpreadsheetDocument document, string sheetName)
//        {
//            foreach (Sheet sheet in document.WorkbookPart.Workbook.Sheets)
//            {
//                if (sheet.Name == sheetName)
//                {
//                    return (WorksheetPart)document.WorkbookPart.GetPartById(sheet.Id);
//                }
//            }
//            return null;
//        }




//        // For example, if you have a Submit button click event, call SubmitSection().

//    }



//}

