using System;

namespace TradeDataStudio.Core.Services
{
    /// <summary>
    /// Provides visual separators and formatting helpers for structured log output.
    /// Implements the hierarchical, user-friendly format shown in LOGGING_BEFORE_AFTER_EXAMPLES.md
    /// </summary>
    public static class LogFormattingHelpers
    {
        // Major separators for phase boundaries
        public static readonly string MajorSeparator = 
            "═══════════════════════════════════════════════════════════════════════════════";
        
        // Minor separators for subphase boundaries
        public static readonly string MinorSeparator = 
            "────────────────────────────────────────────────────────────────────────────────";

        /// <summary>
        /// Creates a phase header with visual separator.
        /// Example: "◆ WORKFLOW EXECUTION"
        /// </summary>
        public static string PhaseHeader(string phaseName)
        {
            return $"◆ {phaseName}";
        }

        /// <summary>
        /// Creates a subphase header.
        /// Example: "▶ STORED PROCEDURE EXECUTION"
        /// </summary>
        public static string SubphaseHeader(string subphaseName)
        {
            return $"▶ {subphaseName}";
        }

        /// <summary>
        /// Creates a completion marker for workflow end.
        /// Example: "✓ WORKFLOW COMPLETE"
        /// </summary>
        public static string CompletionHeader(string operationName)
        {
            return $"✓ {operationName}";
        }

        /// <summary>
        /// Creates a hierarchical bullet point with branch connector.
        /// Example: "├─ Query: 11.25s → 914,238 records loaded"
        /// </summary>
        public static string HierarchyItem(string content, bool isLast = false)
        {
            var connector = isLast ? "└─" : "├─";
            return $"  {connector} {content}";
        }

        /// <summary>
        /// Creates a completion checkmark for operation success.
        /// Example: "✓ Complete"
        /// </summary>
        public static string SuccessMarker(string message = "Complete")
        {
            return $"✓ {message}";
        }

        /// <summary>
        /// Creates an error/failure marker.
        /// Example: "✗ Failed"
        /// </summary>
        public static string FailureMarker(string message = "Failed")
        {
            return $"✗ {message}";
        }

        /// <summary>
        /// Creates an info marker for status updates.
        /// Example: "➜ Processing"
        /// </summary>
        public static string InfoMarker(string message)
        {
            return $"➜ {message}";
        }

        /// <summary>
        /// Formats a workflow phase with full separator structure.
        /// Returns a string array ready to log, one element per line.
        /// </summary>
        public static string[] FormatPhaseWithSeparators(string phaseName)
        {
            return new[]
            {
                MajorSeparator,
                PhaseHeader(phaseName),
                MajorSeparator
            };
        }

        /// <summary>
        /// Formats a subphase with minor separator.
        /// Returns a string array ready to log.
        /// </summary>
        public static string[] FormatSubphaseWithSeparator(string subphaseName)
        {
            return new[]
            {
                SubphaseHeader(subphaseName),
                MinorSeparator
            };
        }

        /// <summary>
        /// Creates a formatted operation line with timing and metrics.
        /// Example: "├─ Query: 11.25s → 914,238 records loaded"
        /// </summary>
        public static string FormatOperationWithMetrics(string operation, string duration, string metrics, bool isLast = false)
        {
            var content = $"{operation}: {duration} → {metrics}";
            return HierarchyItem(content, isLast);
        }

        /// <summary>
        /// Creates a batch operation header with index.
        /// Example: "[1/2] EXP_OTHERS_1"
        /// </summary>
        public static string BatchOperationHeader(int index, int total, string operationName)
        {
            return $"[{index}/{total}] {operationName}";
        }

        /// <summary>
        /// Creates a summary line for batch results.
        /// Example: "Results: 2/2 tables exported"
        /// </summary>
        public static string SummaryLine(string label, string value)
        {
            return $"{label}: {value}";
        }
    }
}
