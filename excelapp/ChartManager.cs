using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using JobSimulation.Models;

namespace JobSimulation.excelApp
{
    public class ChartManager
    {
        private readonly WorkbookPart _workbookPart;

        public ChartManager(WorkbookPart workbookPart)
        {
            _workbookPart = workbookPart ?? throw new ArgumentNullException(nameof(workbookPart));
        }

        public List<ChartData> DetectCharts(string worksheetName, string fromCell, string toCell, string taskId)
        {
            string cellRange = $"{fromCell}:{toCell}";
            var chartDataList = new List<ChartData>();

            // Find the worksheet by name
            Sheet sheet = _workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault(s => s.Name == worksheetName);
            if (sheet == null)
                throw new ArgumentException($"Worksheet '{worksheetName}' not found.");

            // Get the worksheet part
            WorksheetPart worksheetPart = (WorksheetPart)_workbookPart.GetPartById(sheet.Id);
            if (worksheetPart?.DrawingsPart?.WorksheetDrawing != null)
            {
                var worksheetDrawing = worksheetPart.DrawingsPart.WorksheetDrawing;

                // Iterate through all charts in the worksheet
                foreach (var twoCellAnchor in worksheetDrawing.Elements<TwoCellAnchor>())
                {
                    var helper = new ChartHelper(_workbookPart);
                    string anchorCellRange = helper.GetCellPositionFromTwoCellAnchor(twoCellAnchor);

                    // Check if the chart is within the specified range
                    if (helper.IsCellInRange(anchorCellRange, cellRange))
                    {
                        DetectChartInAnchor(twoCellAnchor, worksheetPart, anchorCellRange, chartDataList, taskId);
                    }
                }
            }

            return chartDataList;
        }

        private void DetectChartInAnchor(TwoCellAnchor twoCellAnchor, WorksheetPart worksheetPart, string chartPosition, List<ChartData> chartDataList, string taskId)
        {
            var chartReference = twoCellAnchor.Descendants<ChartReference>().FirstOrDefault();
            if (chartReference != null)
            {
                string chartId = chartReference.Id;
                var chartPart = worksheetPart.DrawingsPart.GetPartById(chartId) as ChartPart;

                if (chartPart != null)
                {
                    var chartData = new ChartData
                    {
                        TaskId = taskId,
                        Name = GetChartName(twoCellAnchor),
                        Title = GetChartTitle(chartPart),
                        Type = GetChartType(chartPart),
                        Axes = GetChartAxes(chartPart),
                        Legend = HasChartLegend(chartPart),
                        DataSource = GetRegularChartData(chartPart)
                    };

                    chartDataList.Add(chartData);
                }
            }
        }

        private string GetChartName(TwoCellAnchor twoCellAnchor)
        {
            return twoCellAnchor.Descendants<GraphicFrame>()
                               .FirstOrDefault()?
                               .NonVisualGraphicFrameProperties?.NonVisualDrawingProperties?.Name ?? "Unnamed Chart";
        }

        private string GetChartTitle(ChartPart chartPart)
        {
            var titleElement = chartPart.ChartSpace.Descendants<Title>().FirstOrDefault();
            if (titleElement != null)
            {
                var textElement = titleElement.ChartText?.RichText?.Descendants<DocumentFormat.OpenXml.Drawing.Run>().FirstOrDefault()?.Text;
                if (textElement != null)
                {
                    return textElement.Text;
                }
            }
            return "No Title";
        }

        private string GetChartType(ChartPart chartPart)
        {
            var plotArea = chartPart.ChartSpace.Descendants<PlotArea>().FirstOrDefault();
            if (plotArea != null)
            {
                var chartElement = plotArea.Elements().FirstOrDefault(e => e.LocalName.EndsWith("Chart"));
                if (chartElement != null)
                {
                    return chartElement.LocalName;
                }
            }
            return "Unknown Chart Type";
        }

        private object GetChartAxes(ChartPart chartPart)
        {
            var axesData = new
            {
                xAxis = GetAxisTitle<CategoryAxis>(chartPart),
                yAxis = GetAxisTitle<ValueAxis>(chartPart),
                secondaryYAxis = GetAxisTitle<ValueAxis>(chartPart, 1),
                zAxis = GetAxisTitle<SeriesAxis>(chartPart)
            };

            if (!string.IsNullOrEmpty(axesData.xAxis) || !string.IsNullOrEmpty(axesData.yAxis) ||
                !string.IsNullOrEmpty(axesData.secondaryYAxis) || !string.IsNullOrEmpty(axesData.zAxis))
            {
                return axesData;
            }

            return "No Axes Titles Found";
        }

        private string GetAxisTitle<T>(ChartPart chartPart, int index = 0) where T : OpenXmlElement
        {
            var axis = chartPart.ChartSpace.Descendants<T>().Skip(index).FirstOrDefault();
            if (axis != null)
            {
                var axisTitle = axis.Descendants<Title>().FirstOrDefault();
                var axisText = axisTitle?.Descendants<DocumentFormat.OpenXml.Drawing.Text>().FirstOrDefault()?.Text;
                return axisText ?? "No Title";
            }
            return null;
        }

        private bool HasChartLegend(ChartPart chartPart)
        {
            return chartPart.ChartSpace.Descendants<Legend>().Any();
        }

        private string GetRegularChartData(ChartPart chartPart)
        {
            try
            {
                var categoryRangeElement = chartPart.ChartSpace.Descendants()
                    .FirstOrDefault(e => e.LocalName == "f" && e.Parent.LocalName == "strRef" && e.Parent.Parent.LocalName == "cat");

                var valueRangeElement = chartPart.ChartSpace.Descendants()
                    .FirstOrDefault(e => e.LocalName == "f" && e.Parent.LocalName == "numRef" && e.Parent.Parent.LocalName == "val");

                if (categoryRangeElement != null && valueRangeElement != null)
                {
                    string categoryRange = categoryRangeElement.InnerText;
                    string valueRange = valueRangeElement.InnerText;

                    string sheetName = categoryRange.Split('!')[0];
                    string categoryStart = categoryRange.Split('!')[1].Split(':')[0];
                    string categoryEnd = categoryRange.Split('!')[1].Split(':')[1];
                    string valueStart = valueRange.Split('!')[1].Split(':')[0];
                    string valueEnd = valueRange.Split('!')[1].Split(':')[1];

                    return $"{sheetName}!{categoryStart}:{valueEnd}";
                }

                return "Data source not found.";
            }
            catch (Exception ex)
            {
                return $"Error retrieving data source: {ex.Message}";
            }
        }
    }
}