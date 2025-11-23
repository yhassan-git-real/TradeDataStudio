namespace TradeDataStudio.Core.Constants;

public static class AppConstants
{
    public const string ApplicationName = "TradeData Studio";
    public const string Version = "1.0.0";
    
    // Configuration file names
    public const string AppSettingsFile = "appsettings.json";
    public const string DatabaseConfigFile = "database.json";
    public const string ExportProceduresFile = "export_procedures.json";
    public const string ImportProceduresFile = "import_procedures.json";
    public const string ExportTablesFile = "export_tables.json";
    public const string ImportTablesFile = "import_tables.json";
    public const string ExportSettingsFile = "export_settings.json";
    
    // Default paths
    public const string ConfigDirectory = "config";
    public const string LogsDirectory = "logs";
    public const string ExportsDirectory = "exports";
    
    // Log file patterns
    public const string MainLogPattern = "tradedataStudio_{0:yyyyMMdd}.log";
    public const string SuccessLogPattern = "success_{0:yyyyMMdd}.log";
    public const string ErrorLogPattern = "error_{0:yyyyMMdd}.log";
    
    // Export file patterns
    public const string ExportFilePattern = "export_{0}_{1:yyyyMMdd_HHmmss}.{2}";
    public const string ImportFilePattern = "import_{0}_{1:yyyyMMdd_HHmmss}.{2}";
    
    // Database connection timeout
    public const int DefaultConnectionTimeout = 30;
    
    // Export formats
    public static readonly string[] SupportedExportFormats = { "Excel", "CSV", "TXT" };
    public static readonly string[] SupportedFileExtensions = { "xlsx", "csv", "txt" };
    
    // Parameter validation
    public const string MonthParameterPattern = @"^\d{6}$"; // YYYYMM format
}