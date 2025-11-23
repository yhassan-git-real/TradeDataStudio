using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;
using TradeDataStudio.Core.Services;

namespace TradeDataStudio.Tests
{
    [TestClass]
    public class CoreServicesIntegrationTest
    {
        private IHost _host = null!;
        private IConfigurationService _configService = null!;
        private IDatabaseService _databaseService = null!;
        private IExportService _exportService = null!;
        private ILoggingService _loggingService = null!;

        [TestInitialize]
        public void Setup()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<ILoggingService, LoggingService>();
                    services.AddSingleton<IConfigurationService, ConfigurationService>();
                    services.AddTransient<IDatabaseService, DatabaseService>();
                    services.AddTransient<IExportService, ExportService>();
                })
                .Build();

            _configService = _host.Services.GetRequiredService<IConfigurationService>();
            _databaseService = _host.Services.GetRequiredService<IDatabaseService>();
            _exportService = _host.Services.GetRequiredService<IExportService>();
            _loggingService = _host.Services.GetRequiredService<ILoggingService>();
        }

        [TestMethod]
        public async Task TestConfigurationService_LoadStoredProcedures()
        {
            // Test loading export procedures
            var exportProcedures = await _configService.GetStoredProceduresAsync(OperationMode.Export);
            Assert.IsNotNull(exportProcedures);
            Assert.IsTrue(exportProcedures.Count > 0, "Should have at least one export procedure");

            var firstProc = exportProcedures.First();
            Assert.IsFalse(string.IsNullOrEmpty(firstProc.Name));
            Assert.IsFalse(string.IsNullOrEmpty(firstProc.DisplayName));
            Assert.IsTrue(firstProc.Parameters.Count >= 2, "Should have at least 2 parameters (@mon and @mon1)");

            await _loggingService.LogMainAsync($"✅ Configuration Service: Loaded {exportProcedures.Count} export procedures");
        }

        [TestMethod]
        public async Task TestConfigurationService_LoadTables()
        {
            // Test loading export tables
            var exportTables = await _configService.GetTablesAsync(OperationMode.Export);
            Assert.IsNotNull(exportTables);
            Assert.IsTrue(exportTables.Count > 0, "Should have at least one export table");

            var firstTable = exportTables.First();
            Assert.IsFalse(string.IsNullOrEmpty(firstTable.Name));
            Assert.IsFalse(string.IsNullOrEmpty(firstTable.DisplayName));

            await _loggingService.LogMainAsync($"✅ Configuration Service: Loaded {exportTables.Count} export tables");
        }

        [TestMethod]
        public async Task TestDatabaseService_Connection()
        {
            // Test database connection
            await _loggingService.LogMainAsync("🔍 Testing database connection...");
            
            var connectionResult = await _databaseService.TestConnectionDetailedAsync();
            
            await _loggingService.LogMainAsync($"Database Connection Test Result:");
            await _loggingService.LogMainAsync($"  Success: {connectionResult.Success}");
            await _loggingService.LogMainAsync($"  Message: {connectionResult.Message}");
            await _loggingService.LogMainAsync($"  Duration: {connectionResult.TestDuration.TotalMilliseconds}ms");
            
            if (!connectionResult.Success)
            {
                await _loggingService.LogMainAsync($"  Error: {connectionResult.ErrorDetails}");
                await _loggingService.LogErrorAsync($"Database connection failed: {connectionResult.Message}");
            }

            Assert.IsTrue(connectionResult.Success, $"Database connection should succeed: {connectionResult.Message}");
        }

        [TestMethod]
        public async Task TestDatabaseService_VerifyStoredProcedures()
        {
            // Verify that configured stored procedures exist in database
            var procedures = await _configService.GetStoredProceduresAsync(OperationMode.Export);
            
            foreach (var proc in procedures)
            {
                await _loggingService.LogMainAsync($"🔍 Checking if stored procedure '{proc.Name}' exists...");
                
                // We'll implement a method to check if SP exists
                // For now, just log that we're checking
                await _loggingService.LogMainAsync($"  Procedure: {proc.DisplayName}");
                await _loggingService.LogMainAsync($"  Parameters: {string.Join(", ", proc.Parameters.Select(p => p.Name))}");
            }
        }

        [TestMethod] 
        public async Task TestExportService_FileGeneration()
        {
            // Test export file name generation
            var fileName = _exportService.GenerateFileName("TEST_TABLE", ExportFormat.Excel, OperationMode.Export);
            Assert.IsFalse(string.IsNullOrEmpty(fileName));
            Assert.IsTrue(fileName.EndsWith(".xlsx"));
            Assert.IsTrue(fileName.Contains("EXPORT"));

            await _loggingService.LogMainAsync($"✅ Export Service: Generated filename: {fileName}");
        }

        [TestCleanup]
        public void Cleanup()
        {
            _host?.Dispose();
        }
    }
}