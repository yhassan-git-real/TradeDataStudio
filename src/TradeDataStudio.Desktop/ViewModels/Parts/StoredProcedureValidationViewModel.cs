using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;

namespace TradeDataStudio.Desktop.ViewModels.Parts;

/// <summary>
/// Manages validation state for stored procedure selections.
/// Displays error messages and visual feedback when an invalid stored procedure is selected.
/// </summary>
public partial class StoredProcedureValidationViewModel : ObservableObject
{
    private readonly IStoredProcedureValidator _validator;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private bool _isValidationError = false;

    [ObservableProperty]
    private string _validationErrorMessage = string.Empty;

    [ObservableProperty]
    private string _selectedProcedureName = string.Empty;

    public StoredProcedureValidationViewModel(
        IStoredProcedureValidator validator,
        ILoggingService loggingService)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
    }

    /// <summary>
    /// Validates a stored procedure name and updates validation state.
    /// </summary>
    /// <param name="procedureName">The stored procedure name to validate</param>
    /// <returns>True if procedure exists, false if validation fails</returns>
    public async Task<bool> ValidateProcedureAsync(string? procedureName)
    {
        try
        {
            // Clear validation on null selection
            if (string.IsNullOrWhiteSpace(procedureName))
            {
                ClearValidationError();
                SelectedProcedureName = string.Empty;
                return false;
            }

            SelectedProcedureName = procedureName;

            // Validate the procedure exists in database
            var validationResult = await _validator.ValidateStoredProcedureAsync(procedureName);

            if (validationResult.IsValid)
            {
                ClearValidationError();
                await _loggingService.LogMainAsync(
                    $"Stored procedure '{procedureName}' passed validation.");
                return true;
            }
            else
            {
                SetValidationError(validationResult.ErrorMessage ?? "Validation failed");
                await _loggingService.LogErrorAsync(
                    $"Stored procedure validation failed for '{procedureName}': {validationResult.ErrorMessage}", null);
                return false;
            }
        }
        catch (Exception ex)
        {
            var errorMsg = "Failed to validate stored procedure";
            SetValidationError(errorMsg);
            await _loggingService.LogErrorAsync($"{errorMsg}: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Clears any validation error messages and resets error state.
    /// </summary>
    public void ClearValidationError()
    {
        IsValidationError = false;
        ValidationErrorMessage = string.Empty;
        SelectedProcedureName = string.Empty;
    }

    /// <summary>
    /// Sets validation error state with a specific error message.
    /// </summary>
    private void SetValidationError(string errorMessage)
    {
        IsValidationError = true;
        ValidationErrorMessage = errorMessage;
    }
}
