using System.Collections.Generic;
using DocumentFormat.OpenXml.Spreadsheet;

namespace JobSimulation.Models
{
    public class MasterJsonModel
    {
        public List<CellData> Cells { get; set; } = new List<CellData>();
        public List<ChartData> Charts { get; set; } = new List<ChartData>();
        public List<ExtendedChartData> ExtendedCharts { get; set; } = new List<ExtendedChartData>();
        public List<PivotTableData> PivotTables { get; set; } = new List<PivotTableData>();
    }
}