using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using JobSimulation.Models;
using DocumentFormat.OpenXml.Drawing.Charts; // Add this at top

namespace JobSimulation.excelApp
{
    public class ExtendedChartManager
    {
        private WorkbookPart _workbookPart;
     

        public ExtendedChartManager(WorkbookPart workbookPart)
        {
           
            _workbookPart = workbookPart;
        }

        // Detects extended charts in the given worksheet and cell range.
        public List<ChartData> DetectExtendedCharts(string worksheetName, string fromCell,string toCell, string taskId)
        {
            string cellRange = fromCell + ":" + toCell;
            var chartDataList = new List<ChartData>();
            Sheet sheet = _workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault(s => s.Name == worksheetName);

            if (sheet == null)
                throw new ArgumentException($"Worksheet '{worksheetName}' not found.");

            WorksheetPart worksheetPart = (WorksheetPart)_workbookPart.GetPartById(sheet.Id);
            if (worksheetPart?.DrawingsPart?.WorksheetDrawing != null)
            {
                var worksheetDrawing = worksheetPart.DrawingsPart.WorksheetDrawing;

                foreach (var twoCellAnchor in worksheetDrawing.Elements<TwoCellAnchor>())
                {
                    string anchorCellRange = new ChartHelper(_workbookPart).GetCellPositionFromTwoCellAnchor(twoCellAnchor);
                    if (new ChartHelper(_workbookPart).IsCellInRange(anchorCellRange, cellRange))
                    {
                        DetectExtendedChartInAnchor(twoCellAnchor, worksheetPart, anchorCellRange, chartDataList, taskId);
                    }
                }
            }

            return chartDataList;
        }
        // Helper to detect extended charts in the specified anchor.
        private void DetectExtendedChartInAnchor(TwoCellAnchor twoCellAnchor, WorksheetPart worksheetPart, string chartPosition, List<ChartData> chartDataList, string taskId)
        {
            // Check for extended charts (cx:chart)
            var extendedChartReference = twoCellAnchor.Descendants<DocumentFormat.OpenXml.OpenXmlUnknownElement>()
                .FirstOrDefault(e => e.LocalName == "chart" && e.NamespaceUri == "http://schemas.microsoft.com/office/drawing/2014/chartex");

            if (extendedChartReference != null)
            {
                string chartId = extendedChartReference.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships").Value;
                var chartExPart = worksheetPart.DrawingsPart.GetPartById(chartId);

                if (chartExPart != null)
                {
                    DetectChartElementsForExtendedChart(chartExPart, chartId, worksheetPart, chartDataList, chartPosition, taskId);
                }
            }
        }



        //private string GetExtendedChartTitle(OpenXmlPart chartExPart)
        //{
        //    var titleElement = chartExPart.RootElement.Descendants<DocumentFormat.OpenXml.Drawing.Charts.Title>().FirstOrDefault();
        //    if (titleElement != null)
        //    {
        //        var textElement = titleElement.Descendants<DocumentFormat.OpenXml.Drawing.Text>().FirstOrDefault();
        //        if (textElement != null)
        //        {
        //            return textElement.Text;
        //        }
        //    }
        //    return "No Title";
        //}

        private string GetExtendedChartType(OpenXmlPart chartExPart)
        {
            try
            {
                var layoutIdElement = chartExPart.RootElement.Descendants<DocumentFormat.OpenXml.OpenXmlElement>()
                    .FirstOrDefault(e => e.LocalName == "series" && e.HasAttributes);

                if (layoutIdElement != null)
                {
                    var layoutId = layoutIdElement.GetAttribute("layoutId", "").Value;
                    return !string.IsNullOrEmpty(layoutId) ? layoutId : "Unknown Chart Type";
                }

                return "Unknown Chart Type";
            }
            catch (Exception ex)
            {
                return $"Error detecting chart type: {ex.Message}";
            }
        }

        private List<string> GetExtendedChartAxesTitles(OpenXmlPart chartExPart)
        {
            var axisTitles = new List<string>();

            try
            {
                var axisElements = chartExPart.RootElement
                    .Descendants()
                    .Where(el => el.LocalName == "axis" && el.Descendants().Any(d => d.LocalName == "title"));

                foreach (var axis in axisElements)
                {
                    var titleElement = axis.Descendants()
                        .FirstOrDefault(d => d.LocalName == "title");

                    if (titleElement != null)
                    {
                        var textElement = titleElement.Descendants()
                            .FirstOrDefault(t => t.LocalName == "t");
                        if (textElement != null)
                        {
                            axisTitles.Add(textElement.InnerText);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                axisTitles.Add($"Error extracting axis titles: {ex.Message}");
            }

            return axisTitles.Count > 0 ? axisTitles : new List<string> { "No Axis Titles Found" };
        }

        private bool IsLegendPresentInExtendedChart(OpenXmlPart chartExPart)
        {
            var legendElement = chartExPart.RootElement
                .Descendants<DocumentFormat.OpenXml.OpenXmlElement>()
                .FirstOrDefault(e => e.LocalName == "legend");

            return legendElement != null;
        }

        private List<string> GetExtendedChartData(OpenXmlPart chartExPart, WorkbookPart workbookPart)
        {
            var chartDataLocations = new List<string>();

            try
            {
                var numDimElement = chartExPart.RootElement.Descendants()
                    .FirstOrDefault(e => e.LocalName == "numDim" && e.HasAttributes);

                if (numDimElement != null)
                {
                    var formulaReference = numDimElement.Descendants()
                        .FirstOrDefault(e => e.LocalName == "f")?.InnerText;

                    if (!string.IsNullOrEmpty(formulaReference))
                    {
                        var definedName = workbookPart.Workbook.DefinedNames
                            .Descendants<DefinedName>()
                            .FirstOrDefault(dn => dn.Name == formulaReference);

                        if (definedName != null)
                        {
                            chartDataLocations.Add(definedName.InnerText);
                        }
                        else
                        {
                            chartDataLocations.Add($"Defined name '{formulaReference}' not found in workbook.");
                        }
                    }
                    else
                    {
                        chartDataLocations.Add("No formula reference found for chart data.");
                    }
                }
                else
                {
                    chartDataLocations.Add("No <cx:numDim> element found in chartEx XML.");
                }
            }
            catch (Exception ex)
            {
                chartDataLocations.Add($"Error retrieving extended chart data: {ex.Message}");
            }

            return chartDataLocations;
        }
        private string GetExtendedChartTitle(OpenXmlPart chartExPart)
        {
            // Locate the <cx:title> or any title-equivalent element
            var titleElement = chartExPart.RootElement.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>()
                .FirstOrDefault(p => p.Descendants<DocumentFormat.OpenXml.Drawing.Text>().Any());

            if (titleElement != null)
            {
                return titleElement.InnerText;
            }

            return "No Title";
        }
        //private string GetExtendedChartTitle(OpenXmlPart chartExPart)

        private void DetectChartElementsForExtendedChart(OpenXmlPart chartExPart, string chartId, WorksheetPart worksheetPart, List<ChartData> chartDataList, string chartPosition, string taskId)
        {
            try
            {
                string title = "No Title";
                List<string> axesTitles = new List<string>();
                string chartType = "Unknown";
                bool hasLegend = false;
                List<string> chartData = new List<string>();

                try { title = GetExtendedChartTitle(chartExPart); } catch (Exception ex) { /* Handle error */ }
                try { axesTitles = GetExtendedChartAxesTitles(chartExPart); } catch (Exception ex) { /* Handle error */ }
                try { chartType = GetExtendedChartType(chartExPart); } catch (Exception ex) { /* Handle error */ }
                try { hasLegend = IsLegendPresentInExtendedChart(chartExPart); } catch (Exception ex) { /* Handle error */ }
                try { chartData = GetExtendedChartData(chartExPart, _workbookPart); } catch (Exception ex) { /* Handle error */ }

                chartDataList.Add(new ChartData
                {
                    TaskId = taskId, // Placeholder for taskId
                    Name = GetExtendedChartName(worksheetPart, chartId),
                    Title = title,
                    Type = chartType,
                    Axes = string.Join(", ", axesTitles),
                    Legend = hasLegend,
                    DataSource = string.Join(", ", chartData),
                });
            }
            catch (Exception ex)
            {
                // Handle errors
            }
        }

        private string GetExtendedChartName(WorksheetPart worksheetPart, string chartId)
        {
            try
            {
                // Locate the GraphicFrame with the matching r:id in drawing1.xml
                var graphicFrame = worksheetPart.DrawingsPart.WorksheetDrawing
                    .Descendants<DocumentFormat.OpenXml.Drawing.Spreadsheet.GraphicFrame>()
                    .FirstOrDefault(gf =>
                    {
                        var chartReference = gf.Descendants<DocumentFormat.OpenXml.Drawing.Spreadsheet.NonVisualGraphicFrameProperties>()
                            .FirstOrDefault()?.NonVisualDrawingProperties;
                        if (chartReference != null)
                        {
                            var chartElement = gf.Descendants<DocumentFormat.OpenXml.OpenXmlUnknownElement>()
                                .FirstOrDefault(e => e.LocalName == "chart" &&
                                      e.NamespaceUri == "http://schemas.microsoft.com/office/drawing/2014/chartex");
                            if (chartElement != null)
                            {
                                return chartElement.GetAttribute("id",
                                    "http://schemas.openxmlformats.org/officeDocument/2006/relationships").Value == chartId;
                            }
                        }
                        return false;
                    });

                // Extract the name attribute from the matching GraphicFrame
                if (graphicFrame != null)
                {
                    var nonVisualProps = graphicFrame.NonVisualGraphicFrameProperties?.NonVisualDrawingProperties;
                    if (nonVisualProps != null && !string.IsNullOrEmpty(nonVisualProps.Name))
                    {
                        return nonVisualProps.Name;
                    }

                    // If no name found, return a default name with position info
                    var chartIndex = worksheetPart.DrawingsPart.WorksheetDrawing
                        .Descendants<DocumentFormat.OpenXml.Drawing.Spreadsheet.GraphicFrame>()
                        .TakeWhile(gf => gf != graphicFrame).Count() + 1;

                    return $"Extended Chart {chartIndex}";
                }

                return "Unnamed Extended Chart";
            }
            catch (Exception ex)
            {
                // Log the error if needed
                return $"Extended Chart (Error: {ex.Message})";
            }
        }
    }
}