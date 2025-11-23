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
            Console.WriteLine($"[LoggingService] Log directory: {logDirectory}");

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

            // Configure rules
            config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, mainLogFile, "MainLogger");
            config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Info, successLogFile, "SuccessLogger");
            config.AddRule(NLog.LogLevel.Error, NLog.LogLevel.Fatal, errorLogFile, "ErrorLogger");

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
                                
                                Console.WriteLine($"[LoggingService] Using configured log path: {logDir}");
                                return logDir;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LoggingService] Error reading config: {ex.Message}");
            }
            
            // Fallback to default
            var fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Console.WriteLine($"[LoggingService] Using fallback log path: {fallbackPath}");
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

        public async Task LogMainAsync(string message, Interfaces.LogLevel level = Interfaces.LogLevel.Information)
        {
            await Task.Run(() =>
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
            });
        }

        public async Task LogSuccessAsync(string message, OperationMode mode, Dictionary<string, object>? metadata = null)
        {
            await Task.Run(() =>
            {
                var logMessage = $"[{mode}] {message}";
                if (metadata != null && metadata.Count > 0)
                {
                    var metaString = string.Join(", ", metadata.Select(kv => $"{kv.Key}={kv.Value}"));
                    logMessage += $" | {metaString}";
                }
                
                _mainLogger.Info(logMessage);
                _successLogger.Info(logMessage);
            });
        }

        public async Task LogErrorAsync(string message, Exception? exception = null, OperationMode? mode = null)
        {
            await Task.Run(() =>
            {
                var logMessage = mode.HasValue ? $"[{mode}] {message}" : message;
                
                _mainLogger.Error(exception, logMessage);
                _errorLogger.Error(exception, logMessage);
            });
        }

        public async Task LogExecutionAsync(string storedProcedure, Dictionary<string, object> parameters, ExecutionResult result, OperationMode mode)
        {
            await Task.Run(() =>
            {
                var paramString = string.Join(", ", parameters.Select(kv => $"{kv.Key}={kv.Value}"));
                var message = $"SP Execution: {storedProcedure} | Parameters: {paramString} | Success: {result.Success} | Records: {result.RecordsAffected} | Time: {result.ExecutionTime.TotalMilliseconds}ms";
                
                if (result.Success)
                {
                    _mainLogger.Info($"[{mode}] {message}");
                    _successLogger.Info($"[{mode}] {message}");
                }
                else
                {
                    _mainLogger.Error($"[{mode}] {message}");
                    _errorLogger.Error($"[{mode}] {message}");
                }
            });
        }

        public async Task LogExportAsync(ExportResult exportResult, OperationMode mode)
        {
            await Task.Run(() =>
            {
                var message = $"Export: {exportResult.FileName} | Format: {exportResult.Format} | Success: {exportResult.Success} | Records: {exportResult.RecordsExported} | Size: {exportResult.FileSize} bytes";
                
                if (exportResult.Success)
                {
                    _mainLogger.Info($"[{mode}] {message}");
                    _successLogger.Info($"[{mode}] {message}");
                }
                else
                {
                    _mainLogger.Error($"[{mode}] {message}");
                    _errorLogger.Error($"[{mode}] {message}");
                }
            });
        }

        public void Dispose()
        {
            LogManager.Shutdown();
        }
    }
}