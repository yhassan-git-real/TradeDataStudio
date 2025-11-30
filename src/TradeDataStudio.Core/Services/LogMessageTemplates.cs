using System;
using System.Collections.Generic;
using System.Linq;

namespace TradeDataStudio.Core.Services
{
    /// <summary>
    /// Centralized log message templates for consistent formatting across the application.
    /// Provides helper methods for formatting numbers, durations, and complex messages.
    /// </summary>
    public static class LogMessageTemplates
    {
        /// <summary>
        /// Formats a number with thousand separators for consistent readability.
        /// Example: 1234567 → "1,234,567"
        /// </summary>
        public static string FormatNumber(long value) => $"{value:N0}";

        /// <summary>
        /// Formats a file size from bytes to megabytes with 2 decimal places.
        /// Example: 67094516 bytes → "63.99 MB"
        /// </summary>
        public static string FormatFileSize(long bytes) => $"{bytes / 1024.0 / 1024.0:F2} MB";

        /// <summary>
        /// Formats a duration intelligently: returns seconds with 2 decimals for durations >= 1 second,
        /// milliseconds for shorter durations.
        /// Example: 54.94 seconds → "54.94s", 250 milliseconds → "250ms"
        /// </summary>
        public static string FormatDuration(TimeSpan duration)
        {
            return duration.TotalSeconds >= 1 
                ? $"{duration.TotalSeconds:F2}s" 
                : $"{duration.TotalMilliseconds:F0}ms";
        }

        /// <summary>
        /// Formats a stored procedure execution completion message.
        /// Example: "Stored procedure 'sp_name' completed in 45.23s (1,234,567 records affected)"
        /// </summary>
        public static string StoredProcedureExecution(string procedureName, long recordsAffected, TimeSpan duration)
        {
            return $"Stored procedure '{procedureName}' completed in {FormatDuration(duration)} " +
                   $"({FormatNumber(recordsAffected)} records affected)";
        }

        /// <summary>
        /// Formats a data query completion message.
        /// Example: "Loaded TableName: 914,238 records in 11.25s"
        /// </summary>
        public static string DataQuery(string tableName, long recordCount, TimeSpan duration)
        {
            return $"Loaded {tableName}: {FormatNumber(recordCount)} records in {FormatDuration(duration)}";
        }

        /// <summary>
        /// Formats a data export completion message.
        /// Example: "Exported TableName → 914,238 records to filename.xlsx (66.76 MB, 54.94s)"
        /// </summary>
        public static string DataExport(string tableName, string fileName, long recordCount, long fileSize, TimeSpan duration)
        {
            return $"Exported {tableName} → {FormatNumber(recordCount)} records to {fileName} " +
                   $"({FormatFileSize(fileSize)}, {FormatDuration(duration)})";
        }

        /// <summary>
        /// Formats a workflow start message with procedure and table list.
        /// Example: "Workflow started: sp_name with 2 table(s) [Table1, Table2]"
        /// </summary>
        public static string WorkflowStart(string procedureName, List<string> tables)
        {
            var tableList = string.Join(", ", tables);
            return $"Workflow started: {procedureName} with {tables.Count} table(s) [{tableList}]";
        }

        /// <summary>
        /// Formats a workflow completion summary message.
        /// Example: "Workflow completed: 2/2 tables exported (1,783,544 total records) to path"
        /// </summary>
        public static string WorkflowCompletion(int successCount, int totalCount, long totalRecords, string outputPath)
        {
            return $"Workflow completed: {successCount}/{totalCount} tables exported " +
                   $"({FormatNumber(totalRecords)} total records) to {outputPath}";
        }

        /// <summary>
        /// Formats an Excel export start message.
        /// Example: "Starting Excel export for TableName with 914,238 rows..."
        /// </summary>
        public static string ExcelExportStart(string tableName, long totalRows)
        {
            return $"Starting Excel export for {tableName} with {FormatNumber(totalRows)} rows...";
        }

        /// <summary>
        /// Formats a CSV export start message.
        /// Example: "Starting CSV export for TableName with 914,238 rows..."
        /// </summary>
        public static string CsvExportStart(string tableName, long totalRows)
        {
            return $"Starting CSV export for {tableName} with {FormatNumber(totalRows)} rows...";
        }

        /// <summary>
        /// Formats a Text export start message.
        /// Example: "Starting text export for TableName with 914,238 rows..."
        /// </summary>
        public static string TextExportStart(string tableName, long totalRows)
        {
            return $"Starting text export for {tableName} with {FormatNumber(totalRows)} rows...";
        }

        /// <summary>
        /// Formats a batch operation start message.
        /// Example: "[1/2] Starting export for TableName"
        /// </summary>
        public static string BatchOperationStart(int currentIndex, int totalCount, string tableName)
        {
            return $"[{currentIndex}/{totalCount}] Starting export for {tableName}";
        }

        /// <summary>
        /// Formats a batch operation completion message.
        /// Example: "[1/2] Completed export for TableName"
        /// </summary>
        public static string BatchOperationComplete(int currentIndex, int totalCount, string tableName)
        {
            return $"[{currentIndex}/{totalCount}] Completed export for {tableName}";
        }
    }
}
