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
using static JobSimulation.excelApp.ExcelValidationService;

namespace JobSimulation.wordApp
{
    public class WordValidationService
    {
        private readonly ValidationService _validationService;
        private readonly WordDatabase _wordDatabase;

        public WordValidationService(ValidationService validationService, WordDatabase wordDatabase)
        {
            _validationService = validationService;
            _wordDatabase = wordDatabase;
        }

        public string GetMasterJsonForSection(string sectionId)
        {
            return _wordDatabase.FetchMasterJson(sectionId);
        }

        public bool ValidateWordTask(TaskSubmission taskSubmission, string masterJson)
        {
            try
            {
                if (!File.Exists(taskSubmission.FilePath))
                {
                    MessageBox.Show("Source file doesn't exist");
                    return false;
                }

                WordprocessingDocument wordDocument = null;
                FileStream stream = null;

                try
                {
                    RetryPolicy(() =>
                    {
                        stream?.Dispose();
                        stream = new FileStream(taskSubmission.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        wordDocument = WordprocessingDocument.Open(stream, false);
                    });

                    var details = taskSubmission.Task.Details as dynamic;

                    dynamic studentResult = ProcessTask(
                        wordDocument,
                        taskSubmission,
                        details.TaskLocation,
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

                    return _validationService.CompareJsonStrings(studentJson, masterJsonForComparison);
                }
                finally
                {
                    wordDocument?.Dispose();
                    stream?.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error validating Word task: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private Dictionary<string, object> ProcessTask(
            WordprocessingDocument wordDocument,
            TaskSubmission taskSubmission,
            string taskLocation,
            string taskId)
        {
            var result = new Dictionary<string, object>();
            var tasks = new List<JobTask> { taskSubmission.Task };

            // Add logic to process the Word task based on taskLocation and taskId
            // For example, extracting specific paragraphs, tables, etc.

            return result;
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
            var taskTypeKey = "TaskLocation"; // Adjust based on Word task details
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

    }
}
