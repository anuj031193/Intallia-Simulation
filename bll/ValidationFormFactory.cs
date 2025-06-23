using JobSimulation.DAL;
using JobSimulation.excelApp;
using JobSimulation.Models;
using JobSimulation.powerBiApp;
using JobSimulation.powerpointApp;
using JobSimulation.wordApp;
using Newtonsoft.Json.Linq;
using System;

namespace JobSimulation.BLL
{
    // Interface for validation forms
    public interface IValidationForm
    {
        bool ValidateTask(TaskSubmission taskSubmission, string masterJson);
        string GetMasterJsonForSection(string sectionId);
    }

    // Validation service that compares JSON strings
    public class ValidationService
    {
        public ValidationService() { }

        public bool CompareJsonStrings(string json1, string json2)
        {
            return JToken.DeepEquals(JToken.Parse(json1), JToken.Parse(json2));
        }
    }

    // Factory to create validation forms based on software type
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

                case "S3": // PowerPoint
                    var pptDatabase = new PowerPointDatabase(fileService);
                    var pptValidationService = new PowerPointValidationService(validationService, pptDatabase);
                    return new PowerPointValidationForm(pptValidationService);

                case "S7": // PowerBI
                    var powerBiDatabase = new PowerBiDatabase(fileService);
                    var powerBiValidationService = new PowerBiValidationService(validationService, powerBiDatabase);
                    return new PowerBiValidationForm(powerBiValidationService);

                default:
                    throw new NotSupportedException($"Software ID '{taskSubmission.SoftwareId}' is not supported.");
            }
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
            return _excelValidationService.GetMasterJsonForSection(sectionId);
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

    public class PowerPointValidationForm : IValidationForm
    {
        private readonly PowerPointValidationService _pptValidationService;

        public PowerPointValidationForm(PowerPointValidationService pptValidationService)
        {
            _pptValidationService = pptValidationService;
        }

        public bool ValidateTask(TaskSubmission taskSubmission, string masterJson)
        {
            return _pptValidationService.ValidatePowerPointTask(taskSubmission, masterJson);
        }

        public string GetMasterJsonForSection(string sectionId)
        {
            return _pptValidationService.GetMasterJsonForSection(sectionId);
        }
    }

    // New PowerBiValidationForm similar to others
    public class PowerBiValidationForm : IValidationForm
    {
        private readonly PowerBiValidationService _powerBiValidationService;

        public PowerBiValidationForm(PowerBiValidationService powerBiValidationService)
        {
            _powerBiValidationService = powerBiValidationService;
        }

        public bool ValidateTask(TaskSubmission taskSubmission, string masterJson)
        {
            return _powerBiValidationService.ValidatePowerBiTask(taskSubmission, masterJson);
        }

        public string GetMasterJsonForSection(string sectionId)
        {
            return _powerBiValidationService.GetMasterJsonForSection(sectionId);
        }
    }
}
