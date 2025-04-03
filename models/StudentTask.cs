
using System.Collections.Generic;

namespace JobSimulation.Models
{
    public class StudentTask
    {
        public List<CellData> Cells { get; set; } = new List<CellData>();
        public List<ChartData> Charts { get; set; } = new List<ChartData>();
        public List<ChartData> ExtendedCharts { get; set; } = new List<ChartData>();
        public List<PivotTableData> Pivots { get; set; } = new List<PivotTableData>();
    }
}
