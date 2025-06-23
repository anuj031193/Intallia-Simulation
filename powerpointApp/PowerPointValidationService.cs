


//using System;
//using System.IO;
//using System.Linq;
//using System.Windows.Forms;
//using DocumentFormat.OpenXml;
//using DocumentFormat.OpenXml.Packaging;
//using DocumentFormat.OpenXml.Presentation;
//using JobSimulation.BLL;
//using JobSimulation.DAL;
//using JobSimulation.Models;
//using Newtonsoft.Json.Linq;

//namespace JobSimulation.powerpointApp
//{
//    public class PowerPointValidationService
//    {
//        private readonly ValidationService _validationService;
//        private readonly PowerPointDatabase _pptDatabase;

//        public PowerPointValidationService(ValidationService validationService, PowerPointDatabase pptDatabase)
//        {
//            _validationService = validationService;
//            _pptDatabase = pptDatabase;
//        }

//        public string GetMasterJsonForSection(string sectionId)
//        {
//            return _pptDatabase.FetchMasterJson(sectionId);
//        }

//        public bool ValidatePowerPointTask(TaskSubmission taskSubmission, string masterJson)
//        {
//            try
//            {
//                if (!File.Exists(taskSubmission.FilePath))
//                {
//                    MessageBox.Show("Source file doesn't exist.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                    taskSubmission.Result = "false";
//                    return false;
//                }

//                using (var stream = new FileStream(taskSubmission.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
//                using (var ppt = PresentationDocument.Open(stream, false))
//                {
//                    var taskDetails = taskSubmission.Task.Details as PowerPointTaskDetails;
//                    if (taskDetails == null)
//                    {
//                        MessageBox.Show("Task details are missing or invalid.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                        taskSubmission.Result = "false";
//                        return false;
//                    }

//                    var masterRoot = JObject.Parse(masterJson);
//                    var tt1Array = masterRoot["TT1"] as JArray ?? throw new Exception("Invalid master JSON structure");

//                    var masterTask = tt1Array.FirstOrDefault(t =>
//                        t["TaskId"]?.ToString() == taskSubmission.Task.TaskId) as JObject
//                        ?? throw new Exception($"Task ID {taskSubmission.Task.TaskId} not found in master JSON");

//                    int slideNo = FindSlideNumber(ppt, taskDetails.ObjectName);
//                    if (slideNo == -1)
//                    {
//                        MessageBox.Show($"Object '{taskDetails.ObjectName}' not found in any slide.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                        taskSubmission.Result = "false";
//                        return false;
//                    }

//                    var slideParts = ppt.PresentationPart.Presentation.SlideIdList.ChildElements
//                        .OfType<DocumentFormat.OpenXml.Presentation.SlideId>()
//                        .Select(slideId => (SlidePart)ppt.PresentationPart.GetPartById(slideId.RelationshipId))
//                        .ToList();

//                    var slide = slideParts[slideNo - 1].Slide;

//                    var (foundObjectName, content) = FindSlideObjectContent(slide, taskDetails.ObjectName);

//                    if (string.IsNullOrEmpty(content))
//                    {
//                        MessageBox.Show($"No content found for object '{taskDetails.ObjectName}'", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                        taskSubmission.Result = "false";
//                        return false;
//                    }

//                    // For Picture and Chart objects, ensure content matches the object name
//                    if (foundObjectName.StartsWith("Picture", StringComparison.OrdinalIgnoreCase) ||
//                        foundObjectName.StartsWith("Chart", StringComparison.OrdinalIgnoreCase))
//                    {
//                        content = foundObjectName; // Use the object name as content
//                    }

//                    var studentTask = new JObject
//                    {
//                        ["TaskId"] = taskSubmission.Task.TaskId,
//                        ["slide no."] = slideNo,
//                        ["object"] = foundObjectName,
//                        ["content"] = content
//                    };

//                    bool result = NormalizeFormatting(studentTask["content"]?.ToString()) ==
//                                  NormalizeFormatting(masterTask["content"]?.ToString());

//                    taskSubmission.Result = result ? "true" : "false";

//                    if (!result)
//                    {
//                        MessageBox.Show($"Mismatch detected!\nMaster Content: {NormalizeFormatting(masterTask["content"]?.ToString())}\n" +
//                                        $"Student Content: {NormalizeFormatting(studentTask["content"]?.ToString())}",
//                                        "Validation Result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                    }

//                    return result;
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Validation Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                taskSubmission.Result = "false";
//                return false;
//            }
//        }
//        private int FindSlideNumber(PresentationDocument ppt, string targetObjectName)
//        {
//            targetObjectName = NormalizeFormatting(targetObjectName);

//            var slideParts = ppt.PresentationPart.Presentation.SlideIdList.ChildElements
//                .OfType<DocumentFormat.OpenXml.Presentation.SlideId>()
//                .Select(slideId => (SlidePart)ppt.PresentationPart.GetPartById(slideId.RelationshipId))
//                .ToList();

//            for (int i = 0; i < slideParts.Count; i++)
//            {
//                var slide = slideParts[i].Slide;

//                foreach (var element in slide.CommonSlideData.ShapeTree.Elements())
//                {
//                    var objectName = element switch
//                    {
//                        Shape s => s.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value,
//                        Picture p => p.NonVisualPictureProperties?.NonVisualDrawingProperties?.Name?.Value,
//                        GraphicFrame g => g.NonVisualGraphicFrameProperties?.NonVisualDrawingProperties?.Name?.Value,
//                        _ => null
//                    };

//                    if (!string.IsNullOrEmpty(objectName) &&
//                        NormalizeFormatting(objectName).Equals(targetObjectName, StringComparison.OrdinalIgnoreCase))
//                    {
//                        return i + 1;
//                    }
//                }
//            }

//            return -1;
//        }

//        private (string objectName, string content) FindSlideObjectContent(Slide slide, string targetObjectName)
//        {
//            foreach (var element in slide.CommonSlideData.ShapeTree.Elements())
//            {
//                var result = CheckElement(element, targetObjectName);
//                if (result.found) return (result.objectName, result.content);
//            }

//            return (null, null);
//        }

//        private (bool found, string objectName, string content) CheckElement(OpenXmlElement element, string targetObjectName)
//        {
//            string elementName = element switch
//            {
//                Shape s => s.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value,
//                Picture p => p.NonVisualPictureProperties?.NonVisualDrawingProperties?.Name?.Value,
//                GraphicFrame g => g.NonVisualGraphicFrameProperties?.NonVisualDrawingProperties?.Name?.Value,
//                _ => null
//            };

//            if (!string.IsNullOrEmpty(elementName) &&
//                NormalizeFormatting(elementName).Equals(targetObjectName, StringComparison.OrdinalIgnoreCase))
//            {
//                return (true, elementName, ExtractContent(element));
//            }

//            if (element is GroupShape group)
//            {
//                foreach (var child in group.Elements())
//                {
//                    var childResult = CheckElement(child, targetObjectName);
//                    if (childResult.found) return childResult;
//                }
//            }

//            return (false, null, null);
//        }

//        private string ExtractContent(OpenXmlElement element)
//        {
//            var extractedText = element switch
//            {
//                Shape shape => shape.TextBody?.Descendants<DocumentFormat.OpenXml.Drawing.Text>()
//                                     .Select(t => t.Text)
//                                     .Aggregate((a, b) => $"{a}{Environment.NewLine}{b}"),
//                Picture p => p.NonVisualPictureProperties?.NonVisualDrawingProperties?.Name?.Value, // Name for Picture
//                GraphicFrame g => g.NonVisualGraphicFrameProperties?.NonVisualDrawingProperties?.Name?.Value, // Name for Chart
//                _ => null
//            };

//            return NormalizeFormatting(extractedText ?? string.Empty);
//        }
//        private string NormalizeFormatting(string content)
//        {
//            if (string.IsNullOrEmpty(content)) return string.Empty;

//            content = content.Replace("\t", " ").Replace("\r", " ");
//            content = System.Text.RegularExpressions.Regex.Replace(content, @"\s+", " ");
//            return content.Trim();
//        }
//    }
//}




using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using JobSimulation.BLL;
using JobSimulation.DAL;
using JobSimulation.Models;
using Newtonsoft.Json.Linq;

namespace JobSimulation.powerpointApp
{
    public class PowerPointValidationService
    {
        private readonly ValidationService _validationService;
        private readonly PowerPointDatabase _pptDatabase;

        public PowerPointValidationService(ValidationService validationService, PowerPointDatabase pptDatabase)
        {
            _validationService = validationService;
            _pptDatabase = pptDatabase;
        }

        public string GetMasterJsonForSection(string sectionId)
        {
            return _pptDatabase.FetchMasterJson(sectionId);
        }

        public bool ValidatePowerPointTask(TaskSubmission taskSubmission, string masterJson)
        {
            try
            {
                if (!File.Exists(taskSubmission.FilePath))
                {
                    MessageBox.Show("Source file doesn't exist.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    taskSubmission.Result = "false";
                    return false;
                }

                using (var stream = new FileStream(taskSubmission.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var ppt = PresentationDocument.Open(stream, false))
                {
                    var taskDetails = taskSubmission.Task.Details as PowerPointTaskDetails;
                    if (taskDetails == null)
                    {
                        MessageBox.Show("Task details are missing or invalid.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        taskSubmission.Result = "false";
                        return false;
                    }

                    // Parse the master JSON as a dictionary keyed by TaskID
                    var masterRoot = JObject.Parse(masterJson);

                    // Directly access the task using TaskID as the key
                    var masterTask = masterRoot[taskSubmission.Task.TaskId] as JObject;
                    if (masterTask == null)
                    {
                        throw new Exception($"Task ID {taskSubmission.Task.TaskId} not found in master JSON");
                    }

                    // Get the expected object name from master JSON
                    string masterObjectName = masterTask["object"]?.ToString();
                    if (string.IsNullOrEmpty(masterObjectName))
                    {
                        throw new Exception($"Master JSON missing object name for task {taskSubmission.Task.TaskId}");
                    }

                    int slideNo = FindSlideNumber(ppt, masterObjectName);
                    if (slideNo == -1)
                    {
                        MessageBox.Show($"Object '{masterObjectName}' not found in any slide.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        taskSubmission.Result = "false";
                        return false;
                    }

                    var slideParts = ppt.PresentationPart.Presentation.SlideIdList.ChildElements
                        .OfType<DocumentFormat.OpenXml.Presentation.SlideId>()
                        .Select(slideId => (SlidePart)ppt.PresentationPart.GetPartById(slideId.RelationshipId))
                        .ToList();

                    var slide = slideParts[slideNo - 1].Slide;

                    // Find the object content using the expected name from master JSON
                    var (foundObjectName, content) = FindSlideObjectContent(slide, masterObjectName);

                    if (string.IsNullOrEmpty(content))
                    {
                        MessageBox.Show($"No content found for object '{masterObjectName}'", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        taskSubmission.Result = "false";
                        return false;
                    }

                    // For Picture and Chart objects, use object name as content
                    if (foundObjectName.StartsWith("Picture", StringComparison.OrdinalIgnoreCase) ||
                        foundObjectName.StartsWith("Chart", StringComparison.OrdinalIgnoreCase))
                    {
                        content = foundObjectName;
                    }

                    string normalizedStudentContent = NormalizeFormatting(content);
                    string normalizedMasterContent = NormalizeFormatting(masterTask["content"]?.ToString());

                    bool result = normalizedStudentContent == normalizedMasterContent;
                    taskSubmission.Result = result ? "true" : "false";

                    if (!result)
                    {
                        MessageBox.Show($"Mismatch detected!\nMaster Content: {normalizedMasterContent}\n" +
                                        $"Student Content: {normalizedStudentContent}",
                                        "Validation Result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Validation Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                taskSubmission.Result = "false";
                return false;
            }
        }
        private int FindSlideNumber(PresentationDocument ppt, string targetObjectName)
        {
            targetObjectName = NormalizeFormatting(targetObjectName);

            var slideParts = ppt.PresentationPart.Presentation.SlideIdList.ChildElements
                .OfType<DocumentFormat.OpenXml.Presentation.SlideId>()
                .Select(slideId => (SlidePart)ppt.PresentationPart.GetPartById(slideId.RelationshipId))
                .ToList();

            for (int i = 0; i < slideParts.Count; i++)
            {
                var slide = slideParts[i].Slide;

                foreach (var element in slide.CommonSlideData.ShapeTree.Elements())
                {
                    var objectName = element switch
                    {
                        Shape s => s.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value,
                        Picture p => p.NonVisualPictureProperties?.NonVisualDrawingProperties?.Name?.Value,
                        GraphicFrame g => g.NonVisualGraphicFrameProperties?.NonVisualDrawingProperties?.Name?.Value,
                        _ => null
                    };

                    if (!string.IsNullOrEmpty(objectName) &&
                        NormalizeFormatting(objectName).Equals(targetObjectName, StringComparison.OrdinalIgnoreCase))
                    {
                        return i + 1;
                    }
                }
            }

            return -1;
        }

        private (string objectName, string content) FindSlideObjectContent(Slide slide, string targetObjectName)
        {
            foreach (var element in slide.CommonSlideData.ShapeTree.Elements())
            {
                var result = CheckElement(element, targetObjectName);
                if (result.found) return (result.objectName, result.content);
            }

            return (null, null);
        }

        private (bool found, string objectName, string content) CheckElement(OpenXmlElement element, string targetObjectName)
        {
            string elementName = element switch
            {
                Shape s => s.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value,
                Picture p => p.NonVisualPictureProperties?.NonVisualDrawingProperties?.Name?.Value,
                GraphicFrame g => g.NonVisualGraphicFrameProperties?.NonVisualDrawingProperties?.Name?.Value,
                _ => null
            };

            if (!string.IsNullOrEmpty(elementName) &&
                NormalizeFormatting(elementName).Equals(targetObjectName, StringComparison.OrdinalIgnoreCase))
            {
                return (true, elementName, ExtractContent(element));
            }

            if (element is GroupShape group)
            {
                foreach (var child in group.Elements())
                {
                    var childResult = CheckElement(child, targetObjectName);
                    if (childResult.found) return childResult;
                }
            }

            return (false, null, null);
        }

        private string ExtractContent(OpenXmlElement element)
        {
            var extractedText = element switch
            {
                Shape shape => shape.TextBody?.Descendants<DocumentFormat.OpenXml.Drawing.Text>()
                                     .Select(t => t.Text)
                                     .Aggregate((a, b) => $"{a}{Environment.NewLine}{b}"),
                Picture p => p.NonVisualPictureProperties?.NonVisualDrawingProperties?.Name?.Value, // Name for Picture
                GraphicFrame g => g.NonVisualGraphicFrameProperties?.NonVisualDrawingProperties?.Name?.Value, // Name for Chart
                _ => null
            };

            return NormalizeFormatting(extractedText ?? string.Empty);
        }
        private string NormalizeFormatting(string content)
        {
            if (string.IsNullOrEmpty(content)) return string.Empty;

            content = content.Replace("\t", " ").Replace("\r", " ");
            content = System.Text.RegularExpressions.Regex.Replace(content, @"\s+", " ");
            return content.Trim();
        }
    }
}

