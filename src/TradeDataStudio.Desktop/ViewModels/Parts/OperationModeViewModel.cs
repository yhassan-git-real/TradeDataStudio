using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;

namespace TradeDataStudio.Desktop.ViewModels.Parts;

/// <summary>
/// Manages operation mode state (Export/Import) and stored procedure selection.
/// </summary>
public partial class OperationModeViewModel : ObservableObject
{
    private readonly IConfigurationService _configurationService;
    private readonly ILoggingService _loggingService;
    private readonly IStoredProcedureValidator _procedureValidator;

    [ObservableProperty]
    private bool _isExportMode = true;

    [ObservableProperty]
    private bool _isImportMode = false;

    [ObservableProperty]
    private StoredProcedureDefinition? _selectedStoredProcedure;

    [ObservableProperty]
    private bool _isStoredProcedureValidationError = false;

    [ObservableProperty]
    private string _storedProcedureValidationErrorMessage = string.Empty;

    public ObservableCollection<StoredProcedureDefinition> AvailableStoredProcedures { get; } = new();

    public OperationMode CurrentMode => IsExportMode ? OperationMode.Export : OperationMode.Import;

    public event EventHandler? ModeChanged;
    public event EventHandler? StoredProcedureChanged;

    public OperationModeViewModel(
        IConfigurationService configurationService,
        ILoggingService loggingService,
        IStoredProcedureValidator procedureValidator)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        _procedureValidator = procedureValidator ?? throw new ArgumentNullException(nameof(procedureValidator));
    }

    /// <summary>
    /// Loads stored procedures for the current operation mode.
    /// </summary>
    public async Task LoadStoredProceduresAsync()
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

    partial void OnIsExportModeChanged(bool value)
    {
        if (value)
        {
            IsImportMode = false;
            ModeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    partial void OnIsImportModeChanged(bool value)
    {
        if (value)
        {
            IsExportMode = false;
            ModeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    partial void OnSelectedStoredProcedureChanged(StoredProcedureDefinition? value)
    {
        Console.WriteLine($"\n*** OperationModeViewModel.OnSelectedStoredProcedureChanged TRIGGERED ***");
        Console.WriteLine($"    New value: {value?.DisplayName ?? "NULL"}");
        Console.WriteLine($"    Invoking StoredProcedureChanged event...");
        StoredProcedureChanged?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"    Event invoked. Subscriber count: {StoredProcedureChanged?.GetInvocationList().Length ?? 0}");
        Console.WriteLine($"*** OperationModeViewModel.OnSelectedStoredProcedureChanged COMPLETED ***\n");
    }

    /// <summary>
    /// Validates the selected stored procedure asynchronously.
    /// Updates validation error state if procedure doesn't exist.
    /// </summary>
    public async Task ValidateSelectedProcedureAsync()
    {
        try
        {
            // Clear validation if no procedure selected
            if (SelectedStoredProcedure == null)
            {
                ClearValidationError();
                return;
            }

            // Validate the selected procedure
            var validationResult = await _procedureValidator.ValidateStoredProcedureAsync(
                SelectedStoredProcedure.Name);

            if (!validationResult.IsValid)
            {
                SetValidationError(validationResult.ErrorMessage ?? "Stored procedure validation failed");
            }
            else
            {
                ClearValidationError();
            }
        }
        catch (Exception ex)
        {
            var errorMsg = "Error validating stored procedure";
            SetValidationError(errorMsg);
            await _loggingService.LogErrorAsync($"{errorMsg}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Clears validation error state.
    /// </summary>
    public void ClearValidationError()
    {
        IsStoredProcedureValidationError = false;
        StoredProcedureValidationErrorMessage = string.Empty;
    }

    /// <summary>
    /// Sets validation error state with error message.
    /// </summary>
    private void SetValidationError(string errorMessage)
    {
        IsStoredProcedureValidationError = true;
        StoredProcedureValidationErrorMessage = errorMessage;
    }
}