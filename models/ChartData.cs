using System;
using System.Collections.Generic;
using System.Linq;

namespace JobSimulation.Models
{
    public class ChartData
    {
      
        public string TaskId { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public object Axes { get; set; }
        public bool Legend { get; set; }
        public string DataSource { get; set; }
       
    }

   
}
