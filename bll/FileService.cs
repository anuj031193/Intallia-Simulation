//using System;
//using System.Configuration;
//using System.IO;
//using System.Runtime.InteropServices;
//using System.Threading;
//using DocumentFormat.OpenXml.Packaging;
//using DocumentFormat.OpenXml.Spreadsheet;
//using Microsoft.Data.SqlClient;
//using JobSimulation.DAL;
//using JobSimulation.Models;
//using System.Text;

//namespace JobSimulation.BLL
//{
//    public class FileService
//    {
//        private readonly string _connectionString;

//        public FileService()
//        {
//            _connectionString = ConfigurationManager.ConnectionStrings["JobSimulationDB"].ConnectionString
//                                ?? throw new ArgumentNullException(nameof(_connectionString));
//        }

//        public string ConvertFileToBase64(string filePath)
//        {
//            if (!File.Exists(filePath))
//            {
//                throw new FileNotFoundException("File not found.", filePath);
//            }

//            byte[] fileBytes = File.ReadAllBytes(filePath);
//            return Convert.ToBase64String(fileBytes);
//        }

//        public byte[] ConvertBase64ToFile(string base64File)
//        {
//            return Convert.FromBase64String(base64File);
//        }

//        public void DeleteFile(string filePath)
//        {
//            if (File.Exists(filePath))
//            {
//                File.Delete(filePath);
//            }
//            else
//            {
//                throw new FileNotFoundException("File not found.", filePath);
//            }
//        }



//        public string FetchSectionJson(string sectionId)
//        {
//            string jsonString = string.Empty;
//            using (SqlConnection connection = new SqlConnection(_connectionString))
//            {
//                connection.Open();
//                string query = "SELECT JsonFile FROM Section WHERE SectionId = @SectionId";
//                using (SqlCommand command = new SqlCommand(query, connection))
//                {
//                    command.Parameters.AddWithValue("@SectionId", sectionId);
//                    var result = command.ExecuteScalar();
//                    if (result != null)
//                    {
//                        jsonString = result.ToString();
//                    }
//                }
//            }
//            var decodedJson = Encoding.UTF8.GetString(Convert.FromBase64String(jsonString));
//            return decodedJson;
//        }


//        public void SaveStudentFileToDatabase(string sectionId, string userId, string base64File)
//        {
//            try
//            {
//                using (SqlConnection connection = new SqlConnection(_connectionString))
//                {
//                    connection.Open();
//                    string query = @"
//                        IF EXISTS (SELECT 1 FROM Activity a
//                                   INNER JOIN Task t ON a.TaskId = t.TaskId
//                                   WHERE t.SectionId = @SectionId AND a.UserId = @UserId)
//                        BEGIN
//                            UPDATE Activity 
//                            SET StudentFile = @StudentFile, ModifyBy = @ModifyBy, ModifyDate = @ModifyDate, Status = @Status
//                            FROM Activity a
//                            INNER JOIN Task t ON a.TaskId = t.TaskId
//                            WHERE t.SectionId = @SectionId AND a.UserId = @UserId
//                        END
//                        ELSE
//                        BEGIN
//                            INSERT INTO Activity (UserId, StudentFile, ModifyBy, ModifyDate, SectionId, Status) 
//                            SELECT @UserId, @StudentFile, @ModifyBy, @ModifyDate, @SectionId, @Status
//                            FROM Task t
//                            WHERE t.SectionId = @SectionId
//                        END";

//                    using (SqlCommand command = new SqlCommand(query, connection))
//                    {
//                        command.Parameters.AddWithValue("@SectionId", sectionId);
//                        command.Parameters.AddWithValue("@UserId", userId);
//                        command.Parameters.AddWithValue("@StudentFile", base64File);
//                        command.Parameters.AddWithValue("@ModifyBy", userId);
//                        command.Parameters.AddWithValue("@ModifyDate", DateTime.UtcNow);
//                        command.Parameters.AddWithValue("@Status", "Incomplete");

//                        command.ExecuteNonQuery();
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new Exception("An error occurred while saving the student file to the database.", ex);
//            }
//        }

//        public byte[] FetchStudentFile(string userId, string sectionId)
//        {
//            string base64File = string.Empty;
//            byte[] studentFile = null;
//            string query = @"
//                SELECT a.StudentFile
//                FROM Activity a
//                WHERE a.UserId = @UserId AND a.SectionId = @SectionId";

//            using (SqlConnection connection = new SqlConnection(_connectionString))
//            {
//                connection.Open();
//                using (SqlCommand command = new SqlCommand(query, connection))
//                {
//                    command.Parameters.AddWithValue("@UserId", userId);
//                    command.Parameters.AddWithValue("@SectionId", sectionId);

//                    var result = command.ExecuteScalar();
//                    if (result != DBNull.Value && result != null)
//                    {
//                        base64File = result.ToString();
//                    }
//                }
//            }

//            if (!string.IsNullOrEmpty(base64File))
//            {
//                studentFile = ConvertBase64ToFile(base64File);
//            }

//            return studentFile;
//        }

//        public byte[] FetchInitialFile(string sectionId)
//        {
//            string base64File = string.Empty;
//            byte[] initialFile = null;
//            string query = "SELECT StudentFile FROM Section WHERE SectionId = @SectionId";

//            using (SqlConnection connection = new SqlConnection(_connectionString))
//            {
//                connection.Open();
//                using (SqlCommand command = new SqlCommand(query, connection))
//                {
//                    command.Parameters.AddWithValue("@SectionId", sectionId);

//                    var result = command.ExecuteScalar();
//                    if (result != DBNull.Value && result != null)
//                    {
//                        base64File = result.ToString();
//                        initialFile = ConvertBase64ToFile(base64File);
//                    }
//                }
//            }

//            return initialFile;
//        }

//        public void OpenStudentFile(string sectionId, string softwareId, string userId)
//        {
//            byte[] studentFileBytes = FetchStudentFile(userId, sectionId) ?? FetchInitialFile(sectionId);
//            if (studentFileBytes == null || studentFileBytes.Length == 0)
//            {
//                throw new FileNotFoundException("Student file not found in the database.");
//            }

//            string fileDirectory = Path.Combine(Path.GetTempPath(), "JobSimulationFiles");
//            Directory.CreateDirectory(fileDirectory);
//            string filePath = Path.Combine(fileDirectory, $"{sectionId}_{userId}.file");

//            File.WriteAllBytes(filePath, studentFileBytes);

//            OpenFile(filePath, softwareId);
//        }

//        public void OpenFile(string filePath, string softwareId)
//        {
//            switch (softwareId)
//            {
//                case "S1":
//                    OpenExcelFile(filePath);
//                    break;
//                case "S2":
//                    OpenWordFile(filePath);
//                    break;
//                case "S3":
//                    OpenPowerPointFile(filePath);
//                    break;
//                default:
//                    throw new NotSupportedException($"Software ID '{softwareId}' is not supported.");
//            }
//        }

//        private void OpenExcelFile(string filePath)
//        {
//            using (var spreadsheetDocument = SpreadsheetDocument.Open(filePath, false))
//            {
//                // Perform Excel-specific operations
//            }
//        }

//        private void OpenWordFile(string filePath)
//        {
//            // Logic to open Word file
//            Console.WriteLine($"Opening Word file: {filePath}");
//        }

//        private void OpenPowerPointFile(string filePath)
//        {
//            // Logic to open PowerPoint file
//            Console.WriteLine($"Opening PowerPoint file: {filePath}");
//        }

//        private bool IsFileLocked(string filePath)
//        {
//            try
//            {
//                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
//                {
//                    stream.Close();
//                }
//            }
//            catch (IOException)
//            {
//                // The file is locked
//                return true;
//            }
//            return false;
//        }

//    }
//}

using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Data.SqlClient;

namespace JobSimulation.BLL
{
    public class FileService
    {
        private readonly string _connectionString;
        private readonly string _userDirectoryPath;

        public FileService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["JobSimulationDB"].ConnectionString
                                ?? throw new ArgumentNullException(nameof(_connectionString));
            _userDirectoryPath = Path.Combine(Path.GetTempPath(), "JobSimulationFiles");
            Directory.CreateDirectory(_userDirectoryPath);
        }

        public string ConvertFileToBase64(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found.", filePath);
            }

            byte[] fileBytes = File.ReadAllBytes(filePath);
            return Convert.ToBase64String(fileBytes);
        }

        public byte[] ConvertBase64ToFile(string base64File)
        {
            return Convert.FromBase64String(base64File);
        }

        public void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            else
            {
                throw new FileNotFoundException("File not found.", filePath);
            }
        }

        public string FetchSectionJson(string sectionId)
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
            return Encoding.UTF8.GetString(Convert.FromBase64String(jsonString));
        }

        public void SaveStudentFileToDatabase(string sectionId, string userId, string base64File)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                    IF EXISTS (SELECT 1 FROM Activity a
                               INNER JOIN Task t ON a.TaskId = t.TaskId
                               WHERE t.SectionId = @SectionId AND a.UserId = @UserId)
                    BEGIN
                        UPDATE Activity 
                        SET StudentFile = @StudentFile, ModifyBy = @ModifyBy, ModifyDate = @ModifyDate, Status = @Status
                        FROM Activity a
                        INNER JOIN Task t ON a.TaskId = t.TaskId
                        WHERE t.SectionId = @SectionId AND a.UserId = @UserId
                    END
                    ELSE
                    BEGIN
                        INSERT INTO Activity (UserId, StudentFile, ModifyBy, ModifyDate, SectionId, Status) 
                        SELECT @UserId, @StudentFile, @ModifyBy, @ModifyDate, @SectionId, @Status
                        FROM Task t
                        WHERE t.SectionId = @SectionId
                    END";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SectionId", sectionId);
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@StudentFile", base64File);
                    command.Parameters.AddWithValue("@ModifyBy", userId);
                    command.Parameters.AddWithValue("@ModifyDate", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@Status", "Incomplete");
                    command.ExecuteNonQuery();
                }
            }
        }

        public byte[] FetchStudentFile(string userId, string sectionId)
        {
            string base64File = string.Empty;
            byte[] studentFile = null;
            string query = @"
                SELECT a.StudentFile
                FROM Activity a
                WHERE a.UserId = @UserId AND a.SectionId = @SectionId";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@SectionId", sectionId);

                    var result = command.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {
                        base64File = result.ToString();
                    }
                }
            }

            return !string.IsNullOrEmpty(base64File) ? ConvertBase64ToFile(base64File) : null;
        }

        public byte[] FetchInitialFile(string sectionId)
        {
            string base64File = string.Empty;
            byte[] initialFile = null;
            string query = "SELECT StudentFile FROM Section WHERE SectionId = @SectionId";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SectionId", sectionId);

                    var result = command.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {
                        base64File = result.ToString();
                        initialFile = ConvertBase64ToFile(base64File);
                    }
                }
            }

            return initialFile;
        }

        public string GetFileExtension(string softwareId) => softwareId switch
        {
            "S1" => ".xlsx",
            "S2" => ".docx",
            "S3" => ".pptx",
            "S4" => ".gsheet",
            "S5" => ".gdoc",
            "S6" => ".gslides",
            _ => throw new ArgumentException("Unknown software ID")
        };

        public string SaveFileToUserDirectory(byte[] fileBytes, string fileExtension, string sectionId, string userId)
        {
            string filePath = Path.Combine(_userDirectoryPath, $"{sectionId}_{userId}{fileExtension}");
            File.WriteAllBytes(filePath, fileBytes);
            return filePath;
        }

        public void OpenFileMaximized(string filePath)
        {
            try
            {
                string extension = Path.GetExtension(filePath).ToLower();
                if (extension is ".gdoc" or ".gsheet" or ".gslides")
                {
                    Process.Start(new ProcessStartInfo("https://drive.google.com") { UseShellExecute = true });
                }
                else
                {
                    Process.Start(new ProcessStartInfo(filePath)
                    {
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Maximized
                    })?.WaitForInputIdle();
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error opening file: {ex.Message}");
            }
        }

        public void CloseFile(string filePath)
        {
            try
            {
                var processName = GetProcessNameForFileType(filePath);
                if (!string.IsNullOrEmpty(processName))
                {
                    foreach (var process in Process.GetProcessesByName(processName))
                    {
                        if (process.MainWindowTitle.Contains(Path.GetFileNameWithoutExtension(filePath)))
                        {
                            process.Kill();
                        }
                    }
                }

                if (File.Exists(filePath)) File.Delete(filePath);
            }
            catch
            {
                // Silent fail
            }
        }
public string OpenStudentFileFromBytes(byte[] fileBytes, string sectionId, string softwareId, string userId)
{
    string userDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JobSimulationFiles", userId);
    Directory.CreateDirectory(userDirectory);

    string fileExtension = GetFileExtension(softwareId); // e.g., ".docx", ".xls", etc.
    string fileName = $"{sectionId}_{Guid.NewGuid()}{fileExtension}";
    string filePath = Path.Combine(userDirectory, fileName);

    File.WriteAllBytes(filePath, fileBytes);

    // ✅ Open the file
    Process.Start(new ProcessStartInfo
    {
        FileName = filePath,
        UseShellExecute = true
    });

    // ✅ Return the full file path
    return filePath;
}


        private string GetProcessNameForFileType(string filePath) =>
            Path.GetExtension(filePath)?.ToLower() switch
            {
                ".xlsx" or ".xls" => "EXCEL",
                ".pdf" => "Acrobat",
                ".docx" or ".doc" => "WINWORD",
                ".pptx" or ".ppt" => "POWERPNT",
                ".txt" => "notepad",
                _ => null
            };

        private bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                return true;
            }
            return false;
        }
    }
}