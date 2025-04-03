using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using JobSimulation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Font = DocumentFormat.OpenXml.Spreadsheet.Font;
using Task = JobSimulation.Models;

namespace JobSimulation.excelApp
{
    public class CellManager
    {
        private readonly WorkbookPart _workbookPart;
        private readonly Stylesheet _stylesheet;

        public CellManager(WorkbookPart workbookPart)
        {
            _workbookPart = workbookPart ?? throw new ArgumentNullException(nameof(workbookPart));
            _stylesheet = _workbookPart.WorkbookStylesPart?.Stylesheet ?? throw new InvalidOperationException("Workbook stylesheet not found.");
        }

        public List<CellData> DetectCells(string sheetName, string fromCell, string toCell, string TaskId)
        {
            List<CellData> cellDataList = new List<CellData>();

            // Get the worksheet part
            WorksheetPart worksheetPart = (WorksheetPart)_workbookPart.GetPartById(
                _workbookPart.Workbook.Descendants<Sheet>().First(s => s.Name == sheetName).Id);

            // Get the sheet data
            SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

            // Get cell references
            var cellReferences = GetCellReferences(fromCell, toCell);

            foreach (var cellReference in cellReferences)
            {
                Cell cell = sheetData.Descendants<Cell>().FirstOrDefault(c => string.Equals(c.CellReference, cellReference, StringComparison.OrdinalIgnoreCase));

                if (cell != null)
                {
                    string cellValue = GetCellValue(cell);

                    var cellData = new CellData
                    {
                        TaskId = TaskId,
                        Value = cellValue,
                        Formula = cell.CellFormula?.Text,
                        BackgroundColor = GetCellBackgroundColor(cell),
                        FontColor = GetCellFontColor(cell),
                        Bold = GetCellFontBold(cell),
                        Italic = GetCellFontItalic(cell),
                        FontName = GetCellFontName(cell),
                        FontSize = GetCellFontSize(cell)
                    };

                    cellDataList.Add(cellData);
                }
            }

            return cellDataList;
        }

        private string GetCellValue(Cell cell)
        {
            if (cell.DataType?.Value == CellValues.SharedString)
            {
                var sharedStringTable = _workbookPart?.SharedStringTablePart?.SharedStringTable;
                if (sharedStringTable != null && int.TryParse(cell.InnerText, out int index))
                {
                    return sharedStringTable.ElementAt(index).InnerText;
                }
            }
            else if (cell.DataType?.Value == CellValues.Number || cell.DataType == null)
            {
                return cell.CellValue?.Text ?? "Empty";
            }
            else if (cell.DataType?.Value == CellValues.Boolean)
            {
                return cell.CellValue?.Text == "1" ? "TRUE" : "FALSE";
            }
            else if (cell.DataType?.Value == CellValues.Date)
            {
                return cell.CellValue?.Text ?? "Empty";
            }
            else if (cell.CellValue != null)
            {
                return cell.CellValue.Text;
            }

            return "Empty";
        }

        private List<int> GetCellBackgroundColor(Cell cell)
        {
            var cellFormat = GetCellFormat(cell);

            if (cellFormat?.FillId != null)
            {
                var fill = _stylesheet.Fills.ElementAtOrDefault((int)cellFormat.FillId.Value) as Fill;
                var patternFill = fill?.PatternFill;

                if (patternFill?.ForegroundColor?.Rgb != null)
                {
                    return ConvertHexToRgb(patternFill.ForegroundColor.Rgb.Value);
                }
                else if (patternFill?.BackgroundColor?.Rgb != null)
                {
                    return ConvertHexToRgb(patternFill.BackgroundColor.Rgb.Value);
                }
            }

            // Default custom background color [244, 176, 132]
            return new List<int> { 244, 176, 132 };
        }

        private List<int> GetCellFontColor(Cell cell)
        {
            var cellFormat = GetCellFormat(cell);
            var font = cellFormat?.FontId != null ? _stylesheet.Fonts.ElementAt((int)cellFormat.FontId.Value) as Font : null;

            if (font?.Color?.Rgb != null)
            {
                return ConvertHexToRgb(font.Color.Rgb.Value);
            }

            // Default font color [0, 0, 0] (black)
            return new List<int> { 0, 0, 0 };
        }

        private bool GetCellFontBold(Cell cell)
        {
            var cellFormat = GetCellFormat(cell);
            var font = cellFormat?.FontId != null ? _stylesheet.Fonts.ElementAt((int)cellFormat.FontId.Value) as Font : null;
            return font?.Bold != null;
        }

        private bool GetCellFontItalic(Cell cell)
        {
            var cellFormat = GetCellFormat(cell);
            var font = cellFormat?.FontId != null ? _stylesheet.Fonts.ElementAt((int)cellFormat.FontId.Value) as Font : null;
            return font?.Italic != null;
        }

        private string GetCellFontName(Cell cell)
        {
            var cellFormat = GetCellFormat(cell);
            var font = cellFormat?.FontId != null ? _stylesheet.Fonts.ElementAt((int)cellFormat.FontId.Value) as Font : null;
            return font?.FontName?.Val ?? "Calibri"; // Default font name
        }

        private double GetCellFontSize(Cell cell)
        {
            var cellFormat = GetCellFormat(cell);
            var font = cellFormat?.FontId != null ? _stylesheet.Fonts.ElementAt((int)cellFormat.FontId.Value) as Font : null;
            return font?.FontSize?.Val ?? 11; // Default font size
        }

        private CellFormat GetCellFormat(Cell cell)
        {
            if (cell.StyleIndex == null) return null;
            return _stylesheet.CellFormats.ElementAtOrDefault((int)cell.StyleIndex.Value) as CellFormat;
        }

        private List<int> ConvertHexToRgb(string hex)
        {
            if (hex.Length == 8)
            {
                hex = hex.Substring(2);
            }
            hex = hex.Replace("#", "");

            int r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            int g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            int b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            return new List<int> { r, g, b };
        }

        private List<string> GetCellReferences(string fromCell, string toCell)
        {
            var cellReferences = new List<string>();

            if (string.Equals(fromCell, toCell, StringComparison.OrdinalIgnoreCase))
            {
                cellReferences.Add(fromCell);
                return cellReferences;
            }

            var startColumn = new string(fromCell.Where(char.IsLetter).ToArray());
            var endColumn = new string(toCell.Where(char.IsLetter).ToArray());

            var startRow = int.Parse(new string(fromCell.Where(char.IsDigit).ToArray()));
            var endRow = int.Parse(new string(toCell.Where(char.IsDigit).ToArray()));

            for (int row = startRow; row <= endRow; row++)
            {
                for (char col = startColumn[0]; col <= endColumn[0]; col++)
                {
                    cellReferences.Add($"{col}{row}");
                }
            }

            return cellReferences;
        }
    }
}

