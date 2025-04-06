using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using JobSimulation.BLL;
using JobSimulation.DAL;
using JobSimulation.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace JobSimulation.excelApp
{
    public class ExcelValidationService
    {
        private readonly ValidationService _validationService;
        private readonly ExcelDatabase _excelDatabase;
        private static readonly ConcurrentDictionary<string, string> _sectionMasterJsonCache
            = new ConcurrentDictionary<string, string>();

        public ExcelValidationService(ValidationService validationService, ExcelDatabase excelDatabase)
        {
            _validationService = validationService;
            _excelDatabase = excelDatabase ?? throw new ArgumentNullException(nameof(excelDatabase));
        }

        public string GetMasterJsonForSection(string sectionId)
        {
            return _excelDatabase.FetchMasterJson(sectionId);
        }

        public bool ValidateExcelTask(TaskSubmission taskSubmission, string masterJson)
        {
            try
            {
                if (!File.Exists(taskSubmission.FilePath))
                {
                    MessageBox.Show("Source file doesn't exist");
                    return false;
                }

                SpreadsheetDocument spreadsheetDocument = null;
                FileStream stream = null;

                try
                {
                    RetryPolicy(() =>
                    {
                        stream?.Dispose();
                        stream = new FileStream(taskSubmission.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        spreadsheetDocument = SpreadsheetDocument.Open(stream, false);
                    });

                    var workbookPart = spreadsheetDocument.WorkbookPart;
                    var details = taskSubmission.Task.Details as dynamic;

                    if (!GetSheetNames(workbookPart).Contains(details.SheetName))
                    {
                        MessageBox.Show($"Sheet '{details.SheetName}' not found.");
                        return false;
                    }

                    dynamic studentResult = ProcessTask(
                        spreadsheetDocument,
                        taskSubmission,
                        details.SelectTask,
                        details.SheetName,
                        details.From,
                        details.To,
                        taskSubmission.Task.TaskId
                    );

                    var masterData = JsonConvert.DeserializeObject<MasterJsonModel>(masterJson);

                    var comparison = new
                    {
                        Student = GetTaskSpecificData(studentResult, taskSubmission),
                        Master = GetTaskSpecificData(masterData, taskSubmission)
                    };

                    var settings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.None,
                        NullValueHandling = NullValueHandling.Ignore,
                        ContractResolver = new OrderedContractResolver()
                    };

                    string studentJson = JsonConvert.SerializeObject(comparison.Student, settings);
                    string masterJsonForComparison = JsonConvert.SerializeObject(comparison.Master, settings);

                    return JToken.DeepEquals(JToken.Parse(studentJson), JToken.Parse(masterJsonForComparison));
                }
                finally
                {
                    spreadsheetDocument?.Dispose();
                    stream?.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error validating Excel task: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private Dictionary<string, object> ProcessTask(
             SpreadsheetDocument spreadsheetDocument,
             TaskSubmission taskSubmission,
             string selectTask,
             string sheetName,
             string fromCell,
             string toCell,
             string taskId)
        {
            var result = new Dictionary<string, object>();
            var tasks = new List<JobTask> { taskSubmission.Task };
            switch (selectTask)
            {
                case "Cell":
                    var cellManager = new CellManager(spreadsheetDocument.WorkbookPart);
                    var cells = cellManager.DetectCells(sheetName, fromCell, toCell, taskId);
                    if (cells.Any())
                        result["Cells"] = cells;
                    break;

                case "CHART":
                    var chartManager = new ChartManager(spreadsheetDocument.WorkbookPart);
                    var charts = chartManager.DetectCharts(sheetName, fromCell, toCell, taskId);
                    if (charts.Any())
                    {
                        result["Charts"] = charts;
                    }
                    break;
                case "EXTENDED CHART":
                    var extendedChartManager = new ExtendedChartManager(spreadsheetDocument.WorkbookPart);
                    var extendedCharts = extendedChartManager.DetectExtendedCharts(sheetName, fromCell, toCell, taskId);
                    if (extendedCharts.Any())
                        result["ExtendedCharts"] = extendedCharts;
                    break;
                case "PIVOT":
                    var pivotManager = new PivotTableManager(spreadsheetDocument.WorkbookPart);
                    var pivots = pivotManager.DetectPivotTables(
                        GetWorksheetPartByName(spreadsheetDocument, sheetName),
                        fromCell,
                        toCell,
                        taskId,
                        sheetName);
                    if (pivots.Any())
                        result["PivotTables"] = pivots;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown task type: {selectTask}");
            }

            return result;
        }

        private WorksheetPart GetWorksheetPartByName(SpreadsheetDocument document, string sheetName)
        {
            var workbookPart = document.WorkbookPart;
            var sheet = workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault(s => s.Name == sheetName);
            if (sheet == null)
                throw new ArgumentException($"Sheet '{sheetName}' not found.");
            return (WorksheetPart)workbookPart.GetPartById(sheet.Id);
        }

        private List<string> GetSheetNames(WorkbookPart workbookPart)
        {
            if (workbookPart == null)
                throw new InvalidOperationException("WorkbookPart is not loaded.");
            return workbookPart.Workbook.Sheets.Elements<Sheet>().Select(s => s.Name.Value).ToList();
        }

        private void RetryPolicy(Action action, int maxRetries = 3, int delay = 1000)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    action();
                    return;
                }
                catch (IOException) when (i < maxRetries - 1)
                {
                    Thread.Sleep(delay);
                }
                catch (UnauthorizedAccessException) when (i < maxRetries - 1)
                {
                    Thread.Sleep(delay);
                }
            }
        }

        private object GetTaskSpecificData(object source, TaskSubmission task)
        {
            var details = task.Task.Details as dynamic;
            var taskTypeKey = GetTaskTypeKey(details.SelectTask);
            var taskId = task.Task.TaskId;

            if (source is IDictionary<string, object> sourceDict)
            {
                if (!sourceDict.ContainsKey(taskTypeKey)) return null;

                var items = sourceDict[taskTypeKey] as IEnumerable;
                return FindItemById(items, taskId);
            }
            else
            {
                var property = source.GetType().GetProperty(taskTypeKey);
                if (property == null) return null;

                var items = property.GetValue(source) as IEnumerable;
                return FindItemById(items, taskId);
            }
        }

        private object FindItemById(IEnumerable items, string taskId)
        {
            if (items == null) return null;

            foreach (var item in items)
            {
                var idProperty = item.GetType().GetProperty("TaskId");
                if (idProperty == null) continue;

                var idValue = idProperty.GetValue(item) as string;
                if (idValue == taskId) return item;
            }
            return null;
        }

        private string GetTaskTypeKey(string selectTask)
        {
            return selectTask switch
            {
                "Cell" => "Cells",
                "CHART" => "Charts",
                "EXTENDED CHART" => "ExtendedCharts",
                "PIVOT" => "PivotTables",
                _ => throw new ArgumentException("Invalid task type"),
            };
        }

        public class ForceStringConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => true;

            public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer)
                => throw new NotImplementedException("Not needed for serialization");

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteNull();
                    return;
                }

                writer.WriteValue(value.ToString());
            }
        }

        public class OrderedContractResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(
                Type type,
                MemberSerialization memberSerialization)
            {
                return base.CreateProperties(type, memberSerialization)
                    .OrderBy(p => p.PropertyName)
                    .ToList();
            }
        }
    }
}