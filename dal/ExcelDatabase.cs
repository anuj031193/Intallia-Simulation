using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Data.SqlClient;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace JobSimulation.DAL
{
    public class ExcelDatabase
    {
        private readonly string _connectionString;

        public ExcelDatabase()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["JobSimulationDB"].ConnectionString
                                ?? throw new ArgumentNullException(nameof(_connectionString));
        }

        //public void ReadFromExcel(string filePath)
        //{
        //    if (!File.Exists(filePath))
        //    {
        //        throw new FileNotFoundException("File not found.", filePath);
        //    }

        //    using (var spreadsheetDocument = SpreadsheetDocument.Open(filePath, false))
        //    {
        //        var workbookPart = spreadsheetDocument.WorkbookPart;
        //        var sheets = workbookPart.Workbook.Sheets;
        //        foreach (Sheet sheet in sheets)
        //        {
        //            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
        //            var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();
        //            if (sheetData != null)
        //            {
        //                foreach (Row row in sheetData.Elements<Row>())
        //                {
        //                    foreach (Cell cell in row.Elements<Cell>())
        //                    {
        //                        string cellValue = GetCellValue(spreadsheetDocument, cell);
        //                        Console.WriteLine($"Row {row.RowIndex}, Cell {cell.CellReference}: {cellValue}");
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //public void WriteToExcel(string filePath, string data)
        //{
        //    using (var spreadsheetDocument = SpreadsheetDocument.Create(filePath, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook))
        //    {
        //        var workbookPart = spreadsheetDocument.AddWorkbookPart();
        //        workbookPart.Workbook = new Workbook();

        //        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        //        worksheetPart.Worksheet = new Worksheet(new SheetData());

        //        var sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());
        //        var sheet = new Sheet()
        //        {
        //            Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
        //            SheetId = 1,
        //            Name = "Sheet1"
        //        };
        //        sheets.Append(sheet);

        //        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

        //        string[] rows = data.Split('\n');
        //        foreach (string rowData in rows)
        //        {
        //            var row = new Row();
        //            string[] cells = rowData.Split('\t');
        //            foreach (string cellData in cells)
        //            {
        //                var cell = new Cell()
        //                {
        //                    CellValue = new CellValue(cellData),
        //                    DataType = CellValues.String
        //                };
        //                row.Append(cell);
        //            }
        //            sheetData.Append(row);
        //        }

        //        workbookPart.Workbook.Save();
        //    }
        //}

        //public void SaveExcelFile(string filePath)
        //{
        //    int maxRetries = 5;
        //    int delay = 1000;
        //    bool isSaved = false;

        //    for (int retry = 0; retry < maxRetries; retry++)
        //    {
        //        try
        //        {
        //            // Example save logic for Excel
        //            isSaved = true; // Placeholder for actual save logic
        //            break;
        //        }
        //        catch (Exception ex)
        //        {
        //            if (retry == maxRetries - 1)
        //                throw new IOException($"Failed to save after {maxRetries} attempts", ex);

        //            Thread.Sleep(delay);
        //        }
        //    }

        //    if (!isSaved)
        //        throw new IOException($"Failed to save Excel file '{filePath}'");
        //}

        public string FetchMasterJson(string sectionId)
        {
            string jsonString = string.Empty;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT JsonFile FROM Section WHERE SectionId = @SectionId";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SectionId", sectionId);
                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        jsonString = result.ToString();
                    }
                }
            }
            var decodedJson = Encoding.UTF8.GetString(Convert.FromBase64String(jsonString));
            return decodedJson;
        }

        //private string GetCellValue(SpreadsheetDocument doc, Cell cell)
        //{
        //    var value = cell.CellValue?.Text;

        //    if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
        //    {
        //        var stringTable = doc.WorkbookPart.SharedStringTablePart.SharedStringTable;
        //        value = stringTable.ElementAt(int.Parse(value)).InnerText;
        //    }

        //    return value;
        //}
    }
}