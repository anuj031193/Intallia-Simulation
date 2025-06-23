using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobSimulation.Models
{
    public enum SectionNavigationAction
    {
        Next,
        Previous,
        Retry,
        Complete
    }
    public class SectionNavigationResult
    {
        public Section Section { get; set; }
        public string ActivityId { get; set; }
        public int TaskIndex { get; set; }
    }

}
