using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;

namespace TradeDataStudio.Desktop.Services;

/// <summary>
/// Validates export operations, particularly checking Excel row limits and table sizes.
/// </summary>
public class ExportValidationService
{
    private readonly IDatabaseService _databaseService;
    private readonly ILoggingService _loggingService;
    private const int ExcelMaxRows = 1048575; // Excel maximum data rows (excluding header)

    public ExportValidationService(
        IDatabaseService databaseService,
        ILoggingService loggingService)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
    }

    /// <summary>
    /// Validates table sizes for Excel export. Returns validation results.
    /// </summary>
    public async Task<ExportValidationResult> ValidateExcelExportAsync(
        List<TableDefinition> tablesToExport,
        OperationMode currentMode,
        Action<string, string> logActivity)
    {
        logActivity("Validating table sizes for Excel export...", "🔍");
        
        var tableSizeIssues = new List<string>();
        
        foreach (var table in tablesToExport)
        {
            try
            {
                var rowCount = await _databaseService.GetTableRecordCountAsync(table.Name);
                
                if (rowCount > ExcelMaxRows)
                {
                    tableSizeIssues.Add($"{table.Name}: {rowCount:N0} rows (exceeds Excel limit of {ExcelMaxRows:N0})");
                }
                else if (rowCount > ExcelMaxRows * 0.9) // Warn if >90% of limit
                {
                    logActivity($"WARNING: {table.Name} has {rowCount:N0} rows (near Excel limit)", "⚠");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Failed to get row count for {table.Name}", ex, currentMode);
            }
        }
        
        if (tableSizeIssues.Any())
        {
            logActivity("VALIDATION FAILED: Excel row limit exceeded", "❌");
            foreach (var issue in tableSizeIssues)
            {
                logActivity($"  → {issue}", "⚠");
            }
            logActivity("SOLUTION: Use CSV format or filter the data in stored procedure", "💡");
            
            return new ExportValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Cannot export to Excel - {tableSizeIssues.Count} table(s) exceed Excel row limit:\n" + 
                              string.Join("\n", tableSizeIssues),
                TableIssues = tableSizeIssues,
                Suggestions = new List<string> 
                { 
                    "Switch to CSV format (supports unlimited rows)",
                    "Add date/filter parameters to stored procedure to reduce data",
                    "Split export into multiple smaller date ranges"
                }
            };
        }
        
        logActivity($"Validation passed: All {tablesToExport.Count} table(s) within Excel limits", "✓");
        
        return new ExportValidationResult { IsValid = true };
    }
}

/// <summary>
/// Result of export validation operation.
/// </summary>
public class ExportValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public List<string> TableIssues { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
}
