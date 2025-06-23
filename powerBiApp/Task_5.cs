using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace JobSimulation.powerBiApp
{
    public class Task_5
    {
        /// <summary>
        /// Extracts textbox details from the PBIX/PBIT file and returns the data in JSON format.
        /// </summary>
        public string TextboxManager(string pbixFilePath)
        {
            try
            {
                // Specify extraction folder.
                string extractTo = Path.Combine(Directory.GetCurrentDirectory(), "extracted_pbix1");
                string layoutPath = ExtractPbix(pbixFilePath, extractTo);

                if (layoutPath == null)
                {
                    return "{\"error\": \"Failed to locate the layout file.\"}";
                }

                // Read and extract textbox details from the layout file.
                TextboxLayoutData layoutData = ReadTextboxLayoutFile(layoutPath);
                if (layoutData == null)
                {
                    return "{\"error\": \"No textbox data found in layout.\"}";
                }

                // Build final output object.
                var output = new
                {
                    TextboxManager = layoutData
                };
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = null, // Keeps your [JsonPropertyName] attributes
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Prevents \u0027, keeps real quotes
                };
                return JsonSerializer.Serialize(output, options);
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"{ex.Message}\"}}";
            }
        }

        /// <summary>
        /// Extracts the PBIX/PBIT file (a ZIP archive) to the specified folder and returns the layout file path.
        /// </summary>
        public string ExtractPbix(string pbixFilePath, string extractTo)
        {
            try
            {
                if (!File.Exists(pbixFilePath))
                {
                    Console.WriteLine("PBIX file not found: " + pbixFilePath);
                    return null;
                }

                // Ensure the extraction folder exists.
                Directory.CreateDirectory(extractTo);
                string layoutRelativePath = "Report/Layout";
                string extractedLayoutFilePath = null;

                using (ZipArchive archive = ZipFile.OpenRead(pbixFilePath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName.Equals(layoutRelativePath, StringComparison.OrdinalIgnoreCase))
                        {
                            string destinationPath = Path.Combine(extractTo, entry.FullName);
                            string directoryPath = Path.GetDirectoryName(destinationPath);
                            if (!string.IsNullOrEmpty(directoryPath))
                            {
                                Directory.CreateDirectory(directoryPath);
                            }
                            entry.ExtractToFile(destinationPath, overwrite: true);
                            extractedLayoutFilePath = destinationPath;
                            break;
                        }
                    }
                }

                if (extractedLayoutFilePath != null)
                    return extractedLayoutFilePath;
                else
                {
                    Console.WriteLine("Layout file not found in the PBIX archive.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error extracting PBIX: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Reads the layout file and extracts textbox details.
        /// </summary>
        public TextboxLayoutData ReadTextboxLayoutFile(string layoutPath)
        {
            try
            {
                if (!File.Exists(layoutPath))
                {
                    Console.WriteLine("Layout file does not exist: " + layoutPath);
                    return null;
                }

                string jsonContent = File.ReadAllText(layoutPath, Encoding.Unicode);
                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                {
                    JsonElement root = doc.RootElement;
                    TextboxLayoutData layoutData = new TextboxLayoutData();
                    layoutData.ReportTitle = root.TryGetProperty("displayName", out JsonElement displayNameElem)
                        ? displayNameElem.GetString()
                        : "No Title";

                    if (root.TryGetProperty("sections", out JsonElement sectionsElem) && sectionsElem.ValueKind == JsonValueKind.Array)
                    {
                        List<TextboxSectionData> sectionsList = new List<TextboxSectionData>();
                        foreach (JsonElement sectionElem in sectionsElem.EnumerateArray())
                        {
                            TextboxSectionData sectionData = new TextboxSectionData();
                            sectionData.SectionName = sectionElem.TryGetProperty("name", out JsonElement nameElem)
                                ? nameElem.GetString() : "Unknown Section";
                            sectionData.DisplayName = sectionElem.TryGetProperty("displayName", out JsonElement secDisplayNameElem)
                                ? secDisplayNameElem.GetString() : "No Display Name";

                            List<TextboxData> textboxesList = new List<TextboxData>();

                            if (sectionElem.TryGetProperty("visualContainers", out JsonElement visualContainersElem) &&
                                visualContainersElem.ValueKind == JsonValueKind.Array)
                            {
                                foreach (JsonElement container in visualContainersElem.EnumerateArray())
                                {
                                    if (container.TryGetProperty("config", out JsonElement configElem))
                                    {
                                        JsonElement configObj;
                                        if (configElem.ValueKind == JsonValueKind.String)
                                        {
                                            string configStr = configElem.GetString();
                                            using (JsonDocument configDoc = JsonDocument.Parse(configStr))
                                            {
                                                configObj = configDoc.RootElement.Clone();
                                            }
                                        }
                                        else if (configElem.ValueKind == JsonValueKind.Object)
                                        {
                                            configObj = configElem;
                                        }
                                        else
                                        {
                                            continue;
                                        }

                                        try
                                        {
                                            if (!configObj.TryGetProperty("singleVisual", out JsonElement singleVisual))
                                                continue;

                                            // Process only visuals that are textboxes.
                                            string visualType = singleVisual.TryGetProperty("visualType", out JsonElement typeElem)
                                                ? typeElem.GetString() : "Unknown";
                                            if (!visualType.ToLower().Contains("textbox"))
                                                continue;

                                            TextboxData textboxData = new TextboxData();
                                            textboxData.VisualType = visualType;
                                            textboxData.Id = container.TryGetProperty("id", out JsonElement idElem)
                                                ? idElem.GetString() : "N/A";
                                            textboxData.Height = container.TryGetProperty("height", out JsonElement heightElem)
                                                ? heightElem.ToString() : "N/A";
                                            textboxData.Width = container.TryGetProperty("width", out JsonElement widthElem)
                                                ? widthElem.ToString() : "N/A";
                                            textboxData.X = container.TryGetProperty("x", out JsonElement xElem)
                                                ? xElem.ToString() : "N/A";
                                            textboxData.Y = container.TryGetProperty("y", out JsonElement yElem)
                                                ? yElem.ToString() : "N/A";

                                            // Extract text content and formatting.
                                            if (singleVisual.TryGetProperty("objects", out JsonElement objects) &&
                                                objects.ValueKind == JsonValueKind.Object &&
                                                objects.TryGetProperty("general", out JsonElement generalArray) &&
                                                generalArray.ValueKind == JsonValueKind.Array &&
                                                generalArray.GetArrayLength() > 0)
                                            {
                                                JsonElement general0 = generalArray[0];
                                                if (general0.TryGetProperty("properties", out JsonElement properties) &&
                                                    properties.ValueKind == JsonValueKind.Object &&
                                                    properties.TryGetProperty("paragraphs", out JsonElement paragraphs) &&
                                                    paragraphs.ValueKind == JsonValueKind.Array &&
                                                    paragraphs.GetArrayLength() > 0)
                                                {
                                                    JsonElement paragraph0 = paragraphs[0];
                                                    if (paragraph0.TryGetProperty("textRuns", out JsonElement textRuns) &&
                                                        textRuns.ValueKind == JsonValueKind.Array &&
                                                        textRuns.GetArrayLength() > 0)
                                                    {
                                                        JsonElement textRun0 = textRuns[0];
                                                        if (textRun0.TryGetProperty("value", out JsonElement valueElem))
                                                        {
                                                            textboxData.Content = valueElem.GetString();
                                                        }
                                                        else
                                                        {
                                                            textboxData.Content = "No Content";
                                                        }
                                                        if (textRun0.TryGetProperty("textStyle", out JsonElement textStyle) &&
                                                            textStyle.ValueKind == JsonValueKind.Object)
                                                        {
                                                            textboxData.FontWeight = textStyle.TryGetProperty("fontWeight", out JsonElement fw) ? fw.GetString() : "N/A";
                                                            textboxData.FontStyle = textStyle.TryGetProperty("fontStyle", out JsonElement fs) ? fs.GetString() : "N/A";
                                                            textboxData.FontSize = textStyle.TryGetProperty("fontSize", out JsonElement fsize) ? fsize.GetString() : "N/A";
                                                            textboxData.TextDecoration = textStyle.TryGetProperty("textDecoration", out JsonElement td) ? td.GetString() : "N/A";
                                                        }
                                                        else
                                                        {
                                                            textboxData.FontWeight = "N/A";
                                                            textboxData.FontStyle = "N/A";
                                                            textboxData.FontSize = "N/A";
                                                            textboxData.TextDecoration = "N/A";
                                                        }
                                                    }
                                                }
                                            }
                                            textboxesList.Add(textboxData);
                                        }
                                        catch (JsonException je)
                                        {
                                            Console.WriteLine("Error parsing config JSON: " + je.Message);
                                        }
                                    }
                                }
                            }

                            sectionData.Textboxes = textboxesList.ToArray();
                            sectionsList.Add(sectionData);
                        }
                        layoutData.Sections = sectionsList.ToArray();
                    }
                    else
                    {
                        layoutData.Sections = Array.Empty<TextboxSectionData>();
                    }
                    return layoutData;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading layout: " + ex.Message);
                return null;
            }
        }

        // Data structures for textbox layout.
        public class TextboxLayoutData
        {
            public string ReportTitle { get; set; }
            public TextboxSectionData[] Sections { get; set; }
        }

        public class TextboxSectionData
        {
            public string SectionName { get; set; }
            public string DisplayName { get; set; }
            public TextboxData[] Textboxes { get; set; }
        }

        public class TextboxData
        {
            public string Id { get; set; }
            public string VisualType { get; set; }  // Typically "textbox"
            public string Height { get; set; }
            public string Width { get; set; }
            public string X { get; set; }
            public string Y { get; set; }
            public string Content { get; set; }
            public string FontWeight { get; set; }
            public string FontStyle { get; set; }
            public string FontSize { get; set; }
            public string TextDecoration { get; set; }
        }
    }

}
