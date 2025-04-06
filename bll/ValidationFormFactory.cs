using System;
using JobSimulation.DAL;
using JobSimulation.excelApp;
using JobSimulation.Models;
using JobSimulation.wordApp;
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

  
    public static class ValidationFormFactory
    {
        public static IValidationForm CreateValidationForm(TaskSubmission taskSubmission, FileService fileService)
        {
            var validationService = new ValidationService();

            switch (taskSubmission.SoftwareId.ToUpper())
            {
                case "S1": // Excel
                    var excelDatabase = new ExcelDatabase(fileService);
                    var excelValidationService = new ExcelValidationService(validationService, excelDatabase);
                    return new ExcelValidationForm(excelValidationService);

                case "S2": // Word
                    var wordDatabase = new WordDatabase(fileService);
                    var wordValidationService = new WordValidationService(validationService, wordDatabase);
                    return new WordValidationForm(wordValidationService);


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



        public string GetMasterJsonForSection(string sectionId)
        {
            return _excelValidationService.GetMasterJsonForSection(sectionId); // This is where you call it
        }
    }



    public class WordValidationForm : IValidationForm
    {
        private readonly WordValidationService _wordValidationService;

        public WordValidationForm(WordValidationService wordValidationService)
        {
            _wordValidationService = wordValidationService;
        }

        public bool ValidateTask(TaskSubmission taskSubmission, string masterJson)
        {
            return _wordValidationService.ValidateWordTask(taskSubmission, masterJson);
        }

        public string GetMasterJsonForSection(string sectionId)
        {
            return _wordValidationService.GetMasterJsonForSection(sectionId);
        }
    }
}