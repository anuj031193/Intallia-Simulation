//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Diagnostics;
//using System.Linq;
//using System.Text.Json;
//using Microsoft.AnalysisServices.AdomdClient;

//public class Task_3
//{
//    public class Relationship
//    {
//        public string name { get; set; }
//        public string fromTable { get; set; }
//        public string fromCardinality { get; set; }
//        public string fromColumn { get; set; }
//        public string toTable { get; set; }
//        public string toColumn { get; set; }
//        public string crossFilteringBehavior { get; set; }
//    }

//    public string DataRelationshipManagerViaPort(int timeoutSeconds = 60)
//    {
//        string port = GetPowerBIPortFromMsmdsrv();
//        if (string.IsNullOrEmpty(port))
//            return "{\"error\": \"Power BI port not found.\"}";

//        string initialConnStr = $"Data Source=localhost:{port};Integrated Security=SSPI;";
//        string catalogName = null;

//        using (var conn = new AdomdConnection(initialConnStr))
//        {
//            if (!TryConnect(conn, timeoutSeconds))
//                return "{\"error\": \"Connection to Analysis Services failed.\"}";

//            catalogName = GetCatalogName(conn);
//            if (string.IsNullOrEmpty(catalogName))
//                return "{\"error\": \"No catalogs found.\"}";
//        }

//        using (var conn = new AdomdConnection(
//            $"Data Source=localhost:{port};Initial Catalog={catalogName};Integrated Security=SSPI;"))
//        {
//            if (!TryConnect(conn, timeoutSeconds / 2))
//                return "{\"error\": \"Connection to catalog failed.\"}";

//            // Build table ID to name mapping
//            var tableNames = new Dictionary<string, string>();
//            var tableColumnsAvailable = ListAvailableColumns(conn, "TMSCHEMA_TABLES");
//            string tableIdCol = tableColumnsAvailable.FirstOrDefault(x => x.Equals("ID", StringComparison.OrdinalIgnoreCase)
//                || x.Equals("TableID", StringComparison.OrdinalIgnoreCase)
//                || x.Equals("Table Id", StringComparison.OrdinalIgnoreCase)
//                || x.Equals("Table_Id", StringComparison.OrdinalIgnoreCase));
//            string tableNameCol = tableColumnsAvailable.FirstOrDefault(x => x.Equals("Name", StringComparison.OrdinalIgnoreCase));
//            if (tableIdCol != null && tableNameCol != null)
//            {
//                string tableQuery = $"SELECT [{tableIdCol}], [{tableNameCol}] FROM $SYSTEM.TMSCHEMA_TABLES";
//                using (var cmd = new AdomdCommand(tableQuery, conn))
//                using (var reader = cmd.ExecuteReader())
//                {
//                    while (reader.Read())
//                        tableNames[reader[tableIdCol].ToString().Trim()] = reader[tableNameCol].ToString();
//                }
//            }

//            // Build (TableID, ColumnID) -> ColumnName mapping (GUID-based)
//            var columnNames = new Dictionary<(string TableId, string ColumnId), string>();
//            var colColumnsAvailable = ListAvailableColumns(conn, "TMSCHEMA_COLUMNS");
//            string colIdCol = colColumnsAvailable.FirstOrDefault(x => x.Equals("ID", StringComparison.OrdinalIgnoreCase));
//            string colNameCol = colColumnsAvailable.FirstOrDefault(x => x.Equals("Name", StringComparison.OrdinalIgnoreCase));
//            string colTableIdCol = colColumnsAvailable.FirstOrDefault(x => x.Equals("TableID", StringComparison.OrdinalIgnoreCase)
//                || x.Equals("Table Id", StringComparison.OrdinalIgnoreCase)
//                || x.Equals("Table_Id", StringComparison.OrdinalIgnoreCase));
//            if (colIdCol != null && colNameCol != null && colTableIdCol != null)
//            {
//                string colQuery = $"SELECT [{colTableIdCol}], [{colIdCol}], [{colNameCol}] FROM $SYSTEM.TMSCHEMA_COLUMNS";
//                using (var cmd = new AdomdCommand(colQuery, conn))
//                using (var reader = cmd.ExecuteReader())
//                {
//                    while (reader.Read())
//                    {
//                        string tableId = reader[colTableIdCol].ToString().Trim();
//                        string colId = reader[colIdCol].ToString().Trim();
//                        string colName = reader[colNameCol].ToString();
//                        columnNames[(tableId, colId)] = colName;
//                    }
//                }
//            }

//            // Add this dictionary for LineageID-based column mapping
//            var columnNamesByLineage = new Dictionary<(string TableId, int LineageId), string>();

//            // Query DISCOVER_CALC_DEPENDENCY to get LineageID mappings
//            try
//            {
//                string depQuery = "SELECT [Object] FROM $SYSTEM.DISCOVER_CALC_DEPENDENCY WHERE [ObjectType] = 'COLUMN'";
//                using (var cmd = new AdomdCommand(depQuery, conn))
//                using (var reader = cmd.ExecuteReader())
//                {
//                    while (reader.Read())
//                    {
//                        string objectXml = reader["Object"].ToString();
//                        var doc = new System.Xml.XmlDocument();
//                        doc.LoadXml(objectXml);

//                        string tableId = doc.SelectSingleNode("/Object/TableID")?.InnerText?.Trim();
//                        var lineageNode = doc.SelectSingleNode("/Object/LineageID");
//                        var nameNode = doc.SelectSingleNode("/Object/Name");

//                        if (tableId != null && lineageNode != null && nameNode != null &&
//                            int.TryParse(lineageNode.InnerText, out int lineageId))
//                        {
//                            columnNamesByLineage[(tableId, lineageId)] = nameNode.InnerText;
//                        }
//                    }
//                }
//            }
//            catch { /* Gracefully handle errors */ }

//            // Detect columns in TMSCHEMA_RELATIONSHIPS
//            var relColumns = ListAvailableColumns(conn, "TMSCHEMA_RELATIONSHIPS");
//            string idCol = relColumns.FirstOrDefault(x => string.Equals(x, "ID", StringComparison.OrdinalIgnoreCase));
//            string nameCol = relColumns.FirstOrDefault(x => string.Equals(x, "Name", StringComparison.OrdinalIgnoreCase));
//            string fromTableCol = relColumns.FirstOrDefault(x => string.Equals(x, "FromTableID", StringComparison.OrdinalIgnoreCase)) ??
//                                  relColumns.FirstOrDefault(x => x.IndexOf("FromTable", StringComparison.OrdinalIgnoreCase) >= 0);
//            string fromColumnCol = relColumns.FirstOrDefault(x => string.Equals(x, "FromColumnID", StringComparison.OrdinalIgnoreCase)) ??
//                                   relColumns.FirstOrDefault(x => x.IndexOf("FromColumn", StringComparison.OrdinalIgnoreCase) >= 0);
//            string toTableCol = relColumns.FirstOrDefault(x => string.Equals(x, "ToTableID", StringComparison.OrdinalIgnoreCase)) ??
//                                relColumns.FirstOrDefault(x => x.IndexOf("ToTable", StringComparison.OrdinalIgnoreCase) >= 0);
//            string toColumnCol = relColumns.FirstOrDefault(x => string.Equals(x, "ToColumnID", StringComparison.OrdinalIgnoreCase)) ??
//                                 relColumns.FirstOrDefault(x => x.IndexOf("ToColumn", StringComparison.OrdinalIgnoreCase) >= 0);

//            string fromCardinalityCol = relColumns.FirstOrDefault(x => x.IndexOf("FromCardinality", StringComparison.OrdinalIgnoreCase) >= 0);
//            string crossFilteringBehaviorCol = relColumns.FirstOrDefault(x => x.IndexOf("CrossFilteringBehavior", StringComparison.OrdinalIgnoreCase) >= 0);

//            // If required columns are missing, return empty relationships array
//            if (idCol == null || fromTableCol == null || fromColumnCol == null || toTableCol == null || toColumnCol == null)
//                return "{\"DataRelationshipManager\": {\"relationships\": [] }}";

//            // Build and run the dynamic relationships query
//            string selectCols = $"[{idCol}] AS RelId, [{fromTableCol}] AS FromTableId, [{fromColumnCol}] AS FromColumnId, [{toTableCol}] AS ToTableId, [{toColumnCol}] AS ToColumnId";
//            if (fromCardinalityCol != null)
//                selectCols += $", [{fromCardinalityCol}] AS FromCardinality";
//            if (crossFilteringBehaviorCol != null)
//                selectCols += $", [{crossFilteringBehaviorCol}] AS CrossFilteringBehavior";
//            if (nameCol != null)
//                selectCols += $", [{nameCol}] AS RelName";
//            string query = $"SELECT {selectCols} FROM $SYSTEM.TMSCHEMA_RELATIONSHIPS";

//            var relationships = new List<Relationship>();
//            using (var cmd = new AdomdCommand(query, conn))
//            using (var reader = cmd.ExecuteReader())
//            {
//                while (reader.Read())
//                {
//                    string relGuidOrId = reader["RelId"]?.ToString();
//                    string relNameValue = nameCol != null && reader["RelName"] != DBNull.Value ? reader["RelName"].ToString() : null;
//                    string nameValue = !string.IsNullOrEmpty(relGuidOrId) && Guid.TryParse(relGuidOrId, out _) ? relGuidOrId : relNameValue ?? relGuidOrId;

//                    string fromTableId = reader["FromTableId"]?.ToString().Trim();
//                    string fromColumnId = reader["FromColumnId"]?.ToString().Trim();
//                    string toTableId = reader["ToTableId"]?.ToString().Trim();
//                    string toColumnId = reader["ToColumnId"]?.ToString().Trim();

//                    string fromCardinality = fromCardinalityCol != null && reader["FromCardinality"] != DBNull.Value
//                        ? MapCardinality(reader["FromCardinality"].ToString())
//                        : null;
//                    string crossFilteringBehavior = crossFilteringBehaviorCol != null && reader["CrossFilteringBehavior"] != DBNull.Value
//                        ? MapCrossFiltering(reader["CrossFilteringBehavior"].ToString())
//                        : null;

//                    // Resolve COLUMN NAMES using LineageID or GUID mapping
//                    string fromColumnName = ResolveColumnName(
//                        fromTableId, fromColumnId,
//                        columnNames, columnNamesByLineage
//                    );

//                    string toColumnName = ResolveColumnName(
//                        toTableId, toColumnId,
//                        columnNames, columnNamesByLineage
//                    );

//                    relationships.Add(new Relationship
//                    {
//                        name = nameValue,
//                        fromTable = tableNames.GetValueOrDefault(fromTableId, fromTableId),
//                        fromCardinality = fromCardinality,
//                        fromColumn = fromColumnName,
//                        toTable = tableNames.GetValueOrDefault(toTableId, toTableId),
//                        toColumn = toColumnName,
//                        crossFilteringBehavior = crossFilteringBehavior
//                    });
//                }
//            }

//            // Output JSON
//            var output = new
//            {
//                DataRelationshipManager = new
//                {
//                    relationships = relationships
//                }
//            };

//            var options = new JsonSerializerOptions { WriteIndented = true };
//            return JsonSerializer.Serialize(output, options);
//        }
//    }

//    private static string MapCardinality(string code)
//    {
//        if (string.IsNullOrEmpty(code)) return null;
//        if (code == "1" || code.Equals("one", StringComparison.OrdinalIgnoreCase)) return "one";
//        if (code == "2" || code.Equals("many", StringComparison.OrdinalIgnoreCase)) return "many";
//        return code;
//    }

//    private static string MapCrossFiltering(string code)
//    {
//        if (string.IsNullOrEmpty(code)) return null;
//        if (code == "1" || code.Equals("OneDirection", StringComparison.OrdinalIgnoreCase)) return "OneDirection";
//        if (code == "2" || code.Equals("BothDirections", StringComparison.OrdinalIgnoreCase)) return "BothDirections";
//        return code;
//    }

//    private List<string> ListAvailableColumns(AdomdConnection conn, string rowsetName)
//    {
//        var columns = new List<string>();
//        using (var cmd = new AdomdCommand($"SELECT TOP 0 * FROM $SYSTEM.{rowsetName}", conn))
//        using (var reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
//        {
//            var schemaTable = reader.GetSchemaTable();
//            foreach (DataRow row in schemaTable.Rows)
//                columns.Add(row["ColumnName"].ToString());
//        }
//        return columns;
//    }

//    private bool TryConnect(AdomdConnection conn, int timeoutSeconds)
//    {
//        int retries = timeoutSeconds;
//        while (retries-- > 0)
//        {
//            try
//            {
//                conn.Open();
//                return true;
//            }
//            catch
//            {
//                System.Threading.Thread.Sleep(1000);
//            }
//        }
//        return false;
//    }

//    private string GetCatalogName(AdomdConnection conn)
//    {
//        try
//        {
//            using (var cmd = new AdomdCommand("SELECT [CATALOG_NAME] FROM $SYSTEM.DBSCHEMA_CATALOGS", conn))
//            using (var reader = cmd.ExecuteReader())
//            {
//                if (reader.Read())
//                    return reader.GetString(0);
//            }
//            return null;
//        }
//        catch
//        {
//            return null;
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
//                        var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+\.\d+\.\d+\.\d+):(\d+)\s+.*LISTENING\s+" + pid);
//                        if (match.Success)
//                        {
//                            return match.Groups[2].Value;
//                        }
//                    }
//                }
//            }
//        }
//        catch { }
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

//    // Helper method to resolve column names with both mappings
//    private string ResolveColumnName(
//        string tableId,
//        string columnId,
//        Dictionary<(string, string), string> guidMappings,
//        Dictionary<(string, int), string> lineageMappings)
//    {
//        if (tableId == null || columnId == null)
//            return columnId;

//        // Try LineageID mapping (for integer IDs)
//        if (int.TryParse(columnId, out int lineageId) &&
//            lineageMappings.TryGetValue((tableId, lineageId), out var lineageName))
//        {
//            return lineageName;
//        }
//        // Try GUID mapping
//        else if (guidMappings.TryGetValue((tableId, columnId), out var guidName))
//        {
//            return guidName;
//        }
//        // Fallback to original ID
//        return columnId;
//    }
//}