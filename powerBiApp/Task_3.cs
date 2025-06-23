using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AnalysisServices.AdomdClient;

namespace JobSimulation.powerBiApp
{
    public class Task_3
    {
        //public class Relationship
        //{
        //    public string name { get; set; }
        //    public string fromTable { get; set; }
        //    public string fromCardinality { get; set; }
        //    public string fromColumn { get; set; }
        //    public string toTable { get; set; }
        //    public string toColumn { get; set; }
        //    public string crossFilteringBehavior { get; set; }
        //}

        public class Relationship
        {
            public string name { get; set; }
            public string fromTable { get; set; }
            public string fromColumn { get; set; }
            public string toTable { get; set; }
            public string toColumn { get; set; }
            public string crossFilteringBehavior { get; set; }
            public string relationshipType { get; set; } // Renamed from "fromCardinality"
        }
        public string DataRelationshipManagerViaPort(int timeoutSeconds = 60)
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

            using (var conn = new AdomdConnection(
                $"Data Source=localhost:{port};Initial Catalog={catalogName};Integrated Security=SSPI;"))
            {
                if (!TryConnect(conn, timeoutSeconds / 2))
                    return "{\"error\": \"Connection to catalog failed.\"}";

                // Table ID → Name mapping
                var tableNames = new Dictionary<string, string>();
                var tableColumnsAvailable = ListAvailableColumns(conn, "TMSCHEMA_TABLES");
                string tableIdCol = tableColumnsAvailable.FirstOrDefault(x => x.Equals("ID", StringComparison.OrdinalIgnoreCase));
                string tableNameCol = tableColumnsAvailable.FirstOrDefault(x => x.Equals("Name", StringComparison.OrdinalIgnoreCase));
                if (tableIdCol != null && tableNameCol != null)
                {
                    string tableQuery = $"SELECT * FROM $SYSTEM.TMSCHEMA_TABLES";
                    using (var cmd = new AdomdCommand(tableQuery, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            tableNames[reader[tableIdCol].ToString().Trim()] = reader[tableNameCol].ToString();
                    }
                }

                // (TableID, ColumnID) → ColumnName mapping
                var columnNames = new Dictionary<(string TableId, string ColId), string>();
                var colColumnsAvailable = ListAvailableColumns(conn, "TMSCHEMA_COLUMNS");
                string colIdCol = colColumnsAvailable.FirstOrDefault(x => x.Equals("ID", StringComparison.OrdinalIgnoreCase));
                string colTableIdCol = colColumnsAvailable.FirstOrDefault(x => x.Equals("TableID", StringComparison.OrdinalIgnoreCase));
                string colExplicitNameCol = colColumnsAvailable.FirstOrDefault(x => x.Equals("ExplicitName", StringComparison.OrdinalIgnoreCase));
                string colInferredNameCol = colColumnsAvailable.FirstOrDefault(x => x.Equals("InferredName", StringComparison.OrdinalIgnoreCase));
                if (colIdCol != null && colTableIdCol != null)
                {
                    string colQuery = $"SELECT * FROM $SYSTEM.TMSCHEMA_COLUMNS";
                    using (var cmd = new AdomdCommand(colQuery, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tableId = reader[colTableIdCol]?.ToString().Trim();
                            string colId = reader[colIdCol]?.ToString().Trim();
                            string explicitName = colExplicitNameCol != null ? reader[colExplicitNameCol]?.ToString() : null;
                            string inferredName = colInferredNameCol != null ? reader[colInferredNameCol]?.ToString() : null;
                            string colName = !string.IsNullOrEmpty(explicitName) ? explicitName :
                                             !string.IsNullOrEmpty(inferredName) ? inferredName :
                                             colId;
                            columnNames[(tableId, colId)] = colName;
                        }
                    }
                }

                // Relationships
                var relColumns = ListAvailableColumns(conn, "TMSCHEMA_RELATIONSHIPS");
                string idCol = relColumns.FirstOrDefault(x => x.Equals("ID", StringComparison.OrdinalIgnoreCase));
                string nameCol = relColumns.FirstOrDefault(x => x.Equals("Name", StringComparison.OrdinalIgnoreCase));
                string fromTableCol = relColumns.FirstOrDefault(x => x.Equals("FromTableID", StringComparison.OrdinalIgnoreCase));
                string fromColumnCol = relColumns.FirstOrDefault(x => x.Equals("FromColumnID", StringComparison.OrdinalIgnoreCase));
                string toTableCol = relColumns.FirstOrDefault(x => x.Equals("ToTableID", StringComparison.OrdinalIgnoreCase));
                string toColumnCol = relColumns.FirstOrDefault(x => x.Equals("ToColumnID", StringComparison.OrdinalIgnoreCase));
                string fromCardinalityCol = relColumns.FirstOrDefault(x => x.IndexOf("FromCardinality", StringComparison.OrdinalIgnoreCase) >= 0);
                string toCardinalityCol = relColumns.FirstOrDefault(x => x.IndexOf("ToCardinality", StringComparison.OrdinalIgnoreCase) >= 0);
                string crossFilteringBehaviorCol = relColumns.FirstOrDefault(x => x.IndexOf("CrossFilteringBehavior", StringComparison.OrdinalIgnoreCase) >= 0);

                if (idCol == null || fromTableCol == null || fromColumnCol == null || toTableCol == null || toColumnCol == null)
                    return "{\"DataRelationshipManager\": {\"relationships\": [] }}";

                string selectCols = $"[{idCol}] AS RelId, [{fromTableCol}] AS FromTableId, [{fromColumnCol}] AS FromColumnId, [{toTableCol}] AS ToTableId, [{toColumnCol}] AS ToColumnId";
                if (fromCardinalityCol != null)
                    selectCols += $", [{fromCardinalityCol}] AS FromCardinality";
                if (toCardinalityCol != null)
                    selectCols += $", [{toCardinalityCol}] AS ToCardinality";
                if (crossFilteringBehaviorCol != null)
                    selectCols += $", [{crossFilteringBehaviorCol}] AS CrossFilteringBehavior";
                if (nameCol != null)
                    selectCols += $", [{nameCol}] AS RelName";

                string query = $"SELECT {selectCols} FROM $SYSTEM.TMSCHEMA_RELATIONSHIPS";

                var relationships = new List<Relationship>();
                using (var cmd = new AdomdCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string relGuidOrId = reader["RelId"]?.ToString();
                        string relNameValue = nameCol != null && reader["RelName"] != DBNull.Value ? reader["RelName"].ToString() : null;
                        string nameValue = !string.IsNullOrEmpty(relGuidOrId) && Guid.TryParse(relGuidOrId, out _) ? relGuidOrId : relNameValue ?? relGuidOrId;

                        string fromTableId = reader["FromTableId"]?.ToString().Trim();
                        string fromColumnId = reader["FromColumnId"]?.ToString().Trim();
                        string toTableId = reader["ToTableId"]?.ToString().Trim();
                        string toColumnId = reader["ToColumnId"]?.ToString().Trim();

                        string fromCardinality = fromCardinalityCol != null && reader["FromCardinality"] != DBNull.Value
                            ? MapCardinality(reader["FromCardinality"].ToString())
                            : null;
                        string toCardinality = toCardinalityCol != null && reader["ToCardinality"] != DBNull.Value
                            ? MapCardinality(reader["ToCardinality"].ToString())
                            : null;

                        string relationshipType = "unknown";
                        if (fromCardinality == "many" && toCardinality == "one")
                            relationshipType = "many-to-one";
                        else if (fromCardinality == "one" && toCardinality == "many")
                            relationshipType = "one-to-many";
                        else if (fromCardinality == "one" && toCardinality == "one")
                            relationshipType = "one-to-one";
                        else if (fromCardinality == "many" && toCardinality == "many")
                            relationshipType = "many-to-many";

                        string crossFilteringBehavior = crossFilteringBehaviorCol != null && reader["CrossFilteringBehavior"] != DBNull.Value
                            ? MapCrossFiltering(reader["CrossFilteringBehavior"].ToString())
                            : null;

                        string fromColumnName = columnNames.TryGetValue((fromTableId, fromColumnId), out var n1) ? n1 : fromColumnId;
                        string toColumnName = columnNames.TryGetValue((toTableId, toColumnId), out var n2) ? n2 : toColumnId;

                        relationships.Add(new Relationship
                        {
                            name = nameValue,
                            fromTable = tableNames.GetValueOrDefault(fromTableId, fromTableId),
                            fromColumn = fromColumnName,
                            toTable = tableNames.GetValueOrDefault(toTableId, toTableId),
                            toColumn = toColumnName,
                            crossFilteringBehavior = crossFilteringBehavior,
                            relationshipType = relationshipType
                        });
                    }
                }

                var output = new
                {
                    DataRelationshipManager = new
                    {
                        relationships = relationships
                    }
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                return JsonSerializer.Serialize(output, options);
            }
        }



        //public string DataRelationshipManagerViaPort(int timeoutSeconds = 60)
        //{
        //    string port = GetPowerBIPortFromMsmdsrv();
        //    if (string.IsNullOrEmpty(port))
        //        return "{\"error\": \"Power BI port not found.\"}";

        //    string initialConnStr = $"Data Source=localhost:{port};Integrated Security=SSPI;";
        //    string catalogName = null;

        //    using (var conn = new AdomdConnection(initialConnStr))
        //    {
        //        if (!TryConnect(conn, timeoutSeconds))
        //            return "{\"error\": \"Connection to Analysis Services failed.\"}";

        //        catalogName = GetCatalogName(conn);
        //        if (string.IsNullOrEmpty(catalogName))
        //            return "{\"error\": \"No catalogs found.\"}";
        //    }

        //    using (var conn = new AdomdConnection(
        //        $"Data Source=localhost:{port};Initial Catalog={catalogName};Integrated Security=SSPI;"))
        //    {
        //        if (!TryConnect(conn, timeoutSeconds / 2))
        //            return "{\"error\": \"Connection to catalog failed.\"}";

        //        // Build table ID -> name mapping
        //        var tableNames = new Dictionary<string, string>();
        //        var tableColumnsAvailable = ListAvailableColumns(conn, "TMSCHEMA_TABLES");
        //        string tableIdCol = tableColumnsAvailable.FirstOrDefault(x => x.Equals("ID", StringComparison.OrdinalIgnoreCase));
        //        string tableNameCol = tableColumnsAvailable.FirstOrDefault(x => x.Equals("Name", StringComparison.OrdinalIgnoreCase));
        //        if (tableIdCol != null && tableNameCol != null)
        //        {
        //            string tableQuery = $"SELECT * FROM $SYSTEM.TMSCHEMA_TABLES";
        //            using (var cmd = new AdomdCommand(tableQuery, conn))
        //            using (var reader = cmd.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                    tableNames[reader[tableIdCol].ToString().Trim()] = reader[tableNameCol].ToString();
        //            }
        //        }

        //        // Build (TableID, ColumnID) -> ColumnName mapping
        //        var columnNames = new Dictionary<(string TableId, string ColId), string>();
        //        var colColumnsAvailable = ListAvailableColumns(conn, "TMSCHEMA_COLUMNS");
        //        string colIdCol = colColumnsAvailable.FirstOrDefault(x => x.Equals("ID", StringComparison.OrdinalIgnoreCase));
        //        string colTableIdCol = colColumnsAvailable.FirstOrDefault(x => x.Equals("TableID", StringComparison.OrdinalIgnoreCase));
        //        string colExplicitNameCol = colColumnsAvailable.FirstOrDefault(x => x.Equals("ExplicitName", StringComparison.OrdinalIgnoreCase));
        //        string colInferredNameCol = colColumnsAvailable.FirstOrDefault(x => x.Equals("InferredName", StringComparison.OrdinalIgnoreCase));
        //        if (colIdCol != null && colTableIdCol != null)
        //        {
        //            string colQuery = $"SELECT * FROM $SYSTEM.TMSCHEMA_COLUMNS";
        //            using (var cmd = new AdomdCommand(colQuery, conn))
        //            using (var reader = cmd.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                {
        //                    string tableId = reader[colTableIdCol]?.ToString().Trim();
        //                    string colId = reader[colIdCol]?.ToString().Trim();
        //                    string explicitName = colExplicitNameCol != null ? reader[colExplicitNameCol]?.ToString() : null;
        //                    string inferredName = colInferredNameCol != null ? reader[colInferredNameCol]?.ToString() : null;
        //                    string colName =
        //                        !string.IsNullOrEmpty(explicitName) ? explicitName :
        //                        !string.IsNullOrEmpty(inferredName) ? inferredName :
        //                        colId;
        //                    columnNames[(tableId, colId)] = colName;
        //                }
        //            }
        //        }

        //        // Relationships
        //        var relColumns = ListAvailableColumns(conn, "TMSCHEMA_RELATIONSHIPS");
        //        string idCol = relColumns.FirstOrDefault(x => x.Equals("ID", StringComparison.OrdinalIgnoreCase));
        //        string nameCol = relColumns.FirstOrDefault(x => x.Equals("Name", StringComparison.OrdinalIgnoreCase));
        //        string fromTableCol = relColumns.FirstOrDefault(x => x.Equals("FromTableID", StringComparison.OrdinalIgnoreCase));
        //        string fromColumnCol = relColumns.FirstOrDefault(x => x.Equals("FromColumnID", StringComparison.OrdinalIgnoreCase));
        //        string toTableCol = relColumns.FirstOrDefault(x => x.Equals("ToTableID", StringComparison.OrdinalIgnoreCase));
        //        string toColumnCol = relColumns.FirstOrDefault(x => x.Equals("ToColumnID", StringComparison.OrdinalIgnoreCase));
        //        string fromCardinalityCol = relColumns.FirstOrDefault(x => x.IndexOf("FromCardinality", StringComparison.OrdinalIgnoreCase) >= 0);
        //        string crossFilteringBehaviorCol = relColumns.FirstOrDefault(x => x.IndexOf("CrossFilteringBehavior", StringComparison.OrdinalIgnoreCase) >= 0);

        //        if (idCol == null || fromTableCol == null || fromColumnCol == null || toTableCol == null || toColumnCol == null)
        //            return "{\"DataRelationshipManager\": {\"relationships\": [] }}";

        //        string selectCols = $"[{idCol}] AS RelId, [{fromTableCol}] AS FromTableId, [{fromColumnCol}] AS FromColumnId, [{toTableCol}] AS ToTableId, [{toColumnCol}] AS ToColumnId";
        //        if (fromCardinalityCol != null)
        //            selectCols += $", [{fromCardinalityCol}] AS FromCardinality";
        //        if (crossFilteringBehaviorCol != null)
        //            selectCols += $", [{crossFilteringBehaviorCol}] AS CrossFilteringBehavior";
        //        if (nameCol != null)
        //            selectCols += $", [{nameCol}] AS RelName";
        //        string query = $"SELECT {selectCols} FROM $SYSTEM.TMSCHEMA_RELATIONSHIPS";

        //        var relationships = new List<Relationship>();
        //        using (var cmd = new AdomdCommand(query, conn))
        //        using (var reader = cmd.ExecuteReader())
        //        {
        //            while (reader.Read())
        //            {
        //                string relGuidOrId = reader["RelId"]?.ToString();
        //                string relNameValue = nameCol != null && reader["RelName"] != DBNull.Value ? reader["RelName"].ToString() : null;
        //                string nameValue = !string.IsNullOrEmpty(relGuidOrId) && Guid.TryParse(relGuidOrId, out _) ? relGuidOrId : relNameValue ?? relGuidOrId;

        //                string fromTableId = reader["FromTableId"]?.ToString().Trim();
        //                string fromColumnId = reader["FromColumnId"]?.ToString().Trim();
        //                string toTableId = reader["ToTableId"]?.ToString().Trim();
        //                string toColumnId = reader["ToColumnId"]?.ToString().Trim();

        //                string fromCardinality = fromCardinalityCol != null && reader["FromCardinality"] != DBNull.Value
        //                    ? MapCardinality(reader["FromCardinality"].ToString())
        //                    : null;
        //                string crossFilteringBehavior = crossFilteringBehaviorCol != null && reader["CrossFilteringBehavior"] != DBNull.Value
        //                    ? MapCrossFiltering(reader["CrossFilteringBehavior"].ToString())
        //                    : null;

        //                string fromColumnName = columnNames.TryGetValue((fromTableId, fromColumnId), out var n1) ? n1 : fromColumnId;
        //                string toColumnName = columnNames.TryGetValue((toTableId, toColumnId), out var n2) ? n2 : toColumnId;

        //                relationships.Add(new Relationship
        //                {
        //                    name = nameValue,
        //                    fromTable = tableNames.GetValueOrDefault(fromTableId, fromTableId),
        //                    fromCardinality = fromCardinality,
        //                    fromColumn = fromColumnName,
        //                    toTable = tableNames.GetValueOrDefault(toTableId, toTableId),
        //                    toColumn = toColumnName,
        //                    crossFilteringBehavior = crossFilteringBehavior
        //                });
        //            }
        //        }

        //        var output = new
        //        {
        //            DataRelationshipManager = new
        //            {
        //                relationships = relationships
        //            }
        //        };

        //        var options = new JsonSerializerOptions { WriteIndented = true };
        //        return JsonSerializer.Serialize(output, options);
        //    }
        //}

        private static string MapCardinality(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            if (code == "1" || code.Equals("one", StringComparison.OrdinalIgnoreCase)) return "one";
            if (code == "2" || code.Equals("many", StringComparison.OrdinalIgnoreCase)) return "many";
            return code;
        }

        private static string MapCrossFiltering(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            if (code == "1" || code.Equals("OneDirection", StringComparison.OrdinalIgnoreCase)) return "OneDirection";
            if (code == "2" || code.Equals("BothDirections", StringComparison.OrdinalIgnoreCase)) return "BothDirections";
            return code;
        }

        private List<string> ListAvailableColumns(AdomdConnection conn, string rowsetName)
        {
            var columns = new List<string>();
            using (var cmd = new AdomdCommand($"SELECT * FROM $SYSTEM.{rowsetName}", conn))
            using (var reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
            {
                var schemaTable = reader.GetSchemaTable();
                foreach (DataRow row in schemaTable.Rows)
                    columns.Add(row["ColumnName"].ToString());
            }
            return columns;
        }

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
                                return match.Groups[2].Value;
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
    }

}
