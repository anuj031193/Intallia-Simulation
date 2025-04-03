using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JobSimulation.Models;

//// WordValidationForm.cs (example)
////public class WordValidationForm : IValidationForm
////{
////    private readonly WordValidationService _wordValidationService;

////    public WordValidationForm(WordValidationService wordValidationService)
////    {
////        _wordValidationService = wordValidationService;
////    }

////    public bool ValidateTask(TaskSubmission taskSubmission)
////    {
////        return _wordValidationService.ValidateWordTask(taskSubmission);
////    }

////    public void WriteResultToFile(TaskSubmission taskSubmission, bool isCorrect)
////    {
////        _wordValidationService.WriteResultToWord(taskSubmission, isCorrect);
////    }
////}