using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JobSimulation.powerBiApp
{
    public class Task_6
    {
        /// <summary>
        /// Extracts slicer details from the given PBIX/PBIT file and returns the data as a JSON-formatted string.
        /// </summary>
        /// <param name="pbixFilePath">The dynamic PBIX/PBIT file path.</param>
        /// <returns>JSON-formatted string with slicer details.</returns>
        public string SlicerManager(string pbixFilePath)
        {
            // Use the dynamic file path passed as argument.
            string extractTo = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "extracted_pbix1");

            // Extract the layout file.
            string layoutPath = ExtractPbix(pbixFilePath, extractTo);

            List<SlicerDataFormatted> allSlicers = new List<SlicerDataFormatted>(); // This will hold the final list of slicers

            if (layoutPath != null)
            {
                // Read and extract slicer details from the layout file.
                SlicerLayoutData layoutData = ReadSlicerLayoutFile(layoutPath);
                if (layoutData != null)
                {
                    // Iterate through sections and slicers to format the data
                    foreach (SlicerSectionData section in layoutData.Sections)
                    {
                        foreach (SlicerData slicer in section.Slicers)
                        {
                            SlicerDataFormatted formattedSlicer = new SlicerDataFormatted
                            {
                                ID = "T14",  //Hardcoded, change if required to be from PBIX
                                sectionName = section.SectionName,
                                PageName = "11", //Hardcoded, change if required to be from PBIX
                                slicerID = slicer.Id,
                                visualType = slicer.VisualType,
                                orientation = slicer.Orientation,
                                height = ParseDouble(slicer.Height),  // Parse to double if possible
                                width = ParseDouble(slicer.Width),    // Parse to double if possible
                                x = ParseDouble(slicer.X),            // Parse to double if possible
                                y = ParseDouble(slicer.Y),            // Parse to double if possible
                                title = slicer.Title
                            };
                            allSlicers.Add(formattedSlicer);
                        }
                    }
                }
            }

            // Wrap output in a JSON object with the "slicerManager" root.
            var finalOutput = new { slicerManager = new { slicers = allSlicers } };  //  <-- JSON Structure here!
            var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            return JsonSerializer.Serialize(finalOutput, options);
        }

        //Helper method to parse double safely
        private double? ParseDouble(string value)
        {
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }
            return null; // Or some other default value if parsing fails
        }

        public string ExtractPbix(string pbixFilePath, string extractTo)
        {
            try
            {
                if (!File.Exists(pbixFilePath))
                {
                    Console.WriteLine("PBIX file not found: " + pbixFilePath);
                    return null;
                }

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
                                Directory.CreateDirectory(directoryPath);

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

        public SlicerLayoutData ReadSlicerLayoutFile(string layoutPath)
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
                    SlicerLayoutData layoutData = new SlicerLayoutData();
                    layoutData.ReportTitle = root.TryGetProperty("displayName", out JsonElement displayNameElem)
                        ? UnescapeString(displayNameElem.GetString())  //Unescape
                        : "No Title";

                    if (root.TryGetProperty("sections", out JsonElement sectionsElem) && sectionsElem.ValueKind == JsonValueKind.Array)
                    {
                        List<SlicerSectionData> sectionsList = new List<SlicerSectionData>();
                        foreach (JsonElement sectionElem in sectionsElem.EnumerateArray())
                        {
                            SlicerSectionData sectionData = new SlicerSectionData();
                            sectionData.SectionName = sectionElem.TryGetProperty("name", out JsonElement nameElem)
                                ? UnescapeString(nameElem.GetString()) //Unescape
                                : "Unknown Section";
                            sectionData.DisplayName = sectionElem.TryGetProperty("displayName", out JsonElement secDisplayNameElem)
                                ? UnescapeString(secDisplayNameElem.GetString())  //Unescape
                                : "No Display Name";

                            List<SlicerData> slicersList = new List<SlicerData>();

                            if (sectionElem.TryGetProperty("visualContainers", out JsonElement visualContainersElem) && visualContainersElem.ValueKind == JsonValueKind.Array)
                            {
                                foreach (JsonElement container in visualContainersElem.EnumerateArray())
                                {
                                    if (container.TryGetProperty("config", out JsonElement configElem) && configElem.ValueKind == JsonValueKind.String)
                                    {
                                        string configStr = container.TryGetProperty("config", out JsonElement configElem2) ? UnescapeString(configElem2.GetString()) : "";

                                        try
                                        {
                                            using (JsonDocument configDoc = JsonDocument.Parse(configStr))
                                            {
                                                if (!configDoc.RootElement.TryGetProperty("singleVisual", out JsonElement singleVisual))
                                                    continue;

                                                // Process only visuals that are slicers.
                                                string visualType = singleVisual.TryGetProperty("visualType", out JsonElement typeElem)
                                                    ? UnescapeString(typeElem.GetString())  //Unescape
                                                    : "Unknown";
                                                if (!visualType.ToLower().Contains("slicer"))
                                                    continue;

                                                SlicerData slicerData = new SlicerData();
                                                slicerData.VisualType = visualType;
                                                slicerData.Id = container.TryGetProperty("id", out JsonElement idElem)
                                                    ? UnescapeString(idElem.GetString())  //Unescape
                                                    : "N/A";
                                                slicerData.Height = container.TryGetProperty("height", out JsonElement heightElem)
                                                    ? UnescapeString(heightElem.ToString())  //Unescape
                                                    : "N/A";
                                                slicerData.Width = container.TryGetProperty("width", out JsonElement widthElem)
                                                    ? UnescapeString(widthElem.ToString())  //Unescape
                                                    : "N/A";
                                                slicerData.X = container.TryGetProperty("x", out JsonElement xElem)
                                                    ? UnescapeString(xElem.ToString()) //Unescape
                                                    : "N/A";
                                                slicerData.Y = container.TryGetProperty("y", out JsonElement yElem)
                                                    ? UnescapeString(yElem.ToString())  //Unescape
                                                    : "N/A";

                                                // Extract title from vcObjects (if available).
                                                if (singleVisual.TryGetProperty("vcObjects", out JsonElement vcObjects) && vcObjects.ValueKind == JsonValueKind.Object)
                                                {
                                                    if (vcObjects.TryGetProperty("title", out JsonElement titleArray) &&
                                                        titleArray.ValueKind == JsonValueKind.Array &&
                                                        titleArray.GetArrayLength() > 0)
                                                    {
                                                        JsonElement titleObj = titleArray[0];
                                                        if (titleObj.TryGetProperty("properties", out JsonElement titleProps) &&
                                                            titleProps.TryGetProperty("text", out JsonElement textProps) &&
                                                            textProps.TryGetProperty("expr", out JsonElement exprProps) &&
                                                            exprProps.TryGetProperty("Literal", out JsonElement literalProps) &&
                                                            literalProps.TryGetProperty("Value", out JsonElement valueElem))
                                                        {
                                                            slicerData.Title = UnescapeString(valueElem.GetString()); //Unescape
                                                        }
                                                        else
                                                        {
                                                            slicerData.Title = "No Title";
                                                        }
                                                    }
                                                    else
                                                    {
                                                        slicerData.Title = "No Title";
                                                    }
                                                }
                                                else
                                                {
                                                    slicerData.Title = "No Title";
                                                }

                                                // Extract slicer orientation from objects.data array.
                                                slicerData.Orientation = "N/A";
                                                if (singleVisual.TryGetProperty("objects", out JsonElement objects) && objects.ValueKind == JsonValueKind.Object)
                                                {
                                                    if (objects.TryGetProperty("data", out JsonElement dataElem) &&
                                                        dataElem.ValueKind == JsonValueKind.Array &&
                                                        dataElem.GetArrayLength() > 0)
                                                    {
                                                        JsonElement data0 = dataElem[0];
                                                        if (data0.TryGetProperty("properties", out JsonElement props) && props.ValueKind == JsonValueKind.Object)
                                                        {
                                                            if (props.TryGetProperty("mode", out JsonElement modeProp) &&
                                                                modeProp.TryGetProperty("expr", out JsonElement exprElem) &&
                                                                exprElem.TryGetProperty("Literal", out JsonElement literalElem) &&
                                                                literalElem.TryGetProperty("Value", out JsonElement valueElem))
                                                            {
                                                                slicerData.Orientation = UnescapeString(valueElem.GetString()); //Unescape
                                                            }
                                                        }
                                                    }
                                                }
                                                slicersList.Add(slicerData);
                                            }
                                        }
                                        catch (JsonException je)
                                        {
                                            Console.WriteLine("Error parsing config JSON: " + je.Message);
                                        }
                                    }
                                }
                            }
                            sectionData.Slicers = slicersList.ToArray();
                            sectionsList.Add(sectionData);
                        }
                        layoutData.Sections = sectionsList.ToArray();
                    }
                    else
                    {
                        layoutData.Sections = Array.Empty<SlicerSectionData>();
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

        // Helper method to unescape strings
        private string UnescapeString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return Regex.Unescape(input);
        }

        // Data structure for the desired JSON output format.  This is the MOST important change!
        public class SlicerDataFormatted
        {
            [JsonPropertyName("ID")]
            public string ID { get; set; }

            [JsonPropertyName("sectionName")]
            public string sectionName { get; set; }

            [JsonPropertyName("PageName")]
            public string PageName { get; set; }

            [JsonPropertyName("slicerID")]
            public string slicerID { get; set; }

            [JsonPropertyName("visualType")]
            public string visualType { get; set; }

            [JsonPropertyName("orientation")]
            public string orientation { get; set; }

            [JsonPropertyName("height")]
            public double? height { get; set; }

            [JsonPropertyName("width")]
            public double? width { get; set; }

            [JsonPropertyName("x")]
            public double? x { get; set; }

            [JsonPropertyName("y")]
            public double? y { get; set; }

            [JsonPropertyName("title")]
            public string title { get; set; }
        }

        // Data structures for slicer layout details (no changes needed to these classes).
        public class SlicerLayoutData
        {
            public string ReportTitle { get; set; }
            public SlicerSectionData[] Sections { get; set; }
        }

        public class SlicerSectionData
        {
            public string SectionName { get; set; }
            public string DisplayName { get; set; }
            public SlicerData[] Slicers { get; set; }
        }

        public class SlicerData
        {
            public string Id { get; set; }
            public string VisualType { get; set; }  // Typically "slicer"
            public string Height { get; set; }
            public string Width { get; set; }
            public string X { get; set; }
            public string Y { get; set; }
            public string Title { get; set; }
            public string Orientation { get; set; } // e.g., Dropdown, Vertical, etc.
        }
    }
}
