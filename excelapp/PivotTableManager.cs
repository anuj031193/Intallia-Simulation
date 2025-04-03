using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using JobSimulation.Models;
using DocumentFormat.OpenXml.Office2021.DocumentTasks;

namespace JobSimulation.excelApp
{
    public class PivotTableManager
    {
        private readonly WorkbookPart _workbookPart;
 


        public PivotTableManager(WorkbookPart workbookPart)
        {
           
            _workbookPart = workbookPart ?? throw new ArgumentNullException(nameof(workbookPart));
        }

        public List<PivotTableData> DetectPivotTables(
     WorksheetPart worksheetPart,
     string startCell,
     string endCell,
     string taskId,
     string worksheetName)
        {
            var pivotTables = new List<PivotTableData>();
            var targetRange = $"{startCell}:{endCell}";

            foreach (var pivotTablePart in worksheetPart.PivotTableParts)
            {
                var pivotDef = pivotTablePart.PivotTableDefinition;
                var location = pivotDef?.Location?.Reference?.Value;

                if (string.IsNullOrEmpty(location)) continue;

                // Check if pivot table is within our target range
                if (IsPivotTableInRange(location, startCell, endCell))
                {
                    var pivotData = new PivotTableData
                    {
                        TaskId = taskId,
                        Values = new PivotTableValues()
                    };

                    try
                    {
                        if (TryParseCellRange(location, out string pivotStart, out string pivotEnd))
                        {
                            ExtractHeaders(pivotData, worksheetPart, pivotStart, pivotEnd);
                            ExtractPivotTableValues(pivotData, worksheetPart, location);
                        }

                        pivotTables.Add(pivotData);
                    }
                    catch (Exception ex)
                    {
                        pivotTables.Add(new PivotTableData
                        {
                            TaskId = taskId,
                            Values = new PivotTableValues
                            {
                                //Error = $"Error processing pivot table: {ex.Message}"
                            }
                        });
                    }
                }
            }

            return pivotTables;
        }

        private bool IsPivotTableInRange(string pivotLocation, string startCell, string endCell)
        {
            if (!TryParseCellRange(pivotLocation, out string pivotStart, out string pivotEnd))
                return false;

            // Check if pivot table start is within our range
            if (!IsCellInRange(pivotStart, startCell, endCell))
                return false;

            // Check if pivot table end is within our range
            if (!IsCellInRange(pivotEnd, startCell, endCell))
                return false;

            return true;
        }

        private void ExtractHeaders(PivotTableData pivotData, WorksheetPart worksheetPart, string startCell, string endCell)
        {
            var headerRowIndex = GetRowIndex(startCell);
            var headerRow = GetRow(worksheetPart, headerRowIndex);
            if (headerRow == null) return;

            foreach (var cell in headerRow.Elements<Cell>())
            {
                if (IsCellInRange(cell.CellReference.Value, startCell, endCell))
                {
                    var value = GetCellValue(cell, _workbookPart);
                    pivotData.Values.Headers.Add(
                        cell.CellReference.Value,
                        CleanHeaderValue(value)
                    );
                }
            }
        }

        private void ExtractPivotTableValues(PivotTableData pivotData, WorksheetPart worksheetPart, string location)
        {
            if (!TryParseCellRange(location, out string startCell, out string endCell)) return;

            // Extract headers from first row
            var headerRow = GetRow(worksheetPart, GetRowIndex(startCell));
            if (headerRow != null)
            {
                foreach (var cell in headerRow.Elements<Cell>())
                {
                    if (IsCellInRange(cell.CellReference.Value, startCell, endCell))
                    {
                        var value = GetCellValue(cell, _workbookPart);
                        pivotData.Values.Headers[cell.CellReference.Value] = value;
                    }
                }
            }

            // Extract data from subsequent rows
            var startRow = GetRowIndex(startCell) + 1;
            var endRow = GetRowIndex(endCell);
            for (int rowNum = startRow; rowNum <= endRow; rowNum++)
            {
                var row = GetRow(worksheetPart, rowNum);
                if (row == null) continue;

                foreach (var cell in row.Elements<Cell>())
                {
                    if (IsCellInRange(cell.CellReference.Value, startCell, endCell))
                    {
                        var value = GetCellValue(cell, _workbookPart);
                        pivotData.Values.Data[cell.CellReference.Value] = value;
                    }
                }
            }
        }
        private bool TryParseCellRange(string range, out string startCell, out string endCell)
        {
            var parts = range?.Split(':') ?? Array.Empty<string>();
            if (parts.Length == 2)
            {
                startCell = parts[0];
                endCell = parts[1];
                return true;
            }
            startCell = endCell = string.Empty;
            return false;
        }

        private Row GetRow(WorksheetPart worksheetPart, int rowIndex)
        {
            return worksheetPart.Worksheet.Descendants<Row>()
                .FirstOrDefault(r => (int?)r.RowIndex?.Value == rowIndex);
        }

        private string CleanHeaderValue(string value)
        {
            return value.Replace("Count of", "").Replace("Sum of", "").Trim();
        }

        #region Helper Methods
        private bool IsCellInRange(string cellRef, string startCell, string endCell)
        {
            int cellCol = GetColumnIndex(cellRef);
            int cellRow = GetRowIndex(cellRef);

            int startCol = GetColumnIndex(startCell);
            int endCol = GetColumnIndex(endCell);
            int startRow = GetRowIndex(startCell);
            int endRow = GetRowIndex(endCell);

            return cellCol >= startCol && cellCol <= endCol &&
                   cellRow >= startRow && cellRow <= endRow;
        }

        private int GetColumnIndex(string cellReference)
        {
            string column = new string(cellReference.TakeWhile(char.IsLetter).ToArray());
            int index = 0;
            foreach (char c in column.ToUpperInvariant())
            {
                index = index * 26 + (c - 'A' + 1);
            }
            return index - 1;
        }

        private int GetRowIndex(string cellReference)
        {
            string rowPart = new string(cellReference.SkipWhile(char.IsLetter).ToArray());
            return int.TryParse(rowPart, out int row) ? row : 0;
        }

        private string GetColumnLetter(string cellReference)
        {
            return new string(cellReference.TakeWhile(char.IsLetter).ToArray());
        }

        private string GetCellValue(Cell cell, WorkbookPart workbookPart)
        {
            if (cell?.CellValue == null) return string.Empty;

            if (cell.DataType?.Value == CellValues.SharedString)
            {
                var sst = workbookPart.SharedStringTablePart?.SharedStringTable;
                return sst?.ElementAt(int.Parse(cell.CellValue.Text))?.InnerText ?? string.Empty;
            }
            return cell.CellValue.Text;
        }
        #endregion
    }
}