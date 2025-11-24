using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Desktop.Views;
using TradeDataStudio.Desktop.ViewModels;

namespace TradeDataStudio.Desktop.Commands;

/// <summary>
/// Handles menu-related commands (File, Edit, View, Help menus).
/// </summary>
public class MenuCommandHandler
{
    private readonly IConfigurationService _configurationService;
    private readonly IDatabaseService _databaseService;
    private readonly ILoggingService _loggingService;
    private SettingsWindow? _settingsWindow;

    public ICommand ExitCommand { get; }
    public ICommand CopyOutputLocationCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ShowUserGuideCommand { get; }
    public ICommand ShowShortcutsCommand { get; }
    public ICommand ShowAboutCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand ViewLogsCommand { get; }

    public MenuCommandHandler(
        IConfigurationService configurationService,
        IDatabaseService databaseService,
        ILoggingService loggingService,
        Action<string> setStatusMessage,
        Action<string, string> logActivity,
        Func<Task> refreshData,
        Func<Task> updateConnectionStatus)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));

        ExitCommand = new RelayCommand(() => ExitApplication());
        CopyOutputLocationCommand = new RelayCommand<string>(location => CopyOutputLocation(location, setStatusMessage));
        RefreshCommand = new RelayCommand(() => RefreshData(refreshData, setStatusMessage));
        ShowUserGuideCommand = new RelayCommand(() => ShowUserGuide(setStatusMessage));
        ShowShortcutsCommand = new RelayCommand(() => ShowKeyboardShortcuts(setStatusMessage, logActivity));
        ShowAboutCommand = new RelayCommand(() => ShowAbout(setStatusMessage, logActivity));
        SettingsCommand = new AsyncRelayCommand(() => ShowSettingsAsync(setStatusMessage, logActivity, updateConnectionStatus));
        ViewLogsCommand = new RelayCommand(() => ViewLogs(setStatusMessage));
    }

    private void ExitApplication()
    {
        try
        {
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            // Silently fail - application is exiting anyway
        }
    }

    private void CopyOutputLocation(string? customOutputLocation, Action<string> setStatusMessage)
    {
        try
        {
            if (!string.IsNullOrEmpty(customOutputLocation))
            {
                setStatusMessage($"Output location: {customOutputLocation}");
            }
            else
            {
                setStatusMessage("No output location set");
            }
        }
        catch (Exception ex)
        {
            setStatusMessage("Error accessing output location");
        }
    }

    private void RefreshData(Func<Task> refreshData, Action<string> setStatusMessage)
    {
        try
        {
            _ = refreshData();
            setStatusMessage("Data refreshed successfully");
        }
        catch (Exception ex)
        {
            setStatusMessage("Error refreshing data");
        }
    }

    private void ShowUserGuide(Action<string> setStatusMessage)
    {
        try
        {
            setStatusMessage("User Guide: Use the dropdown menus to select procedures, configure output settings, and click Start to process data.");
        }
        catch (Exception ex)
        {
            setStatusMessage("Error showing user guide");
        }
    }

    private void ShowKeyboardShortcuts(Action<string> setStatusMessage, Action<string, string> logActivity)
    {
        try
        {
            setStatusMessage("Shortcuts: F1=Help, F5=Refresh, Ctrl+R=Reset, Ctrl+,=Settings, Ctrl+L=Logs, Alt+F4=Exit");
        }
        catch (Exception ex)
        {
            logActivity($"Error showing shortcuts: {ex.Message}", "⚠");
        }
    }

    private void ShowAbout(Action<string> setStatusMessage, Action<string, string> logActivity)
    {
        try
        {
            setStatusMessage("TradeData Studio v1.0.0 - Professional Data Processing Platform for Trade Data Analysis");
        }
        catch (Exception ex)
        {
            logActivity($"Error showing about information: {ex.Message}", "⚠");
        }
    }

    private async Task ShowSettingsAsync(
        Action<string> setStatusMessage,
        Action<string, string> logActivity,
        Func<Task> updateConnectionStatus)
    {
        try
        {
            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                _settingsWindow.Activate();
                return;
            }

            var settingsViewModel = new SettingsViewModel(_configurationService, _databaseService);
            _settingsWindow = new SettingsWindow(settingsViewModel);
            
            _settingsWindow.Closed += (sender, e) => 
            {
                _settingsWindow = null;
                _ = updateConnectionStatus();
            };
            
            _settingsWindow.Show();
            setStatusMessage("Settings window opened");
        }
        catch (Exception ex)
        {
            setStatusMessage($"Failed to open settings: {ex.Message}");
            logActivity($"Settings error: {ex.Message}", "⚠");
        }
    }

    private async void ViewLogs(Action<string> setStatusMessage)
    {
        try
        {
            var appSettings = await _configurationService.GetApplicationSettingsAsync();
            var configuredLogPath = appSettings.Paths.Logs;
            
            string logDirectory;
            if (!Path.IsPathRooted(configuredLogPath))
            {
                logDirectory = Path.GetFullPath(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    configuredLogPath.Replace("/", Path.DirectorySeparatorChar.ToString())));
            }
            else
            {
                logDirectory = configuredLogPath.Replace("/", Path.DirectorySeparatorChar.ToString());
            }
            
            if (Directory.Exists(logDirectory))
            {
                var latestLogFile = Directory.GetFiles(logDirectory, "main-*.log")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .FirstOrDefault();
                
                if (!string.IsNullOrEmpty(latestLogFile))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = latestLogFile,
                        UseShellExecute = true
                    });
                    
                    setStatusMessage("Opened latest log file");
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = logDirectory,
                        UseShellExecute = true
                    });
                    
                    setStatusMessage("Opened logs directory");
                }
            }
            else
            {
                setStatusMessage("Logs directory not found");
            }
        }
        catch (Exception ex)
        {
            setStatusMessage($"Failed to open logs: {ex.Message}");
        }
    }
}
