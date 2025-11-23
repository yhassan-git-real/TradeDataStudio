using System;
using System.Collections.Generic;
using System.Linq;

namespace TradeDataStudio.Core.Services
{
    /// <summary>
    /// Enhanced error handling service that provides user-friendly error messages,
    /// error categorization, and standardized error reporting throughout the application.
    /// </summary>
    public static class ErrorHandler
    {
        /// <summary>
        /// Get a user-friendly error message based on exception type and context
        /// </summary>
        public static string GetUserFriendlyMessage(Exception exception, string context = "")
        {
            return exception switch
            {
                ArgumentNullException => "Required information is missing. Please check your input.",
                ArgumentException => "Invalid information provided. Please verify your settings.",
                UnauthorizedAccessException => "Access denied. Please check your permissions.",
                System.IO.FileNotFoundException => "Required file not found. Please check your configuration.",
                System.IO.DirectoryNotFoundException => "Required directory not found. Please check file paths.",
                System.IO.IOException io => $"File operation failed: {io.Message}",
                TimeoutException => "Operation timed out. Please try again or check your network connection.",
                System.Net.Sockets.SocketException => "Network error occurred. Please check your internet connection.",
                Microsoft.Data.SqlClient.SqlException sql => GetSqlErrorMessage(sql),
                System.Text.Json.JsonException => "Configuration file format error. Please check your JSON files.",
                NotImplementedException => "This feature is not yet implemented.",
                InvalidOperationException invalid => $"Operation failed: {invalid.Message}",
                OutOfMemoryException => "Not enough memory to complete this operation. Try with smaller data sets.",
                _ => string.IsNullOrEmpty(context) 
                    ? $"An unexpected error occurred: {exception.Message}"
                    : $"Error in {context}: {exception.Message}"
            };
        }

        /// <summary>
        /// Get SQL-specific user-friendly error messages
        /// </summary>
        private static string GetSqlErrorMessage(Microsoft.Data.SqlClient.SqlException sqlEx)
        {
            return sqlEx.Number switch
            {
                2 => "Cannot connect to the database server. Please check the server name and network connectivity.",
                4060 => "The specified database does not exist or you don't have access to it.",
                18456 => "Login failed. Please check your username and password.",
                18487 => "Your password has expired. Please contact your administrator.",
                18488 => "Your account is locked. Please contact your administrator.",
                -2 => "Connection timeout. The database server is taking too long to respond.",
                53 => "Could not find the database server. Please verify the server name.",
                233 => "Connection was refused by the server.",
                2146893022 => "SSL/TLS connection error. Check certificate settings.",
                8152 => "Data too long for database field. Please reduce the data size.",
                547 => "Foreign key constraint violation. Referenced data may be missing.",
                2627 => "Duplicate key error. This record already exists.",
                515 => "Required database field is missing a value.",
                _ => $"Database error (Code {sqlEx.Number}): {sqlEx.Message}"
            };
        }

        /// <summary>
        /// Categorize errors for logging and reporting
        /// </summary>
        public static ErrorCategory CategorizeError(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException or ArgumentException => ErrorCategory.UserInput,
                UnauthorizedAccessException => ErrorCategory.Permission,
                System.IO.FileNotFoundException or System.IO.DirectoryNotFoundException => ErrorCategory.Configuration,
                System.IO.IOException => ErrorCategory.FileSystem,
                Microsoft.Data.SqlClient.SqlException => ErrorCategory.Database,
                TimeoutException => ErrorCategory.Performance,
                System.Net.Sockets.SocketException => ErrorCategory.Network,
                System.Text.Json.JsonException => ErrorCategory.Configuration,
                NotImplementedException => ErrorCategory.Development,
                OutOfMemoryException => ErrorCategory.System,
                _ => ErrorCategory.Unknown
            };
        }

        /// <summary>
        /// Determine if an error is recoverable and can be retried
        /// </summary>
        public static bool IsRecoverable(Exception exception)
        {
            return exception switch
            {
                TimeoutException => true,
                System.Net.Sockets.SocketException => true,
                Microsoft.Data.SqlClient.SqlException sql => sql.Number switch
                {
                    -2 => true,    // Timeout
                    233 => true,   // Connection refused (might be temporary)
                    _ => false
                },
                System.IO.IOException => true,  // File might be temporarily locked
                _ => false
            };
        }

        /// <summary>
        /// Get suggested actions for common errors
        /// </summary>
        public static List<string> GetSuggestedActions(Exception exception)
        {
            var suggestions = new List<string>();

            switch (CategorizeError(exception))
            {
                case ErrorCategory.UserInput:
                    suggestions.AddRange(new[]
                    {
                        "Verify all required fields are filled in",
                        "Check that dates are in the correct format",
                        "Ensure numeric values are valid"
                    });
                    break;

                case ErrorCategory.Database:
                    suggestions.AddRange(new[]
                    {
                        "Check database connection settings",
                        "Verify database server is running",
                        "Test connection in Settings window",
                        "Contact your database administrator"
                    });
                    break;

                case ErrorCategory.Network:
                    suggestions.AddRange(new[]
                    {
                        "Check your internet connection",
                        "Verify firewall settings",
                        "Try again in a few moments"
                    });
                    break;

                case ErrorCategory.Configuration:
                    suggestions.AddRange(new[]
                    {
                        "Check configuration file paths",
                        "Verify JSON file format",
                        "Review application settings"
                    });
                    break;

                case ErrorCategory.FileSystem:
                    suggestions.AddRange(new[]
                    {
                        "Check file and folder permissions",
                        "Ensure sufficient disk space",
                        "Verify file paths exist"
                    });
                    break;

                case ErrorCategory.Performance:
                    suggestions.AddRange(new[]
                    {
                        "Try with a smaller data set",
                        "Increase timeout settings",
                        "Try again during off-peak hours"
                    });
                    break;

                case ErrorCategory.System:
                    suggestions.AddRange(new[]
                    {
                        "Close other applications to free memory",
                        "Restart the application",
                        "Contact system administrator"
                    });
                    break;
            }

            if (IsRecoverable(exception))
            {
                suggestions.Add("Try the operation again");
            }

            return suggestions;
        }

        /// <summary>
        /// Create a comprehensive error report for logging
        /// </summary>
        public static ErrorReport CreateErrorReport(Exception exception, string operationName, Dictionary<string, object>? context = null)
        {
            return new ErrorReport
            {
                Timestamp = DateTime.Now,
                OperationName = operationName,
                ExceptionType = exception.GetType().Name,
                ErrorCategory = CategorizeError(exception),
                UserFriendlyMessage = GetUserFriendlyMessage(exception, operationName),
                TechnicalMessage = exception.Message,
                StackTrace = exception.StackTrace,
                IsRecoverable = IsRecoverable(exception),
                SuggestedActions = GetSuggestedActions(exception),
                Context = context ?? new Dictionary<string, object>()
            };
        }
    }

    /// <summary>
    /// Error categories for classification and handling
    /// </summary>
    public enum ErrorCategory
    {
        Unknown,
        UserInput,
        Database,
        Network,
        FileSystem,
        Configuration,
        Permission,
        Performance,
        System,
        Development
    }

    /// <summary>
    /// Comprehensive error report structure
    /// </summary>
    public class ErrorReport
    {
        public DateTime Timestamp { get; set; }
        public string OperationName { get; set; } = string.Empty;
        public string ExceptionType { get; set; } = string.Empty;
        public ErrorCategory ErrorCategory { get; set; }
        public string UserFriendlyMessage { get; set; } = string.Empty;
        public string TechnicalMessage { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public bool IsRecoverable { get; set; }
        public List<string> SuggestedActions { get; set; } = new();
        public Dictionary<string, object> Context { get; set; } = new();
    }
}