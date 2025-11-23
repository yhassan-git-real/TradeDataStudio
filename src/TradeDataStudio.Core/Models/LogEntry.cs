using TradeDataStudio.Core.Interfaces;

namespace TradeDataStudio.Core.Models
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public LogLevel Level { get; set; } = LogLevel.Information;
        public string Message { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public bool IsSuccess { get; set; } = false;

        public string FormattedMessage => $"{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level.ToString().ToUpperInvariant()}] {Source}: {Message}";
        
        public override string ToString()
        {
            return FormattedMessage;
        }
    }
}