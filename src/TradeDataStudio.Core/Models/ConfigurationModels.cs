namespace TradeDataStudio.Core.Models;

public enum OperationMode
{
    Export,
    Import
}

public enum ExportFormat
{
    Excel,
    CSV,
    TXT,
    JSON,
    XML
}

public class DatabaseConfiguration
{
    public string Server { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseWindowsAuthentication { get; set; } = true;
    public int ConnectionTimeout { get; set; } = 30;
    public bool TrustServerCertificate { get; set; } = true;
    public string ConnectionString => BuildConnectionString();
    
    private string BuildConnectionString()
    {
        var trustCert = TrustServerCertificate ? "TrustServerCertificate=true;" : "";
        
        if (UseWindowsAuthentication)
        {
            return $"Server={Server};Database={Database};Integrated Security=true;Connection Timeout={ConnectionTimeout};{trustCert}";
        }
        else
        {
            return $"Server={Server};Database={Database};User Id={Username};Password={Password};Connection Timeout={ConnectionTimeout};{trustCert}";
        }
    }
}

public class StoredProcedureDefinition
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ParameterDefinition> Parameters { get; set; } = new();
    public List<string> OutputTables { get; set; } = new();
}

public class ParameterDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Required { get; set; } = true;
    public string Description { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
}

public class TableDefinition
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ApplicationSettings
{
    public string Name { get; set; } = "TradeData Studio";
    public string Version { get; set; } = "1.0.0";
    public OperationMode DefaultMode { get; set; } = OperationMode.Export;
    public string LogLevel { get; set; } = "Information";
    public List<string> ExportFormats { get; set; } = new() { "Excel", "CSV", "TXT" };
    public PathSettings Paths { get; set; } = new();
    public PerformanceSettings Performance { get; set; } = new();
}

public class PathSettings
{
    public string Exports { get; set; } = "./exports/";
    public string Imports { get; set; } = "./imports/";
    public string Logs { get; set; } = "./logs/";
    public string Config { get; set; } = "./config/";
}

public class PerformanceSettings
{
    public int BatchSize { get; set; } = 50000;
    public int ExcelMaxRowsPerSheet { get; set; } = 1048576;
    public bool EnableAsyncExport { get; set; } = true;
    public int MemoryThresholdMB { get; set; } = 512;
}

public class ConnectionTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
    public int? SqlErrorNumber { get; set; }
    public TimeSpan TestDuration { get; set; }
}

public class ExecutionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RecordsAffected { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public List<string> OutputTables { get; set; } = new();
    public Exception? Exception { get; set; }
}

public class ExportResult
{
    public bool Success { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int RecordsExported { get; set; }
    public ExportFormat Format { get; set; }
    public string Message { get; set; } = string.Empty;
    public TimeSpan ElapsedTime { get; set; }
    public Exception? Exception { get; set; }
}

public class StoredProcedureValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string ProcedureName { get; set; } = string.Empty;
}