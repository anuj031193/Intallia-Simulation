using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.AnalysisServices.Tabular;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace JobSimulation.powerBiApp
{
    public class Task_1
    {


        public string DataImportManagerViaPort(int timeoutSeconds = 60)
        {
            Console.WriteLine("Locating Power BI Desktop port...");
            string port = GetPowerBIPortFromMsmdsrv();

            if (string.IsNullOrEmpty(port))
            {
                Console.WriteLine("Power BI port not found.");
                return null;
            }

            // Phase 1: Connect to server without catalog to discover databases
            string initialConnStr = $"Data Source=localhost:{port};Integrated Security=SSPI;";
            string catalogName = null;

            using (var conn = new AdomdConnection(initialConnStr))
            {
                if (!TryConnect(conn, timeoutSeconds))
                {
                    Console.WriteLine("Connection to Analysis Services failed");
                    return null;
                }

                catalogName = GetCatalogName(conn);
                if (string.IsNullOrEmpty(catalogName))
                {
                    Console.WriteLine("No catalogs found");
                    return null;
                }
            }

            // Phase 2: Connect to specific catalog
            using (var conn = new AdomdConnection(
                $"Data Source=localhost:{port};Initial Catalog={catalogName};Integrated Security=SSPI;"))
            {
                if (!TryConnect(conn, timeoutSeconds / 2))
                {
                    Console.WriteLine("Connection to catalog failed");
                    return null;
                }

                var tables = new Dictionary<string, List<string>>();
                var relationships = new List<Dictionary<string, string>>();

                GetTablesAndColumns(conn, tables);
                //GetRelationships(conn, relationships, tables);

                return JsonSerializer.Serialize(new { Tables = tables },
                    new JsonSerializerOptions { WriteIndented = true });
            }
        }

        private bool TryConnect(AdomdConnection conn, int timeoutSeconds)
        {
            int retries = timeoutSeconds;
            while (retries-- > 0)
            {
                try
                {
                    Console.WriteLine($"Connection attempt ({timeoutSeconds - retries}/{timeoutSeconds})...");
                    conn.Open();
                    Console.WriteLine("Connection successful!");
                    return true;
                }
                catch (AdomdConnectionException ex)
                {
                    Console.WriteLine($"Connection error: {ex.Message}");
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"General error: {ex.GetType().Name} - {ex.Message}");
                    Thread.Sleep(1000);
                }
            }
            return false;
        }

        private string GetCatalogName(AdomdConnection conn)
        {
            try
            {
                using (var cmd = new AdomdCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT [CATALOG_NAME] FROM $SYSTEM.DBSCHEMA_CATALOGS";

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetString(0);
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Catalog discovery failed: {ex.Message}");
                return null;
            }
        }
        private void GetTablesAndColumns(AdomdConnection conn, Dictionary<string, List<string>> tables)
        {
            try
            {
                // Get columns for TMSCHEMA_TABLES
                var tableColumns = GetSchemaColumns(conn, "TMSCHEMA_TABLES");
                string tableIdColumn = tableColumns.Contains("ID") ? "ID"
                    : tableColumns.FirstOrDefault(c => c.EndsWith("ID", StringComparison.OrdinalIgnoreCase)) ?? "ID";
                string tableNameColumn = tableColumns.Contains("Name") ? "Name"
                    : tableColumns.FirstOrDefault(c => c.EndsWith("Name", StringComparison.OrdinalIgnoreCase)) ?? "Name";

                // Get tables with null checking
                var tableData = new Dictionary<long, string>();
                using (var cmd = new AdomdCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = $@"
                SELECT 
                    [{tableIdColumn}] AS TableID,
                    [{tableNameColumn}] AS TableName
                FROM $SYSTEM.TMSCHEMA_TABLES";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Handle null for ID
                            object idObj = reader["TableID"];
                            if (idObj == null || idObj is DBNull)
                            {
                                Console.WriteLine("Skipping table with null ID");
                                continue;
                            }

                            // Handle null for Name
                            object nameObj = reader["TableName"];
                            string name = (nameObj != null && !(nameObj is DBNull))
                                ? nameObj.ToString()
                                : "UNKNOWN_TABLE";

                            try
                            {
                                long id = Convert.ToInt64(idObj);
                                tableData[id] = name;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error reading table: {ex.Message}");
                            }
                        }
                    }
                }

                // Get columns for TMSCHEMA_COLUMNS
                var columnColumns = GetSchemaColumns(conn, "TMSCHEMA_COLUMNS");
                string columnTableIdColumn = columnColumns.Contains("TableID") ? "TableID"
                    : columnColumns.FirstOrDefault(c => c.EndsWith("TableID", StringComparison.OrdinalIgnoreCase)) ?? "TableID";
                string columnNameColumn = columnColumns.Contains("Name") ? "Name"
                    : columnColumns.FirstOrDefault(c => c.EndsWith("Name", StringComparison.OrdinalIgnoreCase)) ?? "Name";

                // Get columns with null checking
                var columnData = new List<(long TableID, string Name)>();
                using (var cmd = new AdomdCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = $@"
                SELECT 
                    [{columnTableIdColumn}] AS TableID,
                    [{columnNameColumn}] AS ColumnName
                FROM $SYSTEM.TMSCHEMA_COLUMNS";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Handle null for TableID
                            object tableIdObj = reader["TableID"];
                            if (tableIdObj == null || tableIdObj is DBNull)
                            {
                                Console.WriteLine("Skipping column with null TableID");
                                continue;
                            }

                            // Handle null for ColumnName
                            object columnNameObj = reader["ColumnName"];
                            string colName = (columnNameObj != null && !(columnNameObj is DBNull))
                                ? columnNameObj.ToString()
                                : "UNKNOWN_COLUMN";

                            try
                            {
                                long tableId = Convert.ToInt64(tableIdObj);
                                columnData.Add((tableId, colName));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error reading column: {ex.Message}");
                            }
                        }
                    }
                }

                // Build tables dictionary with additional checks
                foreach (var table in tableData)
                {
                    string tableName = table.Value;

                    // Skip auto-generated tables
                    if (tableName.StartsWith("DateTabletemplate_", StringComparison.OrdinalIgnoreCase) ||
                        tableName.StartsWith("LocalDateTable_", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Get columns for this table
                    var columns = columnData
                        .Where(c => c.TableID == table.Key)
                        .Select(c => c.Name)
                        .ToList();

                    // Only add if we have columns
                    if (columns.Count > 0)
                    {
                        tables[tableName] = columns;
                    }
                    else
                    {
                        Console.WriteLine($"Skipping table '{tableName}' with no columns");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Table extraction failed: {ex.Message}");
                // Diagnostic fallback remains same
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
            catch (Exception ex)
            {
                Console.WriteLine($"Column detection failed for {rowsetName}: {ex.Message}");
            }
            return columns;
        }

        // Fix SafeGetString to use IDataReader interface
        private string SafeGetString(IDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                    return "UNKNOWN";
                return reader.GetString(ordinal);
            }
            catch
            {
                return "MISSING_COLUMN";
            }
        }

        // Updated GetRelationships method with table filtering
        private void GetRelationships(AdomdConnection conn, List<Dictionary<string, string>> relationships,
                                   Dictionary<string, List<string>> validTables)
        {
            try
            {
                // First, check if the relationships schema even exists
                if (!SchemaExists(conn, "TMSCHEMA_RELATIONSHIPS"))
                {
                    Console.WriteLine("TMSCHEMA_RELATIONSHIPS schema not available. Skipping relationships.");
                    return;
                }

                // Get relationship schema columns
                var relColumns = GetSchemaColumns(conn, "TMSCHEMA_RELATIONSHIPS");
                if (relColumns.Count == 0)
                {
                    Console.WriteLine("No columns found in TMSCHEMA_RELATIONSHIPS schema.");
                    return;
                }

                // Log available columns for diagnostics
                Console.WriteLine("Available columns in TMSCHEMA_RELATIONSHIPS:");
                foreach (var col in relColumns) Console.WriteLine($"- {col}");

                // Define required columns with multiple possible names
                var columnMappings = new Dictionary<string, List<string>>
                {
                    ["FromTable"] = new List<string> { "FromTable", "FromTableName", "FromTableId" },
                    ["FromColumn"] = new List<string> { "FromColumn", "FromColumnName", "FromColumnId" },
                    ["ToTable"] = new List<string> { "ToTable", "ToTableName", "ToTableId" },
                    ["ToColumn"] = new List<string> { "ToColumn", "ToColumnName", "ToColumnId" }
                };

                // Find actual column names
                var selectedColumns = new Dictionary<string, string>();
                foreach (var mapping in columnMappings)
                {
                    string foundColumn = null;
                    foreach (var candidate in mapping.Value)
                    {
                        if (relColumns.Contains(candidate))
                        {
                            foundColumn = candidate;
                            break;
                        }
                    }

                    if (foundColumn == null)
                    {
                        Console.WriteLine($"No valid column found for {mapping.Key} in TMSCHEMA_RELATIONSHIPS");
                        return;
                    }
                    selectedColumns[mapping.Key] = foundColumn;
                }

                using (var cmd = new AdomdCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = $@"
                SELECT 
                    [{selectedColumns["FromTable"]}] AS FromTable,
                    [{selectedColumns["FromColumn"]}] AS FromColumn,
                    [{selectedColumns["ToTable"]}] AS ToTable,
                    [{selectedColumns["ToColumn"]}] AS ToColumn
                FROM $SYSTEM.TMSCHEMA_RELATIONSHIPS";

                    Console.WriteLine($"Executing relationship query: {cmd.CommandText}");

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string fromTable = SafeGetString(reader, "FromTable");
                            string fromColumn = SafeGetString(reader, "FromColumn");
                            string toTable = SafeGetString(reader, "ToTable");
                            string toColumn = SafeGetString(reader, "ToColumn");

                            // Only add relationships between valid tables
                            if (validTables.ContainsKey(fromTable) && validTables.ContainsKey(toTable))
                            {
                                relationships.Add(new Dictionary<string, string>
                                {
                                    ["FromTable"] = fromTable,
                                    ["FromColumn"] = fromColumn,
                                    ["ToTable"] = toTable,
                                    ["ToColumn"] = toColumn
                                });
                            }
                            else
                            {
                                Console.WriteLine($"Skipping relationship: {fromTable}.{fromColumn} -> {toTable}.{toColumn}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Relationship extraction failed: {ex.Message}");
            }
        }

        private bool SchemaExists(AdomdConnection conn, string schemaName)
        {
            try
            {
                using (var cmd = new AdomdCommand("SELECT * FROM $SYSTEM.DISCOVER_SCHEMA_ROWSETS", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader["Rowset"].ToString().Equals(schemaName, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
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

}
