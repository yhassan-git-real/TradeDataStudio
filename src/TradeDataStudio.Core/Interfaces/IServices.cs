using System.Data;
using TradeDataStudio.Core.Models;

namespace TradeDataStudio.Core.Interfaces;

public interface IConfigurationService
{
    Task<ApplicationSettings> GetApplicationSettingsAsync();
    Task<DatabaseConfiguration> GetDatabaseConfigurationAsync();
    Task<List<StoredProcedureDefinition>> GetStoredProceduresAsync(OperationMode mode);
    Task<List<TableDefinition>> GetTablesAsync(OperationMode mode);
    Task SaveDatabaseConfigurationAsync(DatabaseConfiguration config);
    Task<bool> ValidateConfigurationAsync();
}

public interface IDatabaseService
{
    Task<bool> TestConnectionAsync();
    Task<ConnectionTestResult> TestConnectionDetailedAsync();
    Task<ExecutionResult> ExecuteStoredProcedureAsync(string procedureName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
    Task<DataTable> QueryTableAsync(string tableName, CancellationToken cancellationToken = default);
    Task<List<string>> GetAvailableTablesAsync(OperationMode mode);
    Task<int> GetTableRecordCountAsync(string tableName);
}

public interface IExportService
{
    Task<ExportResult> ExportToExcelAsync(string tableName, string outputPath, DataTable data, OperationMode mode = OperationMode.Export, string startPeriod = "", string endPeriod = "", int tableSequence = 1, bool isDirectDownload = false, CancellationToken cancellationToken = default);
    Task<ExportResult> ExportToCsvAsync(string tableName, string outputPath, DataTable data, OperationMode mode = OperationMode.Export, string startPeriod = "", string endPeriod = "", int tableSequence = 1, bool isDirectDownload = false, CancellationToken cancellationToken = default);
    Task<ExportResult> ExportToTextAsync(string tableName, string outputPath, DataTable data, OperationMode mode = OperationMode.Export, string startPeriod = "", string endPeriod = "", int tableSequence = 1, bool isDirectDownload = false, CancellationToken cancellationToken = default);
    Task<List<ExportResult>> ExportAllTablesAsync(List<string> tableNames, ExportFormat format, string outputDirectory, IDatabaseService? databaseService = null, string startPeriod = "", string endPeriod = "", OperationMode mode = OperationMode.Export, CancellationToken cancellationToken = default, Func<string, Task<bool>>? zeroRecordPromptFunc = null, bool isDirectDownload = false);
    string GenerateFileName(string tableName, ExportFormat format, OperationMode mode, string startPeriod = "", string endPeriod = "", int tableSequence = 1, bool isDirectDownload = false);
}

public interface ILoggingService
{
    Task LogMainAsync(string message, LogLevel level = LogLevel.Information);
    Task LogSuccessAsync(string message, OperationMode mode, Dictionary<string, object>? metadata = null);
    Task LogErrorAsync(string message, Exception? exception = null, OperationMode? mode = null);
    Task LogExecutionAsync(string storedProcedure, Dictionary<string, object> parameters, ExecutionResult result, OperationMode mode);
    Task LogExportAsync(ExportResult exportResult, OperationMode mode);
}

public interface IValidationService
{
    ValidationResult ValidateStoredProcedureParameters(StoredProcedureDefinition procedure, Dictionary<string, object> parameters);
    ValidationResult ValidateTableName(string tableName, OperationMode mode);
    ValidationResult ValidateExportPath(string path);
    ValidationResult ValidateDatabaseConfiguration(DatabaseConfiguration config);
}

public interface IStoredProcedureValidator
{
    Task<StoredProcedureValidationResult> ValidateStoredProcedureAsync(string storedProcedureName);
}

public enum LogLevel
{
    Information,
    Warning,
    Error,
    Success
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    
    public void AddError(string error) => Errors.Add(error);
    public void AddWarning(string warning) => Warnings.Add(warning);
}