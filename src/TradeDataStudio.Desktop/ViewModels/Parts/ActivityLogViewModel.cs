using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TradeDataStudio.Core.Models;
using TradeDataStudio.Desktop.Models;

namespace TradeDataStudio.Desktop.ViewModels.Parts;

/// <summary>
/// Manages activity log entries and recent activity display.
/// </summary>
public partial class ActivityLogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _recentActivity = "Application started. Ready for operation.";

    public ObservableCollection<ActivityLog> ActivityLogs { get; } = new();

    public ICommand ClearActivityLogsCommand { get; }

    public ActivityLogViewModel()
    {
        ClearActivityLogsCommand = new RelayCommand(Clear);
    }

    /// <summary>
    /// Adds a new activity log entry with timestamp and formatting.
    /// </summary>
    public void UpdateRecentActivity(string message, string status = "✓", OperationMode currentMode = OperationMode.Export)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var mode = currentMode == OperationMode.Export ? "EXP" : "IMP";
        
        // Add to DataGrid
        ActivityLogs.Add(new ActivityLog
        {
            Time = timestamp,
            Mode = mode,
            Status = status,
            Details = message
        });
        
        // Keep only last 50 entries
        while (ActivityLogs.Count > 50)
        {
            ActivityLogs.RemoveAt(0);
        }
        
        // Also update old string format for backward compatibility
        var formattedLine = $"{timestamp,-10} │ {mode,-4} │ {status,-3} │ {message}";
        
        if (string.IsNullOrEmpty(RecentActivity))
        {
            RecentActivity = $"Time       │ Mode │ ●   │ Operation Details\n{new string('─', 100)}\n{formattedLine}";
        }
        else
        {
            RecentActivity += $"\n{formattedLine}";
            
            var lines = RecentActivity.Split('\n');
            if (lines.Length > 17)
            {
                var headerLines = lines.Take(2).ToList();
                var dataLines = lines.Skip(2).TakeLast(15).ToList();
                RecentActivity = string.Join('\n', headerLines.Concat(dataLines));
            }
        }
    }

    /// <summary>
    /// Clears all activity log entries.
    /// </summary>
    public void Clear()
    {
        ActivityLogs.Clear();
        RecentActivity = "Activity log cleared. Ready for operation.";
    }
}
