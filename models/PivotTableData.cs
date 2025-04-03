using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace JobSimulation.Models

{
    public class PivotTableData
    {
        public string TaskId { get; set; }
        public PivotTableValues Values { get; set; } = new PivotTableValues();
    }

    public class PivotTableValues
    {
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        //public string Error { get; set; }
    }
}
