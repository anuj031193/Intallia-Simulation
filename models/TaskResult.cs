using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobSimulation.Models
{
    public class SectionProgress
    {
      
        public string UserId { get; set; }
        public string SectionId { get; set; }

        public string ActivityId { get; set; }
        public List<TaskProgress> TaskProgresses { get; set; } = new List<TaskProgress>(); // Initialize here
    }


    public class TaskProgress
    {
        public string TaskId { get; set; }
        public int TimeSpent { get; set; }
        public int HintsChecked { get; set; }
        public int AttemptstoSolve { get; set; }
        public string Status { get; set; }
        public bool HasCheckedAnswer { get; set; }
        public int TaskAttempt { get; set; }
 
    }
}