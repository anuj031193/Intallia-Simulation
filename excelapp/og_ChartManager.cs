//using DocumentFormat.OpenXml.Packaging;
//using DocumentFormat.OpenXml.Spreadsheet;
//using DocumentFormat.OpenXml.Drawing.Spreadsheet;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using JobSimulation.Models;
//using DocumentFormat.OpenXml.Drawing.Charts;
//using DocumentFormat.OpenXml;

//namespace JobSimulation.excelApp
//{
//	public class ChartManager
//	{
//		private WorkbookPart _workbookPart;
//		private List<Task> _tasks;

//		public ChartManager(WorkbookPart workbookPart, List<Task> tasks)
//		{
//			_workbookPart = workbookPart;
//			_tasks = tasks;
//		}

//		// Detects charts in the given worksheet and cell range.
//		public List<ChartData> DetectCharts(string worksheetName, string cellRange, string taskId)
//		{
//			var chartDataList = new List<ChartData>();
//			Sheet sheet = _workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault(s => s.Name == worksheetName);

//			if (sheet == null)
//				throw new ArgumentException($"Worksheet '{worksheetName}' not found.");

//			WorksheetPart worksheetPart = (WorksheetPart)_workbookPart.GetPartById(sheet.Id);
//			if (worksheetPart?.DrawingsPart?.WorksheetDrawing != null)
//			{
//				var worksheetDrawing = worksheetPart.DrawingsPart.WorksheetDrawing;

//				foreach (var twoCellAnchor in worksheetDrawing.Elements<DocumentFormat.OpenXml.Drawing.Spreadsheet.TwoCellAnchor>())
//				{
//					var helper = new ChartHelper(_workbookPart, _tasks);
//					string anchorCellRange = helper.GetCellPositionFromTwoCellAnchor(twoCellAnchor);
//					if (helper.IsCellInRange(anchorCellRange, cellRange))
//					{
//						DetectChartInAnchor(twoCellAnchor, worksheetPart, anchorCellRange, chartDataList, taskId);
//					}
//				}
//			}

//			return chartDataList;
//		}

//		// Helper to detect charts in the specified anchor.
//		private void DetectChartInAnchor(DocumentFormat.OpenXml.Drawing.Spreadsheet.TwoCellAnchor twoCellAnchor, WorksheetPart worksheetPart, string chartPosition, List<ChartData> chartDataList, string taskId)
//		{
//			// Check for regular charts
//			var chartReference = twoCellAnchor.Descendants<ChartReference>().FirstOrDefault();
//			if (chartReference != null)
//			{
//				string chartId = chartReference.Id;
//				var chartPart = worksheetPart.DrawingsPart.GetPartById(chartId) as ChartPart;

//				if (chartPart != null)
//				{
//					var chartData = new ChartData
//					{
//						TaskId = taskId,
//						Name = GetChartName(twoCellAnchor),
//						Title = GetChartTitle(chartPart),
//						Type = GetChartType(chartPart),
//						Axes = GetChartAxes(chartPart),
//						Legend = HasChartLegend(chartPart),
//						DataSource = GetRegularChartData(chartPart)
//					};

//					chartDataList.Add(chartData);
//				}
//			}
//		}
//		private string GetRegularChartData(ChartPart chartPart)
//		{
//			try
//			{
//				// For regular charts, look for <c:cat> (categories) and <c:val> (values)
//				var categoryRangeElement = chartPart.ChartSpace.Descendants()
//					.FirstOrDefault(e => e.LocalName == "f" && e.Parent.LocalName == "strRef" && e.Parent.Parent.LocalName == "cat");

//				var valueRangeElement = chartPart.ChartSpace.Descendants()
//					.FirstOrDefault(e => e.LocalName == "f" && e.Parent.LocalName == "numRef" && e.Parent.Parent.LocalName == "val");

//				if (categoryRangeElement != null && valueRangeElement != null)
//				{
//					// Extract the ranges
//					string categoryRange = categoryRangeElement.InnerText; // Example: QuestionPizza!$C$876:$C$885
//					string valueRange = valueRangeElement.InnerText;       // Example: QuestionPizza!$D$876:$D$885

//					// Combine the ranges
//					string sheetName = categoryRange.Split('!')[0];
//					string categoryStart = categoryRange.Split('!')[1].Split(':')[0];
//					string categoryEnd = categoryRange.Split('!')[1].Split(':')[1];
//					string valueStart = valueRange.Split('!')[1].Split(':')[0];
//					string valueEnd = valueRange.Split('!')[1].Split(':')[1];

//					// Return combined range
//					return $"{sheetName}!{categoryStart}:{valueEnd}";
//				}

//				// Handle pivot charts: Check for <c:pivotSource> and associated ranges
//				var pivotSourceElement = chartPart.ChartSpace.Descendants()
//					.FirstOrDefault(e => e.LocalName == "pivotSource");

//				if (pivotSourceElement != null)
//				{
//					// Extract the pivot table name
//					var pivotNameElement = pivotSourceElement.Descendants()
//						.FirstOrDefault(e => e.LocalName == "name");

//					if (pivotNameElement != null)
//					{
//						string pivotName = pivotNameElement.InnerText; // Example: QuestionPizza!PivotTable1

//						// Locate category and value ranges for the pivot chart
//						var pivotCategoryRange = chartPart.ChartSpace.Descendants()
//							.FirstOrDefault(e => e.LocalName == "f" && e.Parent.LocalName == "strRef" && e.Parent.Parent.LocalName == "cat");

//						var pivotValueRange = chartPart.ChartSpace.Descendants()
//							.FirstOrDefault(e => e.LocalName == "f" && e.Parent.LocalName == "numRef" && e.Parent.Parent.LocalName == "val");

//						if (pivotCategoryRange != null && pivotValueRange != null)
//						{
//							// Extract the ranges
//							string categoryRange = pivotCategoryRange.InnerText; // Example: QuestionPizza!$C$876:$C$885
//							string valueRange = pivotValueRange.InnerText;       // Example: QuestionPizza!$D$876:$D$885

//							// Combine the ranges
//							string sheetName = categoryRange.Split('!')[0];
//							string categoryStart = categoryRange.Split('!')[1].Split(':')[0];
//							string categoryEnd = categoryRange.Split('!')[1].Split(':')[1];
//							string valueStart = valueRange.Split('!')[1].Split(':')[0];
//							string valueEnd = valueRange.Split('!')[1].Split(':')[1];

//							// Return combined range
//							return $"{sheetName}!{categoryStart}:{valueEnd}";
//						}

//						// If ranges not found, return just the pivot name
//						return pivotName;
//					}
//				}

//				// Fallback: If no data source found
//				return "Data source not found.";
//			}
//			catch (Exception ex)
//			{
//				return $"Error retrieving data source: {ex.Message}";
//			}
//		}

//		// Helper methods to get various chart details (name, title, axes, etc.)
//		private string GetChartName(DocumentFormat.OpenXml.Drawing.Spreadsheet.TwoCellAnchor twoCellAnchor)
//			=> twoCellAnchor.Descendants<DocumentFormat.OpenXml.Drawing.Spreadsheet.GraphicFrame>()
//						.FirstOrDefault()?
//						.NonVisualGraphicFrameProperties?.NonVisualDrawingProperties?.Name ?? "Unnamed Chart";

//		private string GetChartTitle(ChartPart chartPart)
//		{
//			var titleElement = chartPart.ChartSpace.Descendants<Title>().FirstOrDefault();
//			if (titleElement != null)
//			{
//				var textElement = titleElement.ChartText?.RichText?.Descendants<DocumentFormat.OpenXml.Drawing.Run>().FirstOrDefault()?.Text;
//				if (textElement != null)
//				{
//					return textElement.Text;
//				}
//			}
//			return "No Title";
//		}

//		private string GetChartType(ChartPart chartPart)
//		{
//			try
//			{
//				// Get the PlotArea element from the ChartSpace
//				var plotArea = chartPart.ChartSpace.Descendants<DocumentFormat.OpenXml.Drawing.Charts.PlotArea>().FirstOrDefault();
//				if (plotArea == null)
//				{
//					return "Unknown Chart Type";
//				}

//				// Find the first chart element inside PlotArea
//				var chartElement = plotArea.Elements().FirstOrDefault(e => e.LocalName.EndsWith("Chart"));
//				if (chartElement != null)
//				{
//					return chartElement.LocalName;
//				}

//				return "Unknown Chart Type";
//			}
//			catch (Exception ex)
//			{
//				return $"Error detecting chart type: {ex.Message}";
//			}
//		}

//		private string GetChartAxes(ChartPart chartPart)
//		{
//			try
//			{
//				var axisTitles = new List<string>();

//				// X-Axis (Primary Horizontal Axis)
//				var xAxis = chartPart.ChartSpace.Descendants<CategoryAxis>().FirstOrDefault();
//				if (xAxis != null)
//				{
//					var xAxisTitle = xAxis.Descendants<Title>().FirstOrDefault();
//					var xAxisText = xAxisTitle?.Descendants<DocumentFormat.OpenXml.Drawing.Text>().FirstOrDefault()?.Text;
//					axisTitles.Add($"X-Axis: {xAxisText ?? "No Title"}");
//				}

//				// Y-Axis (Primary Vertical Axis)
//				var yAxis = chartPart.ChartSpace.Descendants<ValueAxis>().FirstOrDefault();
//				if (yAxis != null)
//				{
//					var yAxisTitle = yAxis.Descendants<Title>().FirstOrDefault();
//					var yAxisText = yAxisTitle?.Descendants<DocumentFormat.OpenXml.Drawing.Text>().FirstOrDefault()?.Text;
//					axisTitles.Add($"Y-Axis: {yAxisText ?? "No Title"}");
//				}

//				// Secondary Y-Axis (Secondary Vertical Axis)
//				var secondaryYAxis = chartPart.ChartSpace.Descendants<ValueAxis>().Skip(1).FirstOrDefault();
//				if (secondaryYAxis != null)
//				{
//					var secondaryYAxisTitle = secondaryYAxis.Descendants<Title>().FirstOrDefault();
//					var secondaryYAxisText = secondaryYAxisTitle?.Descendants<DocumentFormat.OpenXml.Drawing.Text>().FirstOrDefault()?.Text;
//					axisTitles.Add($"Secondary Y-Axis: {secondaryYAxisText ?? "No Title"}");
//				}

//				// Z-Axis (3D Charts)
//				var zAxis = chartPart.ChartSpace.Descendants<SeriesAxis>().FirstOrDefault();
//				if (zAxis != null)
//				{
//					var zAxisTitle = zAxis.Descendants<Title>().FirstOrDefault();
//					var zAxisText = zAxisTitle?.Descendants<DocumentFormat.OpenXml.Drawing.Text>().FirstOrDefault()?.Text;
//					axisTitles.Add($"Z-Axis: {zAxisText ?? "No Title"}");
//				}

//				return axisTitles.Count > 0 ? string.Join(", ", axisTitles) : "No Axes Titles Found";
//			}
//			catch (Exception ex)
//			{
//				return $"Error detecting axis titles: {ex.Message}";
//			}
//		}

//		private bool HasChartLegend(ChartPart chartPart)
//		{
//			return chartPart.ChartSpace.Descendants<Legend>().Any();
//		}

//		//private string GetChartData(ChartPart chartPart)
//		//{
//		//	try
//		//	{
//		//		var dataSources = new List<string>();

//		//		// 1. Get all possible series types
//		//		var seriesList = chartPart.ChartSpace.Descendants<LineChartSeries>()
//		//			.Cast<OpenXmlElement>()
//		//			.Concat(chartPart.ChartSpace.Descendants<BarChartSeries>())
//		//			.Concat(chartPart.ChartSpace.Descendants<PieChartSeries>());

//		//		foreach (var series in seriesList)
//		//		{
//		//			// 2. Get category data
//		//			var catFormula = GetDataRangeFromAxisData(series.Descendants<CategoryAxisData>().FirstOrDefault());

//		//			// 3. Get value data
//		//			var valFormula = GetDataRangeFromAxisData(series.Descendants<DocumentFormat.OpenXml.Drawing.Charts.Values>().FirstOrDefault());

//		//			if (!string.IsNullOrEmpty(catFormula))
//		//				dataSources.Add($"Category: {catFormula}");

//		//			if (!string.IsNullOrEmpty(valFormula))
//		//				dataSources.Add($"Value: {valFormula}");
//		//		}

//		//		// 4. Handle pivot sources
//		//		var pivotSource = chartPart.ChartSpace.Descendants<PivotSource>().FirstOrDefault();
//		//		if (pivotSource != null)
//		//		{
//		//			// Get pivot cache ID manually
//		//			var cacheIdAttr = pivotSource.GetAttribute("cacheId", "");
//		//			string cacheId = !string.IsNullOrEmpty(cacheIdAttr.Value) ? cacheIdAttr.Value : null;

//		//			if (!string.IsNullOrEmpty(cacheId))
//		//			{
//		//				var pivotCache = _workbookPart.PivotTableCacheDefinitionParts.FirstOrDefault(p =>
//		//				{
//		//					var attr = p.PivotCacheDefinition.GetAttribute("cacheId", "");
//		//					return !string.IsNullOrEmpty(attr.Value) && attr.Value == cacheId;
//		//				});

//		//				var pivotName = pivotCache?.PivotCacheDefinition
//		//					.Descendants<CacheSource>()
//		//					.FirstOrDefault()?.WorksheetSource?.Name;

//		//				if (!string.IsNullOrEmpty(pivotName))
//		//				{
//		//					dataSources.Add($"Pivot Table: {pivotName}");
//		//				}
//		//			}
//		//		}

//		//		if (dataSources.Count > 0)
//		//		{
//		//			var distinctDataSources = dataSources.Distinct().ToList();
//		//			return string.Join("; ", distinctDataSources);
//		//		}
//		//		else
//		//		{
//		//			return "No data sources detected";
//		//		}
//		//	}
//		//	catch (Exception ex)
//		//	{
//		//		return $"Error retrieving chart data: {ex.Message}";
//		//	}
//		//}

//		// Helper method to get data range from axis data
//		private string GetDataRangeFromAxisData(OpenXmlElement axisData)
//		{
//			if (axisData == null) return null;

//			var strRef = axisData.Elements<StringReference>().FirstOrDefault();
//			var numRef = axisData.Elements<NumberReference>().FirstOrDefault();

//			var formula = strRef?.Formula ?? numRef?.Formula;
//			return formula?.Text;
//		}
//	}
//}