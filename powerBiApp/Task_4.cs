

using JobSimulation.BLL;
using JobSimulation.DAL;
using JobSimulation.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Microsoft.AnalysisServices.AdomdClient;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Newtonsoft.Json.Linq;
using System.Buffers;
using Microsoft.Win32;
using Microsoft.AnalysisServices; // Add this namespace
using Microsoft.AnalysisServices.Tabular;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Management;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Data;
using System.Security.Principal;
using System.Security;
//using Microsoft.AnalysisServices.AdomdClient;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AnalysisServices.AdomdClient;
using System.Xml;
using System.Data.Common;

namespace JobSimulation.powerBiApp
{
    public class Task_4
    {
        public string DataLayoutProcessor(string pbixFilePath)
        {
            try
            {
                string extractTo = Path.Combine(Directory.GetCurrentDirectory(), "extracted_pbix1");
                string layoutPath = ExtractPbix(pbixFilePath, extractTo);
                if (layoutPath == null)
                    return "{\"error\": \"Failed to locate the layout file.\"}";

                LayoutData layoutData = ReadLayoutFile(layoutPath);
                if (layoutData == null)
                    return "{\"error\": \"No data found in layout.\"}";

                // Clean all string properties in all ChartData objects
                if (layoutData.Sections != null)
                {
                    foreach (var section in layoutData.Sections)
                    {
                        if (section.Charts != null)
                        {
                            foreach (var chart in section.Charts)
                            {
                                chart.Title = StringCleaner.Clean(chart.Title);
                                chart.FontFamily = StringCleaner.Clean(chart.FontFamily);
                                chart.FontColor = StringCleaner.Clean(chart.FontColor);
                                chart.Alignment = StringCleaner.Clean(chart.Alignment);
                                chart.Bold = StringCleaner.Clean(chart.Bold);
                                chart.Underline = StringCleaner.Clean(chart.Underline);
                                chart.Id = StringCleaner.Clean(chart.Id);
                                chart.Type = StringCleaner.Clean(chart.Type);
                                chart.Filters = StringCleaner.Clean(chart.Filters);
                                chart.FontWeight = StringCleaner.Clean(chart.FontWeight);
                                chart.FontStyle = StringCleaner.Clean(chart.FontStyle);
                                chart.FontSize = StringCleaner.Clean(chart.FontSize);
                                chart.TextDecoration = StringCleaner.Clean(chart.TextDecoration);
                                chart.Height = StringCleaner.Clean(chart.Height);
                                chart.Width = StringCleaner.Clean(chart.Width);
                                chart.X = StringCleaner.Clean(chart.X);
                                chart.Y = StringCleaner.Clean(chart.Y);
                            }
                        }
                    }
                }

                var output = new
                {
                    DataLayoutProcessor = layoutData
                };
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = null, // use property names as defined by [JsonPropertyName]
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                return JsonSerializer.Serialize(output, options);
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"{ex.Message}\"}}";
            }
        }

        public string ExtractPbix(string pbixFilePath, string extractTo)
        {
            try
            {
                if (!File.Exists(pbixFilePath))
                    return null;

                if (Directory.Exists(extractTo))
                    Directory.Delete(extractTo, true);
                Directory.CreateDirectory(extractTo);

                ZipFile.ExtractToDirectory(pbixFilePath, extractTo);

                string reportFolder = Path.Combine(extractTo, "Report");
                if (!Directory.Exists(reportFolder))
                    return null;

                string layoutPath = Path.Combine(reportFolder, "Layout.json");
                if (!File.Exists(layoutPath))
                    layoutPath = Path.Combine(reportFolder, "Layout");
                return File.Exists(layoutPath) ? layoutPath : null;
            }
            catch
            {
                return null;
            }
        }

        public LayoutData ReadLayoutFile(string layoutPath)
        {
            try
            {
                if (!File.Exists(layoutPath))
                    return null;

                string jsonContent = File.ReadAllText(layoutPath, Encoding.Unicode);

                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                {
                    JsonElement root = doc.RootElement;
                    var layoutData = new LayoutData();

                    layoutData.ReportTitle = root.TryGetProperty("displayName", out JsonElement displayNameElem)
                        ? GetPropertyAsString(displayNameElem)
                        : "No Title";

                    if (root.TryGetProperty("sections", out JsonElement sectionsElem) && sectionsElem.ValueKind == JsonValueKind.Array)
                    {
                        var sectionsList = new List<SectionData>();
                        foreach (JsonElement sectionElem in sectionsElem.EnumerateArray())
                        {
                            var sectionData = new SectionData
                            {
                                SectionName = sectionElem.TryGetProperty("name", out JsonElement nameElem)
                                                ? nameElem.GetString() : "Unknown Section",
                                DisplayName = sectionElem.TryGetProperty("displayName", out JsonElement secDisplayNameElem)
                                                ? secDisplayNameElem.GetString() : "No Display Name"
                            };

                            var chartsList = new List<ChartData>();
                            if (sectionElem.TryGetProperty("visualContainers", out JsonElement visualContainersElem) &&
                                visualContainersElem.ValueKind == JsonValueKind.Array)
                            {
                                foreach (JsonElement container in visualContainersElem.EnumerateArray())
                                {
                                    if (container.TryGetProperty("config", out JsonElement configElem) &&
                                        configElem.ValueKind == JsonValueKind.String)
                                    {
                                        string configStr = configElem.GetString();
                                        try
                                        {
                                            using (JsonDocument configDoc = JsonDocument.Parse(configStr))
                                            {
                                                if (!configDoc.RootElement.TryGetProperty("singleVisual", out JsonElement singleVisual))
                                                    continue;

                                                string visualType = singleVisual.TryGetProperty("visualType", out JsonElement typeElem)
                                                    ? typeElem.GetString() : "Unknown";
                                                string lowerType = visualType.ToLower();

                                                string[] allowedChartTypes = new string[]
                                                {
                                                "bar", "column", "line", "pie", "donut", "doughnut", "donutchart", "table", "matrix",
                                                "card", "multirowcard", "kpi", "scatter", "bubble", "map", "filledmap", "shapemap",
                                                "treemap", "funnel", "gauge", "waterfall", "combo", "lineandstackedcolumnchart",
                                                "lineandclusteredcolumnchart", "area", "stackedarea", "100stackedarea", "radar",
                                                "spider", "ribbonchart", "decompostiontree"
                                                };

                                                bool isAllowed = allowedChartTypes.Any(allowed => lowerType.Contains(allowed));
                                                if (!isAllowed)
                                                    continue;

                                                var chartData = new ChartData();
                                                chartData.Id = container.TryGetProperty("id", out JsonElement idElem)
                                                    ? idElem.GetString() : "N/A";
                                                chartData.Type = visualType;
                                                chartData.Height = container.TryGetProperty("height", out JsonElement heightElem)
                                                    ? heightElem.ToString() : "N/A";
                                                chartData.Width = container.TryGetProperty("width", out JsonElement widthElem)
                                                    ? widthElem.ToString() : "N/A";
                                                chartData.X = container.TryGetProperty("x", out JsonElement xElem)
                                                    ? xElem.ToString() : "N/A";
                                                chartData.Y = container.TryGetProperty("y", out JsonElement yElem)
                                                    ? yElem.ToString() : "N/A";

                                                // Title & style props
                                                if (singleVisual.TryGetProperty("vcObjects", out JsonElement vcObjects) &&
                                                    vcObjects.ValueKind == JsonValueKind.Object)
                                                {
                                                    if (vcObjects.TryGetProperty("title", out JsonElement titleArray) &&
                                                        titleArray.ValueKind == JsonValueKind.Array &&
                                                        titleArray.GetArrayLength() > 0)
                                                    {
                                                        JsonElement titleObj = titleArray[0];
                                                        if (titleObj.TryGetProperty("properties", out JsonElement titleProps))
                                                        {
                                                            chartData.Title = titleProps.TryGetProperty("text", out JsonElement textProps)
                                                                ? GetPropertyAsString(textProps) : "No Title";
                                                            chartData.FontWeight = titleProps.TryGetProperty("fontWeight", out JsonElement fontWeightProp)
                                                                ? GetPropertyAsString(fontWeightProp) : "Normal";
                                                            chartData.FontStyle = titleProps.TryGetProperty("fontStyle", out JsonElement fontStyleProp)
                                                                ? GetPropertyAsString(fontStyleProp) : "Normal";
                                                            chartData.FontSize = titleProps.TryGetProperty("fontSize", out JsonElement fontSizeProp)
                                                                ? GetPropertyAsString(fontSizeProp) : "Default";
                                                            chartData.TextDecoration = titleProps.TryGetProperty("textDecoration", out JsonElement textDecorationProp)
                                                                ? GetPropertyAsString(textDecorationProp) : "None";
                                                            chartData.FontFamily = titleProps.TryGetProperty("fontFamily", out JsonElement fontFamilyProp)
                                                                ? GetPropertyAsString(fontFamilyProp) : "Default";
                                                            chartData.FontColor = titleProps.TryGetProperty("fontColor", out JsonElement fontColorProp)
                                                                ? GetPropertyAsString(fontColorProp) : "Default";
                                                            chartData.Alignment = titleProps.TryGetProperty("alignment", out JsonElement alignmentProp)
                                                                ? GetPropertyAsString(alignmentProp) : "Left";
                                                            chartData.Bold = titleProps.TryGetProperty("bold", out JsonElement boldProp)
                                                                ? GetPropertyAsString(boldProp) : "False";
                                                            chartData.Underline = titleProps.TryGetProperty("underline", out JsonElement underlineProp)
                                                                ? GetPropertyAsString(underlineProp) : "False";
                                                        }
                                                    }
                                                }

                                                // Aggregations from projections
                                                if (singleVisual.TryGetProperty("projections", out JsonElement projections) &&
                                                    projections.ValueKind == JsonValueKind.Object)
                                                {
                                                    foreach (JsonProperty prop in projections.EnumerateObject())
                                                    {
                                                        if (prop.Value.ValueKind == JsonValueKind.Array)
                                                        {
                                                            foreach (JsonElement item in prop.Value.EnumerateArray())
                                                            {
                                                                if (item.TryGetProperty("queryRef", out JsonElement queryRef))
                                                                {
                                                                    chartData.Aggregations.Add(queryRef.GetString());
                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                                // Filters (raw JSON)
                                                if (container.TryGetProperty("filters", out JsonElement filtersElem))
                                                {
                                                    chartData.Filters = filtersElem.GetRawText();
                                                }

                                                chartsList.Add(chartData);
                                            }
                                        }
                                        catch (JsonException) { }
                                    }
                                }
                            }
                            sectionData.Charts = chartsList.ToArray();
                            sectionsList.Add(sectionData);
                        }
                        layoutData.Sections = sectionsList.ToArray();
                    }
                    else
                    {
                        layoutData.Sections = Array.Empty<SectionData>();
                    }
                    return layoutData;
                }
            }
            catch
            {
                return null;
            }
        }

        private string GetPropertyAsString(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("expr", out JsonElement expr) &&
                    expr.ValueKind == JsonValueKind.Object &&
                    expr.TryGetProperty("Literal", out JsonElement literal) &&
                    literal.ValueKind == JsonValueKind.Object &&
                    literal.TryGetProperty("Value", out JsonElement value))
                {
                    return value.GetString();
                }
                return element.ToString();
            }
            else
            {
                return element.ToString();
            }
        }

        public class LayoutData
        {
            public string ReportTitle { get; set; }
            public SectionData[] Sections { get; set; }
        }

        public class SectionData
        {
            public string SectionName { get; set; }
            public string DisplayName { get; set; }
            public ChartData[] Charts { get; set; }
        }

        public class ChartData
        {
            [JsonPropertyName("Id")]
            public string Id { get; set; }
            [JsonPropertyName("Type")]
            public string Type { get; set; }
            [JsonPropertyName("Title")]
            public string Title { get; set; }
            [JsonPropertyName("Font")]
            public string FontFamily { get; set; }
            [JsonPropertyName("FontColor")]
            public string FontColor { get; set; }
            [JsonPropertyName("Height")]
            public string Height { get; set; }
            [JsonPropertyName("Width")]
            public string Width { get; set; }
            [JsonPropertyName("X")]
            public string X { get; set; }
            [JsonPropertyName("Y")]
            public string Y { get; set; }
            [JsonPropertyName("Bold")]
            public string Bold { get; set; }
            [JsonPropertyName("Underline")]
            public string Underline { get; set; }
            [JsonPropertyName("Filters")]
            public string Filters { get; set; }
            [JsonPropertyName("Aggregations")]
            public List<string> Aggregations { get; set; } = new List<string>();
            [JsonPropertyName("Colors")]
            public List<string> Colors { get; set; } = new List<string>();
            [JsonPropertyName("Fonts")]
            public List<string> Fonts { get; set; } = new List<string>();
            [JsonPropertyName("FontWeight")]
            public string FontWeight { get; set; }
            [JsonPropertyName("FontStyle")]
            public string FontStyle { get; set; }
            [JsonPropertyName("FontSize")]
            public string FontSize { get; set; }
            [JsonPropertyName("TextDecoration")]
            public string TextDecoration { get; set; }
            [JsonPropertyName("Alignment")]
            public string Alignment { get; set; }
        }

        public static class StringCleaner
        {
            public static string Clean(string input)
            {
                if (string.IsNullOrEmpty(input)) return input;
                // Replace unicode single quote and double quote with actual quote
                input = input.Replace("\\u0027", "'").Replace("\u0027", "'").Replace("\\u0022", "\"").Replace("\u0022", "\"");
                // Replace escaped slashes
                input = input.Replace(@"\\", @"\");
                // Remove unnecessary outer quotes if present
                if (input.Length > 1 && input.StartsWith("\"") && input.EndsWith("\""))
                    input = input.Substring(1, input.Length - 2);
                return input;
            }
        }


    }

}
