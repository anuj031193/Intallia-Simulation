

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using JobSimulation.BLL;
using JobSimulation.DAL;
using JobSimulation.Models;
using Newtonsoft.Json.Linq;

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
                    MessageBox.Show("Source file doesn't exist.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                using (var stream = new FileStream(taskSubmission.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var wordDoc = WordprocessingDocument.Open(stream, false))
                {
                    // Extract content for the specific TaskId
                    var taskDetails = taskSubmission.Task.Details as WordTaskDetails;
                    if (taskDetails == null || string.IsNullOrEmpty(taskDetails.TaskLocation))
                    {
                        MessageBox.Show("Task details do not contain a valid TaskLocation.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    var contentControl = ExtractContentControlForTask(wordDoc, taskSubmission.Task.TaskId, taskDetails.TaskLocation);
                    if (contentControl == null)
                    {
                        MessageBox.Show($"No content found for TaskId: {taskSubmission.Task.TaskId} at TaskLocation: {taskDetails.TaskLocation}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    // Prepare the student's JSON for comparison
                    var studentJson = new JObject
                    {
                        [taskSubmission.Task.TaskId] = new JObject
                        {
                            ["Value"] = contentControl.Content
                        }
                    };

                    // Parse the master JSON and extract the specific task data
                    JToken masterJsonToken;
                    try
                    {
                        masterJsonToken = JToken.Parse(masterJson);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Master JSON is not valid: {ex.Message}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    var masterTaskJson = masterJsonToken[taskSubmission.Task.TaskId];
                    if (masterTaskJson == null)
                    {
                        MessageBox.Show($"Master JSON does not contain data for TaskId: {taskSubmission.Task.TaskId}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    // Compare the student JSON and master JSON for the specific TaskId
                    return JToken.DeepEquals(studentJson[taskSubmission.Task.TaskId], masterTaskJson);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error validating Word task: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        private ContentControlDetail ExtractContentControlForTask(WordprocessingDocument wordDoc, string taskId, string taskLocation)
        {
            // Descendants of SdtElement to traverse structured content controls
            var sdtElements = wordDoc.MainDocumentPart.Document.Body.Descendants<SdtElement>();

            foreach (var sdt in sdtElements)
            {
                // Get the <w:tag> value
                var tag = sdt.SdtProperties.GetFirstChild<Tag>()?.Val?.Value;

                // Log for debugging
                Console.WriteLine($"Found Tag: {tag}, TaskLocation: {taskLocation}");

                // Match TaskId with <w:tag> and TaskLocation
                if (!string.IsNullOrEmpty(tag) &&
                    (tag.Equals(taskId, StringComparison.OrdinalIgnoreCase) || tag.Equals("Task" + taskId, StringComparison.OrdinalIgnoreCase) || tag.Equals(taskLocation, StringComparison.OrdinalIgnoreCase)))
                {
                    // Retrieve the content
                    string content = sdt.InnerText.Trim();

                    // Log the content for debugging
                    Console.WriteLine($"Match found for TaskId: {taskId}, TaskLocation: {taskLocation}, Content: {content}");

                    // Return the matched content
                    return new ContentControlDetail
                    {
                        Tag = tag,
                        Content = content
                    };
                }
            }

            // Log if no match is found
            Console.WriteLine($"No match found for TaskId: {taskId} or TaskLocation: {taskLocation}");
            return null; // Return null if no matching content is found
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
                    Thread.Sleep(delay); // Wait before retrying
                }
                catch (UnauthorizedAccessException) when (i < maxRetries - 1)
                {
                    Thread.Sleep(delay); // Wait before retrying
                }
            }
        }
    }

    public class ContentControlDetail
    {
        public string Tag { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
