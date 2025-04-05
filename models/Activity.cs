using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobSimulation.Models
{
    public class Activity
    {
        public string ActivityId { get; set; }
        public string UserId { get; set; }
        public string SimulationId { get; set; }
        public string SectionId { get; set; }
        public string Status { get; set; }
        public int SectionAttempt { get; set; }
        public string StudentFile { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime ModifyDate { get; set; }
        public string CreateBy { get; set; }
        public string ModifyBy { get; set; }
        public string Result { get; set; }

    }
}
