using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TradeDataStudio.Core.Models;
using TradeDataStudio.Core.Services;

namespace TradeDataStudio.Tests
{
    [TestClass]
    public class ExportServiceTests
    {
        private ExportService _exportService;
        private LoggingService _loggingService;
        private ConfigurationService _configService;
        private string _testOutputPath;

        [TestInitialize]
        public void Setup()
        {
            _loggingService = new LoggingService();
            _configService = new ConfigurationService();
            _exportService = new ExportService(_configService, _loggingService);
            
            _testOutputPath = Path.Combine(Path.GetTempPath(), "TradeDataStudio_ExportTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testOutputPath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testOutputPath))
            {
                Directory.Delete(_testOutputPath, true);
            }
            
            _exportService?.Dispose();
            _loggingService?.Dispose();
        }

        private DataTable CreateTestDataTable()
        {
            var table = new DataTable("TestTable");
            table.Columns.Add("ID", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Date", typeof(DateTime));
            table.Columns.Add("Amount", typeof(decimal));

            table.Rows.Add(1, "Test Item 1", DateTime.Now, 100.50m);
            table.Rows.Add(2, "Test Item 2", DateTime.Now.AddDays(-1), 250.75m);
            table.Rows.Add(3, "Test Item 3", DateTime.Now.AddDays(-2), 75.25m);

            return table;
        }

        [TestMethod]
        public async Task ExportToExcelAsync_WithValidData_ShouldCreateExcelFile()
        {
            // Arrange
            var testData = CreateTestDataTable();

            // Act
            var result = await _exportService.ExportToExcelAsync("TestTable", _testOutputPath, testData);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(ExportFormat.Excel, result.Format);
            Assert.AreEqual(3, result.RecordsExported);
            Assert.IsTrue(File.Exists(result.FilePath));
            Assert.IsTrue(result.FileName.EndsWith(".xlsx"));
            Assert.IsTrue(result.FileSize > 0);
        }

        [TestMethod]
        public async Task ExportToCsvAsync_WithValidData_ShouldCreateCsvFile()
        {
            // Arrange
            var testData = CreateTestDataTable();

            // Act
            var result = await _exportService.ExportToCsvAsync("TestTable", _testOutputPath, testData);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(ExportFormat.CSV, result.Format);
            Assert.AreEqual(3, result.RecordsExported);
            Assert.IsTrue(File.Exists(result.FilePath));
            Assert.IsTrue(result.FileName.EndsWith(".csv"));
            Assert.IsTrue(result.FileSize > 0);
            
            // Verify CSV content
            var csvContent = File.ReadAllText(result.FilePath);
            Assert.IsTrue(csvContent.Contains("ID,Name,Date,Amount"));
            Assert.IsTrue(csvContent.Contains("Test Item 1"));
        }

        [TestMethod]
        public async Task ExportToTextAsync_WithValidData_ShouldCreateTextFile()
        {
            // Arrange
            var testData = CreateTestDataTable();

            // Act
            var result = await _exportService.ExportToTextAsync("TestTable", _testOutputPath, testData);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(ExportFormat.TXT, result.Format);
            Assert.AreEqual(3, result.RecordsExported);
            Assert.IsTrue(File.Exists(result.FilePath));
            Assert.IsTrue(result.FileName.EndsWith(".txt"));
            Assert.IsTrue(result.FileSize > 0);
            
            // Verify text content (tab-delimited)
            var textContent = File.ReadAllText(result.FilePath);
            Assert.IsTrue(textContent.Contains("ID\tName\tDate\tAmount"));
            Assert.IsTrue(textContent.Contains("Test Item 1"));
        }

        [TestMethod]
        public void GenerateFileName_WithValidInputs_ShouldReturnCorrectFormat()
        {
            // Act
            var fileName = _exportService.GenerateFileName("TestTable", ExportFormat.Excel, OperationMode.Export);

            // Assert
            Assert.IsTrue(fileName.StartsWith("Export_TestTable_"));
            Assert.IsTrue(fileName.EndsWith(".xlsx"));
            Assert.IsTrue(fileName.Length > 20); // Should include timestamp
        }

        [TestMethod]
        public async Task ExportToExcelAsync_WithEmptyData_ShouldCreateEmptyFile()
        {
            // Arrange
            var emptyTable = new DataTable("EmptyTable");
            emptyTable.Columns.Add("ID", typeof(int));
            emptyTable.Columns.Add("Name", typeof(string));

            // Act
            var result = await _exportService.ExportToExcelAsync("EmptyTable", _testOutputPath, emptyTable);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.RecordsExported);
            Assert.IsTrue(File.Exists(result.FilePath));
        }

        [TestMethod]
        public async Task ExportToCsvAsync_WithNullValues_ShouldHandleGracefully()
        {
            // Arrange
            var table = new DataTable("TestTable");
            table.Columns.Add("ID", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("OptionalField", typeof(string));

            table.Rows.Add(1, "Test Item 1", "Value1");
            table.Rows.Add(2, null, null); // Null values
            table.Rows.Add(3, "Test Item 3", DBNull.Value); // DBNull value

            // Act
            var result = await _exportService.ExportToCsvAsync("TestTable", _testOutputPath, table);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.RecordsExported);
            Assert.IsTrue(File.Exists(result.FilePath));
            
            var csvContent = File.ReadAllText(result.FilePath);
            Assert.IsTrue(csvContent.Contains("Test Item 1"));
            Assert.IsTrue(csvContent.Contains("Test Item 3"));
        }

        [TestMethod]
        public async Task ExportAllTablesAsync_WithMultipleTables_ShouldExportAll()
        {
            // Arrange
            var tableNames = new List<string> { "Table1", "Table2", "Table3" };
            
            // Act
            var results = await _exportService.ExportAllTablesAsync(tableNames, ExportFormat.CSV, _testOutputPath);

            // Assert
            Assert.AreEqual(3, results.Count);
            foreach (var result in results)
            {
                Assert.IsTrue(result.Success);
                Assert.AreEqual(ExportFormat.CSV, result.Format);
                Assert.AreEqual(0, result.RecordsExported); // Empty tables
            }
        }

        [TestMethod]
        public async Task ExportToExcelAsync_InvalidPath_ShouldHandleError()
        {
            // Arrange
            var testData = CreateTestDataTable();
            var invalidPath = "Z:\\NonExistentDrive\\InvalidPath";

            // Act
            var result = await _exportService.ExportToExcelAsync("TestTable", invalidPath, testData);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Message);
            Assert.IsNotNull(result.Exception);
        }
    }
}