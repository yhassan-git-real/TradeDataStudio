using System;
using System.Threading.Tasks;
using TradeDataStudio.Core.Interfaces;

namespace TradeDataStudio.Core.Services
{
    /// <summary>
    /// Extension methods for ILoggingService to simplify formatted logging.
    /// Provides convenient methods for logging with visual separators and hierarchical structure.
    /// </summary>
    public static class FormattedLoggingExtensions
    {
        /// <summary>
        /// Logs a phase header with visual separators.
        /// </summary>
        public static async Task LogPhaseAsync(this ILoggingService service, string phaseName)
        {
            var lines = LogFormattingHelpers.FormatPhaseWithSeparators(phaseName);
            foreach (var line in lines)
            {
                await service.LogMainAsync(line);
            }
        }

        /// <summary>
        /// Logs a subphase header with minor separator.
        /// </summary>
        public static async Task LogSubphaseAsync(this ILoggingService service, string subphaseName)
        {
            var lines = LogFormattingHelpers.FormatSubphaseWithSeparator(subphaseName);
            foreach (var line in lines)
            {
                await service.LogMainAsync(line);
            }
        }

        /// <summary>
        /// Logs a completion header.
        /// </summary>
        public static async Task LogCompletionAsync(this ILoggingService service, string operationName)
        {
            await service.LogMainAsync(LogFormattingHelpers.CompletionHeader(operationName));
        }

        /// <summary>
        /// Logs a hierarchical item with optional final marker.
        /// </summary>
        public static async Task LogHierarchyItemAsync(this ILoggingService service, 
            string content, bool isLast = false)
        {
            var item = LogFormattingHelpers.HierarchyItem(content, isLast);
            await service.LogMainAsync(item);
        }

        /// <summary>
        /// Logs an operation with metrics in formatted structure.
        /// </summary>
        public static async Task LogOperationWithMetricsAsync(this ILoggingService service,
            string operation, string duration, string metrics, bool isLast = false)
        {
            var formatted = LogFormattingHelpers.FormatOperationWithMetrics(operation, duration, metrics, isLast);
            await service.LogMainAsync(formatted);
        }

        /// <summary>
        /// Logs a batch operation header.
        /// </summary>
        public static async Task LogBatchOperationAsync(this ILoggingService service,
            int index, int total, string operationName)
        {
            var header = LogFormattingHelpers.BatchOperationHeader(index, total, operationName);
            await service.LogMainAsync(header);
        }

        /// <summary>
        /// Logs a summary line.
        /// </summary>
        public static async Task LogSummaryAsync(this ILoggingService service,
            string label, string value)
        {
            var summary = LogFormattingHelpers.SummaryLine(label, value);
            await service.LogMainAsync(summary);
        }

        /// <summary>
        /// Logs an empty line (separator).
        /// </summary>
        public static async Task LogEmptyLineAsync(this ILoggingService service)
        {
            await service.LogMainAsync("");
        }

        /// <summary>
        /// Logs a success marker.
        /// </summary>
        public static async Task LogSuccessMarkerAsync(this ILoggingService service, string message = "Complete")
        {
            var marker = LogFormattingHelpers.SuccessMarker(message);
            await service.LogMainAsync(marker);
        }

        /// <summary>
        /// Logs a failure marker.
        /// </summary>
        public static async Task LogFailureMarkerAsync(this ILoggingService service, string message = "Failed")
        {
            var marker = LogFormattingHelpers.FailureMarker(message);
            await service.LogMainAsync(marker);
        }

        /// <summary>
        /// Logs an info marker.
        /// </summary>
        public static async Task LogInfoMarkerAsync(this ILoggingService service, string message)
        {
            var marker = LogFormattingHelpers.InfoMarker(message);
            await service.LogMainAsync(marker);
        }
    }
}
