using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TradeDataStudio.Core.Models;
using TradeDataStudio.Core.Services;

namespace TradeDataStudio.Tests
{
    [TestClass]
    public class ConfigurationServiceTests
    {
        private ConfigurationService _configService;
        private string _testConfigPath;

        [TestInitialize]
        public void Setup()
        {
            // Create temporary test directory
            _testConfigPath = Path.Combine(Path.GetTempPath(), "TradeDataStudio_Tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testConfigPath);
            
            // Mock configuration service to use test directory
            _configService = new ConfigurationService();
            
            // Create test configuration files
            CreateTestConfigFiles();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up test directory
            if (Directory.Exists(_testConfigPath))
            {
                Directory.Delete(_testConfigPath, true);
            }
        }

        private void CreateTestConfigFiles()
        {
            // Create test database.json
            var dbConfig = @"{
                ""server"": ""localhost"",
                ""database"": ""TestDB"",
                ""useWindowsAuthentication"": true,
                ""connectionTimeout"": 30,
                ""trustServerCertificate"": true
            }";
            File.WriteAllText(Path.Combine(_testConfigPath, "database.json"), dbConfig);

            // Create test export_procedures.json
            var exportProcs = @"{
                ""procedures"": [
                    {
                        ""name"": ""TestExportProcedure"",
                        ""displayName"": ""Test Export Procedure"",
                        ""description"": ""A test procedure"",
                        ""outputTables"": [""Table1"", ""Table2""]
                    }
                ]
            }";
            File.WriteAllText(Path.Combine(_testConfigPath, "export_procedures.json"), exportProcs);

            // Create test export_tables.json
            var exportTables = @"{
                ""tables"": [
                    {
                        ""name"": ""TestTable"",
                        ""displayName"": ""Test Table"",
                        ""description"": ""A test table""
                    }
                ]
            }";
            File.WriteAllText(Path.Combine(_testConfigPath, "export_tables.json"), exportTables);
            
            // Create basic appsettings.json
            var appSettings = @"{
                ""defaultMode"": ""Export"",
                ""exportFormats"": [""Excel"", ""CSV"", ""TXT""]
            }";
            File.WriteAllText(Path.Combine(_testConfigPath, "appsettings.json"), appSettings);
        }

        [TestMethod]
        public async Task GetApplicationSettingsAsync_ShouldReturnValidSettings()
        {
            // Act
            var settings = await _configService.GetApplicationSettingsAsync();

            // Assert
            Assert.IsNotNull(settings);
            Assert.AreEqual(OperationMode.Export, settings.DefaultMode);
            Assert.IsTrue(settings.ExportFormats.Contains("Excel"));
            Assert.IsTrue(settings.ExportFormats.Contains("CSV"));
            Assert.IsTrue(settings.ExportFormats.Contains("TXT"));
        }

        [TestMethod]
        public async Task GetDatabaseConfigurationAsync_ShouldReturnValidConfiguration()
        {
            // Act
            var dbConfig = await _configService.GetDatabaseConfigurationAsync();

            // Assert
            Assert.IsNotNull(dbConfig);
            Assert.AreEqual("localhost", dbConfig.Server);
            Assert.AreEqual("TestDB", dbConfig.Database);
            Assert.IsTrue(dbConfig.UseWindowsAuthentication);
            Assert.AreEqual(30, dbConfig.ConnectionTimeout);
            Assert.IsTrue(dbConfig.TrustServerCertificate);
        }

        [TestMethod]
        public async Task GetStoredProceduresAsync_ExportMode_ShouldReturnProcedures()
        {
            // Act
            var procedures = await _configService.GetStoredProceduresAsync(OperationMode.Export);

            // Assert
            Assert.IsNotNull(procedures);
            Assert.AreEqual(1, procedures.Count);
            Assert.AreEqual("TestExportProcedure", procedures[0].Name);
            Assert.AreEqual("Test Export Procedure", procedures[0].DisplayName);
            Assert.AreEqual(2, procedures[0].OutputTables.Count);
            Assert.IsTrue(procedures[0].OutputTables.Contains("Table1"));
            Assert.IsTrue(procedures[0].OutputTables.Contains("Table2"));
        }

        [TestMethod]
        public async Task GetTablesAsync_ExportMode_ShouldReturnTables()
        {
            // Act
            var tables = await _configService.GetTablesAsync(OperationMode.Export);

            // Assert
            Assert.IsNotNull(tables);
            Assert.AreEqual(1, tables.Count);
            Assert.AreEqual("TestTable", tables[0].Name);
            Assert.AreEqual("Test Table", tables[0].DisplayName);
            Assert.AreEqual("A test table", tables[0].Description);
        }

        [TestMethod]
        public async Task SaveDatabaseConfigurationAsync_ShouldPersistConfiguration()
        {
            // Arrange
            var newConfig = new DatabaseConfiguration
            {
                Server = "newserver",
                Database = "newdb",
                UseWindowsAuthentication = false,
                Username = "testuser",
                Password = "testpass",
                ConnectionTimeout = 60
            };

            // Act
            await _configService.SaveDatabaseConfigurationAsync(newConfig);
            var savedConfig = await _configService.GetDatabaseConfigurationAsync();

            // Assert
            Assert.AreEqual(newConfig.Server, savedConfig.Server);
            Assert.AreEqual(newConfig.Database, savedConfig.Database);
            Assert.AreEqual(newConfig.UseWindowsAuthentication, savedConfig.UseWindowsAuthentication);
            Assert.AreEqual(newConfig.ConnectionTimeout, savedConfig.ConnectionTimeout);
        }

        [TestMethod]
        public async Task ValidateConfigurationAsync_WithValidFiles_ShouldReturnTrue()
        {
            // Act
            var isValid = await _configService.ValidateConfigurationAsync();

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public async Task ValidateConfigurationAsync_WithMissingFiles_ShouldReturnFalse()
        {
            // Arrange - Delete required file
            File.Delete(Path.Combine(_testConfigPath, "database.json"));

            // Act
            var isValid = await _configService.ValidateConfigurationAsync();

            // Assert
            Assert.IsFalse(isValid);
        }
    }
}