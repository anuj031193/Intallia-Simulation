using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AnalysisServices.AdomdClient;

namespace JobSimulation.powerBiApp
{
    public class Task_2
    {
        public class ColumnInfo
        {
            public string Name { get; set; }
            public string DataType { get; set; }
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
            public string name { get; set; }
            public List<ColumnInfo> columns { get; set; } = new List<ColumnInfo>();
            public List<PartitionInfo> partitions { get; set; } = new List<PartitionInfo>();
        }

        // Mapping from DataType integer code to Power BI data type string
        ////private static string MapDataType(string code)
        ////{
        ////    switch (code)
        ////    {
        ////        case "1": return "binary";
        ////        case "2": return "string";
        ////        case "3": return "int64";
        ////        case "4": return "decimal";
        ////        case "5": return "double";
        ////        case "6": return "int64";
        ////        case "7": return "boolean";
        ////        case "8": return "double";
        ////        case "9": return "dateTime";
        ////        case "10": return "date";
        ////        case "11": return "time";
        ////        case "12": return "dateTime";
        ////        default: return code; // fallback to original if unknown
        ////    }
        ////}
        private static string MapDataType(string code)
        {
            if (string.IsNullOrEmpty(code)) return "unknown";

            switch (code)
            {
                case "1": return "binary";
                case "2": return "string";
                case "3": return "int64";
                case "4": return "decimal";
                case "5": return "decimal";   // treat double as decimal for UI consistency
                case "6": return "int64";
                case "7": return "boolean";
                case "8": return "decimal";   // treat single as decimal for UI consistency
                case "9": return "dateTime";
                case "10": return "date";
                case "11": return "time";
                case "12": return "dateTime";
                default: return "unknown";
            }
        }



        /// <summary>
        /// Extracts detailed table structure from a running Power BI Desktop instance (pbix file loaded) via port,
        /// and returns the result in the required JSON format (same as for DataModelSchema in pbit).
        /// </summary>
        public string DataTransformationManagerViaPort(int timeoutSeconds = 60)
        {
            string port = GetPowerBIPortFromMsmdsrv();
            if (string.IsNullOrEmpty(port))
                return "{\"error\": \"Power BI port not found.\"}";

            string initialConnStr = $"Data Source=localhost:{port};Integrated Security=SSPI;";
            string catalogName = null;

            using (var conn = new AdomdConnection(initialConnStr))
            {
                if (!TryConnect(conn, timeoutSeconds))
                    return "{\"error\": \"Connection to Analysis Services failed.\"}";

                catalogName = GetCatalogName(conn);
                if (string.IsNullOrEmpty(catalogName))
                    return "{\"error\": \"No catalogs found.\"}";
            }

            // Now connect to catalog and extract table+column info
            using (var conn = new AdomdConnection(
                $"Data Source=localhost:{port};Initial Catalog={catalogName};Integrated Security=SSPI;"))
            {
                if (!TryConnect(conn, timeoutSeconds / 2))
                    return "{\"error\": \"Connection to catalog failed.\"}";

                List<TableInfo> tables = new List<TableInfo>();

                // Get table and column info
                var tableColumns = GetSchemaColumns(conn, "TMSCHEMA_TABLES");
                string tableIdColumn = tableColumns.Contains("ID") ? "ID"
                    : tableColumns.FirstOrDefault(c => c.EndsWith("ID", StringComparison.OrdinalIgnoreCase)) ?? "ID";
                string tableNameColumn = tableColumns.Contains("Name") ? "Name"
                    : tableColumns.FirstOrDefault(c => c.EndsWith("Name", StringComparison.OrdinalIgnoreCase)) ?? "Name";

                var tableData = new Dictionary<long, string>();
                using (var cmd = new AdomdCommand($@"
                SELECT [{tableIdColumn}] AS TableID, [{tableNameColumn}] AS TableName
                FROM $SYSTEM.TMSCHEMA_TABLES", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        object idObj = reader["TableID"];
                        object nameObj = reader["TableName"];
                        if (idObj == null || idObj is DBNull) continue;
                        string name = (nameObj != null && !(nameObj is DBNull)) ? nameObj.ToString() : "UNKNOWN_TABLE";
                        long id = Convert.ToInt64(idObj);

                        // Exclude date tables
                        if (name.StartsWith("DateTabletemplate_", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("LocalDateTable_", StringComparison.OrdinalIgnoreCase))
                            continue;

                        tableData[id] = name;
                    }
                }

                // Column info
                var columnColumns = GetSchemaColumns(conn, "TMSCHEMA_COLUMNS");
                string columnTableIdColumn = columnColumns.Contains("TableID") ? "TableID"
                    : columnColumns.FirstOrDefault(c => c.EndsWith("TableID", StringComparison.OrdinalIgnoreCase)) ?? "TableID";
                string columnNameColumn = columnColumns.Contains("Name") ? "Name"
                    : columnColumns.FirstOrDefault(c => c.EndsWith("Name", StringComparison.OrdinalIgnoreCase)) ?? "Name";
                string dataTypeColumn = columnColumns.Contains("DataType") ? "DataType"
                    : columnColumns.FirstOrDefault(c => c.IndexOf("DataType", StringComparison.OrdinalIgnoreCase) >= 0) ?? "DataType";

                var columnsByTable = new Dictionary<long, List<ColumnInfo>>();
                using (var cmd = new AdomdCommand($@"
                SELECT [{columnTableIdColumn}] AS TableID, [{columnNameColumn}] AS ColumnName, [{dataTypeColumn}] AS DataType
                FROM $SYSTEM.TMSCHEMA_COLUMNS", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        object tableIdObj = reader["TableID"];
                        object colNameObj = reader["ColumnName"];
                        object dataTypeObj = reader["DataType"];
                        if (tableIdObj == null || tableIdObj is DBNull) continue;

                        long tableId = Convert.ToInt64(tableIdObj);
                        string colName = (colNameObj != null && !(colNameObj is DBNull)) ? colNameObj.ToString() : "UNKNOWN_COLUMN";
                        string dataType = (dataTypeObj != null && !(dataTypeObj is DBNull)) ? dataTypeObj.ToString() : "N/A";

                        // Exclude RowNumber and technical columns
                        if (colName.StartsWith("RowNumber-", StringComparison.OrdinalIgnoreCase)) continue;

                        if (!columnsByTable.ContainsKey(tableId))
                            columnsByTable[tableId] = new List<ColumnInfo>();

                        columnsByTable[tableId].Add(new ColumnInfo { Name = colName, DataType = MapDataType(dataType) });
                    }
                }

                // No partitions info available via port, but for compatibility, we add an empty list
                foreach (var kv in tableData)
                {
                    tables.Add(new TableInfo
                    {
                        name = kv.Value,
                        columns = columnsByTable.ContainsKey(kv.Key) ? columnsByTable[kv.Key] : new List<ColumnInfo>(),
                        partitions = new List<PartitionInfo>() // Could be empty, as port extraction can't get M partitions
                    });
                }

                var output = new
                {
                    DataTransformationManager = new
                    {
                        tables = tables
                    }
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                return JsonSerializer.Serialize(output, options);
            }
        }

        // Helper methods (same as in Task_1, can be refactored to a common class)

        private bool TryConnect(AdomdConnection conn, int timeoutSeconds)
        {
            int retries = timeoutSeconds;
            while (retries-- > 0)
            {
                try
                {
                    conn.Open();
                    return true;
                }
                catch
                {
                    System.Threading.Thread.Sleep(1000);
                }
            }
            return false;
        }

        private string GetCatalogName(AdomdConnection conn)
        {
            try
            {
                using (var cmd = new AdomdCommand("SELECT [CATALOG_NAME] FROM $SYSTEM.DBSCHEMA_CATALOGS", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return reader.GetString(0);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private List<string> GetSchemaColumns(AdomdConnection conn, string rowsetName)
        {
            var columns = new List<string>();
            try
            {
                using (var cmd = new AdomdCommand($"SELECT TOP 0 * FROM $SYSTEM.{rowsetName}", conn))
                using (var colReader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
                {
                    var schemaTable = colReader.GetSchemaTable();
                    foreach (DataRow row in schemaTable.Rows)
                    {
                        columns.Add(row["ColumnName"].ToString());
                    }
                }
            }
            catch { }
            return columns;
        }

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
                            var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+\.\d+\.\d+\.\d+):(\d+)\s+.*LISTENING\s+" + pid);
                            if (match.Success)
                            {
                                return match.Groups[2].Value; // The port
                            }
                        }
                    }
                }
            }
            catch { }
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

      
//public static string MergeColumnsAndPartitions(string portJson, string fileJson)
//    {
//        // Parse both JSON results
//        var portRoot = JsonNode.Parse(portJson)?["DataTransformationManager"];
//        var fileRoot = JsonNode.Parse(fileJson)?["DataTransformationManager"];

//        var portTables = portRoot?["tables"]?.AsArray() ?? new JsonArray();
//        var fileTables = fileRoot?["tables"]?.AsArray() ?? new JsonArray();

//        // Build a lookup for partitions by table name (from file)
//        var partitionLookup = new Dictionary<string, JsonNode>();
//        foreach (var ft in fileTables)
//        {
//            string name = ft?["name"]?.ToString();
//            if (!string.IsNullOrWhiteSpace(name) && ft?["partitions"] is JsonNode partitions)
//            {
//                partitionLookup[name] = partitions;
//            }
//        }

//        // Merge partitions into port tables
//        foreach (var pt in portTables)
//        {
//            string name = pt?["name"]?.ToString();
//            if (!string.IsNullOrWhiteSpace(name) && partitionLookup.TryGetValue(name, out var partitions))
//            {
//                pt["partitions"] = partitions.DeepClone();
//            }
//            else
//            {
//                pt["partitions"] = new JsonArray(); // fallback to empty if not found
//            }
//        }

//        // Build final output
//        var merged = new JsonObject
//        {
//            ["DataTransformationManager"] = new JsonObject
//            {
//                ["tables"] = portTables
//            }
//        };

//        var options = new JsonSerializerOptions
//        {
//            WriteIndented = true,
//            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
//        };
//        return merged.ToJsonString(options);
//    }

//        public string DataTransformationManager(string pbitPath)
//        {
//            try
//            {
//                // Open the pbit file as a ZIP archive.
//                using (ZipArchive archive = ZipFile.OpenRead(pbitPath))
//                {
//                    // Retrieve the DataModelSchema entry.
//                    ZipArchiveEntry entry = archive.GetEntry("DataModelSchema");
//                    if (entry == null)
//                        return "{\"error\": \"DataModelSchema not found in the pbit archive.\"}";

//                    using (Stream s = entry.Open())
//                    using (MemoryStream ms = new MemoryStream())
//                    {
//                        s.CopyTo(ms);
//                        string jsonString = Encoding.UTF8.GetString(ms.ToArray()).Replace("\0", "");
//                        var masterTables = ExtractTables(jsonString);

//                        // Build an anonymous object for JSON output.
//                        var tableList = masterTables.Select(kvp => new
//                        {
//                            name = kvp.Key,
//                            columns = kvp.Value.Columns,
//                            partitions = kvp.Value.Partitions
//                        }).ToList();

//                        var output = new
//                        {
//                            DataTransformationManager = new
//                            {
//                                tables = tableList
//                            }
//                        };

//                        var options = new JsonSerializerOptions { WriteIndented = true };
//                        return JsonSerializer.Serialize(output, options);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                return $"{{\"error\": \"{ex.Message}\"}}";
//            }
//        }

    }

}
