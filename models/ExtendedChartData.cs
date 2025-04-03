using System.Collections.Generic;

namespace JobSimulation.Models
{
    public class ExtendedChartData
    {
        public string TaskId { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public dynamic Axes { get; set; } // Changed to List<string>
        public bool Legend { get; set; }
        public string DataSource { get; set; }
        
    }
}