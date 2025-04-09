    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using JobSimulation.Extensions;


namespace JobSimulation.Models
{
    public static class StatusTypes
    {
        //Section
        public const string NotStarted = "NotStarted";
        public const string InProgress = "InProgress";
        public const string Completed = "Completed";
        public const string InComplete = "InComplete";
        public const string PartiallyCompleted = "PartiallyCompleted";

        //result
        public const string NeedsImprovement = "NeedsImprovement";
        public const string Mastered = "Mastered";
        public const string Proficient = "Proficient";
        public const string Developing = "Developing";

        // Task-specific statuses
        public const string Visited = "Visited";
        public const string Attempted = "Attempted";
    
  
      
        public const string New = "New";
    }

    // Helper Classes
    public static class StatusHelpers
    {
        public static bool IsValidTaskStatus(string status) =>
            new[] { "NotStarted", "InProgress", "Completed" }.Contains(status);

        public static bool CanTransitionTask(string current, string next) =>
            _validTransitions[current].Contains(next);

        private static Dictionary<string, List<string>> _validTransitions = new()
        {
            ["NotStarted"] = new() { "InProgress" },
            ["InProgress"] = new() { "Completed", "Paused" },
            ["Paused"] = new() { "InProgress" }
        };
    }

    public class RetryLimitExceededException : Exception
    {
        public RetryLimitExceededException()
            : base("Maximum section retry attempts exceeded") { }
    }
}