using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using JobSimulation.DAL;

namespace JobSimulation.Models
{
    public class TaskSubmission
    {
        public string FilePath { get; set; } // Path to the student's file
        public string SoftwareId { get; set; } // Software ID (e.g., Excel, Word, PowerPoint)
        public string SimulationId { get; set; } // Simulation ID
        public string SectionId { get; set; } // Section ID
        public JobTask Task { get; set; } // Current task details
        public int HintsChecked { get; set; } // Number of hints checked
        public int AttemptstoSolve { get; set; } // Number of attempts to solve the task
        public string Result { get; set; } // Result of the task
        public int TaskAttempt { get; set; } // Task attempt index

     
    }
}