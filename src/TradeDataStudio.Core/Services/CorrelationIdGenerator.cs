using System;
using System.Collections.Concurrent;
using System.Threading;

namespace TradeDataStudio.Core.Services
{
    /// <summary>
    /// Generates and tracks correlation IDs for operation tracing.
    /// Each operation (export, import, workflow) gets a unique ID that persists throughout its lifecycle.
    /// This enables tracking of related log messages across different components.
    /// </summary>
    public class CorrelationIdGenerator
    {
        private static readonly AsyncLocal<string> _correlationId = new AsyncLocal<string>();
        private static readonly ConcurrentDictionary<string, CorrelationIdContext> _activeContexts 
            = new ConcurrentDictionary<string, CorrelationIdContext>();

        /// <summary>
        /// Gets the current correlation ID for the async context.
        /// If no ID exists, returns null.
        /// </summary>
        public static string GetCurrentCorrelationId()
        {
            return _correlationId.Value;
        }

        /// <summary>
        /// Sets the correlation ID for the current async context.
        /// </summary>
        public static void SetCorrelationId(string correlationId)
        {
            _correlationId.Value = correlationId;
        }

        /// <summary>
        /// Generates a new unique correlation ID and sets it as current.
        /// Format: {OperationType}-{Timestamp}-{RandomGuid}
        /// Example: EXPORT-20251201-093015-a1b2c3d4
        /// </summary>
        public static string GenerateNewCorrelationId(string operationType = "OPERATION")
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var uniquePart = Guid.NewGuid().ToString("N").Substring(0, 8);
            var correlationId = $"{operationType}-{timestamp}-{uniquePart}";
            
            SetCorrelationId(correlationId);
            
            // Track in active contexts
            var context = new CorrelationIdContext
            {
                CorrelationId = correlationId,
                OperationType = operationType,
                StartTime = DateTime.UtcNow,
                Status = CorrelationIdStatus.Active
            };
            
            _activeContexts.TryAdd(correlationId, context);
            
            return correlationId;
        }

        /// <summary>
        /// Clears the current correlation ID from the async context.
        /// </summary>
        public static void ClearCorrelationId()
        {
            _correlationId.Value = null;
        }

        /// <summary>
        /// Marks a correlation ID as completed and tracks completion time.
        /// </summary>
        public static void CompleteCorrelationId(string correlationId, bool success = true)
        {
            if (_activeContexts.TryGetValue(correlationId, out var context))
            {
                context.EndTime = DateTime.UtcNow;
                context.Status = success ? CorrelationIdStatus.Completed : CorrelationIdStatus.Failed;
                context.Duration = context.EndTime.Value - context.StartTime;
            }
        }

        /// <summary>
        /// Gets the context (metadata) for a correlation ID.
        /// Returns null if the correlation ID is not tracked.
        /// </summary>
        public static CorrelationIdContext GetContext(string correlationId)
        {
            _activeContexts.TryGetValue(correlationId, out var context);
            return context;
        }

        /// <summary>
        /// Gets all active (non-completed) correlation IDs.
        /// Useful for monitoring ongoing operations.
        /// </summary>
        public static string[] GetActiveCorrelationIds()
        {
            var activeIds = new System.Collections.Generic.List<string>();
            
            foreach (var kvp in _activeContexts)
            {
                if (kvp.Value.Status == CorrelationIdStatus.Active)
                {
                    activeIds.Add(kvp.Key);
                }
            }
            
            return activeIds.ToArray();
        }

        /// <summary>
        /// Clears completed correlation IDs older than the specified duration.
        /// Call periodically to prevent memory leaks.
        /// </summary>
        public static void CleanupOldContexts(TimeSpan maxAge)
        {
            var cutoffTime = DateTime.UtcNow - maxAge;
            var keysToRemove = new System.Collections.Generic.List<string>();
            
            foreach (var kvp in _activeContexts)
            {
                if (kvp.Value.Status != CorrelationIdStatus.Active && kvp.Value.StartTime < cutoffTime)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _activeContexts.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// Represents the status of a correlation ID.
    /// </summary>
    public enum CorrelationIdStatus
    {
        Active,
        Completed,
        Failed
    }

    /// <summary>
    /// Stores metadata about a correlation ID context.
    /// </summary>
    public class CorrelationIdContext
    {
        public string CorrelationId { get; set; }
        public string OperationType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public CorrelationIdStatus Status { get; set; }
    }
}
