using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;
using TradeDataStudio.Desktop.Helpers;
using TradeDataStudio.Desktop.Services;

namespace TradeDataStudio.Desktop.Services;

/// <summary>
/// Orchestrates complex workflow operations including stored procedure execution and data export.
/// </summary>
public class WorkflowOrchestrator
{
    private readonly IDatabaseService _databaseService;
    private readonly IExportService _exportService;
    private readonly ILoggingService _loggingService;
    private readonly OutputPathResolver _pathResolver;
    private readonly ExportValidationService _validationService;
    private readonly DialogService _dialogService;

    public WorkflowOrchestrator(
        IDatabaseService databaseService,
        IExportService exportService,
        ILoggingService loggingService,
        OutputPathResolver pathResolver,
        ExportValidationService validationService)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _dialogService = new DialogService();
    }

    /// <summary>
    /// Executes a stored procedure with the provided parameters.
    /// </summary>
    public async Task<ExecutionResult> ExecuteStoredProcedureAsync(
        StoredProcedureDefinition storedProcedure,
        string startPeriod,
        string endPeriod,
        OperationMode currentMode,
        Action<double> updateProgress,
        Action<string, string> logActivity,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        // Build parameters dynamically from stored procedure definition
        var parameters = BuildParameters(storedProcedure, startPeriod, endPeriod);
        
        var paramInfo = string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"));
        logActivity($"EXECUTE: {storedProcedure.DisplayName} | Params: {paramInfo}", "▶");
        updateProgress(25);

        var result = await _databaseService.ExecuteStoredProcedureAsync(
            storedProcedure.Name, parameters, cancellationToken);

        updateProgress(100);

        if (result.Success)
        {
            logActivity($"SUCCESS: {storedProcedure.DisplayName} | {result.RecordsAffected:N0} records | {result.ExecutionTime.TotalSeconds:F1}s", "✓");
            await _loggingService.LogExecutionAsync(storedProcedure.Name, parameters, result, currentMode);
            // Add separator after successful SP execution
            await _loggingService.LogExecutionSeparatorAsync();
        }
        else
        {
            logActivity($"Error: {result.Message}", "⚠");
            await _loggingService.LogErrorAsync($"Stored procedure execution failed: {result.Message}", result.Exception, currentMode);
        }

        return result;
    }

    /// <summary>
    /// Exports data tables to the specified format and location.
    /// </summary>
    public async Task<List<ExportResult>> ExportDataAsync(
        List<TableDefinition> tablesToExport,
        ExportFormat format,
        string startPeriod,
        string endPeriod,
        OperationMode currentMode,
        bool useCustomLocation,
        string customOutputLocation,
        Action<double> updateProgress,
        Action<string, string> logActivity,
        CancellationToken cancellationToken,
        bool isDirectDownload = true)
    {
        // Validate Excel exports
        if (format == ExportFormat.Excel)
        {
            var validationResult = await _validationService.ValidateExcelExportAsync(
                tablesToExport, currentMode, logActivity);
            
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException(validationResult.ErrorMessage);
            }
        }

        var tableNamesList = string.Join(", ", tablesToExport.Select(t => t.Name));
        logActivity($"DOWNLOAD: {tablesToExport.Count} table(s) → {format} | Tables: {tableNamesList}", "📥");
        
        // Get export path
        var outputPath = await _pathResolver.ResolveOutputPathAsync(
            useCustomLocation, customOutputLocation, currentMode);
        
        var results = await _exportService.ExportAllTablesAsync(
            tablesToExport.Select(t => t.Name).ToList(), 
            format, 
            outputPath, 
            _databaseService,
            startPeriod,
            endPeriod,
            currentMode,
            cancellationToken,
            async (tableName) => await _dialogService.ShowZeroRecordPromptAsync(tableName),
            isDirectDownload);
        
        var successCount = results.Count(r => r.Success);
        var totalRecords = results.Where(r => r.Success).Sum(r => r.RecordsExported);
        
        // Log individual export results with timing
        foreach (var result in results)
        {
            if (result.Success)
            {
                var durationStr = result.ElapsedTime.TotalSeconds < 1 
                    ? $"{result.ElapsedTime.TotalMilliseconds:F0}ms" 
                    : $"{result.ElapsedTime.TotalSeconds:F2}s";
                logActivity($"Exported {result.FileName}: {result.RecordsExported:N0} records in {durationStr}", "✓");
            }
        }
        
        // Log errors
        var failedExports = results.Where(r => !r.Success).ToList();
        if (failedExports.Any())
        {
            foreach (var failed in failedExports)
            {
                var errorMsg = failed.Message?.Contains("Row out of range") == true 
                    ? "Export failed: Data too large for Excel (>1M rows). Try CSV format or filter data."
                    : $"Export failed: {failed.Message}";
                logActivity(errorMsg, "⚠");
            }
            
            // If all exports failed, throw exception to trigger cleanup
            if (successCount == 0)
            {
                var errorMessage = failedExports.Count == 1 
                    ? failedExports[0].Message 
                    : $"{failedExports.Count} table(s) failed to export";
                throw new InvalidOperationException(errorMessage);
            }
        }
        
        updateProgress(100);

        if (successCount == tablesToExport.Count)
        {
            logActivity($"Successfully exported {successCount} tables with {totalRecords} records", "✓");
        }
        else
        {
            logActivity($"Partial success: {successCount}/{tablesToExport.Count} tables exported", "⚠");
        }
        
        // Log all export results
        foreach (var result in results)
        {
            await _loggingService.LogExportAsync(result, currentMode);
            // Add separator after each export completion
            await _loggingService.LogExecutionSeparatorAsync();
        }

        return results;
    }

    /// <summary>
    /// Executes the full workflow: stored procedure execution followed by data export.
    /// </summary>
    public async Task<WorkflowResult> ExecuteWorkflowAsync(
        StoredProcedureDefinition storedProcedure,
        List<TableDefinition> tablesToExport,
        ExportFormat format,
        string startPeriod,
        string endPeriod,
        OperationMode currentMode,
        bool useCustomLocation,
        string customOutputLocation,
        Action<double> updateProgress,
        Action<string, string> logActivity,
        CancellationToken cancellationToken)
    {
        var workflowResult = new WorkflowResult();

        try
        {
            // Add separator before starting new workflow
            await _loggingService.LogExecutionSeparatorAsync();
            
            await _loggingService.LogMainAsync($"[{currentMode}] Workflow started for procedure: {storedProcedure.Name}");
            
            cancellationToken.ThrowIfCancellationRequested();
            
            // Execute stored procedure
            workflowResult.ExecutionResult = await ExecuteStoredProcedureAsync(
                storedProcedure, startPeriod, endPeriod, currentMode, 
                updateProgress, logActivity, cancellationToken);
            
            if (!workflowResult.ExecutionResult.Success)
            {
                workflowResult.Success = false;
                return workflowResult;
            }
            
            cancellationToken.ThrowIfCancellationRequested();
            
            // Export data
            if (tablesToExport.Any())
            {
                var outputPath = await _pathResolver.ResolveOutputPathAsync(
                    useCustomLocation, customOutputLocation, currentMode);
                
                await _loggingService.LogMainAsync($"[{currentMode}] Workflow: Exporting {tablesToExport.Count} selected table(s): {string.Join(", ", tablesToExport.Select(t => t.Name))}");
                
                workflowResult.ExportResults = await ExportDataAsync(
                    tablesToExport, format, startPeriod, endPeriod, currentMode,
                    useCustomLocation, customOutputLocation, updateProgress, logActivity, cancellationToken,
                    isDirectDownload: false);
                
                var successCount = workflowResult.ExportResults.Count(r => r.Success);
                var totalExportedRecords = workflowResult.ExportResults.Where(r => r.Success).Sum(r => r.RecordsExported);
                var exportedFiles = string.Join(", ", workflowResult.ExportResults.Where(r => r.Success).Select(r => r.FileName));
                
                logActivity($"WORKFLOW COMPLETE: {successCount}/{workflowResult.ExportResults.Count} tables | {totalExportedRecords:N0} records | Path: {outputPath}", "✓");
                logActivity($"Files: {exportedFiles}", "  ");
                
                await _loggingService.LogMainAsync($"[{currentMode}] Workflow completed: {successCount}/{workflowResult.ExportResults.Count} tables exported to {outputPath}");
                // Add separator after workflow completion
                await _loggingService.LogExecutionSeparatorAsync();
                
                workflowResult.Success = successCount > 0;
            }
            else
            {
                await _loggingService.LogMainAsync($"[{currentMode}] Workflow completed - no tables selected for export");
                workflowResult.Success = true;
            }
        }
        catch (OperationCanceledException)
        {
            await _loggingService.LogMainAsync($"[{currentMode}] Workflow cancelled by user");
            throw;
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync($"[{currentMode}] Workflow failed", ex, currentMode);
            workflowResult.Success = false;
            workflowResult.ErrorMessage = ex.Message;
        }

        return workflowResult;
    }

    private Dictionary<string, object> BuildParameters(
        StoredProcedureDefinition storedProcedure,
        string startPeriod,
        string endPeriod)
    {
        var parameters = new Dictionary<string, object>();
        
        if (storedProcedure.Parameters != null && storedProcedure.Parameters.Count >= 1)
        {
            var firstParam = storedProcedure.Parameters[0];
            var paramName = firstParam.Name.StartsWith("@") ? firstParam.Name : $"@{firstParam.Name}";
            
            if (firstParam.Type.ToLower().Contains("int"))
            {
                parameters[paramName] = int.Parse(startPeriod);
            }
            else
            {
                parameters[paramName] = startPeriod;
            }
        }
        
        if (storedProcedure.Parameters != null && storedProcedure.Parameters.Count >= 2)
        {
            var secondParam = storedProcedure.Parameters[1];
            var paramName = secondParam.Name.StartsWith("@") ? secondParam.Name : $"@{secondParam.Name}";
            
            if (secondParam.Type.ToLower().Contains("int"))
            {
                parameters[paramName] = int.Parse(endPeriod);
            }
            else
            {
                parameters[paramName] = endPeriod;
            }
        }
        
        return parameters;
    }
}

/// <summary>
/// Result of a complete workflow execution.
/// </summary>
public class WorkflowResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public ExecutionResult? ExecutionResult { get; set; }
    public List<ExportResult> ExportResults { get; set; } = new();
}
