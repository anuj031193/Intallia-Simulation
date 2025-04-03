using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace JobSimulation.excelApp
{
    public class WorkbookManager
    {
        public WorkbookPart WorkbookPart { get; private set; }

        public void LoadWorkbookFromResource(string resourceName)
        {
            Stream stream = GetResourceStream(resourceName);
            WorkbookPart = LoadWorkbookPart(stream);
        }

        public void LoadWorkbookFromFile(string tempCopyPath)
        {
            WorkbookPart = LoadWorkbookPart(tempCopyPath);
        }

        public void LoadWorkbookFromStream(Stream stream)
        {
            WorkbookPart = LoadWorkbookPart(stream);
        }

        private WorkbookPart LoadWorkbookPart(Stream stream)
        {
            SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(stream, false);
            return spreadsheetDocument.WorkbookPart;
        }

        private WorkbookPart LoadWorkbookPart(string tempCopyPath)
        {
            SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(tempCopyPath, false);
            return spreadsheetDocument.WorkbookPart;
        }

        private Stream GetResourceStream(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceFullName = assembly.GetManifestResourceNames()
                                           .FirstOrDefault(name => name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));

            if (resourceFullName == null)
            {
                throw new FileNotFoundException("Resource not found.", resourceName);
            }

            var stream = assembly.GetManifestResourceStream(resourceFullName);

            if (stream == null)
            {
                throw new FileNotFoundException($"Resource stream for '{resourceFullName}' could not be retrieved.", resourceFullName);
            }

            return stream;
        }

        public List<string> GetSheetNames()
        {
            if (WorkbookPart == null)
            {
                throw new InvalidOperationException("WorkbookPart is not loaded.");
            }

            return WorkbookPart.Workbook.Sheets.Elements<Sheet>().Select(s => s.Name.Value).ToList();
        }
    }
}