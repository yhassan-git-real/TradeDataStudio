using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;
using TradeDataStudio.Core.Services;
using TradeDataStudio.Desktop.Views;
using TradeDataStudio.Desktop.Models;

namespace TradeDataStudio.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IConfigurationService _configurationService;
    private readonly IDatabaseService _databaseService;
    private readonly IExportService _exportService;
    private readonly ILoggingService _loggingService;
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    private bool _isExportMode = true;

    [ObservableProperty]
    private bool _isImportMode = false;

    [ObservableProperty]
    private string _startPeriod = string.Empty;

    [ObservableProperty]
    private string _endPeriod = string.Empty;

    [ObservableProperty]
    private StoredProcedureDefinition? _selectedStoredProcedure;

    [ObservableProperty]
    private TableDefinition? _selectedOutputTable;

    [ObservableProperty]
    private string _selectedExportFormat = "Excel";

    [ObservableProperty]
    private string _customOutputLocation = "";

    [ObservableProperty]
    private bool _useCustomLocation = false;

    [ObservableProperty]
    private bool _isTableSelectionPopupOpen = false;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isOperationInProgress = false;

    [ObservableProperty]
    private double _progressValue = 0;

    [ObservableProperty]
    private string _recentActivity = "Application started. Ready for operation.";

    [ObservableProperty]
    private string _currentOperationStatus = "";

    public ObservableCollection<ActivityLog> ActivityLogs { get; } = new();

    [ObservableProperty]
    private string _connectionStatus = "Not connected";

    [ObservableProperty]
    private string _connectedServer = "";

    [ObservableProperty]
    private string _connectedDatabase = "";

    [ObservableProperty]
    private string _connectedUser = "";

    public ObservableCollection<StoredProcedureDefinition> AvailableStoredProcedures { get; } = new();
    public ObservableCollection<SelectableTableDefinition> AvailableOutputTables { get; } = new();
    public ObservableCollection<string> AvailableExportFormats { get; } = new() { "Excel", "CSV", "TXT" };

    public OperationMode CurrentMode => IsExportMode ? OperationMode.Export : OperationMode.Import;

    private readonly AsyncRelayCommand _executeCommand;
    private readonly AsyncRelayCommand _exportCommand;
    private readonly AsyncRelayCommand _startCommand;

    // Parameterless constructor for XAML designer
    public MainWindowViewModel() : this(
        null!,
        null!,
        null!,
        null!)
    {
    }

    public MainWindowViewModel(
        IConfigurationService configurationService,
        IDatabaseService databaseService,
        IExportService exportService,
        ILoggingService loggingService)
    {
        _configurationService = configurationService;
        _databaseService = databaseService;
        _exportService = exportService;
        _loggingService = loggingService;

        // Initialize collections
        AvailableOutputTables.CollectionChanged += AvailableOutputTablesOnCollectionChanged;

        // Initialize commands FIRST
        _executeCommand = new AsyncRelayCommand(async () => await ExecuteStoredProcedureAsync(), CanExecute);
        _exportCommand = new AsyncRelayCommand(ExportDataAsync, CanExport);
        _startCommand = new AsyncRelayCommand(StartWorkflowAsync, CanExecute);
        ExecuteCommand = _executeCommand;
        ExportCommand = _exportCommand;
        StartCommand = _startCommand;
        StopCommand = new RelayCommand(StopOperation, CanStop);
        ResetCommand = new RelayCommand(ResetForm);
        SettingsCommand = new AsyncRelayCommand(ShowSettingsAsync);
        ViewLogsCommand = new RelayCommand(ViewLogs);
        BrowseOutputLocationCommand = new AsyncRelayCommand(BrowseOutputLocationAsync);
        ShowTableSelectionPopupCommand = new RelayCommand(ShowTableSelectionPopup);
        CloseTableSelectionPopupCommand = new RelayCommand(CloseTableSelectionPopup);
        
        // Menu Commands
        ExitCommand = new RelayCommand(ExitApplication);
        CopyOutputLocationCommand = new RelayCommand(CopyOutputLocation);
        RefreshCommand = new RelayCommand(RefreshData);
        ShowUserGuideCommand = new RelayCommand(ShowUserGuide);
        ShowShortcutsCommand = new RelayCommand(ShowKeyboardShortcuts);
        ShowAboutCommand = new RelayCommand(ShowAbout);

        // Initialize with real data via DI - only if services are available (not design time)
        if (_configurationService != null && _loggingService != null)
        {
            _ = InitializeAsync();
        }
    }

    public ICommand ExecuteCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand ViewLogsCommand { get; }
    public ICommand BrowseOutputLocationCommand { get; }
    public ICommand ShowTableSelectionPopupCommand { get; }
    public ICommand CloseTableSelectionPopupCommand { get; }
    
    // Menu Commands
    public ICommand ExitCommand { get; }
    public ICommand CopyOutputLocationCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ShowUserGuideCommand { get; }
    public ICommand ShowShortcutsCommand { get; }
    public ICommand ShowAboutCommand { get; }

    private async Task InitializeAsync()
    {
        try
        {
            Console.WriteLine("=== InitializeAsync STARTED ===");
            Console.WriteLine($"ConfigurationService is null: {_configurationService == null}");
            Console.WriteLine($"LoggingService is null: {_loggingService == null}");
            
            await _loggingService.LogMainAsync("Starting MainWindowViewModel initialization...");
            
            Console.WriteLine("About to call LoadStoredProceduresAsync...");
            await LoadStoredProceduresAsync();
            Console.WriteLine($"After LoadStoredProceduresAsync - Count: {AvailableStoredProcedures.Count}");
            
            await LoadOutputTablesAsync();
            await UpdateConnectionStatusAsync();
            
            var appSettings = await _configurationService.GetApplicationSettingsAsync();
            IsExportMode = appSettings.DefaultMode == OperationMode.Export;
            IsImportMode = !IsExportMode;
            
            await _loggingService.LogMainAsync("MainWindowViewModel initialized successfully.");
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed to initialize application";
            await _loggingService.LogErrorAsync("Failed to initialize application", ex);
        }
    }

    private async Task UpdateConnectionStatusAsync()
    {
        try
        {
            var dbConfig = await _configurationService.GetDatabaseConfigurationAsync();
            var isConnected = await _databaseService.TestConnectionAsync();
            
            if (isConnected)
            {
                ConnectionStatus = "Connected";
                ConnectedServer = dbConfig.Server;
                ConnectedDatabase = dbConfig.Database;
                ConnectedUser = dbConfig.UseWindowsAuthentication ? 
                    Environment.UserName : 
                    (!string.IsNullOrEmpty(dbConfig.Username) ? dbConfig.Username : "SQL Server");
            }
            else
            {
                ConnectionStatus = "Not connected";
                ConnectedServer = "";
                ConnectedDatabase = "";
                ConnectedUser = "";
            }
        }
        catch
        {
            ConnectionStatus = "Connection error";
            ConnectedServer = "";
            ConnectedDatabase = "";
            ConnectedUser = "";
        }
    }

    partial void OnIsExportModeChanged(bool value)
    {
        if (value)
        {
            IsImportMode = false;
            _ = OnModeChangedAsync();
            RefreshCommandStates();
        }
    }

    partial void OnIsImportModeChanged(bool value)
    {
        if (value)
        {
            IsExportMode = false;
            _ = OnModeChangedAsync();
            RefreshCommandStates();
        }
    }

    partial void OnSelectedStoredProcedureChanged(StoredProcedureDefinition? value)
    {
        _ = FilterOutputTablesForSelectedProcedureAsync();
        RefreshCommandStates();
    }

    partial void OnSelectedOutputTableChanged(TableDefinition? value)
    {
        RefreshCommandStates();
    }

    partial void OnStartPeriodChanged(string value)
    {
        RefreshCommandStates();
    }

    partial void OnEndPeriodChanged(string value)
    {
        RefreshCommandStates();
    }

    partial void OnIsOperationInProgressChanged(bool value)
    {
        RefreshCommandStates();
    }

    private async Task OnModeChangedAsync()
    {
        try
        {
            // Clear current selections
            SelectedStoredProcedure = null;
            SelectedOutputTable = null;

            // Reload data for new mode
            await LoadStoredProceduresAsync();
            await LoadOutputTablesAsync();

            var modeText = IsExportMode ? "Export" : "Import";
            StatusMessage = $"Switched to {modeText} mode";
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Failed to change mode", ex);
        }
    }

    private async Task FilterOutputTablesForSelectedProcedureAsync()
    {
        try
        {
            if (SelectedStoredProcedure == null)
            {
                AvailableOutputTables.Clear();
                return;
            }

            // Clear current tables first
            AvailableOutputTables.Clear();

            // Load all available tables from configuration
            var allTables = await _configurationService.GetTablesAsync(CurrentMode);
            
            // Only add tables that are associated with the selected procedure
            if (SelectedStoredProcedure.OutputTables?.Any() == true)
            {
                foreach (var tableName in SelectedStoredProcedure.OutputTables)
                {
                    var table = allTables.FirstOrDefault(t => t.Name == tableName);
                    if (table != null)
                    {
                        AvailableOutputTables.Add(new SelectableTableDefinition(table));
                    }
                }
                
                var tableCount = AvailableOutputTables.Count;
            }
            else
            {
                // No output tables defined for this stored procedure
            }
        }
        catch (Exception ex)
        {
            if (_loggingService != null)
                await _loggingService.LogErrorAsync("Failed to filter output tables", ex);
        }
    }

    private void AvailableOutputTablesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (SelectableTableDefinition table in e.OldItems)
            {
                table.PropertyChanged -= TableOnPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (SelectableTableDefinition table in e.NewItems)
            {
                table.PropertyChanged += TableOnPropertyChanged;
            }
        }

        RefreshCommandStates();
    }

    private void TableOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectableTableDefinition.IsSelected))
        {
            RefreshCommandStates();
        }
    }

    private async Task LoadStoredProceduresAsync()
    {
        try
        {
            Console.WriteLine($"=== LoadStoredProceduresAsync STARTED - CurrentMode: {CurrentMode} ===");
            await _loggingService.LogMainAsync($"Loading stored procedures for mode: {CurrentMode}");
            AvailableStoredProcedures.Clear();
            
            Console.WriteLine("Calling _configurationService.GetStoredProceduresAsync...");
            var procedures = await _configurationService.GetStoredProceduresAsync(CurrentMode);
            Console.WriteLine($"Received {procedures.Count} procedures from ConfigurationService");
            
            foreach (var procedure in procedures)
            {
                Console.WriteLine($"Adding procedure: {procedure.DisplayName}");
                AvailableStoredProcedures.Add(procedure);
            }
            
            Console.WriteLine($"Final AvailableStoredProcedures.Count: {AvailableStoredProcedures.Count}");
            await _loggingService.LogMainAsync($"Loaded {procedures.Count} stored procedures");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in LoadStoredProceduresAsync: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            await _loggingService.LogErrorAsync("Failed to load stored procedures", ex);
        }
    }

    private async Task LoadOutputTablesAsync()
    {
        try
        {
            await _loggingService.LogMainAsync($"Loading output tables for mode: {CurrentMode}");
            AvailableOutputTables.Clear();
            var tables = await _configurationService.GetTablesAsync(CurrentMode);
            
            // Filter out main table - only show Table1 and Table2 (or similar output tables)
            var outputTables = tables.Where(t => !t.Name.Contains("MAIN", StringComparison.OrdinalIgnoreCase)).ToList();
            
            foreach (var table in outputTables)
            {
                AvailableOutputTables.Add(new SelectableTableDefinition(table));
            }
            
            await _loggingService.LogMainAsync($"Loaded {outputTables.Count} output tables (excluded main table)");
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Failed to load output tables", ex);
            UpdateRecentActivity($"Error loading tables: {ex.Message}", "⚠");
        }
    }

    private bool CanExecute()
    {
        return !IsOperationInProgress && 
               SelectedStoredProcedure != null && 
               !string.IsNullOrWhiteSpace(StartPeriod) && 
               !string.IsNullOrWhiteSpace(EndPeriod);
    }

    private bool CanExport()
    {
        return !IsOperationInProgress && (SelectedOutputTable != null || AvailableOutputTables.Any(t => t.IsSelected));
    }

    private async Task ExecuteStoredProcedureAsync(bool isCalledFromWorkflow = false)
    {
        if (SelectedStoredProcedure == null || _databaseService == null || _loggingService == null) return;

        // Only create new cancellation token if not called from workflow
        if (!isCalledFromWorkflow)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            ((RelayCommand)StopCommand).NotifyCanExecuteChanged();
        }
        
        var cancellationToken = _cancellationTokenSource?.Token ?? CancellationToken.None;

        try
        {
            IsOperationInProgress = true;
            ProgressValue = 0;
            StatusMessage = "Executing stored procedure...";
            
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();
            
            // Build parameters dynamically from stored procedure definition
            var parameters = new Dictionary<string, object>();
            
            Console.WriteLine($"Building parameters for {SelectedStoredProcedure.Name}:");
            Console.WriteLine($"  StartPeriod: {StartPeriod}");
            Console.WriteLine($"  EndPeriod: {EndPeriod}");
            Console.WriteLine($"  Parameters object is null: {SelectedStoredProcedure.Parameters == null}");
            Console.WriteLine($"  Parameter count in definition: {SelectedStoredProcedure.Parameters?.Count ?? -999}");
            
            // For now, we'll use StartPeriod and EndPeriod for the first two parameters
            // TODO: In future, create a dynamic parameter input UI
            if (SelectedStoredProcedure.Parameters != null && SelectedStoredProcedure.Parameters.Count >= 1)
            {
                var firstParam = SelectedStoredProcedure.Parameters[0];
                var paramName = firstParam.Name.StartsWith("@") ? firstParam.Name : $"@{firstParam.Name}";
                
                Console.WriteLine($"  First param: {paramName} (Type: {firstParam.Type})");
                
                if (firstParam.Type.ToLower().Contains("int"))
                {
                    var intValue = int.Parse(StartPeriod);
                    parameters[paramName] = intValue;
                    Console.WriteLine($"    Added as int: {intValue}");
                }
                else
                {
                    parameters[paramName] = StartPeriod;
                    Console.WriteLine($"    Added as string: {StartPeriod}");
                }
            }
            
            if (SelectedStoredProcedure.Parameters != null && SelectedStoredProcedure.Parameters.Count >= 2)
            {
                var secondParam = SelectedStoredProcedure.Parameters[1];
                var paramName = secondParam.Name.StartsWith("@") ? secondParam.Name : $"@{secondParam.Name}";
                
                Console.WriteLine($"  Second param: {paramName} (Type: {secondParam.Type})");
                
                if (secondParam.Type.ToLower().Contains("int"))
                {
                    var intValue = int.Parse(EndPeriod);
                    parameters[paramName] = intValue;
                    Console.WriteLine($"    Added as int: {intValue}");
                }
                else
                {
                    parameters[paramName] = EndPeriod;
                    Console.WriteLine($"    Added as string: {EndPeriod}");
                }
            }
            
            Console.WriteLine($"Final parameters dictionary count: {parameters.Count}");
            foreach (var p in parameters)
            {
                Console.WriteLine($"  {p.Key} = {p.Value}");
            }
            
            var paramInfo = string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"));
            CurrentOperationStatus = $"⏳ Executing: {SelectedStoredProcedure.DisplayName}...";
            UpdateRecentActivity($"EXECUTE: {SelectedStoredProcedure.DisplayName} | Params: {paramInfo}", "▶");
            ProgressValue = 25;

            var result = await _databaseService.ExecuteStoredProcedureAsync(
                SelectedStoredProcedure.Name, parameters);

            ProgressValue = 100;

            if (result.Success)
            {
                CurrentOperationStatus = "";
                StatusMessage = $"Execution completed successfully. {result.RecordsAffected} records processed.";
                UpdateRecentActivity($"SUCCESS: {SelectedStoredProcedure.DisplayName} | {result.RecordsAffected:N0} records | {result.ExecutionTime.TotalSeconds:F1}s", "✓");
                
                if (_loggingService != null)
                    await _loggingService.LogExecutionAsync(
                        SelectedStoredProcedure.Name, parameters, result, CurrentMode);
            }
            else
            {
                StatusMessage = $"Execution failed: {result.Message}";
                UpdateRecentActivity($"Error: {result.Message}", "⚠");
                
                if (_loggingService != null)
                    await _loggingService.LogErrorAsync(
                        $"Stored procedure execution failed: {result.Message}", 
                        result.Exception, CurrentMode);
            }
        }
        catch (OperationCanceledException)
        {
            CurrentOperationStatus = "";
            StatusMessage = "⏹️ Operation cancelled by user";
            UpdateRecentActivity("STOPPED: User cancelled execution", "⏹");
            if (_loggingService != null)
                await _loggingService.LogMainAsync("Stored procedure execution cancelled by user");
        }
        catch (Exception ex)
        {
            CurrentOperationStatus = "";
            StatusMessage = "Execution failed with exception";
            UpdateRecentActivity($"Exception: {ex.Message}", "⚠");
            if (_loggingService != null)
                await _loggingService.LogErrorAsync("Execution failed with exception", ex, CurrentMode);
        }
        finally
        {
            IsOperationInProgress = false;
            ProgressValue = 0;
            // Only dispose cancellation token if not called from workflow
            if (!isCalledFromWorkflow)
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                ((RelayCommand)StopCommand).NotifyCanExecuteChanged();
            }
        }
    }

    private async Task ExportDataAsync()
    {
        var selectedTableDefs = AvailableOutputTables.Where(t => t.IsSelected).Select(t => t.Table).ToList();
        var tablesToExport = selectedTableDefs.Any() ? selectedTableDefs : 
                            (SelectedOutputTable != null ? new List<TableDefinition> { SelectedOutputTable } : new List<TableDefinition>());
        
        if (!tablesToExport.Any() || _databaseService == null || _exportService == null || _loggingService == null) return;

        try
        {
            // Pre-download validation for Excel format
            if (SelectedExportFormat == "Excel")
            {
                UpdateRecentActivity("Validating table sizes for Excel export...", "🔍");
                
                var excelMaxRows = 1048575; // Excel maximum data rows (excluding header)
                var tableSizeIssues = new List<string>();
                
                foreach (var table in tablesToExport)
                {
                    try
                    {
                        var rowCount = await _databaseService.GetTableRecordCountAsync(table.Name);
                        
                        if (rowCount > excelMaxRows)
                        {
                            tableSizeIssues.Add($"{table.Name}: {rowCount:N0} rows (exceeds Excel limit of {excelMaxRows:N0})");
                        }
                        else if (rowCount > excelMaxRows * 0.9) // Warn if >90% of limit
                        {
                            UpdateRecentActivity($"WARNING: {table.Name} has {rowCount:N0} rows (near Excel limit)", "⚠");
                        }
                    }
                    catch (Exception ex)
                    {
                        await _loggingService.LogErrorAsync($"Failed to get row count for {table.Name}", ex, CurrentMode);
                    }
                }
                
                // Block export if any table exceeds Excel limit
                if (tableSizeIssues.Any())
                {
                    var errorMessage = $"Cannot export to Excel - {tableSizeIssues.Count} table(s) exceed Excel row limit:\n" + 
                                      string.Join("\n", tableSizeIssues);
                    
                    UpdateRecentActivity("VALIDATION FAILED: Excel row limit exceeded", "❌");
                    foreach (var issue in tableSizeIssues)
                    {
                        UpdateRecentActivity($"  → {issue}", "⚠");
                    }
                    UpdateRecentActivity("SOLUTION: Use CSV format or filter the data in stored procedure", "💡");
                    
                    StatusMessage = "Export blocked: Data exceeds Excel limit. Use CSV format.";
                    
                    ShowError(
                        "Excel Row Limit Exceeded",
                        errorMessage,
                        new List<string> 
                        { 
                            "Switch to CSV format (supports unlimited rows)",
                            "Add date/filter parameters to stored procedure to reduce data",
                            "Split export into multiple smaller date ranges"
                        }
                    );
                    
                    return; // Abort export
                }
                
                UpdateRecentActivity($"Validation passed: All {tablesToExport.Count} table(s) within Excel limits", "✓");
            }
            
            // Create cancellation token for Stop button support
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            
            IsOperationInProgress = true;
            ProgressValue = 0;
            StatusMessage = "Exporting data...";

            var tableNamesList = string.Join(", ", tablesToExport.Select(t => t.Name));
            CurrentOperationStatus = $"⏳ Downloading {tablesToExport.Count} table(s)...";
            UpdateRecentActivity($"DOWNLOAD: {tablesToExport.Count} table(s) → {SelectedExportFormat} | Tables: {tableNamesList}", "📥");
            
            // Get export path from configuration
            string outputPath;
            if (UseCustomLocation && !string.IsNullOrEmpty(CustomOutputLocation))
            {
                outputPath = CustomOutputLocation;
            }
            else
            {
                // Read from appsettings.json
                var appSettings = await _configurationService.GetApplicationSettingsAsync();
                var configuredExportPath = appSettings.Paths.Exports;
                
                // Convert to absolute path if needed
                if (!Path.IsPathRooted(configuredExportPath))
                {
                    outputPath = Path.GetFullPath(Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "..", "..", "..",
                        configuredExportPath.Replace("/", Path.DirectorySeparatorChar.ToString())));
                }
                else
                {
                    outputPath = configuredExportPath.Replace("/", Path.DirectorySeparatorChar.ToString());
                }
                
                // Add mode subdirectory
                outputPath = Path.Combine(outputPath, CurrentMode.ToString().ToLower());
            }
            
            var format = Enum.Parse<ExportFormat>(SelectedExportFormat);
            var results = await _exportService.ExportAllTablesAsync(
                tablesToExport.Select(t => t.Name).ToList(), 
                format, 
                outputPath, 
                _databaseService,
                StartPeriod,
                EndPeriod,
                CurrentMode,
                _cancellationTokenSource.Token);
            
            var successCount = results.Count(r => r.Success);
            var totalRecords = results.Where(r => r.Success).Sum(r => r.RecordsExported);
            
            // Show errors in Recent Activity
            var failedExports = results.Where(r => !r.Success).ToList();
            if (failedExports.Any())
            {
                foreach (var failed in failedExports)
                {
                    var errorMsg = failed.Message?.Contains("Row out of range") == true 
                        ? "Export failed: Data too large for Excel (>1M rows). Try CSV format or filter data."
                        : $"Export failed: {failed.Message}";
                    UpdateRecentActivity(errorMsg, "⚠");
                }
            }
            
            ProgressValue = 100;

            if (successCount == tablesToExport.Count)
            {
                CurrentOperationStatus = "";
                StatusMessage = $"Export completed: {successCount} tables, {totalRecords} total records";
                UpdateRecentActivity($"Successfully exported {successCount} tables with {totalRecords} records", "✓");
            }
            else
            {
                CurrentOperationStatus = "";
                StatusMessage = $"Partial export: {successCount}/{tablesToExport.Count} tables succeeded";
                UpdateRecentActivity($"Partial success: {successCount}/{tablesToExport.Count} tables exported", "⚠");
            }
            
            if (_loggingService != null)
            {
                foreach (var result in results)
                {
                    await _loggingService.LogExportAsync(result, CurrentMode);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Export failed with exception";
            UpdateRecentActivity($"Export exception: {ex.Message}", "⚠");
            if (_loggingService != null)
                await _loggingService.LogErrorAsync("Export failed with exception", ex, CurrentMode);
        }
        finally
        {
            IsOperationInProgress = false;
            ProgressValue = 0;
        }
    }

    private async Task StartWorkflowAsync()
    {
        if (_exportService == null || _loggingService == null) return;
        
        // Create new cancellation token source for this operation
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;
        ((RelayCommand)StopCommand).NotifyCanExecuteChanged();
        
        try
        {
            StatusMessage = "Starting workflow...";
            var tableCount = AvailableOutputTables.Count(t => t.IsSelected);
            CurrentOperationStatus = $"⏳ Workflow in progress: {SelectedStoredProcedure?.DisplayName}...";
            UpdateRecentActivity($"START: {SelectedStoredProcedure?.DisplayName} | {tableCount} table(s) selected", "🚀");
            await _loggingService.LogMainAsync($"[{CurrentMode}] Workflow started for procedure: {SelectedStoredProcedure?.Name}");
            
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();
            
            // Execute SP first (pass true to indicate called from workflow)
            await ExecuteStoredProcedureAsync(isCalledFromWorkflow: true);
            
            // Check for cancellation again after SP execution
            cancellationToken.ThrowIfCancellationRequested();
            
            // Get only the selected tables for export
            var selectedTableDefs = AvailableOutputTables.Where(t => t.IsSelected).Select(t => t.Table).ToList();
            
            // If no tables selected, auto-select all available tables (Table1 & Table2, excluding main)
            if (!selectedTableDefs.Any())
            {
                // Auto-select all available output tables
                foreach (var table in AvailableOutputTables)
                {
                    table.IsSelected = true;
                }
                selectedTableDefs = AvailableOutputTables.Select(t => t.Table).ToList();
                UpdateRecentActivity($"Auto-selected all {selectedTableDefs.Count} output table(s)", "📝");
            }
            
            var tablesToExport = selectedTableDefs.Any() ? selectedTableDefs : 
                                (SelectedOutputTable != null ? new List<TableDefinition> { SelectedOutputTable } : new List<TableDefinition>());
            
            if (tablesToExport.Any())
            {
                // Validate table sizes for Excel format AFTER SP execution
                if (SelectedExportFormat == "Excel")
                {
                    UpdateRecentActivity("Validating table sizes for Excel export...", "🔍");
                    
                    var excelMaxRows = 1048575;
                    var tableSizeIssues = new List<string>();
                    
                    foreach (var table in tablesToExport)
                    {
                        try
                        {
                            var rowCount = await _databaseService.GetTableRecordCountAsync(table.Name);
                            
                            if (rowCount > excelMaxRows)
                            {
                                tableSizeIssues.Add($"{table.Name}: {rowCount:N0} rows (exceeds Excel limit of {excelMaxRows:N0})");
                            }
                            else if (rowCount > excelMaxRows * 0.9)
                            {
                                UpdateRecentActivity($"WARNING: {table.Name} has {rowCount:N0} rows (near Excel limit)", "⚠");
                            }
                        }
                        catch (Exception ex)
                        {
                            await _loggingService.LogErrorAsync($"Failed to get row count for {table.Name}", ex, CurrentMode);
                        }
                    }
                    
                    // Block export if any table exceeds Excel limit
                    if (tableSizeIssues.Any())
                    {
                        var errorMessage = $"Cannot export to Excel - {tableSizeIssues.Count} table(s) exceed Excel row limit:\n" + 
                                          string.Join("\n", tableSizeIssues);
                        
                        UpdateRecentActivity("VALIDATION FAILED: Excel row limit exceeded", "❌");
                        foreach (var issue in tableSizeIssues)
                        {
                            UpdateRecentActivity($"  → {issue}", "⚠");
                        }
                        UpdateRecentActivity("SOLUTION: Use CSV format or filter the data in stored procedure", "💡");
                        
                        CurrentOperationStatus = "";
                        StatusMessage = "Workflow failed: Data exceeds Excel limit. Use CSV format.";
                        
                        ShowError(
                            "Excel Row Limit Exceeded",
                            errorMessage,
                            new List<string> 
                            { 
                                "Switch to CSV format (supports unlimited rows)",
                                "Add date/filter parameters to stored procedure to reduce data",
                                "Split export into multiple smaller date ranges"
                            }
                        );
                        
                        return; // Abort workflow
                    }
                    
                    UpdateRecentActivity($"Validation passed: All {tablesToExport.Count} table(s) within Excel limits", "✓");
                }
                
                var format = Enum.Parse<ExportFormat>(SelectedExportFormat);
                
                // Determine output path
                string outputPath;
                if (UseCustomLocation && !string.IsNullOrEmpty(CustomOutputLocation))
                {
                    outputPath = CustomOutputLocation;
                }
                else
                {
                    // Use default path from appsettings.json
                    var appSettings = await _configurationService.GetApplicationSettingsAsync();
                    var basePath = appSettings.Paths.Exports;
                    // Add mode-specific subfolder: export or import
                    outputPath = Path.Combine(basePath, CurrentMode.ToString().ToLower());
                }
                
                // Ensure directory exists
                Directory.CreateDirectory(outputPath);
                
                // Extract table names from TableDefinition objects
                var tableNames = tablesToExport.Select(t => t.Name).ToList();
                
                await _loggingService.LogMainAsync($"[{CurrentMode}] Workflow: Exporting {tableNames.Count} selected table(s): {string.Join(", ", tableNames)}");
                
                var results = await _exportService.ExportAllTablesAsync(
                    tableNames, format, outputPath, _databaseService);
                
                var successCount = results.Count(r => r.Success);
                StatusMessage = $"Workflow completed: {successCount}/{results.Count} tables exported to {outputPath}";
                var exportedFiles = string.Join(", ", results.Where(r => r.Success).Select(r => r.FileName));
                var totalExportedRecords = results.Where(r => r.Success).Sum(r => r.RecordsExported);
                CurrentOperationStatus = "";
                UpdateRecentActivity($"WORKFLOW COMPLETE: {successCount}/{results.Count} tables | {totalExportedRecords:N0} records | Path: {outputPath}", "✓");
                UpdateRecentActivity($"Files: {exportedFiles}", "  ");
                await _loggingService.LogMainAsync($"[{CurrentMode}] Workflow completed: {successCount}/{results.Count} tables exported to {outputPath}");
                
                if (_loggingService != null)
                {
                    foreach (var result in results)
                    {
                        await _loggingService.LogExportAsync(result, CurrentMode);
                        if (result.Success)
                        {
                            await _loggingService.LogMainAsync($"[{CurrentMode}] Exported file '{result.FileName}' to: {result.FilePath}");
                        }
                    }
                }
            }
            else
            {
                StatusMessage = "No tables selected for export";
                await _loggingService.LogMainAsync($"[{CurrentMode}] Workflow completed - no tables selected for export");
            }
        }
        catch (OperationCanceledException)
        {
            CurrentOperationStatus = "";
            StatusMessage = "⏹️ Workflow cancelled by user";
            UpdateRecentActivity("STOPPED: Workflow cancelled by user", "⏹");
            if (_loggingService != null)
                await _loggingService.LogMainAsync($"[{CurrentMode}] Workflow cancelled by user");
        }
        catch (Exception ex)
        {
            CurrentOperationStatus = "";
            StatusMessage = $"Workflow failed: {ex.Message}";
            UpdateRecentActivity($"Workflow error: {ex.Message}", "⚠");
            if (_loggingService != null)
                await _loggingService.LogErrorAsync($"[{CurrentMode}] Workflow failed", ex, CurrentMode);
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            ((RelayCommand)StopCommand).NotifyCanExecuteChanged();
        }
    }

    private void ResetForm()
    {
        SelectedStoredProcedure = null;
        SelectedOutputTable = null;
        foreach (var table in AvailableOutputTables)
        {
            table.IsSelected = false;
        }
        StartPeriod = string.Empty;
        EndPeriod = string.Empty;
        SelectedExportFormat = "Excel";
        UseCustomLocation = false;
        CustomOutputLocation = string.Empty;
        IsTableSelectionPopupOpen = false;
        StatusMessage = "Form reset";
        UpdateRecentActivity("RESET: Form cleared and ready", "🔄");
        RefreshCommandStates();
    }

    private void ShowTableSelectionPopup()
    {
        if (SelectedStoredProcedure == null)
        {
            StatusMessage = "Please select a stored procedure first";
            return;
        }
        
        IsTableSelectionPopupOpen = true;
        StatusMessage = "Table selection popup opened";
    }

    private void CloseTableSelectionPopup()
    {
        IsTableSelectionPopupOpen = false;
        var selectedCount = AvailableOutputTables.Count(t => t.IsSelected);
        StatusMessage = $"Selected {selectedCount} tables for export";
    }

    private SettingsWindow? _settingsWindow;

    private async Task ShowSettingsAsync()
    {
        try
        {
            // Prevent multiple settings windows
            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                _settingsWindow.Activate();
                return;
            }

            var settingsViewModel = new SettingsViewModel(_configurationService, _databaseService);
            _settingsWindow = new SettingsWindow(settingsViewModel);
            
            // Handle window closing to clear reference
            _settingsWindow.Closed += (sender, e) => 
            {
                _settingsWindow = null;
                // Refresh connection status when settings window closes
                _ = UpdateConnectionStatusAsync();
            };
            
            _settingsWindow.Show();
            
            StatusMessage = "Settings window opened";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open settings: {ex.Message}";
            UpdateRecentActivity($"Settings error: {ex.Message}", "⚠");
        }
    }

    private async Task BrowseOutputLocationAsync()
    {
        try
        {
            // Get default path from configuration
            string defaultPath;
            if (!string.IsNullOrEmpty(CustomOutputLocation))
            {
                defaultPath = CustomOutputLocation;
            }
            else
            {
                var appSettings = await _configurationService.GetApplicationSettingsAsync();
                var configuredExportPath = appSettings.Paths.Exports;
                
                if (!Path.IsPathRooted(configuredExportPath))
                {
                    defaultPath = Path.GetFullPath(Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "..", "..", "..",
                        configuredExportPath.Replace("/", Path.DirectorySeparatorChar.ToString())));
                }
                else
                {
                    defaultPath = configuredExportPath.Replace("/", Path.DirectorySeparatorChar.ToString());
                }
            }
            
            // PowerShell script to show folder picker dialog
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
                    CustomOutputLocation = result;
                    StatusMessage = $"Output location set to: {result}";
                }
                else if (result == "CANCELLED")
                {
                    // User cancelled
                }
                else
                {
                    // Fallback: create and set default path
                    if (!Directory.Exists(defaultPath))
                    {
                        Directory.CreateDirectory(defaultPath);
                    }
                    CustomOutputLocation = defaultPath;
                    StatusMessage = $"Using default location: {defaultPath}";
                }
            }
        }
        catch (Exception ex)
        {
            // Fallback to configured default path
            var appSettings = await _configurationService.GetApplicationSettingsAsync();
            var configuredExportPath = appSettings.Paths.Exports;
            
            string defaultPath;
            if (!Path.IsPathRooted(configuredExportPath))
            {
                defaultPath = Path.GetFullPath(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    configuredExportPath.Replace("/", Path.DirectorySeparatorChar.ToString())));
            }
            else
            {
                defaultPath = configuredExportPath.Replace("/", Path.DirectorySeparatorChar.ToString());
            }
            
            if (!Directory.Exists(defaultPath))
            {
                Directory.CreateDirectory(defaultPath);
            }
            CustomOutputLocation = defaultPath;
            StatusMessage = "Error with folder picker, using configured default location";
            if (_loggingService != null)
                await _loggingService.LogErrorAsync("Error opening folder picker", ex);
        }
    }

    private async void ViewLogs()
    {
        try
        {
            // Get log directory from configuration
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
                    // Open log file with default text editor
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = latestLogFile,
                        UseShellExecute = true
                    });
                    
                    StatusMessage = "Opened latest log file";
                }
                else
                {
                    // Open log directory
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = logDirectory,
                        UseShellExecute = true
                    });
                    
                    StatusMessage = "Opened logs directory";
                }
            }
            else
            {
                StatusMessage = "Logs directory not found";
            }
        }
    catch (Exception ex)
    {
        StatusMessage = $"Failed to open logs: {ex.Message}";
    }
}    private void UpdateRecentActivity(string message, string status = "✓")
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var mode = CurrentMode == OperationMode.Export ? "EXP" : "IMP";
        
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
    /// Show user-friendly error message with suggested actions
    /// </summary>
    private void ShowError(string title, string message, List<string> suggestions)
    {
        var fullMessage = message;
        if (suggestions.Any())
        {
            fullMessage += "\n\nSuggested actions:\n" + string.Join("\n", suggestions.Select(s => $"• {s}"));
        }

        StatusMessage = $"❌ {title}: {message}";
        UpdateRecentActivity($"ERROR - {title}: {message}", "⚠");
        
        // For now, just update the status. In a real application, you might show a dialog
        // TODO: Implement actual error dialog window
    }
    
    // Menu Methods
    private void ExitApplication()
    {
        try
        {
            // Note: In a real application, you might want to check for unsaved work
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            StatusMessage = "Error exiting application";
        }
    }
    
    private void CopyOutputLocation()
    {
        try
        {
            if (!string.IsNullOrEmpty(CustomOutputLocation))
            {
                // Note: Clipboard functionality would require additional setup in Avalonia
                StatusMessage = $"Output location: {CustomOutputLocation}";
            }
            else
            {
                StatusMessage = "No output location set";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Error accessing output location";
        }
    }
    
    private void RefreshData()
    {
        try
        {
            _ = LoadStoredProceduresAsync();
            _ = LoadOutputTablesAsync();
            StatusMessage = "Data refreshed successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error refreshing data";
        }
    }
    
    private void ShowUserGuide()
    {
        try
        {
            StatusMessage = "User Guide: Use the dropdown menus to select procedures, configure output settings, and click Start to process data.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error showing user guide";
            UpdateRecentActivity($"Error showing user guide: {ex.Message}", "⚠");
        }
    }
    
    private void ShowKeyboardShortcuts()
    {
        try
        {
            StatusMessage = "Shortcuts: F1=Help, F5=Refresh, Ctrl+R=Reset, Ctrl+,=Settings, Ctrl+L=Logs, Alt+F4=Exit";
        }
        catch (Exception ex)
        {
            UpdateRecentActivity($"Error showing shortcuts: {ex.Message}", "⚠");
        }
    }
    
    private void ShowAbout()
    {
        try
        {
            StatusMessage = "TradeData Studio v1.0.0 - Professional Data Processing Platform for Trade Data Analysis";
        }
        catch (Exception ex)
        {
            UpdateRecentActivity($"Error showing about information: {ex.Message}", "⚠");
        }
    }

    private void StopOperation()
    {
        try
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                StatusMessage = "⏹️ Operation cancelled by user";
                _loggingService.LogMainAsync("User requested operation cancellation");
            }
            else
            {
                StatusMessage = "No operation in progress";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Error stopping operation";
            UpdateRecentActivity($"Error during stop: {ex.Message}", "⚠");
        }
    }

    private bool CanStop()
    {
        return _cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested;
    }

    private void RefreshCommandStates()
    {
        ((AsyncRelayCommand)ExecuteCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)ExportCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)StartCommand).NotifyCanExecuteChanged();
        ((RelayCommand)StopCommand).NotifyCanExecuteChanged();
    }
}