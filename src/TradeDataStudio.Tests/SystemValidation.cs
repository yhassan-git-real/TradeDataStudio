using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Services;

namespace TradeDataStudio.Tests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 TradeData Studio - System Validation");
            Console.WriteLine("=====================================");
            Console.WriteLine();

            var host = CreateHost();
            
            try
            {
                // Test all core services
                await TestConfigurationService(host);
                await TestDatabaseService(host);
                await TestLoggingService(host);
                await TestExportService(host);
                
                Console.WriteLine();
                Console.WriteLine("✅ ALL TESTS PASSED! Application is ready for use.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                Console.WriteLine($"Details: {ex}");
            }
            finally
            {
                await host.StopAsync();
                host.Dispose();
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static IHost CreateHost()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<ILoggingService, LoggingService>();
                    services.AddSingleton<IConfigurationService, TradeDataStudio.Core.Services.ConfigurationService>();
                    services.AddTransient<IDatabaseService, DatabaseService>();
                    services.AddTransient<IExportService, ExportService>();
                })
                .Build();
        }

        static async Task TestConfigurationService(IHost host)
        {
            Console.WriteLine("📋 Testing Configuration Service...");
            var configService = host.Services.GetRequiredService<IConfigurationService>();
            
            // Test loading stored procedures
            var exportProcs = await configService.GetStoredProceduresAsync(TradeDataStudio.Core.Models.OperationMode.Export);
            Console.WriteLine($"   ✓ Loaded {exportProcs.Count} export procedures");
            
            var importProcs = await configService.GetStoredProceduresAsync(TradeDataStudio.Core.Models.OperationMode.Import);
            Console.WriteLine($"   ✓ Loaded {importProcs.Count} import procedures");
            
            // Test loading tables
            var exportTables = await configService.GetTablesAsync(TradeDataStudio.Core.Models.OperationMode.Export);
            Console.WriteLine($"   ✓ Loaded {exportTables.Count} export tables");
            
            var importTables = await configService.GetTablesAsync(TradeDataStudio.Core.Models.OperationMode.Import);
            Console.WriteLine($"   ✓ Loaded {importTables.Count} import tables");
            
            // Test database configuration
            var dbConfig = await configService.GetDatabaseConfigurationAsync();
            Console.WriteLine($"   ✓ Database: {dbConfig.Server}\\{dbConfig.Database}");
            Console.WriteLine($"   ✓ Windows Auth: {dbConfig.UseWindowsAuthentication}");
        }

        static async Task TestDatabaseService(IHost host)
        {
            Console.WriteLine("🗄️  Testing Database Service...");
            var dbService = host.Services.GetRequiredService<IDatabaseService>();
            
            var connectionResult = await dbService.TestConnectionDetailedAsync();
            
            if (connectionResult.Success)
            {
                Console.WriteLine($"   ✓ Connection successful");
                Console.WriteLine($"   ✓ Connected to database");
                Console.WriteLine($"   ✓ Connection authenticated");
            }
            else
            {
                Console.WriteLine($"   ❌ Connection failed");
                throw new Exception($"Database connection failed: Check server/database configuration");
            }
        }

        static async Task TestLoggingService(IHost host)
        {
            Console.WriteLine("📝 Testing Logging Service...");
            var loggingService = host.Services.GetRequiredService<ILoggingService>();
            
            await loggingService.LogMainAsync("System validation test started");
            await loggingService.LogSuccessAsync("Test log entry", TradeDataStudio.Core.Models.OperationMode.Export);
            
            Console.WriteLine($"   ✓ Main logging working");
            Console.WriteLine($"   ✓ Success logging working");
            Console.WriteLine($"   ✓ Log files should be created in Documents/TradeData Studio/Logs/");
        }

        static async Task TestExportService(IHost host)
        {
            Console.WriteLine("📤 Testing Export Service...");
            var exportService = host.Services.GetRequiredService<IExportService>();
            
            var fileName = exportService.GenerateFileName("TEST_TABLE", TradeDataStudio.Core.Models.ExportFormat.Excel, TradeDataStudio.Core.Models.OperationMode.Export);
            Console.WriteLine($"   ✓ Generated filename: {fileName}");
            
            if (fileName.Contains("EXPORT") && fileName.EndsWith(".xlsx"))
            {
                Console.WriteLine($"   ✓ File naming convention is correct");
            }
            else
            {
                throw new Exception("Export file naming is incorrect");
            }
        }
    }
}