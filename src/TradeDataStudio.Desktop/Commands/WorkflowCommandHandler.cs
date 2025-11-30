using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;
using TradeDataStudio.Desktop.Helpers;
using TradeDataStudio.Desktop.Services;

namespace TradeDataStudio.Desktop.Commands;

/// <summary>
/// Handles workflow-related commands (Execute, Export, Start, Stop, Reset, Browse).
/// </summary>
public class WorkflowCommandHandler
{
    private readonly WorkflowOrchestrator _orchestrator;
    private readonly IConfigurationService _configurationService;
    private readonly OutputPathResolver _pathResolver;
    private CancellationTokenSource? _cancellationTokenSource;

    public ICommand ExecuteCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand BrowseOutputLocationCommand { get; }
    public ICommand ShowTableSelectionPopupCommand { get; }
    public ICommand CloseTableSelectionPopupCommand { get; }

    public WorkflowCommandHandler(
        WorkflowOrchestrator orchestrator,
        IConfigurationService configurationService,
        OutputPathResolver pathResolver,
        Func<bool> canExecute,
        Func<bool> canExport,
        Func<Task> executeAction,
        Func<Task> exportAction,
        Func<Task> startWorkflowAction,
        Action stopAction,
        Action resetAction,
        Func<Task> browseOutputAction,
        Action showTablePopupAction,
        Action closeTablePopupAction,
        Action refreshCommandStates)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));

        var executeCmd = new AsyncRelayCommand(executeAction, canExecute);
        var exportCmd = new AsyncRelayCommand(exportAction, canExport);
        var startCmd = new AsyncRelayCommand(startWorkflowAction, canExecute);
        
        ExecuteCommand = executeCmd;
        ExportCommand = exportCmd;
        StartCommand = startCmd;
        StopCommand = new RelayCommand(stopAction, () => CanStop());
        ResetCommand = new RelayCommand(resetAction);
        BrowseOutputLocationCommand = new AsyncRelayCommand(browseOutputAction);
        ShowTableSelectionPopupCommand = new RelayCommand(showTablePopupAction);
        CloseTableSelectionPopupCommand = new RelayCommand(closeTablePopupAction);
    }

    /// <summary>
    /// Executes stored procedure with cancellation support.
    /// </summary>
    public async Task ExecuteStoredProcedureWithCancellationAsync(
        StoredProcedureDefinition selectedProcedure,
        string startPeriod,
        string endPeriod,
        OperationMode currentMode,
        Action<bool> setOperationInProgress,
        Action<double> setProgressValue,
        Action<string> setStatusMessage,
        Action<string> setCurrentOperationStatus,
        Action<string, string> logActivity,
        bool isCalledFromWorkflow = false)
    {
        if (!isCalledFromWorkflow)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            // Notify that Stop button can now be executed
            NotifyStopCommandStateChanged();
        }
        
        var cancellationToken = _cancellationTokenSource?.Token ?? CancellationToken.None;

        try
        {
            setOperationInProgress(true);
            setProgressValue(0);
            setStatusMessage("Executing stored procedure...");
            setCurrentOperationStatus($"⏳ Executing: {selectedProcedure.DisplayName}...");

            var result = await _orchestrator.ExecuteStoredProcedureAsync(
                selectedProcedure, startPeriod, endPeriod, currentMode,
                setProgressValue, logActivity, cancellationToken);

            if (result.Success)
            {
                setCurrentOperationStatus("");
                setStatusMessage($"Execution completed successfully. {result.RecordsAffected} records processed.");
            }
            else
            {
                setCurrentOperationStatus("");
                setStatusMessage($"Execution failed: {result.Message}");
                logActivity("OPERATION TERMINATED: Execution failed", "⚠");
            }
        }
        catch (OperationCanceledException)
        {
            setCurrentOperationStatus("");
            setStatusMessage("⏹️ Operation cancelled by user");
            logActivity("STOPPED: User cancelled execution", "⏹");
        }
        catch (Exception ex)
        {
            setCurrentOperationStatus("");
            setStatusMessage("Execution failed with exception");
            logActivity($"OPERATION TERMINATED: {ex.Message}", "⚠");
        }
        finally
        {
            setOperationInProgress(false);
            setProgressValue(0);
            
            if (!isCalledFromWorkflow)
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                // Notify that Stop button is no longer executable
                NotifyStopCommandStateChanged();
            }
        }
    }

    /// <summary>
    /// Exports data tables with validation and cancellation support.
    /// </summary>
    public async Task ExportDataWithValidationAsync(
        List<TableDefinition> tablesToExport,
        ExportFormat format,
        string startPeriod,
        string endPeriod,
        OperationMode currentMode,
        bool useCustomLocation,
        string customOutputLocation,
        Action<bool> setOperationInProgress,
        Action<double> setProgressValue,
        Action<string> setStatusMessage,
        Action<string> setCurrentOperationStatus,
        Action<string, string> logActivity,
        Action<string, string, List<string>> showError)
    {
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        // Notify that Stop button can now be executed
        NotifyStopCommandStateChanged();

        try
        {
            setOperationInProgress(true);
            setProgressValue(0);
            setStatusMessage("Exporting data...");
            setCurrentOperationStatus($"⏳ Downloading {tablesToExport.Count} table(s)...");

            var results = await _orchestrator.ExportDataAsync(
                tablesToExport, format, startPeriod, endPeriod, currentMode,
                useCustomLocation, customOutputLocation, setProgressValue, logActivity,
                _cancellationTokenSource.Token);

            var successCount = results.Count(r => r.Success);
            var totalRecords = results.Where(r => r.Success).Sum(r => r.RecordsExported);

            if (successCount == tablesToExport.Count)
            {
                setCurrentOperationStatus("");
                setStatusMessage($"Export completed: {successCount} tables, {totalRecords} total records");
            }
            else
            {
                setCurrentOperationStatus("");
                setStatusMessage($"Partial export: {successCount}/{tablesToExport.Count} tables succeeded");
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Excel"))
        {
            setCurrentOperationStatus("");
            setStatusMessage("Export blocked: Data exceeds Excel limit. Use CSV format.");
            logActivity("OPERATION TERMINATED: Excel limit exceeded", "⚠");
            showError("Excel Row Limit Exceeded", ex.Message, new List<string>
            {
                "Switch to CSV format (supports unlimited rows)",
                "Add date/filter parameters to stored procedure to reduce data",
                "Split export into multiple smaller date ranges"
            });
        }
        catch (OperationCanceledException)
        {
            setCurrentOperationStatus("");
            setStatusMessage("⏹️ Export cancelled by user");
            logActivity("STOPPED: User cancelled export", "⏹");
        }
        catch (Exception ex)
        {
            setCurrentOperationStatus("");
            setStatusMessage("Export failed with exception");
            logActivity($"OPERATION TERMINATED: {ex.Message}", "⚠");
        }
        finally
        {
            setOperationInProgress(false);
            setProgressValue(0);
            
            // Dispose cancellation token to ensure Stop button is hidden
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            // Notify that Stop button is no longer executable
            NotifyStopCommandStateChanged();
        }
    }

    /// <summary>
    /// Browses for output folder location using PowerShell folder picker dialog.
    /// </summary>
    public async Task BrowseOutputLocationAsync(
        string currentCustomLocation,
        Action<string> setCustomOutputLocation,
        Action<string> setStatusMessage)
    {
        try
        {
            string defaultPath = !string.IsNullOrEmpty(currentCustomLocation)
                ? currentCustomLocation
                : await _pathResolver.GetDefaultExportPathAsync();
            
            var powershellScript = @"
                Add-Type -AssemblyName System.Windows.Forms
                $folderBrowser = New-Object System.Windows.Forms.FolderBrowserDialog
                $folderBrowser.Description = 'Select output folder for exported files'
                $folderBrowser.ShowNewFolderButton = $true
                $folderBrowser.SelectedPath = '" + defaultPath.Replace("\\", "\\\\") + @"'
                $result = $folderBrowser.ShowDialog()
                if ($result -eq 'OK') {
                    Write-Output $folderBrowser.SelectedPath
                }
                else {
                    Write-Output 'CANCELLED'
                }
            ";
            
            var processInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -Command \"{powershellScript}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                var result = await process.StandardOutput.ReadToEndAsync();
                result = result.Trim();
                
                if (!string.IsNullOrEmpty(result) && result != "CANCELLED" && Directory.Exists(result))
                {
                    setCustomOutputLocation(result);
                    setStatusMessage($"Output location set to: {result}");
                }
                else if (result == "CANCELLED")
                {
                    // User cancelled
                }
                else
                {
                    if (!Directory.Exists(defaultPath))
                    {
                        Directory.CreateDirectory(defaultPath);
                    }
                    setCustomOutputLocation(defaultPath);
                    setStatusMessage($"Using default location: {defaultPath}");
                }
            }
        }
        catch (Exception)
        {
            string defaultPath = await _pathResolver.GetDefaultExportPathAsync();
            
            if (!Directory.Exists(defaultPath))
            {
                Directory.CreateDirectory(defaultPath);
            }
            setCustomOutputLocation(defaultPath);
            setStatusMessage("Error with folder picker, using configured default location");
        }
    }

    public void SetWorkflowCancellationTokenSource(CancellationTokenSource workflowTokenSource)
    {
        // Set the workflow token source so Stop button can cancel it
        _cancellationTokenSource = workflowTokenSource;
    }

    /// <summary>
    /// Notifies the Stop command that its CanExecute state may have changed.
    /// This ensures the Stop button is enabled when a new operation begins.
    /// </summary>
    public void NotifyStopCommandStateChanged()
    {
        if (StopCommand is RelayCommand stopCmd)
        {
            stopCmd.NotifyCanExecuteChanged();
        }
    }

    public void StopOperation(Action<string> setStatusMessage, Action<string, string> logActivity)
    {
        try
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                setStatusMessage("⏹️ Operation cancelled by user");
            }
            else
            {
                setStatusMessage("No operation in progress");
            }
        }
        catch (Exception ex)
        {
            setStatusMessage("Error stopping operation");
            logActivity($"Error during stop: {ex.Message}", "⚠");
        }
    }

    private bool CanStop()
    {
        return _cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested;
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
    }
}
