using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;

namespace DesktopApp.excelApp
{
    public class ChartManager1
    {
        private readonly WorkbookPart _workbookPart;

        public ChartManager1(WorkbookPart workbookPart)
        {
            _workbookPart = workbookPart;
        }

        public ChartDetectionResult DetectCharts(string worksheetName, string fromCell, string toCell, string taskId)
        {
            var result = new ChartDetectionResult
            {
                Charts = new List<ChartData>(),
                ExtendedCharts = new List<ExtendedChartData>()
            };

            var sheet = _workbookPart.Workbook.Descendants<Sheet>()
                .FirstOrDefault(s => s.Name == worksheetName);

            if (sheet == null) throw new ArgumentException($"Worksheet '{worksheetName}' not found.");

            var worksheetPart = (WorksheetPart)_workbookPart.GetPartById(sheet.Id);
            var drawingsPart = worksheetPart?.DrawingsPart;

            if (drawingsPart?.WorksheetDrawing == null) return result;

            foreach (var anchor in drawingsPart.WorksheetDrawing.Elements<TwoCellAnchor>())
            {
                var anchorRange = GetCellPositionFromTwoCellAnchor(anchor);
                if (!IsCellInRange(anchorRange, fromCell, toCell)) continue;

                ProcessRegularCharts(anchor, worksheetPart, result, taskId);
                ProcessExtendedCharts(anchor, worksheetPart, result, taskId);
            }

            return result;
        }

        private void ProcessRegularCharts(TwoCellAnchor anchor, WorksheetPart worksheetPart,
            ChartDetectionResult result, string taskId)
        {
            var chartRef = anchor.Descendants<ChartReference>().FirstOrDefault();
            if (chartRef == null) return;

            var chartPart = worksheetPart.DrawingsPart.GetPartById(chartRef.Id) as ChartPart;
            if (chartPart == null) return;

            result.Charts.Add(new ChartData
            {
                TaskId = taskId,
                Name = GetChartName(anchor) ?? "Unnamed Chart",
                Title = GetChartTitle(chartPart),
                Type = GetChartType(chartPart),
                Axes = GetRegularChartAxes(chartPart),
                HasLegend = HasChartLegend(chartPart),
                DataSource = GetRegularChartData(chartPart)
            });
        }

        private void ProcessExtendedCharts(TwoCellAnchor anchor, WorksheetPart worksheetPart,
            ChartDetectionResult result, string taskId)
        {
            var extChartRef = anchor.Descendants<DocumentFormat.OpenXml.OpenXmlUnknownElement>()
                .FirstOrDefault(e => e.LocalName == "chart" &&
                    e.NamespaceUri == "http://schemas.microsoft.com/office/drawing/2014/chartex");

            if (extChartRef == null) return;

            var chartId = extChartRef.GetAttribute("id",
                "http://schemas.openxmlformats.org/officeDocument/2006/relationships").Value;
            var chartExPart = worksheetPart.DrawingsPart.GetPartById(chartId);

            if (chartExPart == null) return;

            result.ExtendedCharts.Add(new ExtendedChartData
            {
                TaskId = taskId,
                Name = GetExtendedChartName(worksheetPart, chartId) ?? "Unnamed Extended Chart",
                Title = GetExtendedChartTitle(chartExPart),
                Type = GetExtendedChartType(chartExPart),
                Legend = HasExtendedChartLegend(chartExPart),
                Axes = GetExtendedChartAxes(chartExPart),
                DataSource = GetExtendedChartData(chartExPart)
            });
        }

        #region Regular Chart Helpers
        private string GetRegularChartAxes(ChartPart chartPart)
        {
            try
            {
                var axes = new List<string>();

                // X-Axis
                var xAxis = chartPart.ChartSpace.Descendants<CategoryAxis>().FirstOrDefault();
                AddAxisTitle(xAxis, "X-Axis", axes);

                // Y-Axis
                var yAxis = chartPart.ChartSpace.Descendants<ValueAxis>().FirstOrDefault();
                AddAxisTitle(yAxis, "Y-Axis", axes);

                return axes.Count > 0 ? string.Join(", ", axes) : "No Axes Found";
            }
            catch
            {
                return "Error detecting axes";
            }
        }

        private void AddAxisTitle<T>(T axis, string axisName, List<string> axes) where T : OpenXmlElement
        {
            var title = axis?.Descendants<Title>().FirstOrDefault();
            var text = title?.Descendants<DocumentFormat.OpenXml.Drawing.Text>().FirstOrDefault()?.Text;
            axes.Add($"{axisName}: {text ?? "No Title"}");
        }

        private string GetRegularChartData(ChartPart chartPart)
        {
            try
            {
                var catRef = chartPart.ChartSpace.Descendants<CategoryAxisData>().FirstOrDefault()
                    ?.Descendants<Formula>().FirstOrDefault()?.Text;

                var valRef = chartPart.ChartSpace.Descendants<Values>().FirstOrDefault()
                    ?.Descendants<Formula>().FirstOrDefault()?.Text;

                return FormatDataSource(catRef, valRef);
            }
            catch
            {
                return "Data source error";
            }
        }
        #endregion

        #region Extended Chart Helpers
        private List<string> GetExtendedChartAxes(OpenXmlPart chartExPart)
        {
            var axes = new List<string>();
            try
            {
                foreach (var axis in chartExPart.RootElement.Descendants()
                    .Where(e => e.LocalName == "axis"))
                {
                    var title = axis.Descendants().FirstOrDefault(e => e.LocalName == "title");
                    if (title == null) continue;

                    var text = string.Concat(title.Descendants()
                        .Where(e => e.LocalName == "t")
                        .Select(t => t.InnerText));

                    if (!string.IsNullOrWhiteSpace(text))
                        axes.Add($"{GetExtendedAxisType(axis)}: {text}");
                }
                return axes.Any() ? axes : new List<string> { "No Axes Found" };
            }
            catch
            {
                return new List<string> { "Error detecting axes" };
            }
        }

        private string GetExtendedAxisType(OpenXmlElement axis)
        {
            var position = axis.Descendants()
                .FirstOrDefault(e => e.LocalName == "axisPosition")?
                .GetAttribute("val", "").Value;

            return position switch
            {
                "bottom" => "X-Axis",
                "left" => "Y-Axis",
                "right" => "Secondary Y-Axis",
                "top" => "Secondary X-Axis",
                _ => "Other Axis"
            };
        }

        private string GetExtendedChartData(OpenXmlPart chartExPart)
        {
            try
            {
                var formula = chartExPart.RootElement.Descendants()
                    .FirstOrDefault(e => e.LocalName == "f")?.InnerText;

                return !string.IsNullOrEmpty(formula)
                    ? formula
                    : "Data source not found";
            }
            catch
            {
                return "Data source error";
            }
        }
        #endregion

        #region Common Helpers
        private string FormatDataSource(string categoryRef, string valueRef)
        {
            if (string.IsNullOrEmpty(categoryRef) return "No category data";
            if (string.IsNullOrEmpty(valueRef)) return "No value data";

            try
            {
                var catSheet = categoryRef.Split('!')[0];
                var valSheet = valueRef.Split('!')[0];

                if (catSheet != valSheet)
                    return $"{categoryRef} | {valueRef}";

                var catRange = categoryRef.Split('!')[1];
                var valRange = valueRef.Split('!')[1];

                return $"{catSheet}!{catRange}:{valRange.Split(':').Last()}";
            }
            catch
            {
                return $"{categoryRef} | {valueRef}";
            }
        }

        // Keep existing helpers for cell position, range checking, etc.
        #endregion
    }

    public class ChartDetectionResult
    {
        public List<ChartData> Charts { get; set; }
        public List<ExtendedChartData> ExtendedCharts { get; set; }
    }

    public class ChartData
    {
        public string TaskId { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string Axes { get; set; }
        public bool Legend { get; set; }
        public string DataSource { get; set; }
    }

    public class ExtendedChartData
    {
        public string TaskId { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public List<string> Axes { get; set; }
        public bool Legend { get; set; }
        public string DataSource { get; set; }
    }
}