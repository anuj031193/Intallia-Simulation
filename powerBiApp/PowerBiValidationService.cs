

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


        //public bool ValidatePowerBiTask(TaskSubmission taskSubmission, string masterJson)
        //{
        //    try
        //    {
        //        var details = taskSubmission.Task.Details as PowerBiDetails;
        //        if (details == null) throw new ArgumentException("Invalid task details type");
        //        if (!File.Exists(taskSubmission.FilePath)) throw new FileNotFoundException("PBIX file not found");

        //        string selectTask = details.SelectTask;
        //        string pbixFilePath = taskSubmission.FilePath;
        //        string extractedData = null;

        //        switch (selectTask)
        //            {
        //            case "DataImportManager":
        //                var task1 = new Task_1();

        //                extractedData = task1.ExtractModelViaPort(timeoutSeconds: 60);
        //                break;




        //            case "DataTransformationManager":
        //                var task2 = new Task_2();
        //                string portJson = task2.ExtractModelViaPort(timeoutSeconds: 60);
        //                //string fileJson = task2.DataTransformationManager(pbixFilePath);
        //                //string mergedJson = Task_2.MergeColumnsAndPartitions(portJson, fileJson);
        //                //return mergedJson;

        //                break;

        //                case "DataRelationshipManager":
        //                    var task3 = new Task_3();
        //                    extractedData = task3.DataRelationshipManagerViaPort();
        //                break;

        //            case "LayoutVisualizationManager":
        //                var Task_4 = new Task_4();
        //                // Pass the PBIX file path to DataLayoutProcessor
        //                extractedData = Task_4.DataLayoutProcessor(pbixFilePath);
        //                break;


        //            case "TextboxManager":
        //                    var task5 = new Task_5();
        //                    extractedData = task5.TextboxManager(pbixFilePath);
        //                    break;

        //                case "SlicerManager":
        //                    var task6 = new Task_6();
        //                    extractedData = task6.SlicerManager(pbixFilePath);
        //                    break;

        //                default:
        //                    throw new ArgumentException("Invalid task type");
        //            }

        //        if (string.IsNullOrWhiteSpace(extractedData))
        //            throw new InvalidOperationException("No data extracted from PBIT file");

        //        string taskId = taskSubmission.Task.TaskId;

        //        // Wrap extractedData in a TaskId root
        //        using JsonDocument extractedDocInner = JsonDocument.Parse(extractedData);
        //        var wrapped = new Dictionary<string, object>
        //        {
        //            [taskId] = extractedDocInner.RootElement
        //        };
        //        var options = new JsonSerializerOptions
        //        {
        //            WriteIndented = true,
        //            PropertyNamingPolicy = null,
        //            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        //        };
        //        string wrappedExtractedData = JsonSerializer.Serialize(wrapped, options);

        //        using JsonDocument extractedDoc = JsonDocument.Parse(wrappedExtractedData);
        //        using JsonDocument masterDoc = JsonDocument.Parse(masterJson);

        //        JsonElement masterRoot = masterDoc.RootElement;

        //        if (masterRoot.TryGetProperty(taskId, out JsonElement masterTaskJson))
        //        {
        //            return CompareJsonElements(extractedDoc.RootElement.GetProperty(taskId), masterTaskJson);
        //        }

        //        // If taskId not found in master, treat as fail or add handling as needed
        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Validation error: {ex}");
        //        return false;
        //    }
        //}


        public bool ValidatePowerBiTask(TaskSubmission taskSubmission, string masterJson)
        {
            try
            {
                var details = taskSubmission.Task.Details as PowerBiDetails;
                if (details == null) throw new ArgumentException("Invalid task details type");
                if (!File.Exists(taskSubmission.FilePath)) throw new FileNotFoundException("PBIX file not found");

                string taskId = taskSubmission.Task.TaskId;
                string selectTask = details.SelectTask;
                string pbixFilePath = taskSubmission.FilePath;
                string extractedData = null;

                // Only process the current task
                switch (selectTask)
                {
                    case "DataImportManager":
                        var task1 = new Task_1();
                        extractedData = task1.DataImportManagerViaPort(timeoutSeconds: 60);
                        break;

                    case "DataTransformationManager":
                        var task2 = new Task_2();
                         extractedData = task2.DataTransformationManagerViaPort(timeoutSeconds: 60);
                    
                        break;

                    case "DataRelationshipManager":
                        var task3 = new Task_3();
                        extractedData = task3.DataRelationshipManagerViaPort();
                        break;

                    case "LayoutVisualizationManager":
                        var task4 = new Task_4();
                        extractedData = task4.DataLayoutProcessor(pbixFilePath);
                        break;

                    case "TextboxManager":
                        var task5 = new Task_5();
                        extractedData = task5.TextboxManager(pbixFilePath);
                        break;

                    case "SlicerManager":
                        var task6 = new Task_6();
                        extractedData = task6.SlicerManager(pbixFilePath);
                        break;

                    default:
                        throw new ArgumentException("Invalid task type");
                }

                if (string.IsNullOrWhiteSpace(extractedData))
                    throw new InvalidOperationException("No data extracted from PBIX/PBIT file");

                // Wrap extractedData in a TaskId root
                using var extractedDocInner = JsonDocument.Parse(extractedData);
                var wrapped = new Dictionary<string, object>
                {
                    [taskId] = extractedDocInner.RootElement
                };
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = null,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string wrappedExtractedData = JsonSerializer.Serialize(wrapped, options);

                using var extractedDoc = JsonDocument.Parse(wrappedExtractedData);
                using var masterDoc = JsonDocument.Parse(masterJson);

                JsonElement masterRoot = masterDoc.RootElement;

                if (masterRoot.TryGetProperty(taskId, out JsonElement masterTaskJsonElement))
                {
                    // For debugging/testing: print compared JSON fragments
                    string extractedTaskJson = JsonSerializer.Serialize(extractedDoc.RootElement.GetProperty(taskId), options);
                    string masterTaskJsonStr = JsonSerializer.Serialize(masterTaskJsonElement, options);
                    Console.WriteLine("Extracted Task JSON:\n" + extractedTaskJson);
                    Console.WriteLine("Master Task JSON:\n" + masterTaskJsonStr);

                    bool result = CompareJsonElements(extractedDoc.RootElement.GetProperty(taskId), masterTaskJsonElement);
                    Console.WriteLine("Comparison Result: " + result);
                    return result;
                }
                else
                {
                    // TaskId not found in master
                    string extractedTaskJson = JsonSerializer.Serialize(extractedDoc.RootElement.GetProperty(taskId), options);
                    Console.WriteLine("Extracted Task JSON:\n" + extractedTaskJson);
                    Console.WriteLine($"TaskId '{taskId}' not found in master JSON!");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Validation error: {ex}");
                return false;
            }
        }


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













}


//public class Task_2
//{
//    /// <summary>
//    /// Modified DataTransformationManager method which accepts a pbit file path,
//    /// extracts the internal DataModelSchema, then extracts and returns detailed table structures in JSON format.
//    /// </summary>
//    public string DataTransformationManager(string pbitPath)
//    {
//        try
//        {
//            // Open the pbit file as a ZIP archive.
//            using (ZipArchive archive = ZipFile.OpenRead(pbitPath))
//            {
//                // Retrieve the DataModelSchema entry.
//                ZipArchiveEntry entry = archive.GetEntry("DataModelSchema");
//                if (entry == null)
//                    return "{\"error\": \"DataModelSchema not found in the pbit archive.\"}";

//                using (Stream s = entry.Open())
//                using (MemoryStream ms = new MemoryStream())
//                {
//                    s.CopyTo(ms);
//                    string jsonString = Encoding.UTF8.GetString(ms.ToArray()).Replace("\0", "");
//                    var masterTables = ExtractTables(jsonString);

//                    // Build an anonymous object for JSON output.
//                    var tableList = masterTables.Select(kvp => new
//                    {
//                        name = kvp.Key,
//                        columns = kvp.Value.Columns,
//                        partitions = kvp.Value.Partitions
//                    }).ToList();

//                    var output = new
//                    {
//                        DataTransformationManager = new
//                        {
//                            tables = tableList
//                        }
//                    };

//                    var options = new JsonSerializerOptions { WriteIndented = true };
//                    return JsonSerializer.Serialize(output, options);
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            return $"{{\"error\": \"{ex.Message}\"}}";
//        }
//    }

//    /// <summary>
//    /// Extracts a dictionary mapping table names to TableInfo (columns and partitions) from a JSON string.
//    /// Tables whose names start with "DateTable" or "LocalDateTable" are skipped.
//    /// </summary>
//    static Dictionary<string, TableInfo> ExtractTables(string jsonString)
//    {
//        var result = new Dictionary<string, TableInfo>(StringComparer.OrdinalIgnoreCase);

//        try
//        {
//            using (JsonDocument doc = JsonDocument.Parse(jsonString))
//            {
//                JsonElement root = doc.RootElement;
//                if (root.TryGetProperty("model", out JsonElement model) &&
//                    model.TryGetProperty("tables", out JsonElement tables))
//                {
//                    foreach (JsonElement table in tables.EnumerateArray())
//                    {
//                        if (table.TryGetProperty("name", out JsonElement tableNameElement))
//                        {
//                            string tableName = tableNameElement.GetString();

//                            // Skip date hierarchy tables.
//                            if (tableName.StartsWith("DateTable", StringComparison.OrdinalIgnoreCase) ||
//                                tableName.StartsWith("LocalDateTable", StringComparison.OrdinalIgnoreCase))
//                            {
//                                continue;
//                            }

//                            var tableInfo = new TableInfo();

//                            // Extract columns.
//                            if (table.TryGetProperty("columns", out JsonElement columnsProp) &&
//                                columnsProp.ValueKind == JsonValueKind.Array)
//                            {
//                                foreach (JsonElement column in columnsProp.EnumerateArray())
//                                {
//                                    if (column.TryGetProperty("name", out JsonElement colNameElem))
//                                    {
//                                        string colName = colNameElem.GetString();
//                                        // Assuming a dataType property exists; otherwise, default to "N/A"
//                                        string dataType = "N/A";
//                                        if (column.TryGetProperty("dataType", out JsonElement dtElem) &&
//                                            dtElem.ValueKind == JsonValueKind.String)
//                                        {
//                                            dataType = dtElem.GetString();
//                                        }
//                                        tableInfo.Columns.Add(new ColumnInfo { Name = colName, DataType = dataType });
//                                    }
//                                }
//                            }

//                            // Extract partition information.
//                            if (table.TryGetProperty("partitions", out JsonElement partitions) &&
//                                partitions.ValueKind == JsonValueKind.Array)
//                            {
//                                foreach (JsonElement partition in partitions.EnumerateArray())
//                                {
//                                    string partName = "N/A", mode = "N/A", sourceType = "N/A";
//                                    string sourceValue = "N/A", gradeSheet = "N/A";
//                                    string promotedHeaders = "N/A", filteredRows = "N/A", changedType = "N/A";

//                                    if (partition.TryGetProperty("name", out JsonElement partNameElem) &&
//                                        partNameElem.ValueKind == JsonValueKind.String)
//                                        partName = partNameElem.GetString();

//                                    if (partition.TryGetProperty("mode", out JsonElement modeElem) &&
//                                        modeElem.ValueKind == JsonValueKind.String)
//                                        mode = modeElem.GetString();

//                                    if (partition.TryGetProperty("source", out JsonElement sourceElem) &&
//                                        sourceElem.TryGetProperty("type", out JsonElement sourceTypeElem) &&
//                                        sourceTypeElem.ValueKind == JsonValueKind.String)
//                                        sourceType = sourceTypeElem.GetString();

//                                    // Extract transformation expressions.
//                                    if (partition.TryGetProperty("source", out sourceElem) &&
//                                        sourceElem.TryGetProperty("expression", out JsonElement exprElem))
//                                    {
//                                        string expression = exprElem.ValueKind == JsonValueKind.Array
//                                            ? string.Join("\n", exprElem.EnumerateArray().Select(e => e.GetString()))
//                                            : exprElem.GetString();

//                                        // Local helper function (removed 'static' for C# 7.3 compatibility)
//                                        string ExtractRegexValue(string text, string pattern)
//                                        {
//                                            Match match = Regex.Match(text, pattern, RegexOptions.Singleline);
//                                            return match.Success ? match.Groups[1].Value.Trim() : "N/A";
//                                        }

//                                        // Capture entire function calls (handling nested parentheses)
//                                        sourceValue = ExtractRegexValue(expression, @"(?m)^\s*Source\s*=\s*((?:[^\r\n]*?\([^\r\n]*?\))|[^\r\n,]+)");
//                                        gradeSheet = ExtractRegexValue(expression, @"(?m)^\s*Grade_Sheet\s*=\s*([^,\r\n]+)");
//                                        promotedHeaders = ExtractRegexValue(expression, @"(?m)^\s*#""Promoted Headers""\s*=\s*((?:[^\r\n]*?\([^\r\n]*?\))|[^\r\n,]+)");
//                                        filteredRows = ExtractRegexValue(expression, @"(?m)^\s*#""Filtered Rows""\s*=\s*((?:[^\r\n]*?\([^\r\n]*?\))|[^\r\n,]+)");
//                                        changedType = ExtractRegexValue(expression, @"(?m)^\s*#""Changed Type""\s*=\s*((?:[^\r\n]*?\([^\r\n]*?\))|[^\r\n,]+)");
//                                    }

//                                    tableInfo.Partitions.Add(new PartitionInfo
//                                    {
//                                        Name = partName,
//                                        Mode = mode,
//                                        SourceType = sourceType,
//                                        Source = sourceValue,
//                                        Grade_Sheet = gradeSheet,
//                                        PromotedHeaders = promotedHeaders,
//                                        FilteredRows = filteredRows,
//                                        ChangedType = changedType
//                                    });
//                                }
//                            }

//                            result[tableName] = tableInfo;
//                        }
//                    }
//                }
//                else
//                {
//                    Console.WriteLine("No 'model' or 'tables' property found in JSON data.");
//                }
//            }
//        }
//        catch (JsonException ex)
//        {
//            Console.WriteLine("A System.Text.Json error occurred: " + ex.Message);
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Unexpected error processing JSON data: {ex.Message}");
//        }

//        return result;
//    }

//    public class ColumnInfo
//    {
//        public string Name { get; set; }
//        public string DataType { get; set; }
//        public override string ToString() => $"{Name} (DataType: {DataType})";
//    }

//    public class PartitionInfo
//    {
//        public string Name { get; set; }
//        public string Mode { get; set; }
//        public string SourceType { get; set; }
//        public string Source { get; set; }
//        public string Grade_Sheet { get; set; }
//        public string PromotedHeaders { get; set; }
//        public string FilteredRows { get; set; }
//        public string ChangedType { get; set; }
//    }

//    public class TableInfo
//    {
//        public List<ColumnInfo> Columns { get; set; } = new List<ColumnInfo>();
//        public List<PartitionInfo> Partitions { get; set; } = new List<PartitionInfo>();
//    }
//}



















//public class Task_3
//    {
//        public class Relationship
//        {
//            public string name { get; set; }
//            public string fromTable { get; set; }
//            public string fromCardinality { get; set; }
//            public string fromColumn { get; set; }
//            public string toTable { get; set; }
//            public string toColumn { get; set; }
//            public string crossFilteringBehavior { get; set; }
//        }

//        public string DataRelationshipManagerViaPort(int timeoutSeconds = 60)
//        {
//            string port = GetPowerBIPortFromMsmdsrv();
//            if (string.IsNullOrEmpty(port))
//                return "{\"error\": \"Power BI port not found.\"}";

//            string initialConnStr = $"Data Source=localhost:{port};Integrated Security=SSPI;";
//            string catalogName = null;

//            using (var conn = new AdomdConnection(initialConnStr))
//            {
//                if (!TryConnect(conn, timeoutSeconds))
//                    return "{\"error\": \"Connection to Analysis Services failed.\"}";

//                catalogName = GetCatalogName(conn);
//                if (string.IsNullOrEmpty(catalogName))
//                    return "{\"error\": \"No catalogs found.\"}";
//            }

//            using (var conn = new AdomdConnection(
//                $"Data Source=localhost:{port};Initial Catalog={catalogName};Integrated Security=SSPI;"))
//            {
//                if (!TryConnect(conn, timeoutSeconds / 2))
//                    return "{\"error\": \"Connection to catalog failed.\"}";

//                // Build table ID to name mapping
//                var tableNames = new Dictionary<string, string>();
//                try
//                {
//                    using (var cmd = new AdomdCommand(
//                        "SELECT [ID] AS TableID, [Name] AS TableName FROM $SYSTEM.TMSCHEMA_TABLES", conn))
//                    using (var reader = cmd.ExecuteReader())
//                    {
//                        while (reader.Read())
//                        {
//                            string id = reader["TableID"]?.ToString()?.Trim() ?? "N/A";
//                            string name = reader["TableName"]?.ToString() ?? "N/A";
//                            tableNames[id] = name;
//                        }
//                    }
//                }
//                catch (AdomdException ex)
//                {
//                    Debug.WriteLine($"Table mapping error: {ex.Message}");
//                }

//                // Build column lineage tag to name mapping
//                var columnNamesByLineageTag = new Dictionary<(string TableId, int LineageTag), string>();

//                try
//                {
//                    using (var cmd = new AdomdCommand(
//                        "SELECT [TableID], [Name] AS ColumnName, [LineageTag] FROM $SYSTEM.TMSCHEMA_COLUMNS", conn))
//                    using (var reader = cmd.ExecuteReader())
//                    {
//                        while (reader.Read())
//                        {
//                            string tableId = reader["TableID"]?.ToString()?.Trim() ?? "N/A";
//                            string colName = reader["ColumnName"]?.ToString() ?? "N/A";
//                            object lineageTagObj = reader["LineageTag"];

//                            if (lineageTagObj != null && lineageTagObj != DBNull.Value)
//                            {
//                                if (int.TryParse(lineageTagObj.ToString(), out int lineageTag))
//                                {
//                                    columnNamesByLineageTag[(tableId, lineageTag)] = colName;
//                                }
//                            }
//                        }
//                    }
//                }
//                catch (AdomdException ex)
//                {
//                    Debug.WriteLine($"Column mapping error: {ex.Message}");
//                }

//                // Get relationships
//                var relationships = new List<Relationship>();
//                try
//                {
//                    using (var cmd = new AdomdCommand(
//                        "SELECT [ID] AS RelId, [Name] AS RelName, " +
//                        "[FromTableID], [FromColumnID], [FromCardinality], " +
//                        "[ToTableID], [ToColumnID], [CrossFilteringBehavior] " +
//                        "FROM $SYSTEM.TMSCHEMA_RELATIONSHIPS", conn))
//                    using (var reader = cmd.ExecuteReader())
//                    {
//                        while (reader.Read())
//                        {
//                            string relGuidOrId = reader["RelId"]?.ToString() ?? Guid.NewGuid().ToString();
//                            string relNameValue = reader["RelName"] != DBNull.Value
//                                ? reader["RelName"].ToString()
//                                : null;

//                            string nameValue = !string.IsNullOrEmpty(relNameValue)
//                                ? relNameValue
//                                : relGuidOrId;

//                            string fromTableId = reader["FromTableID"]?.ToString()?.Trim() ?? "N/A";
//                            string fromColumnId = reader["FromColumnID"]?.ToString()?.Trim() ?? "N/A";
//                            string toTableId = reader["ToTableID"]?.ToString()?.Trim() ?? "N/A";
//                            string toColumnId = reader["ToColumnID"]?.ToString()?.Trim() ?? "N/A";

//                            string fromCardinality = reader["FromCardinality"] != DBNull.Value
//                                ? MapCardinality(reader["FromCardinality"]?.ToString())
//                                : null;

//                            string crossFilteringBehavior = reader["CrossFilteringBehavior"] != DBNull.Value
//                                ? MapCrossFiltering(reader["CrossFilteringBehavior"]?.ToString())
//                                : null;

//                            // Resolve column names using lineage tags
//                            string fromColumnName = fromColumnId;
//                            if (int.TryParse(fromColumnId, out int fromLineageTag))
//                            {
//                                if (columnNamesByLineageTag.TryGetValue((fromTableId, fromLineageTag), out var name))
//                                {
//                                    fromColumnName = name;
//                                }
//                            }

//                            string toColumnName = toColumnId;
//                            if (int.TryParse(toColumnId, out int toLineageTag))
//                            {
//                                if (columnNamesByLineageTag.TryGetValue((toTableId, toLineageTag), out var name))
//                                {
//                                    toColumnName = name;
//                                }
//                            }

//                            relationships.Add(new Relationship
//                            {
//                                name = nameValue,
//                                fromTable = tableNames.TryGetValue(fromTableId, out var fromTableName)
//                                    ? fromTableName
//                                    : fromTableId,
//                                fromCardinality = fromCardinality,
//                                fromColumn = fromColumnName,
//                                toTable = tableNames.TryGetValue(toTableId, out var toTableName)
//                                    ? toTableName
//                                    : toTableId,
//                                toColumn = toColumnName,
//                                crossFilteringBehavior = crossFilteringBehavior
//                            });
//                        }
//                    }
//                }
//                catch (AdomdException ex)
//                {
//                    Debug.WriteLine($"Relationship query error: {ex.Message}");
//                    return "{\"DataRelationshipManager\": {\"relationships\": [] }}";
//                }

//                // Output JSON
//                var output = new
//                {
//                    DataRelationshipManager = new
//                    {
//                        relationships = relationships
//                    }
//                };

//                var options = new JsonSerializerOptions { WriteIndented = true };
//                return JsonSerializer.Serialize(output, options);
//            }
//        }

//        private static string MapCardinality(string code)
//        {
//            if (string.IsNullOrEmpty(code)) return null;
//            if (code == "1" || code.Equals("one", StringComparison.OrdinalIgnoreCase)) return "one";
//            if (code == "2" || code.Equals("many", StringComparison.OrdinalIgnoreCase)) return "many";
//            return code;
//        }

//        private static string MapCrossFiltering(string code)
//        {
//            if (string.IsNullOrEmpty(code)) return null;
//            if (code == "1" || code.Equals("OneDirection", StringComparison.OrdinalIgnoreCase)) return "OneDirection";
//            if (code == "2" || code.Equals("BothDirections", StringComparison.OrdinalIgnoreCase)) return "BothDirections";
//            return code;
//        }

//        private bool TryConnect(AdomdConnection conn, int timeoutSeconds)
//        {
//            int retries = timeoutSeconds;
//            while (retries-- > 0)
//            {
//                try
//                {
//                    conn.Open();
//                    return true;
//                }
//                catch (AdomdConnectionException)
//                {
//                    System.Threading.Thread.Sleep(1000);
//                }
//            }
//            return false;
//        }

//        private string GetCatalogName(AdomdConnection conn)
//        {
//            try
//            {
//                using (var cmd = new AdomdCommand("SELECT [CATALOG_NAME] FROM $SYSTEM.DBSCHEMA_CATALOGS", conn))
//                using (var reader = cmd.ExecuteReader())
//                {
//                    if (reader.Read())
//                        return reader.GetString(0);
//                }
//                return null;
//            }
//            catch (AdomdException)
//            {
//                return null;
//            }
//        }

//        private static string GetPowerBIPortFromMsmdsrv()
//        {
//            try
//            {
//                var msmdsrvProcs = Process.GetProcessesByName("msmdsrv");
//                if (msmdsrvProcs.Length == 0)
//                    return null;

//                var netstatOutput = GetCommandOutput("netstat", "-aon");

//                foreach (var proc in msmdsrvProcs)
//                {
//                    var pid = proc.Id.ToString();

//                    foreach (var line in netstatOutput)
//                    {
//                        if (!string.IsNullOrEmpty(line) && line.Contains("LISTENING") && line.EndsWith(pid))
//                        {
//                            var match = System.Text.RegularExpressions.Regex.Match(
//                                line,
//                                @"(\d+\.\d+\.\d+\.\d+):(\d+)\s+.*LISTENING\s+" + pid
//                            );

//                            if (match.Success)
//                            {
//                                return match.Groups[2].Value;
//                            }
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Port detection error: {ex.Message}");
//            }
//            return null;
//        }

//        private static List<string> GetCommandOutput(string fileName, string arguments)
//        {
//            var output = new List<string>();
//            try
//            {
//                var process = new Process
//                {
//                    StartInfo = new ProcessStartInfo
//                    {
//                        FileName = fileName,
//                        Arguments = arguments,
//                        RedirectStandardOutput = true,
//                        UseShellExecute = false,
//                        CreateNoWindow = true,
//                        WindowStyle = ProcessWindowStyle.Hidden
//                    }
//                };

//                process.Start();
//                while (!process.StandardOutput.EndOfStream)
//                {
//                    output.Add(process.StandardOutput.ReadLine());
//                }
//                process.WaitForExit(5000);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Command execution error: {ex.Message}");
//            }
//            return output;
//        }
//    }//public class Task_4
//    {
//        /// <summary>
//        /// Connects to a running Power BI Desktop instance (via port), reads the Layout file, and returns visualization details as JSON.
//        /// </summary>
//        /// <param name="timeoutSeconds">Timeout for connection.</param>
//        /// <returns>JSON string with layout & visualization details.</returns>
//        public string DataLayoutProcessorViaPort(int timeoutSeconds = 60)
//        {
//            try
//            {
//                string port = GetPowerBIPortFromMsmdsrv();
//                if (string.IsNullOrEmpty(port))
//                    return "{\"error\": \"Power BI port not found.\"}";

//                string catalogName = null;
//                using (var conn = new AdomdConnection($"Data Source=localhost:{port};Integrated Security=SSPI;"))
//                {
//                    if (!TryConnect(conn, timeoutSeconds))
//                        return "{\"error\": \"Connection to Analysis Services failed.\"}";

//                    catalogName = GetCatalogName(conn);
//                    if (string.IsNullOrEmpty(catalogName))
//                        return "{\"error\": \"No catalogs found.\"}";
//                }

//                // Try to find the Layout file in the Power BI temp folder (not always possible, but we attempt)
//                // In a real scenario, user should provide the PBIX file path or layout JSON directly.
//                // For demonstration, we return an error if not found.
//                return "{\"error\": \"Direct layout extraction via port is not supported. Please supply the PBIX/PBIT file path.\"}";
//            }
//            catch (Exception ex)
//            {
//                return $"{{\"error\": \"{ex.Message}\"}}";
//            }
//        }

//        /// <summary>
//        /// Extracts visualization layout from a PBIX/PBIT file and outputs a normalized JSON structure.
//        /// </summary>
//        public string DataLayoutProcessor(string pbixFilePath)
//        {
//            try
//            {
//                string extractTo = Path.Combine(Directory.GetCurrentDirectory(), "extracted_pbix1");
//                string layoutPath = ExtractPbix(pbixFilePath, extractTo);
//                if (layoutPath == null)
//                    return "{\"error\": \"Failed to locate the layout file.\"}";

//                LayoutData layoutData = ReadLayoutFile(layoutPath);
//                if (layoutData == null)
//                    return "{\"error\": \"No data found in layout.\"}";
//                //return FormatLayoutData(layoutData);
//                var output = new
//                {
//                    DataLayoutProcessor = layoutData
//                };
//                var options = new JsonSerializerOptions { WriteIndented = true };
//                return JsonSerializer.Serialize(output, options);
//            }
//            catch (Exception ex)
//            {
//                return $"{{\"error\": \"{ex.Message}\"}}";
//            }
//        }

//        /// <summary>
//        /// Extracts the PBIX/PBIT file (a ZIP archive) and returns the layout file path.
//        /// </summary>
//        public string ExtractPbix(string pbixFilePath, string extractTo)
//        {
//            try
//            {
//                if (!File.Exists(pbixFilePath))
//                    return null;

//                if (Directory.Exists(extractTo))
//                    Directory.Delete(extractTo, true);
//                Directory.CreateDirectory(extractTo);

//                ZipFile.ExtractToDirectory(pbixFilePath, extractTo);

//                string reportFolder = Path.Combine(extractTo, "Report");
//                if (!Directory.Exists(reportFolder))
//                    return null;

//                string layoutPath = Path.Combine(reportFolder, "Layout.json");
//                if (!File.Exists(layoutPath))
//                    layoutPath = Path.Combine(reportFolder, "Layout");
//                return File.Exists(layoutPath) ? layoutPath : null;
//            }
//            catch
//            {
//                return null;
//            }
//        }

//        /// <summary>
//        /// Reads the layout file and extracts the report title, sections, and chart details.
//        /// </summary>
//        public LayoutData ReadLayoutFile(string layoutPath)
//        {
//            try
//            {
//                if (!File.Exists(layoutPath))
//                    return null;

//                string jsonContent = File.ReadAllText(layoutPath, Encoding.Unicode);

//                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
//                {
//                    JsonElement root = doc.RootElement;
//                    var layoutData = new LayoutData();

//                    // Report title
//                    layoutData.ReportTitle = root.TryGetProperty("displayName", out JsonElement displayNameElem)
//                        ? GetPropertyAsString(displayNameElem)
//                        : "No Title";

//                    // Sections
//                    if (root.TryGetProperty("sections", out JsonElement sectionsElem) && sectionsElem.ValueKind == JsonValueKind.Array)
//                    {
//                        var sectionsList = new List<SectionData>();
//                        foreach (JsonElement sectionElem in sectionsElem.EnumerateArray())
//                        {
//                            var sectionData = new SectionData
//                            {
//                                SectionName = sectionElem.TryGetProperty("name", out JsonElement nameElem)
//                                                ? nameElem.GetString() : "Unknown Section",
//                                DisplayName = sectionElem.TryGetProperty("displayName", out JsonElement secDisplayNameElem)
//                                                ? secDisplayNameElem.GetString() : "No Display Name"
//                            };

//                            var chartsList = new List<ChartData>();
//                            if (sectionElem.TryGetProperty("visualContainers", out JsonElement visualContainersElem) &&
//                                visualContainersElem.ValueKind == JsonValueKind.Array)
//                            {
//                                foreach (JsonElement container in visualContainersElem.EnumerateArray())
//                                {
//                                    if (container.TryGetProperty("config", out JsonElement configElem) &&
//                                        configElem.ValueKind == JsonValueKind.String)
//                                    {
//                                        string configStr = configElem.GetString();
//                                        try
//                                        {
//                                            using (JsonDocument configDoc = JsonDocument.Parse(configStr))
//                                            {
//                                                if (!configDoc.RootElement.TryGetProperty("singleVisual", out JsonElement singleVisual))
//                                                    continue;

//                                                string visualType = singleVisual.TryGetProperty("visualType", out JsonElement typeElem)
//                                                    ? typeElem.GetString() : "Unknown";
//                                                string lowerType = visualType.ToLower();

//                                                string[] allowedChartTypes = new string[]
//                                                {
//                                                "bar", "column", "line", "pie", "donut", "doughnut", "donutchart", "table", "matrix",
//                                                "card", "multirowcard", "kpi", "scatter", "bubble", "map", "filledmap", "shapemap",
//                                                "treemap", "funnel", "gauge", "waterfall", "combo", "lineandstackedcolumnchart",
//                                                "lineandclusteredcolumnchart", "area", "stackedarea", "100stackedarea", "radar",
//                                                "spider", "ribbonchart", "decompostiontree"
//                                                };

//                                                bool isAllowed = allowedChartTypes.Any(allowed => lowerType.Contains(allowed));
//                                                if (!isAllowed)
//                                                    continue;

//                                                var chartData = new ChartData();
//                                                chartData.Id = container.TryGetProperty("id", out JsonElement idElem)
//                                                    ? idElem.GetString() : "N/A";
//                                                chartData.Type = visualType;
//                                                chartData.Height = container.TryGetProperty("height", out JsonElement heightElem)
//                                                    ? heightElem.ToString() : "N/A";
//                                                chartData.Width = container.TryGetProperty("width", out JsonElement widthElem)
//                                                    ? widthElem.ToString() : "N/A";
//                                                chartData.X = container.TryGetProperty("x", out JsonElement xElem)
//                                                    ? xElem.ToString() : "N/A";
//                                                chartData.Y = container.TryGetProperty("y", out JsonElement yElem)
//                                                    ? yElem.ToString() : "N/A";

//                                                // Title & style props
//                                                if (singleVisual.TryGetProperty("vcObjects", out JsonElement vcObjects) &&
//                                                    vcObjects.ValueKind == JsonValueKind.Object)
//                                                {
//                                                    if (vcObjects.TryGetProperty("title", out JsonElement titleArray) &&
//                                                        titleArray.ValueKind == JsonValueKind.Array &&
//                                                        titleArray.GetArrayLength() > 0)
//                                                    {
//                                                        JsonElement titleObj = titleArray[0];
//                                                        if (titleObj.TryGetProperty("properties", out JsonElement titleProps))
//                                                        {
//                                                            chartData.Title = StringCleaner.Clean(
//           titleProps.TryGetProperty("text", out JsonElement textProps)
//               ? GetPropertyAsString(textProps)
//               : "No Title"
//       );
//                                                            chartData.FontWeight = titleProps.TryGetProperty("fontWeight", out JsonElement fontWeightProp)
//                                                                ? GetPropertyAsString(fontWeightProp) : "Normal";
//                                                            chartData.FontStyle = titleProps.TryGetProperty("fontStyle", out JsonElement fontStyleProp)
//                                                                ? GetPropertyAsString(fontStyleProp) : "Normal";
//                                                            chartData.FontSize = titleProps.TryGetProperty("fontSize", out JsonElement fontSizeProp)
//                                                                ? GetPropertyAsString(fontSizeProp) : "Default";
//                                                            chartData.TextDecoration = titleProps.TryGetProperty("textDecoration", out JsonElement textDecorationProp)
//                                                                ? GetPropertyAsString(textDecorationProp) : "None";
//                                                            chartData.FontFamily = StringCleaner.Clean(
//              titleProps.TryGetProperty("fontFamily", out JsonElement fontFamilyProp)
//                  ? GetPropertyAsString(fontFamilyProp)
//                  : "Default"
//          );
//                                                            chartData.FontColor = StringCleaner.Clean(
//    titleProps.TryGetProperty("fontColor", out JsonElement fontColorProp)
//        ? GetPropertyAsString(fontColorProp)
//        : "Default"
//);
//                                                            chartData.Alignment = StringCleaner.Clean(
//     titleProps.TryGetProperty("alignment", out JsonElement alignmentProp)
//         ? GetPropertyAsString(alignmentProp)
//         : "Left"
// );
//                                                            chartData.Bold = titleProps.TryGetProperty("bold", out JsonElement boldProp)
//                                                                ? GetPropertyAsString(boldProp) : "False";
//                                                            chartData.Underline = titleProps.TryGetProperty("underline", out JsonElement underlineProp)
//                                                                ? GetPropertyAsString(underlineProp) : "False";
//                                                        }
//                                                    }
//                                                }

//                                                // Aggregations from projections
//                                                if (singleVisual.TryGetProperty("projections", out JsonElement projections) &&
//                                                    projections.ValueKind == JsonValueKind.Object)
//                                                {
//                                                    foreach (JsonProperty prop in projections.EnumerateObject())
//                                                    {
//                                                        if (prop.Value.ValueKind == JsonValueKind.Array)
//                                                        {
//                                                            foreach (JsonElement item in prop.Value.EnumerateArray())
//                                                            {
//                                                                if (item.TryGetProperty("queryRef", out JsonElement queryRef))
//                                                                {
//                                                                    chartData.Aggregations.Add(queryRef.GetString());
//                                                                }
//                                                            }
//                                                        }
//                                                    }
//                                                }

//                                                // Filters (raw JSON)
//                                                if (container.TryGetProperty("filters", out JsonElement filtersElem))
//                                                {
//                                                    chartData.Filters = filtersElem.GetRawText();
//                                                }

//                                                chartsList.Add(chartData);
//                                            }
//                                        }
//                                        catch (JsonException) { }
//                                    }
//                                }
//                            }
//                            sectionData.Charts = chartsList.ToArray();
//                            sectionsList.Add(sectionData);
//                        }
//                        layoutData.Sections = sectionsList.ToArray();
//                    }
//                    else
//                    {
//                        layoutData.Sections = Array.Empty<SectionData>();
//                    }
//                    return layoutData;
//                }
//            }
//            catch
//            {
//                return null;
//            }
//        }

//        public string FormatLayoutData(LayoutData layoutData)
//        {
//            if (layoutData == null) return "--- No layout data ---";

//            var sb = new StringBuilder();

//            sb.AppendLine($"Report Title: {layoutData.ReportTitle}");

//            if (layoutData.Sections != null)
//            {
//                foreach (var section in layoutData.Sections)
//                {
//                    sb.AppendLine();
//                    sb.AppendLine($"Section Name: {section.SectionName}");
//                    sb.AppendLine($"Display Name: {section.DisplayName}");

//                    if (section.Charts != null)
//                    {
//                        foreach (var chart in section.Charts)
//                        {
//                            sb.AppendLine();
//                            sb.AppendLine($"  Chart ID: {chart.Id}");
//                            sb.AppendLine($"  Type: {chart.Type}");
//                            sb.AppendLine($"  Title: {chart.Title}");
//                            sb.AppendLine($"  Font: {chart.FontFamily}");
//                            sb.AppendLine($"  Font Color: {chart.FontColor}");
//                            sb.AppendLine($"  Size: {chart.Width} x {chart.Height}");
//                            sb.AppendLine($"  Position: ({chart.X}, {chart.Y})");
//                            sb.AppendLine($"  Bold: {chart.Bold}");
//                            sb.AppendLine($"  Underline: {chart.Underline}");
//                            sb.AppendLine($"  Filters: {chart.Filters}");
//                            sb.AppendLine($"  Aggregations: {string.Join(", ", chart.Aggregations)}");
//                            sb.AppendLine($"  Colors: {string.Join(", ", chart.ColorDetails)}");
//                            sb.AppendLine($"  Fonts: {string.Join(", ", chart.Fonts)}");
//                            sb.AppendLine($"  ---");
//                        }
//                    }
//                }
//            }
//            return sb.ToString();
//        }

//        /// <summary>
//        /// Helper: Extracts a string value from a JSON element, handling nested expr/Literal/Value.
//        /// </summary>
//        private string GetPropertyAsString(JsonElement element)
//        {
//            if (element.ValueKind == JsonValueKind.String)
//            {
//                return element.GetString();
//            }
//            else if (element.ValueKind == JsonValueKind.Object)
//            {
//                if (element.TryGetProperty("expr", out JsonElement expr) &&
//                    expr.ValueKind == JsonValueKind.Object &&
//                    expr.TryGetProperty("Literal", out JsonElement literal) &&
//                    literal.ValueKind == JsonValueKind.Object &&
//                    literal.TryGetProperty("Value", out JsonElement value))
//                {
//                    return value.GetString();
//                }
//                return element.ToString();
//            }
//            else
//            {
//                return element.ToString();
//            }
//        }

//        // Data structures for layout info
//        public class LayoutData
//        {
//            public string ReportTitle { get; set; }
//            public SectionData[] Sections { get; set; }
//        }

//        public class SectionData
//        {
//            public string SectionName { get; set; }
//            public string DisplayName { get; set; }
//            public ChartData[] Charts { get; set; }
//        }



//public class ChartData
//    {
//        [JsonPropertyName("Id")]
//        public string Id { get; set; }
//        [JsonPropertyName("Type")]
//        public string Type { get; set; }
//        [JsonPropertyName("Title")]
//        public string Title { get; set; }

//        // Display "Font" in JSON, but use FontFamily in code
//        [JsonPropertyName("Font")]
//        public string FontFamily { get; set; }

//        [JsonPropertyName("FontColor")]
//        public string FontColor { get; set; }
//        [JsonPropertyName("Height")]
//        public string Height { get; set; }
//        [JsonPropertyName("Width")]
//        public string Width { get; set; }
//        [JsonPropertyName("X")]
//        public string X { get; set; }
//        [JsonPropertyName("Y")]
//        public string Y { get; set; }
//        [JsonPropertyName("Bold")]
//        public string Bold { get; set; }
//        [JsonPropertyName("Underline")]
//        public string Underline { get; set; }
//        [JsonPropertyName("Filters")]
//        public string Filters { get; set; }
//        [JsonPropertyName("Aggregations")]
//        public List<string> Aggregations { get; set; } = new List<string>();
//        [JsonPropertyName("Colors")]
//        public List<string> ColorDetails { get; set; } = new List<string>();
//        [JsonPropertyName("Fonts")]
//        public List<string> Fonts { get; set; } = new List<string>();

//        // ADD THESE PROPERTIES:
//        [JsonPropertyName("FontWeight")]
//        public string FontWeight { get; set; }
//        [JsonPropertyName("FontStyle")]
//        public string FontStyle { get; set; }
//        [JsonPropertyName("FontSize")]
//        public string FontSize { get; set; }
//        [JsonPropertyName("TextDecoration")]
//        public string TextDecoration { get; set; }
//        [JsonPropertyName("Alignment")]
//        public string Alignment { get; set; }
//}        // --- Below: Power BI port helpers reused from Task 3 ---

//        private static string GetPowerBIPortFromMsmdsrv()
//        {
//            try
//            {
//                var msmdsrvProcs = Process.GetProcessesByName("msmdsrv");
//                if (msmdsrvProcs.Length == 0)
//                    return null;

//                var netstatOutput = GetCommandOutput("netstat", "-aon");
//                foreach (var proc in msmdsrvProcs)
//                {
//                    var pid = proc.Id.ToString();
//                    foreach (var line in netstatOutput)
//                    {
//                        if (line.Contains("LISTENING") && line.EndsWith(pid))
//                        {
//                            var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+\.\d+\.\d+\.\d+):(\d+)\s+.*LISTENING\s+" + pid);
//                            if (match.Success)
//                                return match.Groups[2].Value;
//                        }
//                    }
//                }
//            }
//            catch { }
//            return null;
//        }

//        private static List<string> GetCommandOutput(string fileName, string arguments)
//        {
//            var output = new List<string>();
//            var process = new Process
//            {
//                StartInfo = new ProcessStartInfo
//                {
//                    FileName = fileName,
//                    Arguments = arguments,
//                    RedirectStandardOutput = true,
//                    UseShellExecute = false,
//                    CreateNoWindow = true
//                }
//            };
//            process.Start();
//            while (!process.StandardOutput.EndOfStream)
//            {
//                output.Add(process.StandardOutput.ReadLine());
//            }
//            process.WaitForExit();
//            return output;
//        }

//        private bool TryConnect(AdomdConnection conn, int timeoutSeconds)
//        {
//            int retries = timeoutSeconds;
//            while (retries-- > 0)
//            {
//                try
//                {
//                    conn.Open();
//                    return true;
//                }
//                catch
//                {
//                    System.Threading.Thread.Sleep(1000);
//                }
//            }
//            return false;
//        }

//        private string GetCatalogName(AdomdConnection conn)
//        {
//            try
//            {
//                using (var cmd = new AdomdCommand("SELECT [CATALOG_NAME] FROM $SYSTEM.DBSCHEMA_CATALOGS", conn))
//                using (var reader = cmd.ExecuteReader())
//                {
//                    if (reader.Read())
//                        return reader.GetString(0);
//                }
//                return null;
//            }
//            catch
//            {
//                return null;
//            }
//        }
//    }

//    public static class StringCleaner
//    {
//        public static string Clean(string input)
//        {
//            if (string.IsNullOrEmpty(input)) return input;
//            // Replace unicode single quote and double quote with actual quote
//            input = input.Replace("\\u0027", "'").Replace("\u0027", "'").Replace("\\u0022", "\"").Replace("\u0022", "\"");
//            // Replace escaped slashes
//            input = input.Replace(@"\\", @"\");
//            // Remove unnecessary outer quotes if present
//            if (input.Length > 1 && input.StartsWith("\"") && input.EndsWith("\""))
//                input = input.Substring(1, input.Length - 2);
//            return input;
//        }
//    }




//    public class PBIXProcessor
//    {
//        /// <summary>
//        /// Processes the PBIX file: extracts the layout file from the ZIP archive, reads its data,
//        /// and returns the layout information in JSON format.
//        /// </summary>
//        public string DataLayoutProcessor(string pbixFilePath)
//        {
//            try
//            {
//                // Define the extraction folder (can be temporary).
//                string extractTo = Path.Combine(Directory.GetCurrentDirectory(), "extracted_pbix1");

//                // Extract the PBIX/PBIT file to the folder and get the layout file path.
//                string layoutPath = ExtractPbix(pbixFilePath, extractTo);
//                if (layoutPath == null)
//                    return "{\"error\": \"Failed to locate the layout file.\"}";

//                // Read the layout file and parse its data.
//                LayoutData layoutData = ReadLayoutFile(layoutPath);
//                if (layoutData == null)
//                    return "{\"error\": \"No data found in layout.\"}";

//                // Build the output object.
//                var output = new
//                {
//                    DataLayoutProcessor = layoutData
//                };

//                var options = new JsonSerializerOptions { WriteIndented = true };
//                return JsonSerializer.Serialize(output, options);
//            }
//            catch (Exception ex)
//            {
//                return $"{{\"error\": \"{ex.Message}\"}}";
//            }
//        }

//        /// <summary>
//        /// Helper method: Safely extracts a string value from a JSON element.
//        /// </summary>
//        private string GetPropertyAsString(JsonElement element)
//        {
//            if (element.ValueKind == JsonValueKind.String)
//            {
//                return element.GetString();
//            }
//            else if (element.ValueKind == JsonValueKind.Object)
//            {
//                // Attempt to extract nested structure: expr > Literal > Value
//                if (element.TryGetProperty("expr", out JsonElement expr) &&
//                    expr.ValueKind == JsonValueKind.Object &&
//                    expr.TryGetProperty("Literal", out JsonElement literal) &&
//                    literal.ValueKind == JsonValueKind.Object &&
//                    literal.TryGetProperty("Value", out JsonElement value))
//                {
//                    return value.GetString();
//                }
//                return element.ToString();
//            }
//            else
//            {
//                return element.ToString();
//            }
//        }

//        /// <summary>
//        /// Extracts the PBIX/PBIT file (a ZIP archive) to a folder and returns the layout file path.
//        /// </summary>
//        public string ExtractPbix(string pbixFilePath, string extractTo)
//        {
//            try
//            {
//                if (!File.Exists(pbixFilePath))
//                {
//                    Console.WriteLine("PBIX file does not exist: " + pbixFilePath);
//                    return null;
//                }

//                // Remove any existing extraction folder.
//                if (Directory.Exists(extractTo))
//                    Directory.Delete(extractTo, true);
//                Directory.CreateDirectory(extractTo);

//                // PBIX/PBIT files are essentially ZIP archives.
//                ZipFile.ExtractToDirectory(pbixFilePath, extractTo);

//                // Look for the Report folder.
//                string reportFolder = Path.Combine(extractTo, "Report");
//                if (!Directory.Exists(reportFolder))
//                {
//                    Console.WriteLine("Report folder not found in extracted PBIX.");
//                    return null;
//                }

//                // First try "Layout.json"; if not found, try "Layout".
//                string layoutPath = Path.Combine(reportFolder, "Layout.json");
//                if (!File.Exists(layoutPath))
//                {
//                    layoutPath = Path.Combine(reportFolder, "Layout");
//                }
//                return File.Exists(layoutPath) ? layoutPath : null;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Error extracting PBIX: " + ex.Message);
//                return null;
//            }
//        }

//        /// <summary>
//        /// Reads the layout file and extracts the report title, sections, and chart details.
//        /// </summary>
//        public LayoutData ReadLayoutFile(string layoutPath)
//        {
//            try
//            {
//                if (!File.Exists(layoutPath))
//                {
//                    Console.WriteLine("Layout file does not exist: " + layoutPath);
//                    return null;
//                }

//                // Read the layout file using UTF-16 LE encoding.
//                string jsonContent = File.ReadAllText(layoutPath, Encoding.Unicode);

//                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
//                {
//                    JsonElement root = doc.RootElement;
//                    var layoutData = new LayoutData();

//                    // Extract the report title.
//                    layoutData.ReportTitle = root.TryGetProperty("displayName", out JsonElement displayNameElem)
//                        ? GetPropertyAsString(displayNameElem)
//                        : "No Title";

//                    // Extract sections.
//                    if (root.TryGetProperty("sections", out JsonElement sectionsElem) && sectionsElem.ValueKind == JsonValueKind.Array)
//                    {
//                        var sectionsList = new List<SectionData>();
//                        foreach (JsonElement sectionElem in sectionsElem.EnumerateArray())
//                        {
//                            var sectionData = new SectionData
//                            {
//                                SectionName = sectionElem.TryGetProperty("name", out JsonElement nameElem)
//                                                ? nameElem.GetString() : "Unknown Section",
//                                DisplayName = sectionElem.TryGetProperty("displayName", out JsonElement secDisplayNameElem)
//                                                ? secDisplayNameElem.GetString() : "No Display Name"
//                            };

//                            var chartsList = new List<ChartData>();

//                            // Extract visual containers (charts).
//                            if (sectionElem.TryGetProperty("visualContainers", out JsonElement visualContainersElem) &&
//                                visualContainersElem.ValueKind == JsonValueKind.Array)
//                            {
//                                foreach (JsonElement container in visualContainersElem.EnumerateArray())
//                                {
//                                    if (container.TryGetProperty("config", out JsonElement configElem) &&
//                                        configElem.ValueKind == JsonValueKind.String)
//                                    {
//                                        string configStr = configElem.GetString();
//                                        try
//                                        {
//                                            using (JsonDocument configDoc = JsonDocument.Parse(configStr))
//                                            {
//                                                if (!configDoc.RootElement.TryGetProperty("singleVisual", out JsonElement singleVisual))
//                                                    continue;

//                                                // Extract visual type and filter by allowed types.
//                                                string visualType = singleVisual.TryGetProperty("visualType", out JsonElement typeElem)
//                                                    ? typeElem.GetString() : "Unknown";
//                                                string lowerType = visualType.ToLower();

//                                                // Allowed chart types list (substrings to search for).
//                                                string[] allowedChartTypes = new string[]
//{
//    "bar",
//    "column",
//    "line",
//    "pie",
//    "donut",
//    "doughnut", // included as an alias for donut
//    "donutchart", // included as an alias for donut
//    "table",
//    "matrix",
//    "card",
//    "multirowcard",
//    "kpi",
//    "scatter",
//    "bubble", //  Scatter chart with size based on a third variable. Often used interchangeably with Scatter
//    "map",
//    "filledmap",
//    "shapemap",
//    "treemap",
//    "funnel",
//    "gauge",
//    "waterfall",
//    "combo", // Generic term, might need further clarification for specific types
//    "lineandstackedcolumnchart",
//    "lineandclusteredcolumnchart",
//    "area",
//    "stackedarea",
//    "100stackedarea",
//    "radar",
//    "spider", // Included as an alias for radar
//    "ribbonchart",
//    "decompostiontree"
//};

//                                                bool isAllowed = false;
//                                                foreach (string allowed in allowedChartTypes)
//                                                {
//                                                    if (lowerType.Contains(allowed))
//                                                    {
//                                                        isAllowed = true;
//                                                        break;
//                                                    }
//                                                }
//                                                if (!isAllowed)
//                                                    continue; // Skip this visual if not an allowed chart type.

//                                                var chartData = new ChartData();
//                                                chartData.Id = container.TryGetProperty("id", out JsonElement idElem)
//                                                    ? idElem.GetString() : "N/A";
//                                                chartData.Type = visualType;
//                                                chartData.Height = container.TryGetProperty("height", out JsonElement heightElem)
//                                                    ? heightElem.ToString() : "N/A";
//                                                chartData.Width = container.TryGetProperty("width", out JsonElement widthElem)
//                                                    ? widthElem.ToString() : "N/A";
//                                                chartData.X = container.TryGetProperty("x", out JsonElement xElem)
//                                                    ? xElem.ToString() : "N/A";
//                                                chartData.Y = container.TryGetProperty("y", out JsonElement yElem)
//                                                    ? yElem.ToString() : "N/A";

//                                                // Extract title and style properties.
//                                                if (singleVisual.TryGetProperty("vcObjects", out JsonElement vcObjects) &&
//                                                    vcObjects.ValueKind == JsonValueKind.Object)
//                                                {
//                                                    if (vcObjects.TryGetProperty("title", out JsonElement titleArray) &&
//                                                        titleArray.ValueKind == JsonValueKind.Array &&
//                                                        titleArray.GetArrayLength() > 0)
//                                                    {
//                                                        JsonElement titleObj = titleArray[0];
//                                                        if (titleObj.TryGetProperty("properties", out JsonElement titleProps))
//                                                        {
//                                                            chartData.Title = titleProps.TryGetProperty("text", out JsonElement textProps)
//                                                                ? GetPropertyAsString(textProps) : "No Title";
//                                                            chartData.FontWeight = titleProps.TryGetProperty("fontWeight", out JsonElement fontWeightProp)
//                                                                ? GetPropertyAsString(fontWeightProp) : "Normal";
//                                                            chartData.FontStyle = titleProps.TryGetProperty("fontStyle", out JsonElement fontStyleProp)
//                                                                ? GetPropertyAsString(fontStyleProp) : "Normal";
//                                                            chartData.FontSize = titleProps.TryGetProperty("fontSize", out JsonElement fontSizeProp)
//                                                                ? GetPropertyAsString(fontSizeProp) : "Default";
//                                                            chartData.TextDecoration = titleProps.TryGetProperty("textDecoration", out JsonElement textDecorationProp)
//                                                                ? GetPropertyAsString(textDecorationProp) : "None";
//                                                            chartData.FontFamily = titleProps.TryGetProperty("fontFamily", out JsonElement fontFamilyProp)
//                                                                ? GetPropertyAsString(fontFamilyProp) : "Default";
//                                                            chartData.FontColor = titleProps.TryGetProperty("fontColor", out JsonElement fontColorProp)
//                                                                ? GetPropertyAsString(fontColorProp) : "Default";
//                                                            chartData.Alignment = titleProps.TryGetProperty("alignment", out JsonElement alignmentProp)
//                                                                ? GetPropertyAsString(alignmentProp) : "Left";
//                                                            chartData.Bold = titleProps.TryGetProperty("bold", out JsonElement boldProp)
//                                                                ? GetPropertyAsString(boldProp) : "False";
//                                                            chartData.Underline = titleProps.TryGetProperty("underline", out JsonElement underlineProp)
//                                                                ? GetPropertyAsString(underlineProp) : "False";
//                                                        }
//                                                    }
//                                                }

//                                                // Extract aggregations from projections.
//                                                if (singleVisual.TryGetProperty("projections", out JsonElement projections) &&
//                                                    projections.ValueKind == JsonValueKind.Object)
//                                                {
//                                                    foreach (JsonProperty prop in projections.EnumerateObject())
//                                                    {
//                                                        if (prop.Value.ValueKind == JsonValueKind.Array)
//                                                        {
//                                                            foreach (JsonElement item in prop.Value.EnumerateArray())
//                                                            {
//                                                                if (item.TryGetProperty("queryRef", out JsonElement queryRef))
//                                                                {
//                                                                    chartData.Aggregations.Add(queryRef.GetString());
//                                                                }
//                                                            }
//                                                        }
//                                                    }
//                                                }

//                                                // Extract filters (as a raw JSON string).
//                                                if (container.TryGetProperty("filters", out JsonElement filtersElem))
//                                                {
//                                                    chartData.Filters = filtersElem.GetRawText();
//                                                }

//                                                chartsList.Add(chartData);
//                                            }
//                                        }
//                                        catch (JsonException je)
//                                        {
//                                            Console.WriteLine("Error parsing config JSON: " + je.Message);
//                                        }
//                                    }
//                                }
//                            }
//                            sectionData.Charts = chartsList.ToArray();
//                            sectionsList.Add(sectionData);
//                        }
//                        layoutData.Sections = sectionsList.ToArray();
//                    }
//                    else
//                    {
//                        layoutData.Sections = Array.Empty<SectionData>();
//                    }
//                    return layoutData;
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Error reading layout: " + ex.Message);
//                return null;
//            }
//        }

//        // Data structures to hold layout information.
//        public class LayoutData
//        {
//            public string ReportTitle { get; set; }
//            public SectionData[] Sections { get; set; }
//        }

//        public class SectionData
//        {
//            public string SectionName { get; set; }
//            public string DisplayName { get; set; }
//            public ChartData[] Charts { get; set; }
//        }

//        public class ChartData
//        {
//            public string Id { get; set; }
//            public string Type { get; set; }
//            public string Height { get; set; }
//            public string Width { get; set; }
//            public string X { get; set; }
//            public string Y { get; set; }
//            public string Title { get; set; }
//            public string FontWeight { get; set; }
//            public string FontStyle { get; set; }
//            public string FontSize { get; set; }
//            public string TextDecoration { get; set; }
//            public string FontFamily { get; set; }
//            public string FontColor { get; set; }
//            public string Alignment { get; set; }
//            public string Bold { get; set; }
//            public string Underline { get; set; }
//            public List<string> Aggregations { get; set; } = new List<string>();
//            public List<string> ColorDetails { get; set; } = new List<string>();
//            public List<string> Fonts { get; set; } = new List<string>();
//            public string Filters { get; set; }
//        }
//    }




