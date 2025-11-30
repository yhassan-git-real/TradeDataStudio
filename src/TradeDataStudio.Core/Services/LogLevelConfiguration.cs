using System;
using System.Collections.Generic;
using NLog;

namespace TradeDataStudio.Core.Services
{
    /// <summary>
    /// Manages runtime configuration of NLog levels.
    /// Allows changing log verbosity without restarting the application.
    /// Supports both global and component-specific log level settings.
    /// </summary>
    public class LogLevelConfiguration
    {
        private static LogLevelConfiguration _instance;
        private static readonly object _lockObject = new object();
        private Dictionary<string, LogLevel> _componentLevels;
        private LogLevel _globalLevel;

        /// <summary>
        /// Gets or creates the singleton instance of LogLevelConfiguration.
        /// </summary>
        public static LogLevelConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new LogLevelConfiguration();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Constructor - initializes with default log level (Info).
        /// </summary>
        private LogLevelConfiguration()
        {
            _componentLevels = new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase);
            _globalLevel = LogLevel.Info;
            InitializeDefaultLevels();
        }

        /// <summary>
        /// Sets the global log level that applies to all components.
        /// Updates NLog configuration immediately.
        /// </summary>
        public void SetGlobalLogLevel(LogLevel level)
        {
            lock (_lockObject)
            {
                _globalLevel = level ?? LogLevel.Info;
                ApplyLoggingConfiguration();
            }
        }

        /// <summary>
        /// Gets the current global log level.
        /// </summary>
        public LogLevel GetGlobalLogLevel()
        {
            return _globalLevel;
        }

        /// <summary>
        /// Sets the log level for a specific component.
        /// Overrides the global log level for that component only.
        /// </summary>
        public void SetComponentLogLevel(string componentName, LogLevel level)
        {
            if (string.IsNullOrEmpty(componentName))
                throw new ArgumentNullException(nameof(componentName));

            lock (_lockObject)
            {
                if (level == null)
                {
                    _componentLevels.Remove(componentName);
                }
                else
                {
                    _componentLevels[componentName] = level;
                }
                ApplyLoggingConfiguration();
            }
        }

        /// <summary>
        /// Gets the log level for a specific component.
        /// Returns component-specific level if set, otherwise returns global level.
        /// </summary>
        public LogLevel GetComponentLogLevel(string componentName)
        {
            if (string.IsNullOrEmpty(componentName))
                return _globalLevel;

            lock (_lockObject)
            {
                if (_componentLevels.TryGetValue(componentName, out var level))
                {
                    return level;
                }
                return _globalLevel;
            }
        }

        /// <summary>
        /// Enables debug mode (sets global level to Debug).
        /// Useful for troubleshooting without specifying individual components.
        /// </summary>
        public void EnableDebugMode()
        {
            SetGlobalLogLevel(LogLevel.Debug);
        }

        /// <summary>
        /// Disables debug mode (sets global level back to Info).
        /// </summary>
        public void DisableDebugMode()
        {
            SetGlobalLogLevel(LogLevel.Info);
        }

        /// <summary>
        /// Checks if debug mode is currently enabled.
        /// </summary>
        public bool IsDebugModeEnabled()
        {
            return _globalLevel <= LogLevel.Debug;
        }

        /// <summary>
        /// Enables verbose logging (sets global level to Trace).
        /// Maximum verbosity for deep troubleshooting.
        /// </summary>
        public void EnableVerboseMode()
        {
            SetGlobalLogLevel(LogLevel.Trace);
        }

        /// <summary>
        /// Disables verbose mode (sets global level back to Info).
        /// </summary>
        public void DisableVerboseMode()
        {
            SetGlobalLogLevel(LogLevel.Info);
        }

        /// <summary>
        /// Resets all component-specific settings to use global level.
        /// </summary>
        public void ResetComponentLevels()
        {
            lock (_lockObject)
            {
                _componentLevels.Clear();
                ApplyLoggingConfiguration();
            }
        }

        /// <summary>
        /// Gets all currently configured component-specific log levels.
        /// </summary>
        public Dictionary<string, LogLevel> GetComponentLevels()
        {
            lock (_lockObject)
            {
                return new Dictionary<string, LogLevel>(_componentLevels, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets the effective log level that will be used (component-specific or global).
        /// </summary>
        public LogLevel GetEffectiveLogLevel(string componentName)
        {
            return GetComponentLogLevel(componentName);
        }

        /// <summary>
        /// Initializes default log levels for known components.
        /// </summary>
        private void InitializeDefaultLevels()
        {
            // Set default levels for critical components
            _componentLevels["TradeDataStudio.Core"] = LogLevel.Info;
            _componentLevels["TradeDataStudio.Desktop"] = LogLevel.Info;
            _componentLevels["DatabaseService"] = LogLevel.Info;
            _componentLevels["ExportService"] = LogLevel.Info;
            _componentLevels["LoggingService"] = LogLevel.Info;
            _componentLevels["WorkflowOrchestrator"] = LogLevel.Info;
        }

        /// <summary>
        /// Applies the current logging configuration to NLog.
        /// </summary>
        private void ApplyLoggingConfiguration()
        {
            try
            {
                var config = LogManager.Configuration;
                if (config == null)
                    return;

                // Update all rules with the appropriate log level
                foreach (var rule in config.LoggingRules)
                {
                    // Set the minimum log level for this rule
                    rule.SetLoggingLevels(_globalLevel, LogLevel.Fatal);
                }

                // Reconfigure NLog with updated configuration
                LogManager.ReconfigExistingLoggers();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying log configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a configuration summary for logging purposes.
        /// </summary>
        public string GetConfigurationSummary()
        {
            lock (_lockObject)
            {
                var summary = $"Global Log Level: {_globalLevel}";
                
                if (_componentLevels.Count > 0)
                {
                    summary += " | Component Overrides: ";
                    var overrides = new List<string>();
                    
                    foreach (var kvp in _componentLevels)
                    {
                        if (kvp.Value != _globalLevel)
                        {
                            overrides.Add($"{kvp.Key}={kvp.Value}");
                        }
                    }
                    
                    summary += (overrides.Count > 0 ? string.Join(", ", overrides) : "None");
                }
                
                return summary;
            }
        }
    }

    /// <summary>
    /// Helper extension methods for log level configuration.
    /// </summary>
    public static class LogLevelExtensions
    {
        /// <summary>
        /// Converts string to NLog LogLevel.
        /// Supports: Trace, Debug, Info, Warn, Error, Fatal
        /// </summary>
        public static LogLevel ParseLogLevel(string levelName)
        {
            return levelName?.ToLower() switch
            {
                "trace" => LogLevel.Trace,
                "debug" => LogLevel.Debug,
                "info" or "information" => LogLevel.Info,
                "warn" or "warning" => LogLevel.Warn,
                "error" => LogLevel.Error,
                "fatal" or "critical" => LogLevel.Fatal,
                _ => LogLevel.Info
            };
        }

        /// <summary>
        /// Checks if a log level is enabled for logging.
        /// </summary>
        public static bool IsEnabled(this LogLevel level, LogLevel minLevel)
        {
            return level >= minLevel;
        }
    }
}
