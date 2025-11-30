using NLog;
using System.Text.Json;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;

namespace TradeDataStudio.Core.Services
{
    public class LoggingService : ILoggingService
    {
        private readonly Logger _mainLogger;
        private readonly Logger _successLogger;
        private readonly Logger _errorLogger;

        public LoggingService()
        {
            // Configure loggers with specific names
            _mainLogger = LogManager.GetLogger("MainLogger");
            _successLogger = LogManager.GetLogger("SuccessLogger");
            _errorLogger = LogManager.GetLogger("ErrorLogger");

            // Configure NLog programmatically
            ConfigureNLog();
        }

        private void ConfigureNLog()
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Read log directory from appsettings.json
            var logDirectory = GetLogDirectoryFromConfig();
            Directory.CreateDirectory(logDirectory);

            // Main log file target
            var mainLogFile = new NLog.Targets.FileTarget("mainLogFile")
            {
                FileName = Path.Combine(logDirectory, "main-${shortdate}.log"),
                Layout = "${longdate} | ${level:uppercase=true} | ${logger} | ${message} ${exception:format=tostring}",
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                MaxArchiveFiles = 30,
                Encoding = System.Text.Encoding.UTF8,
                KeepFileOpen = false
            };

            // Success log file target
            var successLogFile = new NLog.Targets.FileTarget("successLogFile")
            {
                FileName = Path.Combine(logDirectory, "success-${shortdate}.log"),
                Layout = "${longdate} | ${message}",
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                MaxArchiveFiles = 30,
                Encoding = System.Text.Encoding.UTF8,
                KeepFileOpen = false
            };

            // Error log file target
            var errorLogFile = new NLog.Targets.FileTarget("errorLogFile")
            {
                FileName = Path.Combine(logDirectory, "error-${shortdate}.log"),
                Layout = "${longdate} | ${level:uppercase=true} | ${logger} | ${message} ${exception:format=tostring}",
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                MaxArchiveFiles = 60,
                Encoding = System.Text.Encoding.UTF8,
                KeepFileOpen = false
            };

            // Add targets to configuration
            config.AddTarget(mainLogFile);
            config.AddTarget(successLogFile);
            config.AddTarget(errorLogFile);

            // Configure rules: Log all messages to main log
            config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, mainLogFile);
            
            // Route Info-level (success) messages to success.log in addition to main.log
            config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Info, successLogFile);
            
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

        public Task LogMainAsync(string message, Interfaces.LogLevel level = Interfaces.LogLevel.Information)
        {
            var nlogLevel = level switch
            {
                Interfaces.LogLevel.Information => NLog.LogLevel.Info,
                Interfaces.LogLevel.Warning => NLog.LogLevel.Warn,
                Interfaces.LogLevel.Error => NLog.LogLevel.Error,
                Interfaces.LogLevel.Success => NLog.LogLevel.Info,
                _ => NLog.LogLevel.Info
            };

            _mainLogger.Log(nlogLevel, message);
            return Task.CompletedTask;
        }

        public Task LogSuccessAsync(string message, OperationMode mode, Dictionary<string, object>? metadata = null)
        {
            var logMessage = $"[{mode}] {message}";
            if (metadata != null && metadata.Count > 0)
            {
                var metaString = string.Join(", ", metadata.Select(kv => $"{kv.Key}={kv.Value}"));
                logMessage += $" | {metaString}";
            }
            
            // Single write - NLog rules route to both main.log and success.log
            _mainLogger.Info(logMessage);
            return Task.CompletedTask;
        }

        public Task LogErrorAsync(string message, Exception? exception = null, OperationMode? mode = null)
        {
            var logMessage = mode.HasValue ? $"[{mode}] {message}" : message;
            
            // Single write - NLog rules route to both main.log and error.log
            _mainLogger.Error(exception, logMessage);
            return Task.CompletedTask;
        }

        public Task LogExecutionAsync(string storedProcedure, Dictionary<string, object> parameters, ExecutionResult result, OperationMode mode)
        {
            var paramString = string.Join(", ", parameters.Select(kv => $"{kv.Key}={kv.Value}"));
            var message = $"SP Execution: {storedProcedure} | Parameters: {paramString} | Success: {result.Success} | Records: {result.RecordsAffected} | Time: {result.ExecutionTime.TotalMilliseconds}ms";
            
            // Single write - NLog rules route to appropriate files
            if (result.Success)
            {
                _mainLogger.Info($"[{mode}] {message}");
            }
            else
            {
                _mainLogger.Error($"[{mode}] {message}");
            }
            return Task.CompletedTask;
        }

        public Task LogExportAsync(ExportResult exportResult, OperationMode mode)
        {
            var message = $"Export: {exportResult.FileName} | Format: {exportResult.Format} | Success: {exportResult.Success} | Records: {exportResult.RecordsExported} | Size: {exportResult.FileSize} bytes";
            
            // Single write - NLog rules route to appropriate files
            if (exportResult.Success)
            {
                _mainLogger.Info($"[{mode}] {message}");
            }
            else
            {
                _mainLogger.Error($"[{mode}] {message}");
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            LogManager.Shutdown();
        }
    }
}