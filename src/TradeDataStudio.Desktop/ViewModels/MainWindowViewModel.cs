using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;
using TradeDataStudio.Desktop.Models;
using TradeDataStudio.Desktop.ViewModels.Parts;
using TradeDataStudio.Desktop.Commands;
using TradeDataStudio.Desktop.Services;
using TradeDataStudio.Desktop.Helpers;

namespace TradeDataStudio.Desktop.ViewModels;

/// <summary>
/// Main window ViewModel orchestrating UI state, commands, and workflow operations.
/// Refactored to delegate responsibilities to specialized components.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    // Core services
    private readonly IConfigurationService _configurationService;
    private readonly IDatabaseService _databaseService;
    private readonly IExportService _exportService;
    private readonly ILoggingService _loggingService;

    // Extracted sub-viewmodels
    private readonly ConnectionStatusViewModel _connectionStatus;
    private readonly OperationModeViewModel _operationMode;
    private readonly ActivityLogViewModel _activityLog;
    private readonly TableSelectionViewModel _tableSelection;

    // Service orchestrators
    private readonly WorkflowOrchestrator _workflowOrchestrator;
    private readonly ExportValidationService _validationService;
    private readonly OutputPathResolver _pathResolver;

    // Command handlers
    private readonly MenuCommandHandler _menuCommands;
    private readonly WorkflowCommandHandler _workflowCommands;

    // UI State Properties
    [ObservableProperty]
    private string _startPeriod = string.Empty;

    [ObservableProperty]
    private string _endPeriod = string.Empty;

    [ObservableProperty]
    private string _selectedExportFormat = "Excel";

    [ObservableProperty]
    private string _customOutputLocation = "";

    [ObservableProperty]
    private bool _useCustomLocation = false;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isOperationInProgress = false;

    [ObservableProperty]
    private double _progressValue = 0;

    [ObservableProperty]
    private string _currentOperationStatus = "";

    [ObservableProperty]
    private string _selectTablesButtonText = "Select Tables...";

    [ObservableProperty]
    private string _selectTablesButtonTooltip = "Click to select which output tables to include in the export";

    // ... existing code ...
    public ObservableCollection<string> AvailableExportFormats { get; } = new() { "Excel", "CSV", "TXT" };

    // Delegated properties (expose sub-viewmodel properties for XAML bindings)
    public bool IsExportMode
    {
        get => _operationMode.IsExportMode;
        set
        {
            if (_operationMode.IsExportMode != value)
            {
                _operationMode.IsExportMode = value;
                OnPropertyChanged(nameof(IsExportMode));
                OnPropertyChanged(nameof(CurrentMode));
                OnOperationModeChanged();
                // Mode change will trigger through event handler
            }
        }
    }

    public bool IsImportMode
    {
        get => _operationMode.IsImportMode;
        set
        {
            if (_operationMode.IsImportMode != value)
            {
                _operationMode.IsImportMode = value;
                OnPropertyChanged(nameof(IsImportMode));
                OnPropertyChanged(nameof(CurrentMode));
                OnOperationModeChanged();
                // Mode change will trigger through event handler
            }
        }
    }

    public StoredProcedureDefinition? SelectedStoredProcedure
    {
        get => _operationMode.SelectedStoredProcedure;
        set
        {
            Console.WriteLine($"\n>>> SelectedStoredProcedure SETTER called <<<");
            Console.WriteLine($"    Current value: {_operationMode.SelectedStoredProcedure?.DisplayName ?? "NULL"}");
            Console.WriteLine($"    New value: {value?.DisplayName ?? "NULL"}");
            Console.WriteLine($"    Are they different? {_operationMode.SelectedStoredProcedure != value}");
            
            if (_operationMode.SelectedStoredProcedure != value)
            {
                Console.WriteLine($"    ✅ Values are different - updating...");
                _operationMode.SelectedStoredProcedure = value;
                Console.WriteLine($"    🔔 Raising PropertyChanged for SelectedStoredProcedure");
                OnPropertyChanged(nameof(SelectedStoredProcedure));
                Console.WriteLine($"    ✅ PropertyChanged raised - event handler will trigger filtering");
                // NOTE: Filtering will be triggered by the event handler subscription at line 270
                // DO NOT manually call OnStoredProcedureChangedAsync() here to avoid duplicate execution
            }
            else
            {
                Console.WriteLine($"    ❌ Values are the same - no update needed");
            }
        }
    }

    public ObservableCollection<StoredProcedureDefinition> AvailableStoredProcedures
        => _operationMode.AvailableStoredProcedures;

    public TableDefinition? SelectedOutputTable
    {
        get => _tableSelection.SelectedOutputTable;
        set
        {
            if (_tableSelection.SelectedOutputTable != value)
            {
                _tableSelection.SelectedOutputTable = value;
                OnPropertyChanged(nameof(SelectedOutputTable));
            }
        }
    }

    public ObservableCollection<SelectableTableDefinition> AvailableOutputTables
        => _tableSelection.AvailableOutputTables;

    public bool IsTableSelectionPopupOpen
    {
        get => _tableSelection.IsTableSelectionPopupOpen;
        set
        {
            Console.WriteLine($"\n>>> IsTableSelectionPopupOpen SETTER called <<<");
            Console.WriteLine($"    Current value: {_tableSelection.IsTableSelectionPopupOpen}");
            Console.WriteLine($"    New value: {value}");
            
            if (_tableSelection.IsTableSelectionPopupOpen != value)
            {
                Console.WriteLine($"    ✅ Values are different - updating...");
                _tableSelection.IsTableSelectionPopupOpen = value;
                Console.WriteLine($"    🔔 Raising PropertyChanged for IsTableSelectionPopupOpen");
                OnPropertyChanged(nameof(IsTableSelectionPopupOpen));
                Console.WriteLine($"    PropertyChanged raised successfully");
            }
            else
            {
                Console.WriteLine($"    ❌ Values are the same - no update needed");
            }
        }
    }

    public ObservableCollection<ActivityLog> ActivityLogs => _activityLog.ActivityLogs;

    public string RecentActivity
    {
        get => _activityLog.RecentActivity;
        set => _activityLog.RecentActivity = value;
    }

    public string ConnectionStatus
    {
        get => _connectionStatus.ConnectionStatus;
        set => _connectionStatus.ConnectionStatus = value;
    }

    public string ConnectedServer => _connectionStatus.ConnectedServer;
    public string ConnectedDatabase => _connectionStatus.ConnectedDatabase;
    public string ConnectedUser => _connectionStatus.ConnectedUser;

    public OperationMode CurrentMode => _operationMode.CurrentMode;

    public bool IsStoredProcedureValidationError => _operationMode.IsStoredProcedureValidationError;
    public string StoredProcedureValidationErrorMessage => _operationMode.StoredProcedureValidationErrorMessage;

    // Commands (exposed from command handlers)
    public ICommand ExecuteCommand => _workflowCommands.ExecuteCommand;
    public ICommand ExportCommand => _workflowCommands.ExportCommand;
    public ICommand StartCommand => _workflowCommands.StartCommand;
    public ICommand StopCommand => _workflowCommands.StopCommand;
    public ICommand ResetCommand => _workflowCommands.ResetCommand;
    public ICommand SettingsCommand => _menuCommands.SettingsCommand;
    public ICommand ViewLogsCommand => _menuCommands.ViewLogsCommand;
    public ICommand BrowseOutputLocationCommand => _workflowCommands.BrowseOutputLocationCommand;
    public ICommand ShowTableSelectionPopupCommand => _workflowCommands.ShowTableSelectionPopupCommand;
    public ICommand CloseTableSelectionPopupCommand => _workflowCommands.CloseTableSelectionPopupCommand;
    public ICommand ClearActivityLogsCommand => _activityLog.ClearActivityLogsCommand;
    
    // Menu Commands
    public ICommand ExitCommand => _menuCommands.ExitCommand;
    public ICommand CopyOutputLocationCommand => _menuCommands.CopyOutputLocationCommand;
    public ICommand RefreshCommand => _menuCommands.RefreshCommand;
    public ICommand ShowUserGuideCommand => _menuCommands.ShowUserGuideCommand;
    public ICommand ShowShortcutsCommand => _menuCommands.ShowShortcutsCommand;
    public ICommand ShowAboutCommand => _menuCommands.ShowAboutCommand;

    // Parameterless constructor for XAML designer
    public MainWindowViewModel() : this(null!, null!, null!, null!, null!)
    {
    }

    public MainWindowViewModel(
        IConfigurationService configurationService,
        IDatabaseService databaseService,
        IExportService exportService,
        ILoggingService loggingService,
        IStoredProcedureValidator procedureValidator)
    {
        _configurationService = configurationService;
        _databaseService = databaseService;
        _exportService = exportService;
        _loggingService = loggingService;

        // Initialize sub-viewmodels
        _connectionStatus = new ConnectionStatusViewModel(configurationService, databaseService);
        _operationMode = new OperationModeViewModel(configurationService, loggingService, procedureValidator);
        _activityLog = new ActivityLogViewModel();
        _tableSelection = new TableSelectionViewModel(configurationService, loggingService);
        
        // Hook into table selection changes to update footer status
        _tableSelection.SelectionChanged += (s, e) => UpdateFooterStatusMessage();

        // Wire up property change notifications from ConnectionStatusViewModel
        _connectionStatus.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ConnectionStatusViewModel.ConnectionStatus))
                OnPropertyChanged(nameof(ConnectionStatus));
            else if (e.PropertyName == nameof(ConnectionStatusViewModel.ConnectedServer))
                OnPropertyChanged(nameof(ConnectedServer));
            else if (e.PropertyName == nameof(ConnectionStatusViewModel.ConnectedDatabase))
                OnPropertyChanged(nameof(ConnectedDatabase));
            else if (e.PropertyName == nameof(ConnectionStatusViewModel.ConnectedUser))
                OnPropertyChanged(nameof(ConnectedUser));
        };

        // Wire up property change notifications from OperationModeViewModel
        _operationMode.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(OperationModeViewModel.IsStoredProcedureValidationError))
                OnPropertyChanged(nameof(IsStoredProcedureValidationError));
            else if (e.PropertyName == nameof(OperationModeViewModel.StoredProcedureValidationErrorMessage))
                OnPropertyChanged(nameof(StoredProcedureValidationErrorMessage));
        };

        // Initialize services
        _pathResolver = new OutputPathResolver(configurationService);
        _validationService = new ExportValidationService(databaseService, loggingService);
        _workflowOrchestrator = new WorkflowOrchestrator(
            databaseService, exportService, loggingService, _pathResolver, _validationService);

        // Initialize command handlers
        _menuCommands = new MenuCommandHandler(            configurationService, databaseService, loggingService,
            msg => StatusMessage = msg,
            (msg, status) => UpdateRecentActivity(msg, status),
            async () => await RefreshDataAsync(),
            async () => await _connectionStatus.UpdateConnectionStatusAsync());

        _workflowCommands = new WorkflowCommandHandler(
            _workflowOrchestrator, configurationService, _pathResolver,
            () => CanExecute(),
            () => CanExport(),
            async () => await ExecuteStoredProcedureAsync(),
            async () => await ExportDataAsync(),
            async () => await StartWorkflowAsync(),
            () => StopOperation(),
            () => ResetForm(),
            async () => await BrowseOutputLocationAsync(),
            () => ShowTableSelectionPopup(),
            () => CloseTableSelectionPopup(),
            () => RefreshCommandStates());

        // Wire up event handlers for sub-viewmodels
        _operationMode.ModeChanged += async (s, e) => await OnModeChangedAsync();
        _operationMode.StoredProcedureChanged += async (s, e) => await OnStoredProcedureChangedAsync();
        _tableSelection.SelectionChanged += (s, e) => 
        {
            RefreshCommandStates();
            UpdateSelectTablesButtonState();
        };

        // Initialize with real data via DI - only if services are available (not design time)
        if (_configurationService != null && _loggingService != null)
        {
            _ = InitializeAsync();
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            await _loggingService.LogMainAsync("Starting MainWindowViewModel initialization...");
            
            // Load stored procedures and tables
            await _operationMode.LoadStoredProceduresAsync();
            await _tableSelection.LoadOutputTablesAsync(CurrentMode);
            
            // Automatically test database connection on startup
            await _loggingService.LogMainAsync("Testing database connection on startup...");
            await _connectionStatus.UpdateConnectionStatusAsync();
            
            // Notify UI of connection status updates
            OnPropertyChanged(nameof(ConnectionStatus));
            OnPropertyChanged(nameof(ConnectedServer));
            OnPropertyChanged(nameof(ConnectedDatabase));
            OnPropertyChanged(nameof(ConnectedUser));
            
            // Load default operation mode
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

    private async Task OnModeChangedAsync()
    {
        try
        {
            SelectedStoredProcedure = null;
            SelectedOutputTable = null;

            await _operationMode.LoadStoredProceduresAsync();
            await _tableSelection.LoadOutputTablesAsync(CurrentMode);

            var modeText = IsExportMode ? "Export" : "Import";
            StatusMessage = $"Switched to {modeText} mode";
            OnOperationModeChanged();
            RefreshCommandStates();
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Failed to change mode", ex);
        }
    }

    private async Task OnStoredProcedureChangedAsync()
    {
        Console.WriteLine($"\n=== OnStoredProcedureChangedAsync TRIGGERED ===");
        Console.WriteLine($"  SelectedStoredProcedure: {SelectedStoredProcedure?.DisplayName ?? "NULL"}");
        Console.WriteLine($"  CurrentMode: {CurrentMode}");
        
        // Validate the selected stored procedure
        Console.WriteLine($"  Validating stored procedure...");
        await _operationMode.ValidateSelectedProcedureAsync();
        
        if (_operationMode.IsStoredProcedureValidationError)
        {
            Console.WriteLine($"  ❌ Validation failed: {_operationMode.StoredProcedureValidationErrorMessage}");
            OnPropertyChanged(nameof(IsExportMode)); // Trigger UI update for validation display
            RefreshCommandStates();
            return;
        }
        
        Console.WriteLine($"  ✅ Validation passed");
        Console.WriteLine($"  Calling FilterOutputTablesForSelectedProcedureAsync...");
        
        await _tableSelection.FilterOutputTablesForSelectedProcedureAsync(
            SelectedStoredProcedure, CurrentMode);
        
        Console.WriteLine($"  Filtering complete. AvailableOutputTables.Count: {AvailableOutputTables.Count}");
        Console.WriteLine($"  Refreshing command states...");
        RefreshCommandStates();
        Console.WriteLine($"=== OnStoredProcedureChangedAsync COMPLETED ===\n");
    }

    partial void OnStartPeriodChanged(string value) => RefreshCommandStates();
    partial void OnEndPeriodChanged(string value) => RefreshCommandStates();
    partial void OnIsOperationInProgressChanged(bool value) => RefreshCommandStates();

    private bool CanExecute()
    {
        return !IsOperationInProgress && 
               SelectedStoredProcedure != null && 
               !_operationMode.IsStoredProcedureValidationError &&
               !string.IsNullOrWhiteSpace(StartPeriod) && 
               !string.IsNullOrWhiteSpace(EndPeriod);
    }

    private bool CanExport()
    {
        return !IsOperationInProgress && 
               (SelectedOutputTable != null || _tableSelection.GetSelectedTables().Any());
    }
    
    /// <summary>
    /// Called when operation mode changes (Export <-> Import).
    /// Updates footer status message to reflect the new mode.
    /// </summary>
    private void OnOperationModeChanged()
    {
        UpdateFooterStatusMessage();
    }

    private async Task ExecuteStoredProcedureAsync(bool isCalledFromWorkflow = false)
    {
        if (SelectedStoredProcedure == null) return;

        await _workflowCommands.ExecuteStoredProcedureWithCancellationAsync(
            SelectedStoredProcedure, StartPeriod, EndPeriod, CurrentMode,
            v => IsOperationInProgress = v,
            v => ProgressValue = v,
            msg => StatusMessage = msg,
            status => CurrentOperationStatus = status,
            (msg, icon) => UpdateRecentActivity(msg, icon),
            isCalledFromWorkflow);
    }

    private async Task ExportDataAsync()
    {
        var tablesToExport = GetTablesToExport();
        if (!tablesToExport.Any()) return;

        var format = Enum.Parse<ExportFormat>(SelectedExportFormat);

        await _workflowCommands.ExportDataWithValidationAsync(
            tablesToExport, format, StartPeriod, EndPeriod, CurrentMode,
            UseCustomLocation, CustomOutputLocation,
            v => IsOperationInProgress = v,
            v => ProgressValue = v,
            msg => StatusMessage = msg,
            status => CurrentOperationStatus = status,
            (msg, icon) => UpdateRecentActivity(msg, icon),
            (title, msg, suggestions) => ShowError(title, msg, suggestions));
    }

    private async Task StartWorkflowAsync()
    {
        if (SelectedStoredProcedure == null) return;

        var tablesToExport = GetTablesToExport();
        
        // Auto-select all tables if none selected
        if (!tablesToExport.Any())
        {
            _tableSelection.SelectAllTables();
            tablesToExport = _tableSelection.GetSelectedTables();
            if (tablesToExport.Any())
            {
                UpdateRecentActivity($"Auto-selected all {tablesToExport.Count} output table(s)", "📝");
            }
        }

        var format = Enum.Parse<ExportFormat>(SelectedExportFormat);

        try
        {
            IsOperationInProgress = true;
            StatusMessage = "Starting workflow...";
            CurrentOperationStatus = $"⏳ Workflow in progress: {SelectedStoredProcedure.DisplayName}...";
            
            UpdateRecentActivity($"START: {SelectedStoredProcedure.DisplayName} | {tablesToExport.Count} table(s) selected", "🚀");
            
            // Create a cancellation token source for the workflow and pass it via _workflowCommands
            var cancellationTokenSource = new CancellationTokenSource();
            _workflowCommands.SetWorkflowCancellationTokenSource(cancellationTokenSource);
            
            var result = await _workflowOrchestrator.ExecuteWorkflowAsync(
                SelectedStoredProcedure, tablesToExport, format, StartPeriod, EndPeriod,
                CurrentMode, UseCustomLocation, CustomOutputLocation,
                v => ProgressValue = v,
                (msg, icon) => UpdateRecentActivity(msg, icon),
                cancellationTokenSource.Token);

            CurrentOperationStatus = "";
            
            if (result.Success)
            {
                StatusMessage = "Workflow completed successfully";
            }
            else
            {
                StatusMessage = $"Workflow failed: {result.ErrorMessage}";
            }
        }
        catch (OperationCanceledException)
        {
            CurrentOperationStatus = "";
            StatusMessage = "⏹️ Workflow cancelled by user";
            UpdateRecentActivity("Workflow cancelled by user", "⏹");
        }
        catch (Exception ex)
        {
            CurrentOperationStatus = "";
            StatusMessage = $"Workflow failed: {ex.Message}";
            UpdateRecentActivity($"Workflow error: {ex.Message}", "⚠");
        }
        finally
        {
            IsOperationInProgress = false;
        }
    }

    private void StopOperation()
    {
        _workflowCommands.StopOperation(
            msg => StatusMessage = msg,
            (msg, icon) => UpdateRecentActivity(msg, icon));
    }

    private void ResetForm()
    {
        SelectedStoredProcedure = null;
        SelectedOutputTable = null;
        _tableSelection.ClearSelection();
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
        Console.WriteLine($"\n=== ShowTableSelectionPopup CALLED ===");
        Console.WriteLine($"  SelectedStoredProcedure: {SelectedStoredProcedure?.DisplayName ?? "NULL"}");
        Console.WriteLine($"  AvailableOutputTables.Count: {AvailableOutputTables.Count}");
        
        if (SelectedStoredProcedure == null)
        {
            Console.WriteLine($"  ❌ No stored procedure selected - cannot show popup");
            StatusMessage = "Please select a stored procedure first";
            return;
        }
        
        Console.WriteLine($"  ✅ Stored procedure selected: {SelectedStoredProcedure.DisplayName}");
        Console.WriteLine($"  📊 Tables available: {AvailableOutputTables.Count}");
        
        // Log each available table
        foreach (var table in AvailableOutputTables)
        {
            Console.WriteLine($"    - {table.DisplayName} (Selected: {table.IsSelected})");
        }
        
        Console.WriteLine($"  🚀 Setting IsTableSelectionPopupOpen = true...");
        IsTableSelectionPopupOpen = true;
        Console.WriteLine($"  ✅ IsTableSelectionPopupOpen is now: {IsTableSelectionPopupOpen}");
        
        StatusMessage = "Table selection popup opened";
        Console.WriteLine($"=== ShowTableSelectionPopup COMPLETED ===\n");
    }

    private void CloseTableSelectionPopup()
    {
        Console.WriteLine($"\n=== CloseTableSelectionPopup CALLED ===");
        Console.WriteLine($"  Setting IsTableSelectionPopupOpen = false...");
        IsTableSelectionPopupOpen = false;
        Console.WriteLine($"  IsTableSelectionPopupOpen is now: {IsTableSelectionPopupOpen}");
        
        UpdateFooterStatusMessage();
        Console.WriteLine($"=== CloseTableSelectionPopup COMPLETED ===\n");
    }

    /// <summary>
    /// Updates the footer status message based on current table selection.
    /// Shows specific table name for single selection, "Selected X tables" for multiple selections.
    /// Includes operation type (export/import).
    /// </summary>
    private void UpdateFooterStatusMessage()
    {
        var selectedTables = _tableSelection.GetSelectedTables();
        var operationMode = IsExportMode ? "export" : "import";
        
        if (selectedTables.Count == 0)
        {
            StatusMessage = "Ready";
        }
        else if (selectedTables.Count == 1)
        {
            StatusMessage = $"Selected: {selectedTables[0].DisplayName} for {operationMode}";
        }
        else
        {
            StatusMessage = $"Selected {selectedTables.Count} tables for {operationMode}";
        }
    }

    /// <summary>
    /// Updates the Select Tables button text and tooltip based on current table selection.
    /// Button text changes from "Select Tables..." to "Table Selected" (singular) or "Tables Selected" (plural).
    /// Tooltip displays the names of all selected tables.
    /// </summary>
    private void UpdateSelectTablesButtonState()
    {
        var selectedTables = _tableSelection.GetSelectedTables();
        
        if (selectedTables.Count == 0)
        {
            SelectTablesButtonText = "Select Tables...";
            SelectTablesButtonTooltip = "Click to select which output tables to include in the export";
        }
        else if (selectedTables.Count == 1)
        {
            SelectTablesButtonText = "Table Selected";
            SelectTablesButtonTooltip = $"Selected table: {selectedTables[0].DisplayName}";
        }
        else
        {
            SelectTablesButtonText = "Tables Selected";
            var tableNames = string.Join(", ", selectedTables.Select(t => t.DisplayName));
            SelectTablesButtonTooltip = $"Selected tables: {tableNames}";
        }
    }

    private async Task BrowseOutputLocationAsync()
    {
        await _workflowCommands.BrowseOutputLocationAsync(
            CustomOutputLocation,
            location => CustomOutputLocation = location,
            msg => StatusMessage = msg);
    }

    private async Task RefreshDataAsync()
    {
        await _operationMode.LoadStoredProceduresAsync();
        await _tableSelection.LoadOutputTablesAsync(CurrentMode);
    }

    private List<TableDefinition> GetTablesToExport()
    {
        var selected = _tableSelection.GetSelectedTables();
        return selected.Any() ? selected : 
               (SelectedOutputTable != null ? new List<TableDefinition> { SelectedOutputTable } : new List<TableDefinition>());
    }

    private void UpdateRecentActivity(string message, string status = "✓")
    {
        _activityLog.UpdateRecentActivity(message, status, CurrentMode);
    }

    private void ShowError(string title, string message, List<string> suggestions)
    {
        var fullMessage = message;
        if (suggestions.Any())
        {
            fullMessage += "\n\nSuggested actions:\n" + string.Join("\n", suggestions.Select(s => $"• {s}"));
        }

        StatusMessage = $"❌ {title}: {message}";
        UpdateRecentActivity($"ERROR - {title}: {message}", "⚠");
    }

    private void RefreshCommandStates()
    {
        ((AsyncRelayCommand)ExecuteCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)ExportCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)StartCommand).NotifyCanExecuteChanged();
        ((RelayCommand)StopCommand).NotifyCanExecuteChanged();
    }
}


