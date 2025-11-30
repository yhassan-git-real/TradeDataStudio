using System;
using System.Collections.Generic;

namespace TradeDataStudio.Core.Services
{
    /// <summary>
    /// Formats error messages in a user-friendly way while preserving technical details.
    /// Converts technical exception messages into actionable user guidance.
    /// </summary>
    public static class ErrorMessageFormatter
    {
        /// <summary>
        /// Formats an exception into a user-friendly error message with actionable guidance.
        /// Separates user-facing message from technical details.
        /// </summary>
        public static FormattedError FormatError(Exception exception, string context = "")
        {
            if (exception == null)
            {
                return new FormattedError
                {
                    UserMessage = "An unexpected error occurred.",
                    TechnicalDetails = "No exception information available.",
                    Severity = ErrorSeverity.Error,
                    SuggestedAction = "Please try again or contact support if the issue persists."
                };
            }

            var exceptionType = exception.GetType().Name;
            var message = exception.Message ?? "Unknown error";
            var innerException = exception.InnerException?.Message ?? "";

            var formatted = new FormattedError
            {
                Exception = exception,
                ExceptionType = exceptionType,
                Context = context,
                TechnicalDetails = $"{exceptionType}: {message}" + 
                                  (string.IsNullOrEmpty(innerException) ? "" : $" → {innerException}")
            };

            // Map specific exceptions to user-friendly messages
            formatted = MapExceptionToUserMessage(formatted);

            return formatted;
        }

        /// <summary>
        /// Maps common exceptions to user-friendly messages and suggested actions.
        /// </summary>
        private static FormattedError MapExceptionToUserMessage(FormattedError error)
        {
            var exceptionType = error.ExceptionType;
            var message = error.TechnicalDetails;

            switch (exceptionType)
            {
                case nameof(FileNotFoundException):
                    error.UserMessage = "The requested file could not be found.";
                    error.Severity = ErrorSeverity.Warning;
                    error.SuggestedAction = "Check that the file exists and the path is correct. Verify file permissions.";
                    break;

                case nameof(UnauthorizedAccessException):
                    error.UserMessage = "Access denied. You don't have permission to access this resource.";
                    error.Severity = ErrorSeverity.Error;
                    error.SuggestedAction = "Check file/folder permissions or contact your administrator for access.";
                    break;

                case nameof(InvalidOperationException):
                    error.UserMessage = ParseInvalidOperationMessage(message);
                    error.Severity = ErrorSeverity.Error;
                    error.SuggestedAction = "Review the operation details and try again with valid parameters.";
                    break;

                case nameof(TimeoutException):
                    error.UserMessage = "The operation timed out. The server didn't respond in time.";
                    error.Severity = ErrorSeverity.Error;
                    error.SuggestedAction = "Try reducing the data size or increasing timeout settings. Check network connectivity.";
                    break;

                case nameof(ArgumentException):
                case nameof(ArgumentNullException):
                    error.UserMessage = "Invalid input provided. One or more parameters are incorrect.";
                    error.Severity = ErrorSeverity.Warning;
                    error.SuggestedAction = "Review the input values and ensure all required fields are filled correctly.";
                    break;

                case "SqlException":
                case "SqlClient.SqlException":
                    error.UserMessage = ParseDatabaseMessage(message);
                    error.Severity = ErrorSeverity.Error;
                    error.SuggestedAction = "Check database connectivity and credentials. Verify stored procedures exist.";
                    break;

                case nameof(OutOfMemoryException):
                    error.UserMessage = "The operation ran out of memory. The dataset may be too large.";
                    error.Severity = ErrorSeverity.Error;
                    error.SuggestedAction = "Try exporting a smaller dataset, splitting the operation, or increasing available memory.";
                    break;

                case nameof(OperationCanceledException):
                    error.UserMessage = "The operation was cancelled by the user.";
                    error.Severity = ErrorSeverity.Information;
                    error.SuggestedAction = "No action required. The operation was cancelled as requested.";
                    break;

                default:
                    error.UserMessage = "An unexpected error occurred. Please contact support with the technical details below.";
                    error.Severity = ErrorSeverity.Error;
                    error.SuggestedAction = "Provide the technical details to your support team for investigation.";
                    break;
            }

            return error;
        }

        /// <summary>
        /// Parses InvalidOperationException messages to provide specific guidance.
        /// </summary>
        private static string ParseInvalidOperationMessage(string message)
        {
            if (message.Contains("Excel", StringComparison.OrdinalIgnoreCase))
            {
                return "Excel export failed. The dataset may be too large or contains invalid data.";
            }

            if (message.Contains("row", StringComparison.OrdinalIgnoreCase) && 
                message.Contains("1048", StringComparison.OrdinalIgnoreCase))
            {
                return "The dataset exceeds Excel's row limit (1,048,575 rows). Please use CSV format instead.";
            }

            if (message.Contains("table", StringComparison.OrdinalIgnoreCase))
            {
                return "The requested table could not be processed. Verify the table exists and contains valid data.";
            }

            if (message.Contains("failed", StringComparison.OrdinalIgnoreCase))
            {
                return "The operation failed to complete. Check the logs for more information.";
            }

            return "An operation could not be completed as requested. Please verify your input and try again.";
        }

        /// <summary>
        /// Parses database error messages to provide specific guidance.
        /// </summary>
        private static string ParseDatabaseMessage(string message)
        {
            if (message.Contains("connection", StringComparison.OrdinalIgnoreCase))
            {
                return "Failed to connect to the database. Check your connection string and network connectivity.";
            }

            if (message.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("login", StringComparison.OrdinalIgnoreCase))
            {
                return "Database authentication failed. Verify your credentials and permissions.";
            }

            if (message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            {
                return "Database query timed out. The operation took too long to complete.";
            }

            if (message.Contains("stored procedure", StringComparison.OrdinalIgnoreCase))
            {
                return "The stored procedure could not be executed. Verify it exists and has correct permissions.";
            }

            if (message.Contains("deadlock", StringComparison.OrdinalIgnoreCase))
            {
                return "A database deadlock occurred. Please try the operation again.";
            }

            return "A database error occurred. Check your database connection and try again.";
        }

        /// <summary>
        /// Formats an exception for log output with correlation ID integration.
        /// </summary>
        public static string FormatExceptionForLogging(Exception exception, string correlationId = null)
        {
            var message = $"Exception: {exception?.GetType().Name ?? "Unknown"}";
            
            if (!string.IsNullOrEmpty(correlationId))
            {
                message = $"[{correlationId}] {message}";
            }
            
            message += $" | Message: {exception?.Message ?? "No message"}";
            
            if (exception?.InnerException != null)
            {
                message += $" | Inner: {exception.InnerException.Message}";
            }
            
            return message;
        }
    }

    /// <summary>
    /// Represents a formatted error with user-friendly and technical messages.
    /// </summary>
    public class FormattedError
    {
        /// <summary>
        /// User-friendly error message explaining what went wrong.
        /// </summary>
        public string UserMessage { get; set; }

        /// <summary>
        /// Technical details for logging and debugging.
        /// </summary>
        public string TechnicalDetails { get; set; }

        /// <summary>
        /// Severity level of the error.
        /// </summary>
        public ErrorSeverity Severity { get; set; }

        /// <summary>
        /// Actionable suggestion for resolving the error.
        /// </summary>
        public string SuggestedAction { get; set; }

        /// <summary>
        /// The original exception that was formatted.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// The type name of the exception.
        /// </summary>
        public string ExceptionType { get; set; }

        /// <summary>
        /// Context where the error occurred.
        /// </summary>
        public string Context { get; set; }

        /// <summary>
        /// Formatted message suitable for display to users.
        /// </summary>
        public string DisplayMessage => 
            $"{UserMessage} {(string.IsNullOrEmpty(SuggestedAction) ? "" : $"({SuggestedAction})")}";
    }

    /// <summary>
    /// Represents the severity level of an error.
    /// </summary>
    public enum ErrorSeverity
    {
        Information,
        Warning,
        Error,
        Critical
    }
}
