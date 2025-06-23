
//public bool ValidatePowerBiTask(TaskSubmission taskSubmission, string masterJson)
//{
//    try
//    {
//        var details = taskSubmission.Task.Details as dynamic;

//        // Add pre-validation
//        if (!File.Exists(taskSubmission.FilePath))
//        {
//            throw new FileNotFoundException("PBIX file not found");
//        }

//        // Convert PBIX to PBIT and extract data
//        string extractedData = ProcessPowerBiFile(taskSubmission.FilePath, details.SelectTask);

//        // Handle empty responses
//        if (string.IsNullOrWhiteSpace(extractedData))
//        {
//            throw new InvalidOperationException("No data extracted from PBIX file");
//        }

//        // Deserialize with error handling
//        try
//        {
//            var extractedJson = JsonSerializer.Deserialize<Dictionary<string, object>>(extractedData);
//            var masterJsonObj = JsonSerializer.Deserialize<Dictionary<string, object>>(masterJson);
//            return CompareJsonObjects(extractedJson, masterJsonObj);
//        }
//        catch (JsonException jsonEx)
//        {
//            throw new InvalidOperationException("Invalid JSON format in extracted data", jsonEx);
//        }
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"Validation error: {ex.ToString()}");
//        return false;
//    }
//}

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
using static JobSimulation.powerBiApp.PBIXProcessor;
using System.IO.Compression;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
//using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.AnalysisServices.Tabular;
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


namespace JobSimulation.powerBiApp
{

    public class PowerBiValidationService
    {
        private readonly ValidationService _validationService;
        private readonly PowerBiDatabase _powerBiDatabase;

        public PowerBiValidationService(ValidationService validationService, PowerBiDatabase powerBiDatabase)
        {
            _validationService = validationService;
            _powerBiDatabase = powerBiDatabase;
        }

        public string GetMasterJsonForSection(string sectionId)
        {
            return _powerBiDatabase.FetchMasterJson(sectionId);
        }


        public string GeneratePowerBITemplate(string pbixFilePath)
        {
            // Validate input file (same as before)
            string tempDir = null;
            string newZip = null;
            string pbitOutputPath = null;

            try
            {
                // Create temporary working directory
                tempDir = Path.Combine(Path.GetTempPath(), "PbixExtract_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                // Step 1: Create temporary zip copy directly in temp (not in extraction dir)
                string tempZipPath = Path.Combine(Path.GetTempPath(), "temp_" + Guid.NewGuid() + ".zip");
                File.Copy(pbixFilePath, tempZipPath, true);

                // Step 2: Extract zip contents
                ZipFile.ExtractToDirectory(tempZipPath, tempDir);
                File.Delete(tempZipPath);  // Cleanup temp zip

                // Step 3: Remove sensitive data
                string dataMashupPath = Path.Combine(tempDir, "DataMashup");
                if (File.Exists(dataMashupPath))
                    File.Delete(dataMashupPath);

                // Step 4: Create new zip package OUTSIDE the extraction directory
                newZip = Path.Combine(Path.GetTempPath(), "template_" + Guid.NewGuid() + ".zip");

                // Using explicit disposal to release handles immediately
                using (var archive = ZipFile.Open(newZip, ZipArchiveMode.Create))
                {
                    foreach (var file in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
                    {
                        var relativePath = file.Substring(tempDir.Length + 1);
                        archive.CreateEntryFromFile(file, relativePath);
                    }
                }

                // Step 5: Generate output path
                pbitOutputPath = Path.ChangeExtension(pbixFilePath, ".pbit");
                if (File.Exists(pbitOutputPath))
                    File.Delete(pbitOutputPath);

                File.Move(newZip, pbitOutputPath);
                return pbitOutputPath;
            }
            catch (Exception ex)
            {
                // Cleanup partial output
                if (pbitOutputPath != null && File.Exists(pbitOutputPath))
                    File.Delete(pbitOutputPath);
                if (newZip != null && File.Exists(newZip))
                    File.Delete(newZip);

                throw new InvalidOperationException("Failed to generate PBIT template", ex);
            }
            finally
            {
                // Cleanup temporary directory
                try
                {
                    if (tempDir != null && Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
                catch { /* Suppress */ }
            }
        }
        public bool ValidatePowerBiTask(TaskSubmission taskSubmission, string masterJson)
        {
            try
            {
                var details = taskSubmission.Task.Details as PowerBiDetails;
                if (details == null) throw new ArgumentException("Invalid task details type");
                if (!File.Exists(taskSubmission.FilePath)) throw new FileNotFoundException("PBIX file not found");

                string selectTask = details.SelectTask;
                string pbixFilePath = taskSubmission.FilePath;
                string extractedData = null;

                //Generate template for complex tasks

               //string pbitPath = GeneratePowerBITemplate(pbixFilePath);
                //extractedData = ProcessPbitForTask(pbitPath, details.SelectTask);

                    //try
                    //{
                switch (selectTask)
                    {
                    case "DataimportManager":
                        var task1 = new Task_1();
                        //var basicTables = task1.ExtractTablesFromPbit(pbitPath);
                        //extractedData = task1.BuildExtractedDataJsonString(basicTables);
                        extractedData = task1.ExtractModelViaPort(timeoutSeconds: 60);
                        break;




                    case "DataTransformationManager":
                            var task2 = new Task_2();
                            extractedData = task2.DataTransformationManager(pbixFilePath);
                            break;

                        case "DataRelationshipManager":
                            var task3 = new Task_3();
                            extractedData = task3.DataRelationshipManager(pbixFilePath);
                            break;

                        //case "DataLayoutProcessor":
                        //    var pbixProcessor = new PBIXProcessor();
                        //    //string layoutPath = pbixProcessor.ReadLayoutFile(Path.Combine(extractedFolderPath, "LayoutFileName")); // adjust as needed

                        //    if (layoutPath != null)
                        //    {
                        //        LayoutData layoutData = pbixProcessor.ReadLayoutFile(layoutPath);
                        //        if (layoutData != null)
                        //        {
                        //            StringBuilder task4Output = new StringBuilder();
                        //            task4Output.AppendLine("Report Title: " + layoutData.ReportTitle);
                        //            foreach (var section in layoutData.Sections)
                        //            {
                        //                task4Output.AppendLine("Section Name: " + section.SectionName);
                        //                task4Output.AppendLine("Display Name: " + section.DisplayName);
                        //            }
                        //            extractedData = task4Output.ToString();
                        //        }
                        //        else
                        //        {
                        //            throw new Exception("Layout file could not be read.");
                        //        }
                        //    }
                        //    else
                        //    {
                        //        throw new Exception("Failed to extract layout file.");
                        //    }
                        //    break;

                        case "TextboxManager":
                            var task5 = new Task5Runner();
                            extractedData = task5.TextboxManager(pbixFilePath);
                            break;

                        case "slicerManager":
                            var task6 = new Task6Runner();
                            extractedData = task6.SlicerManager(pbixFilePath);
                            break;

                        default:
                            throw new ArgumentException("Invalid task type");
                    }

                    if (string.IsNullOrWhiteSpace(extractedData))
                        throw new InvalidOperationException("No data extracted from PBIT file");

                    using JsonDocument extractedDoc = JsonDocument.Parse(extractedData);
                    using JsonDocument masterDoc = JsonDocument.Parse(masterJson);

                    JsonElement masterRoot = masterDoc.RootElement;

                    if (masterRoot.TryGetProperty(selectTask, out JsonElement masterTaskJson))
                    {
                        return CompareJsonElements(extractedDoc.RootElement, masterTaskJson);
                    }

                    return CompareJsonElements(extractedDoc.RootElement, masterRoot);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Validation error: {ex}");
                    return false;
                }
            }



        //private bool ValidateAgainstMasterJson(string extractedData, string masterJson, string selectTask)
        //{
        //    using JsonDocument extractedDoc = JsonDocument.Parse(extractedData);
        //    using JsonDocument masterDoc = JsonDocument.Parse(masterJson);

        //    JsonElement masterRoot = masterDoc.RootElement;
        //    JsonElement comparisonElement = masterRoot;

        //    // Check if master JSON has task-specific structure
        //    if (masterRoot.TryGetProperty(selectTask, out JsonElement taskSpecificElement))
        //    {
        //        comparisonElement = taskSpecificElement;
        //    }

        //    return CompareJsonElements(extractedDoc.RootElement, comparisonElement);
        //}
        //public bool ValidatePowerBiTask(TaskSubmission taskSubmission, string masterJson)
        //{
        //    try
        //    {
        //        var details = taskSubmission.Task.Details as PowerBiDetails;
        //        if (details == null)
        //            throw new ArgumentException("Invalid task details type");

        //        if (!File.Exists(taskSubmission.FilePath))
        //            throw new FileNotFoundException("PBIX file not found");

        //        string selectTask = details.SelectTask;
        //        string pbixFilePath = taskSubmission.FilePath;

        //        // Convert PBIX to PBIT (actually extract folder path returned)
        //        // Convert PBIX to ZIP (equivalent to PBIT) and extract contents
        //        string pbitFilePath = GeneratePowerBITemplate(pbixFilePath); // updated variable name


        //        string extractedData = string.Empty;

        //        // Pass extracted folder path instead of pbit file path (since your method returns folder)
        //        switch (selectTask)
        //        {
        //            case "DataimportManager":
        //                var task1 = new Task_1();
        //                // Assuming ExtractTablesFromPbit works with extracted folder + DataModelSchema inside
        //                var basicTables = task1.ExtractTablesFromPbit(pbitFilePath);
        //                string jsonOutput1 = task1.BuildExtractedDataJsonString(basicTables);

        //                break;

        //            case "DataTransformationManager":
        //                var task2 = new Task_2();
        //                //extractedData = task2.DataTransformationManager(extractedFolderPath);
        //                break;

        //            case "DataRelationshipManager":
        //                var task3 = new Task_3();
        //                //extractedData = task3.DataRelationshipManager(extractedFolderPath);
        //                break;

        //            //case "DataLayoutProcessor":
        //            //    var pbixProcessor = new PBIXProcessor();
        //            //    //string layoutPath = pbixProcessor.ReadLayoutFile(Path.Combine(extractedFolderPath, "LayoutFileName")); // adjust as needed

        //            //    if (layoutPath != null)
        //            //    {
        //            //        LayoutData layoutData = pbixProcessor.ReadLayoutFile(layoutPath);
        //            //        if (layoutData != null)
        //            //        {
        //            //            StringBuilder task4Output = new StringBuilder();
        //            //            task4Output.AppendLine("Report Title: " + layoutData.ReportTitle);
        //            //            foreach (var section in layoutData.Sections)
        //            //            {
        //            //                task4Output.AppendLine("Section Name: " + section.SectionName);
        //            //                task4Output.AppendLine("Display Name: " + section.DisplayName);
        //            //            }
        //            //            extractedData = task4Output.ToString();
        //            //        }
        //            //        else
        //            //        {
        //            //            throw new Exception("Layout file could not be read.");
        //            //        }
        //            //    }
        //            //    else
        //            //    {
        //            //        throw new Exception("Failed to extract layout file.");
        //            //    }
        //            //    break;

        //            case "TextboxManager":
        //                var task5 = new Task5Runner();
        //                //extractedData = task5.TextboxManager(zipFilePath);
        //                break;

        //            case "slicerManager":
        //                var task6 = new Task6Runner();
        //                //extractedData = task6.SlicerManager(zipFilePath);
        //                break;

        //            default:
        //                throw new ArgumentException("Invalid task type");
        //        }

        //        if (string.IsNullOrWhiteSpace(extractedData))
        //            throw new InvalidOperationException("No data extracted from PBIT file");

        //        using JsonDocument extractedDoc = JsonDocument.Parse(extractedData);
        //        using JsonDocument masterDoc = JsonDocument.Parse(masterJson);

        //        JsonElement masterRoot = masterDoc.RootElement;

        //        if (masterRoot.TryGetProperty(selectTask, out JsonElement masterTaskJson))
        //        {
        //            return CompareJsonElements(extractedDoc.RootElement, masterTaskJson);
        //        }

        //        return CompareJsonElements(extractedDoc.RootElement, masterRoot);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Validation error: {ex}");
        //        return false;
        //    }
        //}



        private bool CompareJsonElements(JsonElement extracted, JsonElement master)
        {
            switch (master.ValueKind)
            {
                case JsonValueKind.Object:
                    if (extracted.ValueKind != JsonValueKind.Object) return false;

                    foreach (var property in master.EnumerateObject())
                    {
                        if (!extracted.TryGetProperty(property.Name, out JsonElement extractedValue))
                            return false;

                        if (!CompareJsonElements(extractedValue, property.Value))
                            return false;
                    }
                    return true;

                case JsonValueKind.Array:
                    if (extracted.ValueKind != JsonValueKind.Array) return false;

                    var masterItems = master.EnumerateArray().ToList();
                    var extractedItems = extracted.EnumerateArray().ToList();

                    if (masterItems.Count != extractedItems.Count) return false;

                    for (int i = 0; i < masterItems.Count; i++)
                    {
                        if (!CompareJsonElements(extractedItems[i], masterItems[i]))
                            return false;
                    }
                    return true;

                case JsonValueKind.String:
                    return extracted.ValueKind == JsonValueKind.String &&
                           extracted.GetString() == master.GetString();

                case JsonValueKind.Number:
                    return extracted.ValueKind == JsonValueKind.Number &&
                           extracted.GetRawText() == master.GetRawText();

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return extracted.ValueKind == master.ValueKind;

                case JsonValueKind.Null:
                    return extracted.ValueKind == JsonValueKind.Null;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }
    //public class Task_1
    //{
    //    public Dictionary<string, List<string>> ExtractTablesFromPbit(string pbitPath)
    //    {
    //        try
    //        {
    //            using (ZipArchive archive = ZipFile.OpenRead(pbitPath))
    //            {
    //                // Look for DataModelSchema in different locations
    //                var schemaEntry = archive.GetEntry("DataModelSchema") ??
    //                                  archive.GetEntry("Metadata/DataModelSchema") ??
    //                                  archive.GetEntry("DataModelSchema");

    //                if (schemaEntry == null)
    //                    return null;

    //                using (var reader = new StreamReader(schemaEntry.Open()))
    //                {
    //                    string jsonString = reader.ReadToEnd();
    //                    return ExtractTablesFromJson(jsonString);
    //                }
    //            }
    //        }
    //        catch
    //        {
    //            return null;
    //        }
    //    }
    //    public Dictionary<string, List<string>> ExtractTablesFromJson(string schemaJson)
    //    {
    //        try
    //        {
    //            // Clean up JSON if needed
    //            schemaJson = schemaJson.Trim('\0', ' ', '\t', '\r', '\n');

    //            // Handle empty content
    //            if (string.IsNullOrWhiteSpace(schemaJson))
    //                return new Dictionary<string, List<string>>();

    //            // Try parsing with different options
    //            var options = new JsonDocumentOptions
    //            {
    //                AllowTrailingCommas = true,
    //                CommentHandling = JsonCommentHandling.Skip
    //            };

    //            using JsonDocument doc = JsonDocument.Parse(schemaJson, options);
    //            JsonElement root = doc.RootElement;

    //            // Handle different schema formats
    //            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("model", out JsonElement model))
    //            {
    //                return ParseModernSchema(model);
    //            }
    //            else if (root.ValueKind == JsonValueKind.Array && root.EnumerateArray().Any())
    //            {
    //                return ParseLegacySchema(root);
    //            }
    //            else
    //            {
    //                // Attempt to parse as Tabular model
    //                return ParseTabularModel(schemaJson);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            // Diagnostic logging
    //            File.WriteAllText($"schema_error_{DateTime.Now.Ticks}.json", schemaJson);
    //            Console.WriteLine($"Schema extraction error: {ex}");
    //            return new Dictionary<string, List<string>>();
    //        }
    //    }
    //    private Dictionary<string, List<string>> ParseLegacySchema(JsonElement root)
    //    {
    //        var result = new Dictionary<string, List<string>>();

    //        foreach (var table in root.EnumerateArray())
    //        {
    //            if (table.TryGetProperty("name", out JsonElement tableNameElement) &&
    //                table.TryGetProperty("columns", out JsonElement columnsElement))
    //            {
    //                string tableName = tableNameElement.GetString();
    //                var columns = new List<string>();

    //                foreach (var column in columnsElement.EnumerateArray())
    //                {
    //                    if (column.TryGetProperty("name", out JsonElement columnNameElement))
    //                    {
    //                        columns.Add(columnNameElement.GetString());
    //                    }
    //                }

    //                result[tableName] = columns;
    //            }
    //        }

    //        return result;
    //    }

    //    private Dictionary<string, List<string>> ParseModernSchema(JsonElement model)
    //    {
    //        var result = new Dictionary<string, List<string>>();

    //        if (model.TryGetProperty("tables", out JsonElement tablesElement))
    //        {
    //            foreach (var table in tablesElement.EnumerateArray())
    //            {
    //                string tableName = table.GetProperty("name").GetString();
    //                var columns = new List<string>();

    //                if (table.TryGetProperty("columns", out JsonElement columnArray))
    //                {
    //                    foreach (var column in columnArray.EnumerateArray())
    //                    {
    //                        columns.Add(column.GetProperty("name").GetString());
    //                    }
    //                }

    //                result[tableName] = columns;
    //            }
    //        }

    //        return result;
    //    }

    //    private Dictionary<string, List<string>> ParseTabularModel(string jsonContent)
    //    {
    //        try
    //        {
    //            // Attempt to parse as Tabular model
    //            var model = JsonSerializer.Deserialize<TabularModel>(jsonContent);
    //            return model?.Tables?.ToDictionary(
    //                t => t.Name,
    //                t => t.Columns?.Select(c => c.Name).ToList() ?? new List<string>()
    //            ) ?? new Dictionary<string, List<string>>();
    //        }
    //        catch
    //        {
    //            return new Dictionary<string, List<string>>();
    //        }
    //    }

    //    // Tabular model classes
    //    private class TabularModel
    //    {
    //        [JsonPropertyName("tables")]
    //        public List<Table> Tables { get; set; }
    //    }

    //    private class Table
    //    {
    //        [JsonPropertyName("name")]
    //        public string Name { get; set; }

    //        [JsonPropertyName("columns")]
    //        public List<Column> Columns { get; set; }
    //    }

    //    private class Column
    //    {
    //        [JsonPropertyName("name")]
    //        public string Name { get; set; }
    //    }
    //    public string BuildExtractedDataJsonString(Dictionary<string, List<string>> tables)
    //    {
    //        return JsonSerializer.Serialize(tables, new JsonSerializerOptions
    //        {
    //            WriteIndented = true,
    //            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    //        });
    //    }
    //}
    public class Task_1
    {


public string ExtractModelViaPort(int timeoutSeconds = 60)
    {
        Console.WriteLine("Waiting for Power BI Desktop to open...");
        string port = GetPowerBIPortFromMsmdsrv();

        if (string.IsNullOrEmpty(port))
        {
            Console.WriteLine("Power BI port not found.");
            return null;
        }

        string connStr = $"localhost:{port}"; // TOM uses just the server string

        // Connect via TOM
        var server = new Server();
        int retries = timeoutSeconds / 2;

        while (retries-- > 0)
        {
            try
            {
                server.Connect(connStr);
                break;
            }
            catch
            {
                Thread.Sleep(2000);
            }
        }

        if (!server.Connected)
        {
            Console.WriteLine("TOM connection failed.");
            return null;
        }

        try
        {
                // Assumes only 1 DB (Power BI model) is active
                var database = server.Databases.Cast<Database>().FirstOrDefault();
                if (database == null)
            {
                Console.WriteLine("No model database found.");
                return null;
            }

            var tables = new Dictionary<string, List<string>>();
            var relationships = new List<Dictionary<string, string>>();

                GetTablesAndColumns_TOM(database, tables);
                GetRelationships_TOM(database, relationships);

            return JsonSerializer.Serialize(new
            {
                Tables = tables,
                Relationships = relationships
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Metadata extraction failed: {ex.Message}");
            return null;
        }
    }


    //        private void GrantFullAccessToUser(AdomdConnection connection, string databaseName, string userName)
    //        {
    //            // Define a role ID — this must already exist in the model or you must create it beforehand
    //            string roleId = "AdminRole";

    //            // Proper XMLA command to add a user to a role
    //            string xmlaCommand = $@"
    //<Batch xmlns='http://schemas.microsoft.com/analysisservices/2003/engine'>
    //  <Alter ObjectExpansion='ExpandFull'>
    //    <Object>
    //      <DatabaseID>{databaseName}</DatabaseID>
    //      <RoleID>{roleId}</RoleID>
    //    </Object>
    //    <ObjectDefinition>
    //      <Role xmlns='http://schemas.microsoft.com/analysisservices/2003/engine'>
    //        <ID>{roleId}</ID>
    //        <Name>{roleId}</Name>
    //        <Members>
    //          <Member>
    //            <Name>{SecurityElement.Escape(userName)}</Name>
    //          </Member>
    //        </Members>
    //      </Role>
    //    </ObjectDefinition>
    //  </Alter>
    //</Batch>";

    //            int retries = 3;
    //            while (retries > 0)
    //            {
    //                try
    //                {
    //                    using (var cmd = new AdomdCommand(xmlaCommand, connection))
    //                    {
    //                        cmd.ExecuteNonQuery();
    //                        Console.WriteLine($"User {userName} added to role '{roleId}' in database '{databaseName}'");
    //                        return;
    //                    }
    //                }
    //                catch (AdomdConnectionException ex)
    //                {
    //                    Console.WriteLine($"Connection issue: {ex.Message}");
    //                    retries--;
    //                    Thread.Sleep(1000);
    //                }
    //                catch (AdomdErrorResponseException ex)
    //                {
    //                    Console.WriteLine($"Permission error: {ex.Message}");
    //                    if (ex.Message.Contains("already exists"))
    //                    {
    //                        Console.WriteLine("User already has permissions. Continuing...");
    //                        return;
    //                    }
    //                    retries--;
    //                    Thread.Sleep(1000);
    //                }
    //                catch (Exception ex)
    //                {
    //                    Console.WriteLine($"General error: {ex.Message}");
    //                    retries--;
    //                    Thread.Sleep(1000);
    //                }
    //            }
    //            Console.WriteLine("Failed to grant permissions after retries");
    //        }

    // Existing methods (GetPowerBIPortFromMsmdsrv, GetCommandOutput, 
    // GetTablesAndColumns, GetRelationships) remain unchanged as in original

    private static string GetPowerBIPortFromMsmdsrv()
        {
            try
            {
                var msmdsrvProcs = Process.GetProcessesByName("msmdsrv");
                if (msmdsrvProcs.Length == 0)
                    return null;

                var netstatOutput = GetCommandOutput("netstat", "-aon");

                foreach (var proc in msmdsrvProcs)
                {
                    var pid = proc.Id.ToString();

                    foreach (var line in netstatOutput)
                    {
                        if (line.Contains("LISTENING") && line.EndsWith(pid))
                        {
                            var match = Regex.Match(line, @"(\d+\.\d+\.\d+\.\d+):(\d+)\s+.*LISTENING\s+" + pid);
                            if (match.Success)
                            {
                                return match.Groups[2].Value; // The port
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return null;
        }
        private static List<string> GetCommandOutput(string fileName, string arguments)
        {
            var output = new List<string>();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            while (!process.StandardOutput.EndOfStream)
            {
                output.Add(process.StandardOutput.ReadLine());
            }
            process.WaitForExit();
            return output;
        }




        public void GetTablesAndColumns_TOM(Database database, Dictionary<string, List<string>> tables)
        {
            foreach (var table in database.Model.Tables)
            {
                tables[table.Name] = table.Columns.Select(c => c.Name).ToList();
            }
        }

        public void GetRelationships_TOM(Database database, List<Dictionary<string, string>> relationships)
        {
            foreach (var rel in database.Model.Relationships.OfType<SingleColumnRelationship>())
            {
                relationships.Add(new Dictionary<string, string>
        {
            { "FromTable", rel.FromTable.Name },
            { "FromColumn", rel.FromColumn.Name },
            { "ToTable", rel.ToTable.Name },
            { "ToColumn", rel.ToColumn.Name }
        });
            }
        }
    }
//public class Task_1
//{

//    public string ExtractModelViaPort(int timeoutSeconds = 60)
//    {
//        Console.WriteLine("Waiting for Power BI Desktop to open...");
//        string port = GetPowerBIPortFromMsmdsrv();

//        if (string.IsNullOrEmpty(port))
//        {
//            Console.WriteLine("Power BI port not found.");
//            return null;
//        }

//        Console.WriteLine($"Found Power BI Analysis Services port: {port}");

//        string connStr = $"Data Source=localhost:{port};Integrated Security=SSPI;";
//        using var conn = new AdomdConnection(connStr);

//        int retries = timeoutSeconds / 2;
//        while (retries > 0)
//        {
//            try
//            {
//                Console.WriteLine("Attempting to connect to Analysis Services...");
//                conn.Open();
//                Console.WriteLine("Connection successful!");
//                break;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Connection failed: {ex.Message}");
//                Thread.Sleep(2000);
//                retries--;
//            }
//        }

//        if (conn.State != System.Data.ConnectionState.Open)
//        {
//            Console.WriteLine("Failed to connect to Analysis Services after retries.");
//            return null;
//        }

//        try
//        {
//            // Discover the actual catalog name (e.g., GUID)
//            var schemaDataSet = conn.GetSchemaDataSet("DBSCHEMA_CATALOGS", null);
//            var catalogsTable = schemaDataSet.Tables[0];

//            string actualCatalog = catalogsTable.Rows
//                                            .Cast<DataRow>()
//                                            .Select(r => r["CATALOG_NAME"].ToString())
//                                            .FirstOrDefault();

//            if (string.IsNullOrEmpty(actualCatalog))
//            {
//                Console.WriteLine("No catalogs found.");
//                return null;
//            }

//            Console.WriteLine($"Using catalog: {actualCatalog}");

//            // Call the GrantFullAccessToUser method to give full rights to the user
//            GrantFullAccessToUser("localhost", actualCatalog, "DESKTOP-B7SJSQT\\Anuj");

//            // Now reconnect to the real catalog
//            conn.Close();
//            conn.ConnectionString = $"Data Source=localhost:{port};Initial Catalog={actualCatalog};Integrated Security=SSPI;";
//            conn.Open();

//            var tables = new Dictionary<string, List<string>>();
//            var relationships = new List<Dictionary<string, string>>();

//            GetTablesAndColumns(conn, tables);
//            GetRelationships(conn, relationships);

//            return JsonSerializer.Serialize(new
//            {
//                Tables = tables,
//                Relationships = relationships
//            }, new JsonSerializerOptions { WriteIndented = true });
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Metadata extraction failed: {ex.Message}");
//            return null;
//        }
//    }

//    public void GrantFullAccessToUser(string instanceName, string databaseName, string userName)
//    {
//        // Construct the XMLA command to grant full admin rights to the user
//        string xmlaCommand = $@"
//<Batch xmlns='http://schemas.microsoft.com/analysisservices/2003/engine'>
//    <Commands>
//        <Alter ObjectExpansion='ObjectProperties'>
//            <Object>
//                <DatabaseID>{databaseName}</DatabaseID>
//            </Object>
//            <ObjectDefinition>
//                <Database xmlns='http://schemas.microsoft.com/analysisservices/2003/engine'>
//                    <Permissions>
//                        <Permission>
//                            <GranteeName>{userName}</GranteeName>
//                            <PermissionType>Administrator</PermissionType>
//                        </Permission>
//                    </Permissions>
//                </Database>
//            </ObjectDefinition>
//        </Alter>
//    </Commands>
//</Batch>";

//        try
//        {
//            // Connect to Analysis Services instance
//            string connectionString = $"Data Source={instanceName};";
//            using (var connection = new AdomdConnection(connectionString))
//            {
//                connection.Open();

//                // Create and execute the XMLA command
//                using (var command = new AdomdCommand(xmlaCommand, connection))
//                {
//                    command.ExecuteNonQuery();
//                    Console.WriteLine("Full access granted successfully.");
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error granting access: {ex.Message}");
//        }
//    }


//    private static string GetPowerBIPortFromMsmdsrv()
//    {
//        try
//        {
//            var msmdsrvProcs = Process.GetProcessesByName("msmdsrv");
//            if (msmdsrvProcs.Length == 0)
//                return null;

//            var netstatOutput = GetCommandOutput("netstat", "-aon");

//            foreach (var proc in msmdsrvProcs)
//            {
//                var pid = proc.Id.ToString();

//                foreach (var line in netstatOutput)
//                {
//                    if (line.Contains("LISTENING") && line.EndsWith(pid))
//                    {
//                        var match = Regex.Match(line, @"(\d+\.\d+\.\d+\.\d+):(\d+)\s+.*LISTENING\s+" + pid);
//                        if (match.Success)
//                        {
//                            return match.Groups[2].Value; // The port
//                        }
//                    }
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine("Error: " + ex.Message);
//        }

//        return null;
//    }
//    private static List<string> GetCommandOutput(string fileName, string arguments)
//    {
//        var output = new List<string>();
//        var process = new Process
//        {
//            StartInfo = new ProcessStartInfo
//            {
//                FileName = fileName,
//                Arguments = arguments,
//                RedirectStandardOutput = true,
//                UseShellExecute = false,
//                CreateNoWindow = true
//            }
//        };
//        process.Start();
//        while (!process.StandardOutput.EndOfStream)
//        {
//            output.Add(process.StandardOutput.ReadLine());
//        }
//        process.WaitForExit();
//        return output;
//    }

//    private void GetTablesAndColumns(AdomdConnection conn, Dictionary<string, List<string>> tables)
//    {
//        // Updated query using $SYSTEM.MDSCHEMA_COLUMNS for tabular models
//        string tablesQuery = "SELECT DISTINCT TABLE_NAME, COLUMN_NAME FROM $SYSTEM.MDSCHEMA_COLUMNS";
//        using var cmd = new AdomdCommand(tablesQuery, conn);
//        using var reader = cmd.ExecuteReader();

//        while (reader.Read())
//        {
//            string table = reader["TABLE_NAME"].ToString();
//            string column = reader["COLUMN_NAME"].ToString();

//            if (!tables.ContainsKey(table))
//                tables[table] = new List<string>();

//            if (!tables[table].Contains(column))
//                tables[table].Add(column);
//        }
//    }

//    private void GetRelationships(AdomdConnection conn, List<Dictionary<string, string>> relationships)
//    {
//        string relQuery = "SELECT FromTable, FromColumn, ToTable, ToColumn FROM $SYSTEM.TMSCHEMA_RELATIONSHIPS";
//        using var relCmd = new AdomdCommand(relQuery, conn);
//        using var relReader = relCmd.ExecuteReader();

//        while (relReader.Read())
//        {
//            relationships.Add(new Dictionary<string, string> {
//            { "FromTable", relReader["FromTable"].ToString() },
//            { "FromColumn", relReader["FromColumn"].ToString() },
//            { "ToTable", relReader["ToTable"].ToString() },
//            { "ToColumn", relReader["ToColumn"].ToString() }
//        });
//        }
//    }

//}
public class Task_2
    {
        /// <summary>
        /// Modified DataTransformationManager method which accepts a pbit file path,
        /// extracts the internal DataModelSchema, then extracts and returns detailed table structures in JSON format.
        /// </summary>
        public string DataTransformationManager(string pbitPath)
        {
            try
            {
                // Open the pbit file as a ZIP archive.
                using (ZipArchive archive = ZipFile.OpenRead(pbitPath))
                {
                    // Retrieve the DataModelSchema entry.
                    ZipArchiveEntry entry = archive.GetEntry("DataModelSchema");
                    if (entry == null)
                        return "{\"error\": \"DataModelSchema not found in the pbit archive.\"}";

                    using (Stream s = entry.Open())
                    using (MemoryStream ms = new MemoryStream())
                    {
                        s.CopyTo(ms);
                        string jsonString = Encoding.UTF8.GetString(ms.ToArray()).Replace("\0", "");
                        var masterTables = ExtractTables(jsonString);

                        // Build an anonymous object for JSON output.
                        var tableList = masterTables.Select(kvp => new
                        {
                            name = kvp.Key,
                            columns = kvp.Value.Columns,
                            partitions = kvp.Value.Partitions
                        }).ToList();

                        var output = new
                        {
                            DataTransformationManager = new
                            {
                                tables = tableList
                            }
                        };

                        var options = new JsonSerializerOptions { WriteIndented = true };
                        return JsonSerializer.Serialize(output, options);
                    }
                }
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"{ex.Message}\"}}";
            }
        }

        /// <summary>
        /// Extracts a dictionary mapping table names to TableInfo (columns and partitions) from a JSON string.
        /// Tables whose names start with "DateTable" or "LocalDateTable" are skipped.
        /// </summary>
        static Dictionary<string, TableInfo> ExtractTables(string jsonString)
        {
            var result = new Dictionary<string, TableInfo>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonString))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("model", out JsonElement model) &&
                        model.TryGetProperty("tables", out JsonElement tables))
                    {
                        foreach (JsonElement table in tables.EnumerateArray())
                        {
                            if (table.TryGetProperty("name", out JsonElement tableNameElement))
                            {
                                string tableName = tableNameElement.GetString();

                                // Skip date hierarchy tables.
                                if (tableName.StartsWith("DateTable", StringComparison.OrdinalIgnoreCase) ||
                                    tableName.StartsWith("LocalDateTable", StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                var tableInfo = new TableInfo();

                                // Extract columns.
                                if (table.TryGetProperty("columns", out JsonElement columnsProp) &&
                                    columnsProp.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (JsonElement column in columnsProp.EnumerateArray())
                                    {
                                        if (column.TryGetProperty("name", out JsonElement colNameElem))
                                        {
                                            string colName = colNameElem.GetString();
                                            // Assuming a dataType property exists; otherwise, default to "N/A"
                                            string dataType = "N/A";
                                            if (column.TryGetProperty("dataType", out JsonElement dtElem) &&
                                                dtElem.ValueKind == JsonValueKind.String)
                                            {
                                                dataType = dtElem.GetString();
                                            }
                                            tableInfo.Columns.Add(new ColumnInfo { Name = colName, DataType = dataType });
                                        }
                                    }
                                }

                                // Extract partition information.
                                if (table.TryGetProperty("partitions", out JsonElement partitions) &&
                                    partitions.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (JsonElement partition in partitions.EnumerateArray())
                                    {
                                        string partName = "N/A", mode = "N/A", sourceType = "N/A";
                                        string sourceValue = "N/A", gradeSheet = "N/A";
                                        string promotedHeaders = "N/A", filteredRows = "N/A", changedType = "N/A";

                                        if (partition.TryGetProperty("name", out JsonElement partNameElem) &&
                                            partNameElem.ValueKind == JsonValueKind.String)
                                            partName = partNameElem.GetString();

                                        if (partition.TryGetProperty("mode", out JsonElement modeElem) &&
                                            modeElem.ValueKind == JsonValueKind.String)
                                            mode = modeElem.GetString();

                                        if (partition.TryGetProperty("source", out JsonElement sourceElem) &&
                                            sourceElem.TryGetProperty("type", out JsonElement sourceTypeElem) &&
                                            sourceTypeElem.ValueKind == JsonValueKind.String)
                                            sourceType = sourceTypeElem.GetString();

                                        // Extract transformation expressions.
                                        if (partition.TryGetProperty("source", out sourceElem) &&
                                            sourceElem.TryGetProperty("expression", out JsonElement exprElem))
                                        {
                                            string expression = exprElem.ValueKind == JsonValueKind.Array
                                                ? string.Join("\n", exprElem.EnumerateArray().Select(e => e.GetString()))
                                                : exprElem.GetString();

                                            // Local helper function (removed 'static' for C# 7.3 compatibility)
                                            string ExtractRegexValue(string text, string pattern)
                                            {
                                                Match match = Regex.Match(text, pattern, RegexOptions.Singleline);
                                                return match.Success ? match.Groups[1].Value.Trim() : "N/A";
                                            }

                                            // Capture entire function calls (handling nested parentheses)
                                            sourceValue = ExtractRegexValue(expression, @"(?m)^\s*Source\s*=\s*((?:[^\r\n]*?\([^\r\n]*?\))|[^\r\n,]+)");
                                            gradeSheet = ExtractRegexValue(expression, @"(?m)^\s*Grade_Sheet\s*=\s*([^,\r\n]+)");
                                            promotedHeaders = ExtractRegexValue(expression, @"(?m)^\s*#""Promoted Headers""\s*=\s*((?:[^\r\n]*?\([^\r\n]*?\))|[^\r\n,]+)");
                                            filteredRows = ExtractRegexValue(expression, @"(?m)^\s*#""Filtered Rows""\s*=\s*((?:[^\r\n]*?\([^\r\n]*?\))|[^\r\n,]+)");
                                            changedType = ExtractRegexValue(expression, @"(?m)^\s*#""Changed Type""\s*=\s*((?:[^\r\n]*?\([^\r\n]*?\))|[^\r\n,]+)");
                                        }

                                        tableInfo.Partitions.Add(new PartitionInfo
                                        {
                                            Name = partName,
                                            Mode = mode,
                                            SourceType = sourceType,
                                            Source = sourceValue,
                                            Grade_Sheet = gradeSheet,
                                            PromotedHeaders = promotedHeaders,
                                            FilteredRows = filteredRows,
                                            ChangedType = changedType
                                        });
                                    }
                                }

                                result[tableName] = tableInfo;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No 'model' or 'tables' property found in JSON data.");
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine("A System.Text.Json error occurred: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error processing JSON data: {ex.Message}");
            }

            return result;
        }

        public class ColumnInfo
        {
            public string Name { get; set; }
            public string DataType { get; set; }
            public override string ToString() => $"{Name} (DataType: {DataType})";
        }

        public class PartitionInfo
        {
            public string Name { get; set; }
            public string Mode { get; set; }
            public string SourceType { get; set; }
            public string Source { get; set; }
            public string Grade_Sheet { get; set; }
            public string PromotedHeaders { get; set; }
            public string FilteredRows { get; set; }
            public string ChangedType { get; set; }
        }

        public class TableInfo
        {
            public List<ColumnInfo> Columns { get; set; } = new List<ColumnInfo>();
            public List<PartitionInfo> Partitions { get; set; } = new List<PartitionInfo>();
        }
    }



    public class Task_3
    {
        /// <summary>
        /// Modified DataRelationshipManager method which accepts a pbit file path,
        /// extracts the internal DataModelSchema, then extracts and returns relationship details in JSON format.
        /// </summary>
        public string DataRelationshipManager(string pbitPath)
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(pbitPath))
                {
                    // Retrieve the DataModelSchema entry from the pbit archive.
                    ZipArchiveEntry entry = archive.GetEntry("DataModelSchema");
                    if (entry == null)
                        return "{\"error\": \"DataModelSchema not found in the pbit archive.\"}";

                    using (Stream s = entry.Open())
                    using (MemoryStream ms = new MemoryStream())
                    {
                        s.CopyTo(ms);
                        // Read the JSON content and remove null characters.
                        string jsonContent = Encoding.UTF8.GetString(ms.ToArray()).Replace("\0", "").Trim();

                        // Optionally trim extra content after the last closing brace.
                        int lastBrace = jsonContent.LastIndexOf('}');
                        if (lastBrace >= 0 && lastBrace < jsonContent.Length - 1)
                        {
                            jsonContent = jsonContent.Substring(0, lastBrace + 1);
                        }

                        // Deserialize the JSON content.
                        RootObject rootObject = JsonSerializer.Deserialize<RootObject>(jsonContent);

                        // Check if relationships exist.
                        if (rootObject?.model?.relationships == null || rootObject.model.relationships.Count == 0)
                        {
                            return "{\"DataRelationshipManager\": {\"relationships\": [] }}";
                        }

                        // Build a list of relationship objects.
                        var relationshipsList = rootObject.model.relationships.Select(rel => new
                        {
                            name = rel.name,
                            fromTable = rel.fromTable,
                            fromCardinality = rel.fromCardinality,
                            fromColumn = rel.fromColumn,
                            toTable = rel.toTable,
                            toColumn = rel.toColumn,
                            crossFilteringBehavior = rel.crossFilteringBehavior
                        }).ToList();

                        // Build the final output object.
                        var output = new
                        {
                            DataRelationshipManager = new
                            {
                                relationships = relationshipsList
                            }
                        };

                        var options = new JsonSerializerOptions { WriteIndented = true };
                        return JsonSerializer.Serialize(output, options);
                    }
                }
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"{ex.Message}\"}}";
            }
        }

        public class Relationship
        {
            public string name { get; set; }
            public string fromTable { get; set; }
            public string fromCardinality { get; set; }
            public string fromColumn { get; set; }
            public string toTable { get; set; }
            public string toColumn { get; set; }
            public string crossFilteringBehavior { get; set; }
        }

        public class Model
        {
            public List<Relationship> relationships { get; set; }
        }

        public class RootObject
        {
            public Model model { get; set; }
        }
    }

    public class PBIXProcessor
    {
        /// <summary>
        /// Processes the PBIX file: extracts the layout file from the ZIP archive, reads its data,
        /// and returns the layout information in JSON format.
        /// </summary>
        public string DataLayoutProcessor(string pbixFilePath)
        {
            try
            {
                // Define the extraction folder (can be temporary).
                string extractTo = Path.Combine(Directory.GetCurrentDirectory(), "extracted_pbix1");

                // Extract the PBIX/PBIT file to the folder and get the layout file path.
                string layoutPath = ExtractPbix(pbixFilePath, extractTo);
                if (layoutPath == null)
                    return "{\"error\": \"Failed to locate the layout file.\"}";

                // Read the layout file and parse its data.
                LayoutData layoutData = ReadLayoutFile(layoutPath);
                if (layoutData == null)
                    return "{\"error\": \"No data found in layout.\"}";

                // Build the output object.
                var output = new
                {
                    DataLayoutProcessor = layoutData
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                return JsonSerializer.Serialize(output, options);
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"{ex.Message}\"}}";
            }
        }

        /// <summary>
        /// Helper method: Safely extracts a string value from a JSON element.
        /// </summary>
        private string GetPropertyAsString(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                // Attempt to extract nested structure: expr > Literal > Value
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

        /// <summary>
        /// Extracts the PBIX/PBIT file (a ZIP archive) to a folder and returns the layout file path.
        /// </summary>
        public string ExtractPbix(string pbixFilePath, string extractTo)
        {
            try
            {
                if (!File.Exists(pbixFilePath))
                {
                    Console.WriteLine("PBIX file does not exist: " + pbixFilePath);
                    return null;
                }

                // Remove any existing extraction folder.
                if (Directory.Exists(extractTo))
                    Directory.Delete(extractTo, true);
                Directory.CreateDirectory(extractTo);

                // PBIX/PBIT files are essentially ZIP archives.
                ZipFile.ExtractToDirectory(pbixFilePath, extractTo);

                // Look for the Report folder.
                string reportFolder = Path.Combine(extractTo, "Report");
                if (!Directory.Exists(reportFolder))
                {
                    Console.WriteLine("Report folder not found in extracted PBIX.");
                    return null;
                }

                // First try "Layout.json"; if not found, try "Layout".
                string layoutPath = Path.Combine(reportFolder, "Layout.json");
                if (!File.Exists(layoutPath))
                {
                    layoutPath = Path.Combine(reportFolder, "Layout");
                }
                return File.Exists(layoutPath) ? layoutPath : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error extracting PBIX: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Reads the layout file and extracts the report title, sections, and chart details.
        /// </summary>
        public LayoutData ReadLayoutFile(string layoutPath)
        {
            try
            {
                if (!File.Exists(layoutPath))
                {
                    Console.WriteLine("Layout file does not exist: " + layoutPath);
                    return null;
                }

                // Read the layout file using UTF-16 LE encoding.
                string jsonContent = File.ReadAllText(layoutPath, Encoding.Unicode);

                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                {
                    JsonElement root = doc.RootElement;
                    var layoutData = new LayoutData();

                    // Extract the report title.
                    layoutData.ReportTitle = root.TryGetProperty("displayName", out JsonElement displayNameElem)
                        ? GetPropertyAsString(displayNameElem)
                        : "No Title";

                    // Extract sections.
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

                            // Extract visual containers (charts).
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

                                                // Extract visual type and filter by allowed types.
                                                string visualType = singleVisual.TryGetProperty("visualType", out JsonElement typeElem)
                                                    ? typeElem.GetString() : "Unknown";
                                                string lowerType = visualType.ToLower();

                                                // Allowed chart types list (substrings to search for).
                                                string[] allowedChartTypes = new string[]
{
    "bar",
    "column",
    "line",
    "pie",
    "donut",
    "doughnut", // included as an alias for donut
    "donutchart", // included as an alias for donut
    "table",
    "matrix",
    "card",
    "multirowcard",
    "kpi",
    "scatter",
    "bubble", //  Scatter chart with size based on a third variable. Often used interchangeably with Scatter
    "map",
    "filledmap",
    "shapemap",
    "treemap",
    "funnel",
    "gauge",
    "waterfall",
    "combo", // Generic term, might need further clarification for specific types
    "lineandstackedcolumnchart",
    "lineandclusteredcolumnchart",
    "area",
    "stackedarea",
    "100stackedarea",
    "radar",
    "spider", // Included as an alias for radar
    "ribbonchart",
    "decompostiontree"
};

                                                bool isAllowed = false;
                                                foreach (string allowed in allowedChartTypes)
                                                {
                                                    if (lowerType.Contains(allowed))
                                                    {
                                                        isAllowed = true;
                                                        break;
                                                    }
                                                }
                                                if (!isAllowed)
                                                    continue; // Skip this visual if not an allowed chart type.

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

                                                // Extract title and style properties.
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

                                                // Extract aggregations from projections.
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

                                                // Extract filters (as a raw JSON string).
                                                if (container.TryGetProperty("filters", out JsonElement filtersElem))
                                                {
                                                    chartData.Filters = filtersElem.GetRawText();
                                                }

                                                chartsList.Add(chartData);
                                            }
                                        }
                                        catch (JsonException je)
                                        {
                                            Console.WriteLine("Error parsing config JSON: " + je.Message);
                                        }
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
            catch (Exception ex)
            {
                Console.WriteLine("Error reading layout: " + ex.Message);
                return null;
            }
        }

        // Data structures to hold layout information.
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
            public string Id { get; set; }
            public string Type { get; set; }
            public string Height { get; set; }
            public string Width { get; set; }
            public string X { get; set; }
            public string Y { get; set; }
            public string Title { get; set; }
            public string FontWeight { get; set; }
            public string FontStyle { get; set; }
            public string FontSize { get; set; }
            public string TextDecoration { get; set; }
            public string FontFamily { get; set; }
            public string FontColor { get; set; }
            public string Alignment { get; set; }
            public string Bold { get; set; }
            public string Underline { get; set; }
            public List<string> Aggregations { get; set; } = new List<string>();
            public List<string> ColorDetails { get; set; } = new List<string>();
            public List<string> Fonts { get; set; } = new List<string>();
            public string Filters { get; set; }
        }
    }

    public class Task5Runner
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

                var options = new JsonSerializerOptions { WriteIndented = true };
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


    public class Task6Runner
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





//using System.Text.Json;
//using System.Text.Json.Serialization;
//using JobSimulation.BLL;
//using JobSimulation.DAL;
//using JobSimulation.Models;

//namespace JobSimulation.powerBiApp
//{
//    public class PowerBiValidationService
//    {
//        private readonly ValidationService _validationService;
//        private readonly PowerBiDatabase _powerBiDatabase;

//        public PowerBiValidationService(
//            ValidationService validationService,
//            PowerBiDatabase powerBiDatabase)
//        {
//            _validationService = validationService;
//            _powerBiDatabase = powerBiDatabase;
//        }
//        public string GetMasterJsonForSection(string sectionId)
//        {
//            return _powerBiDatabase.FetchMasterJson(sectionId);
//        }

//        public bool ValidatePowerBiTask(TaskSubmission taskSubmission, string masterJson)
//        {
//            try
//            {
//                var options = new JsonSerializerOptions
//                {
//                    PropertyNameCaseInsensitive = true,
//                    Converters = { new JsonStringEnumConverter() }
//                };

//                var powerBiDetails = taskSubmission.Task.Details as PowerBiDetails;
//                if (powerBiDetails == null || string.IsNullOrEmpty(powerBiDetails.SelectTask))
//                    return false;

//                return powerBiDetails.SelectTask switch
//                {
//                    "DataimportManager" => ValidateComponent<DataimportManager>(
//                        masterJson,
//                        taskSubmission.GeneratedJson,
//                        (m, u) => ValidateDataimportManager(m, u)),
//                    "DataTransformationManager" => ValidateComponent<DataTransformationManager>(
//                        masterJson,
//                        taskSubmission.GeneratedJson,
//                        (m, u) => ValidateDataTransformationManager(m, u)),
//                    // Add other cases similarly
//                    _ => false
//                };
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        private bool ValidateComponent<T>(
//            string masterJson,
//            string userJson,
//            Func<T, T, bool> validator) where T : class
//        {
//            var master = JsonSerializer.Deserialize<T>(masterJson);
//            var user = JsonSerializer.Deserialize<T>(userJson);
//            return master != null && user != null && validator(master, user);
//        }

//        #region Core Validation Logic
//        private bool ValidateDataimportManager(DataimportManager master, DataimportManager user)
//        {
//            if (master?.Tables == null || user?.Tables == null) return false;

//            return master.Tables.Count == user.Tables.Count &&
//                   master.Tables.All(mt =>
//                       user.Tables.Any(ut =>
//                           mt.Name.Equals(ut.Name, StringComparison.OrdinalIgnoreCase) &&
//                           mt.Columns.OrderBy(c => c).SequenceEqual(
//                               ut.Columns.OrderBy(c => c))));
//        }

//        private bool ValidateDataTransformationManager(
//            DataTransformationManager master,
//            DataTransformationManager user)
//        {
//            if (master?.Tables == null || user?.Tables == null) return false;

//            return master.Tables.Count == user.Tables.Count &&
//                   master.Tables.All(mt =>
//                       user.Tables.Any(ut =>
//                           mt.Name.Equals(ut.Name, StringComparison.OrdinalIgnoreCase) &&
//                           mt.Columns.All(mc =>
//                               ut.Columns.Any(uc =>
//                                   uc.Name.Equals(mc.Name, StringComparison.OrdinalIgnoreCase) &&
//                                   uc.DataType == mc.DataType))));
//        }
//        #endregion

//        #region Proper Data Classes
//        public class DataimportManager
//        {
//            [JsonPropertyName("tables")]
//            public List<TableInfo> Tables { get; set; } = new();
//        }

//        public class TableInfo
//        {
//            [JsonPropertyName("TaskId")]
//            public string TaskId { get; set; }

//            [JsonPropertyName("name")]
//            public string Name { get; set; }

//            [JsonPropertyName("columns")]
//            public List<string> Columns { get; set; } = new();
//        }

//        public class DataTransformationManager
//        {
//            [JsonPropertyName("tables")]
//            public List<TransformationTable> Tables { get; set; } = new();
//        }

//        public class TransformationTable
//        {
//            [JsonPropertyName("name")]
//            public string Name { get; set; }

//            [JsonPropertyName("columns")]
//            public List<ColumnDetail> Columns { get; set; } = new();

//            [JsonPropertyName("partitions")]
//            public List<Partition> Partitions { get; set; } = new();
//        }

//        public class ColumnDetail
//        {
//            [JsonPropertyName("Name")]
//            public string Name { get; set; }

//            [JsonPropertyName("DataType")]
//            public string DataType { get; set; }
//        }

//        public class Partition
//        {
//            [JsonPropertyName("Name")]
//            public string Name { get; set; }

//            [JsonPropertyName("Mode")]
//            public string Mode { get; set; }

//            [JsonPropertyName("Source")]
//            public string Source { get; set; }
//        }
//        #endregion
//    }
//}