using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Data.SqlClient;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using JobSimulation.BLL;

namespace JobSimulation.DAL
{
    // ExcelDatabase.cs
    public class ExcelDatabase
    {
        private readonly FileService _fileService;

        public ExcelDatabase(FileService fileService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }

        public string FetchMasterJson(string sectionId)
        {
            return _fileService.FetchSectionJson(sectionId);
        }
    }

    // WordDatabase.cs
    public class WordDatabase
    {
        private readonly FileService _fileService;

        public WordDatabase(FileService fileService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }

        public string FetchMasterJson(string sectionId)
        {
            return _fileService.FetchSectionJson(sectionId);
        }
    }

}