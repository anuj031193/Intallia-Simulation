using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using DocumentFormat.OpenXml;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;

namespace JobSimulation.excelApp
{
    public class ChartHelper
    {
        private WorkbookPart _workbookPart;

        public ChartHelper(WorkbookPart workbookPart)
        {
            _workbookPart = workbookPart;
        }

        // Public cell/range utilities
        public string GetCellPositionFromTwoCellAnchor(TwoCellAnchor twoCellAnchor)
        {
            var fromMarker = twoCellAnchor.FromMarker;
            var toMarker = twoCellAnchor.ToMarker;

            string fromCell = $"{GetExcelColumnName(int.Parse(fromMarker.ColumnId.Text))}{int.Parse(fromMarker.RowId.Text) + 1}";
            string toCell = $"{GetExcelColumnName(int.Parse(toMarker.ColumnId.Text))}{int.Parse(toMarker.RowId.Text) + 1}";
            return $"{fromCell}:{toCell}";
        }

        public bool IsCellInRange(string anchorCellRange, string cellRange)
        {
            var anchorCells = anchorCellRange.Split(':');
            var rangeCells = cellRange.Split(':');
            return CheckOverlap(anchorCells, rangeCells);
        }

        private bool CheckOverlap(string[] anchorCells, string[] rangeCells)
        {
            int anchorStartCol = GetColumnIndex(anchorCells[0]);
            int anchorEndCol = GetColumnIndex(anchorCells[1]);
            int anchorStartRow = GetRowIndex(anchorCells[0]);
            int anchorEndRow = GetRowIndex(anchorCells[1]);

            int rangeStartCol = GetColumnIndex(rangeCells[0]);
            int rangeEndCol = GetColumnIndex(rangeCells[1]);
            int rangeStartRow = GetRowIndex(rangeCells[0]);
            int rangeEndRow = GetRowIndex(rangeCells[1]);

            return (anchorStartCol <= rangeEndCol) && (anchorEndCol >= rangeStartCol) &&
                   (anchorStartRow <= rangeEndRow) && (anchorEndRow >= rangeStartRow);
        }

        private string GetExcelColumnName(int columnNumber)
        {
            string columnName = "";
            while (columnNumber > 0)
            {
                int modulo = (columnNumber - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                columnNumber = (columnNumber - modulo) / 26;
            }
            return columnName;
        }

        public int GetColumnIndex(string cellReference)
        {
            string columnLetters = Regex.Match(cellReference, @"[A-Za-z]+").Value;
            return ConvertColumnNameToNumber(columnLetters);
        }

        private int ConvertColumnNameToNumber(string columnName)
        {
            int sum = 0;
            foreach (char c in columnName.ToUpper())
            {
                sum = sum * 26 + (c - 'A' + 1);
            }
            return sum;
        }

        private int GetRowIndex(string cellReference)
        {
            return int.Parse(Regex.Match(cellReference, @"\d+").Value);
        }

      
    

   

        }
    }
