//// Use concrete DTO classes
//using JobSimulation.Models;
//using Newtonsoft.Json;
//using System;
//using JobSimulation.excelApp;
//public class ComparisonDto
//{
//    public object Student { get; set; }
//    public object Master { get; set; }

//}
//    // In validation method
////    var comparison = new ComparisonDto
////    {
////        Student = GetTaskSpecificData(studentResult, taskSubmission),
////        Master = GetTaskSpecificData(masterData, taskSubmission)
////    };

////    // Serialize with type handling
////    var json = JsonConvert.SerializeObject(comparison, new JsonSerializerSettings
////    {
////        Formatting = Formatting.Indented,
////        TypeNameHandling = TypeNameHandling.Auto
////    });

////    // Debug output
////    Console.WriteLine($"Comparison Structure:\n{json}");

////}