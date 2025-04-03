using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobSimulation.Models
{
   public class SkillMatrix
    {
        public string ActivityId { get; set; }
        public string TaskId { get; set; }
        public int HintsChecked { get; set; }
        public int TotalTime { get; set; }
        public int AttemptstoSolve { get; set; }
        public string Status { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateDate { get; set; }
        public string ModifyBy { get; set; }
        public DateTime ModifyDate { get; set; } 
        public int TaskAttempt { get; set; }
    }
}
