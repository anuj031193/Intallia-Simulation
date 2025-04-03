using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobSimulation.Models
{
    public class SectionProgress
    {
        public List<JobTask> Tasks { get; set; }
        public string UserId { get; set; }
        public string SectionId { get; set; }
        public int CurrentTaskIndex { get; set; }
        public string Result { get; set; }
        public List<TaskResult> TaskResults { get; set; } = new List<TaskResult>(); // Initialize here
    }

    // Add new class definition
    //public class TaskProgress
    //{
    //    public string ActivityId { get; set; }
    //    public string TaskId { get; set; }
    //    public int TimeSpent { get; set; }
    //    public int HintsUsed { get; set; }
    //    public int Attempts { get; set; }
    //    public bool IsCompleted { get; set; }
    //    public string UserId { get; set; }
    //    public string Status { get; set; } // Add this line
    //}
    public class TaskResult
    {
        public string TaskId { get; set; }
        public bool IsCompleted { get; set; }
        public int AttemptstoSolve { get; set; }
        public int HintsUsed { get; set; } // Number of hints used for the task
        public string Status { get; set; } // Status of the task (e.g., "Incomplete", "Completed", "Failed")
        public DateTime LastAttemptDate { get; set; } // Date and time of the last attempt
        public int TaskAttempt { get; set; }
        public int TimeSpent { get; set; }
    }
}