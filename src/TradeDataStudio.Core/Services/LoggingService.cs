using NLog;
using System.Text.Json;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;

namespace TradeDataStudio.Core.Services
{
    public class LoggingService : ILoggingService
    {
        private readonly Logger _mainLogger;
        private readonly Logger _errorLogger;

        public LoggingService()
        {
            // Configure loggers with specific names
            _mainLogger = LogManager.GetLogger("MainLogger");
            _errorLogger = LogManager.GetLogger("ErrorLogger");

            // Configure NLog programmatically
            ConfigureNLog();
            
            // Log initialization separator
            _mainLogger.Info("═══════════════════════════════════════════════════════════════════════════════");
            _mainLogger.Info("◆ APPLICATION STARTUP");
            _mainLogger.Info("═══════════════════════════════════════════════════════════════════════════════");
        }

        private void ConfigureNLog()
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Read log directory from appsettings.json
            var logDirectory = GetLogDirectoryFromConfig();
            Directory.CreateDirectory(logDirectory);

            // Main log file target - using proper timestamp format
            var mainLogFile = new NLog.Targets.FileTarget("mainLogFile")
            {
                FileName = Path.Combine(logDirectory, "application-${shortdate}.log"),
                Layout = "${longdate} | ${level:uppercase=true} | ${logger} | ${message} ${exception:format=tostring}",
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                MaxArchiveFiles = 30,
                Encoding = System.Text.Encoding.UTF8,
                KeepFileOpen = false
            };

            // Error log file target
            var errorLogFile = new NLog.Targets.FileTarget("errorLogFile")
            {
                FileName = Path.Combine(logDirectory, "error-details-${shortdate}.log"),
                Layout = "${longdate} | ${level:uppercase=true} | ${logger} | ${message} ${exception:format=tostring}",
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                MaxArchiveFiles = 60,
                Encoding = System.Text.Encoding.UTF8,
                KeepFileOpen = false
            };

            // Add targets to configuration
            config.AddTarget(mainLogFile);
            config.AddTarget(errorLogFile);

            // Configure rules: Log all messages to main log
            config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, mainLogFile);
            
            // Route Error-level messages to error.log in addition to main.log
            config.AddRule(NLog.LogLevel.Error, NLog.LogLevel.Fatal, errorLogFile);

            LogManager.Configuration = config;
        }

        private string GetLogDirectoryFromConfig()
        {
            try
            {
                // Find config directory
                var configPath = FindConfigDirectory();
                if (configPath != null)
                {
                    var appSettingsPath = Path.Combine(configPath, "appsettings.json");
                    if (File.Exists(appSettingsPath))
                    {
                        var json = File.ReadAllText(appSettingsPath);
                        var doc = JsonSerializer.Deserialize<JsonElement>(json);
                        
                        if (doc.TryGetProperty("paths", out var paths) &&
                            paths.TryGetProperty("logs", out var logsPath))
                        {
                            var logDir = logsPath.GetString();
                            if (!string.IsNullOrEmpty(logDir))
                            {
                                // Convert relative path to absolute if needed
                                if (!Path.IsPathRooted(logDir))
                                {
                                    logDir = Path.GetFullPath(Path.Combine(
                                        AppDomain.CurrentDomain.BaseDirectory,
                                        "..", "..", "..", "..", "..",
                                        logDir.Replace("/", Path.DirectorySeparatorChar.ToString())));
                                }
                                else
                                {
                                    logDir = logDir.Replace("/", Path.DirectorySeparatorChar.ToString());
                                }
                                return logDir;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Configuration reading error - will fallback to default
            }
            
            // Fallback to default
            var fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            return fallbackPath;
        }

        private string? FindConfigDirectory()
        {
            var probeRoots = new[]
            {
                AppDomain.CurrentDomain.BaseDirectory,
                Directory.GetCurrentDirectory(),
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ""
            };

            foreach (var root in probeRoots)
            {
                var current = new DirectoryInfo(root);
                while (current != null)
                {
                    var candidate = Path.Combine(current.FullName, "config");
                    if (Directory.Exists(candidate))
                    {
                        return candidate;
                    }
                    current = current.Parent;
                }
            }
            
            return null;
        }

        private bool ShouldLogToSuccessFile(string message)
        {
            // Only log EXECUTION PROCESSES to success.log - no initialization or configuration messages
            // Whitelist approach: only specific operation keywords that indicate real execution
            var executionKeywords = new[] 
            { 
                "[Export] SP Execution",      // Actual stored procedure execution
                "[Import] SP Execution",      // Actual stored procedure execution
                "Excel export completed",     // File export completion
                "CSV export completed",       // File export completion
                "[Export] Export:",           // Export result summary
                "[Import] Export:",           // Import export result
                "Workflow completed",         // Workflow completion
                "[Export] Workflow completed", // Workflow completion message
                "[Import] Workflow completed"  // Workflow completion message
            };
            
            // Exclude messages that are just noise/initialization/configuration
            var excludePatterns = new[]
            {
                // Application startup messages
                "Starting MainWindowViewModel",
                "MainWindowViewModel initialized",
                "Export batch size",
                "Testing database connection",
                "Testing connection with",
                "Database connection test",
                
                // Stored procedure validation/loading messages
                "Loading stored procedures",
                "Loading output tables",
                "Loaded",
                "Storing procedure",
                "validated successfully",
                "Warning: Output table",
                "Added output table",
                "FilterOutputTables",
                
                // Query/data loading messages
                "Query completed",
                "Retrieved",
                "Starting Excel export",
                "Bulk data loaded",
                "Writing Excel file",
                "Querying data",
                
                // Old format SP execution messages
                "executed successfully",
                "Stored procedure"            // Exclude generic stored procedure messages
            };
            
            // If message matches exclude patterns, don't log to success file
            if (excludePatterns.Any(pattern => message.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
            
            // Check if message contains execution keywords
            return executionKeywords.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }
        
        private bool IsVerboseInitializationMessage(string message)
        {
            // Messages that provide low value in logs - can be safely skipped
            var verbosePatterns = new[]
            {
                "Loading stored procedures for mode",
                "Loaded",                          // "Loaded 3 stored procedures", "Loaded 4 output tables"
                "Loading output tables for mode",
                "Bulk data loaded, applying column formatting",
                "Writing Excel file to disk"
            };
            
            return verbosePatterns.Any(pattern => message.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }
        
        public Task LogMainAsync(string message, Interfaces.LogLevel level = Interfaces.LogLevel.Information)
        {
            // Skip verbose initialization messages
            if (IsVerboseInitializationMessage(message))
            {
                return Task.CompletedTask;
            }

            var nlogLevel = level switch
            {
                Interfaces.LogLevel.Information => NLog.LogLevel.Info,
                Interfaces.LogLevel.Warning => NLog.LogLevel.Warn,
                Interfaces.LogLevel.Error => NLog.LogLevel.Error,
                Interfaces.LogLevel.Success => NLog.LogLevel.Info,
                _ => NLog.LogLevel.Info
            };

            // Include correlation ID if available
            var correlationId = CorrelationIdGenerator.GetCurrentCorrelationId();
            var logMessage = string.IsNullOrEmpty(correlationId) ? message : $"[{correlationId}] {message}";
            
            _mainLogger.Log(nlogLevel, logMessage);
            return Task.CompletedTask;
        }

        public Task LogExecutionSeparatorAsync()
        {
            // Log a separator line between executions for better readability
            var separator = "═══════════════════════════════════════════════════════════════════════════════";
            _mainLogger.Info(separator);
            return Task.CompletedTask;
        }

        public Task LogErrorAsync(string message, Exception? exception = null, OperationMode? mode = null)
        {
            // Include correlation ID if available
            var correlationId = CorrelationIdGenerator.GetCurrentCorrelationId();
            var logMessage = mode.HasValue ? $"✗ [{mode}] {message}" : $"✗ {message}";
            
            if (!string.IsNullOrEmpty(correlationId))
            {
                logMessage = $"[{correlationId}] {logMessage}";
            }
            
            // Single write - NLog rules route to both main.log and error.log
            _mainLogger.Error(exception, logMessage);
            return Task.CompletedTask;
        }

        public Task LogExecutionAsync(string storedProcedure, Dictionary<string, object> parameters, ExecutionResult result, OperationMode mode)
        {
            var paramString = string.Join(", ", parameters.Select(kv => $"{kv.Key}={kv.Value}"));
            var successEmoji = result.Success ? "✓" : "✗";
            var message = $"{successEmoji} SP Execution: {storedProcedure} | Parameters: {paramString} | Success: {result.Success} | Records: {result.RecordsAffected} | Time: {result.ExecutionTime.TotalSeconds:F2}s";
            
            // Include correlation ID if available
            var correlationId = CorrelationIdGenerator.GetCurrentCorrelationId();
            var logMessage = $"[{mode}] {message}";
            if (!string.IsNullOrEmpty(correlationId))
            {
                logMessage = $"[{correlationId}] {logMessage}";
            }
            
            // Write to appropriate logs based on success status
            if (result.Success)
            {
                _mainLogger.Info(logMessage);
            }
            else
            {
                _mainLogger.Error(logMessage);
            }
            return Task.CompletedTask;
        }

        public Task LogExportAsync(ExportResult exportResult, OperationMode mode)
        {
            var successEmoji = exportResult.Success ? "✓" : "✗";
            var message = $"{successEmoji} Export: {exportResult.FileName} | Format: {exportResult.Format} | Success: {exportResult.Success} | Records: {exportResult.RecordsExported} | Size: {exportResult.FileSize} bytes";
            
            // Include correlation ID if available
            var correlationId = CorrelationIdGenerator.GetCurrentCorrelationId();
            var logMessage = $"[{mode}] {message}";
            if (!string.IsNullOrEmpty(correlationId))
            {
                logMessage = $"[{correlationId}] {logMessage}";
            }
            
            // Write to appropriate logs based on success status
            if (exportResult.Success)
            {
                _mainLogger.Info(logMessage);
            }
            else
            {
                _mainLogger.Error(logMessage);
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            LogManager.Shutdown();
        }
    }
}