using System;
using JobSimulation.DAL;
using JobSimulation.excelApp;
using JobSimulation.Models;
using Newtonsoft.Json.Linq;

namespace JobSimulation.BLL
{
    // Interface for validation forms
    public interface IValidationForm
    {
        bool ValidateTask(TaskSubmission taskSubmission, string masterJson);
        //void WriteResultToExcelOpenXml(TaskSubmission taskSubmission, bool isCorrect);
        string GetMasterJsonForSection(string sectionId);
    }

    // Validation service that compares JSON strings
    public class ValidationService
    {
        public ValidationService() { }

        // Method to compare two JSON strings
        public bool CompareJsonStrings(string json1, string json2)
        {
            return JToken.DeepEquals(JToken.Parse(json1), JToken.Parse(json2));
        }
    }

    // Excel validation form that implements the IValidationForm interface
    public class ExcelValidationForm : IValidationForm
    {
        private readonly ExcelValidationService _excelValidationService;

        public ExcelValidationForm(ExcelValidationService excelValidationService)
        {
            _excelValidationService = excelValidationService;
        }

        public bool ValidateTask(TaskSubmission taskSubmission, string masterJson)
        {
            return _excelValidationService.ValidateExcelTask(taskSubmission, masterJson);
        }

        //public void WriteResultToExcelOpenXml(TaskSubmission taskSubmission, bool isCorrect)
        //{
        //    _excelValidationService.WriteResultToExcelOpenXml(taskSubmission.FilePath, taskSubmission.Task, isCorrect);
        //}

        public string GetMasterJsonForSection(string sectionId)
        {
            return _excelValidationService.GetMasterJsonForSection(sectionId); // This is where you call it
        }
    }

    // Factory to create validation form instances based on software ID
    // ValidationFormFactory
    public static class ValidationFormFactory
    {
        public static IValidationForm CreateValidationForm(TaskSubmission taskSubmission)
        {
            var validationService = new ValidationService();

            switch (taskSubmission.SoftwareId)
            {
                case "S1":
                    // Assuming this is for Excel
                    var excelDatabase = new ExcelDatabase(/* connection string or dependencies */);
                    var excelValidationService = new ExcelValidationService(validationService, excelDatabase);
                    return new ExcelValidationForm(excelValidationService);

                case "S2":
                    // Placeholder for Word validation logic
                    // return new WordValidationForm();
                    break;

                case "S3":
                    // Placeholder for PowerPoint validation logic
                    // return new PowerPointValidationForm();
                    break;

                default:
                    throw new NotSupportedException($"Software ID '{taskSubmission.SoftwareId}' is not supported.");
            }

            return null; // In case no valid option is found
        }
    }
}