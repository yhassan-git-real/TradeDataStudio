using System.Text.Json;
using System.Linq;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;

namespace TradeDataStudio.Core.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly string _baseConfigPath;
    private ApplicationSettings? _applicationSettings;
    private DatabaseConfiguration? _databaseConfiguration;
    private AnimationConfiguration? _animationConfiguration;

    public ConfigurationService()
    {
        _baseConfigPath = ResolveConfigPath();
    }

    public async Task<ApplicationSettings> GetApplicationSettingsAsync()
    {
        if (_applicationSettings == null)
        {
            var appSettingsPath = Path.Combine(_baseConfigPath, "appsettings.json");
            if (File.Exists(appSettingsPath))
            {
                var json = await File.ReadAllTextAsync(appSettingsPath);
                var config = JsonSerializer.Deserialize<JsonElement>(json);
                
                _applicationSettings = new ApplicationSettings
                {
                    DefaultMode = OperationMode.Export,
                    ExportFormats = new List<string> { "Excel", "CSV", "TXT" }
                };
                
                // Parse application section
                if (config.TryGetProperty("application", out var appSection))
                {
                    if (appSection.TryGetProperty("name", out var name))
                        _applicationSettings.Name = name.GetString() ?? "TradeData Studio";
                    if (appSection.TryGetProperty("version", out var version))
                        _applicationSettings.Version = version.GetString() ?? "1.0.0";
                }
                
                // Parse paths section
                if (config.TryGetProperty("paths", out var pathsSection))
                {
                    _applicationSettings.Paths = new PathSettings();
                    if (pathsSection.TryGetProperty("exports", out var exports))
                        _applicationSettings.Paths.Exports = exports.GetString() ?? "./exports/";
                    if (pathsSection.TryGetProperty("imports", out var imports))
                        _applicationSettings.Paths.Imports = imports.GetString() ?? "./imports/";
                    if (pathsSection.TryGetProperty("logs", out var logs))
                        _applicationSettings.Paths.Logs = logs.GetString() ?? "./logs/";
                    if (pathsSection.TryGetProperty("config", out var configPath))
                        _applicationSettings.Paths.Config = configPath.GetString() ?? "./config/";
                }
                
                // Parse performance section
                if (config.TryGetProperty("performance", out var perfSection))
                {
                    _applicationSettings.Performance = new PerformanceSettings();
                    if (perfSection.TryGetProperty("batchSize", out var batchSize))
                        _applicationSettings.Performance.BatchSize = batchSize.GetInt32();
                    if (perfSection.TryGetProperty("excelMaxRowsPerSheet", out var maxRows))
                        _applicationSettings.Performance.ExcelMaxRowsPerSheet = maxRows.GetInt32();
                    if (perfSection.TryGetProperty("enableAsyncExport", out var asyncExport))
                        _applicationSettings.Performance.EnableAsyncExport = asyncExport.GetBoolean();
                    if (perfSection.TryGetProperty("memoryThresholdMB", out var memThreshold))
                        _applicationSettings.Performance.MemoryThresholdMB = memThreshold.GetInt32();
                }
            }
            else
            {
                _applicationSettings = new ApplicationSettings();
            }
        }
        
        return _applicationSettings;
    }

    public async Task<DatabaseConfiguration> GetDatabaseConfigurationAsync()
    {
        if (_databaseConfiguration == null)
        {
            var dbConfigPath = Path.Combine(_baseConfigPath, "database.json");
            if (File.Exists(dbConfigPath))
            {
                var json = await File.ReadAllTextAsync(dbConfigPath);
                var dbConfig = JsonSerializer.Deserialize<JsonElement>(json);
                
                _databaseConfiguration = new DatabaseConfiguration
                {
                    Server = dbConfig.GetProperty("server").GetString() ?? "localhost",
                    Database = dbConfig.GetProperty("database").GetString() ?? "TradeData",
                    Username = dbConfig.TryGetProperty("username", out var user) ? user.GetString() : "",
                    Password = dbConfig.TryGetProperty("password", out var pass) ? pass.GetString() : "",
                    UseWindowsAuthentication = dbConfig.TryGetProperty("useWindowsAuthentication", out var winAuth) ? winAuth.GetBoolean() : true,
                    ConnectionTimeout = dbConfig.TryGetProperty("connectionTimeout", out var timeout) ? timeout.GetInt32() : 30,
                    TrustServerCertificate = dbConfig.TryGetProperty("trustServerCertificate", out var trustCert) ? trustCert.GetBoolean() : true
                };
            }
            else
            {
                _databaseConfiguration = new DatabaseConfiguration();
            }
        }
        
        return _databaseConfiguration;
    }

    public async Task<List<StoredProcedureDefinition>> GetStoredProceduresAsync(OperationMode mode)
    {
        var fileName = mode == OperationMode.Export ? "export_procedures.json" : "import_procedures.json";
        var configPath = Path.Combine(_baseConfigPath, fileName);
        
        Console.WriteLine($"[ConfigService] Loading procedures from: {configPath}");
        
        if (!File.Exists(configPath))
        {
            Console.WriteLine($"[ConfigService] ERROR: File not found: {configPath}");
            return new List<StoredProcedureDefinition>();
        }
        
        try
        {
            var json = await File.ReadAllTextAsync(configPath);
            Console.WriteLine($"[ConfigService] Successfully read {json.Length} characters from {fileName}");
            var config = JsonSerializer.Deserialize<JsonElement>(json);
            var procedures = new List<StoredProcedureDefinition>();
            
            if (config.TryGetProperty("procedures", out var proceduresArray))
            {
                foreach (var proc in proceduresArray.EnumerateArray())
                {
                    var procedure = new StoredProcedureDefinition
                    {
                        Name = proc.GetProperty("name").GetString() ?? "",
                        DisplayName = proc.GetProperty("displayName").GetString() ?? "",
                        Description = proc.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                        Parameters = new List<ParameterDefinition>(),
                        OutputTables = new List<string>()
                    };
                    
                    // Read parameters from JSON
                    if (proc.TryGetProperty("parameters", out var parametersArray))
                    {
                        foreach (var param in parametersArray.EnumerateArray())
                        {
                            var parameter = new ParameterDefinition
                            {
                                Name = param.GetProperty("name").GetString() ?? "",
                                Type = param.GetProperty("type").GetString() ?? "string",
                                Required = param.TryGetProperty("required", out var req) ? req.GetBoolean() : true,
                                Description = param.TryGetProperty("description", out var paramDesc) ? paramDesc.GetString() : "",
                                DefaultValue = param.TryGetProperty("defaultValue", out var defVal) ? defVal.GetString() : null
                            };
                            procedure.Parameters.Add(parameter);
                        }
                    }
                    
                    if (proc.TryGetProperty("outputTables", out var outputTables))
                    {
                        foreach (var table in outputTables.EnumerateArray())
                        {
                            procedure.OutputTables.Add(table.GetString() ?? "");
                        }
                    }
                    
                    procedures.Add(procedure);
                }
                
                Console.WriteLine($"[ConfigService] Loaded {procedures.Count} procedures");
            }
            else
            {
                Console.WriteLine($"[ConfigService] WARNING: No 'procedures' property found in {fileName}");
            }
            
            return procedures;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ConfigService] ERROR loading procedures: {ex.Message}");
            Console.WriteLine($"[ConfigService] Stack trace: {ex.StackTrace}");
            return new List<StoredProcedureDefinition>();
        }
    }

    public async Task<List<TableDefinition>> GetTablesAsync(OperationMode mode)
    {
        var fileName = mode == OperationMode.Export ? "export_tables.json" : "import_tables.json";
        var configPath = Path.Combine(_baseConfigPath, fileName);
        
        Console.WriteLine($"[ConfigService] Loading tables from: {configPath}");
        
        if (!File.Exists(configPath))
        {
            Console.WriteLine($"[ConfigService] ERROR: File not found: {configPath}");
            return new List<TableDefinition>();
        }
        
        try
        {
            var json = await File.ReadAllTextAsync(configPath);
            Console.WriteLine($"[ConfigService] Successfully read {json.Length} characters from {fileName}");
            var config = JsonSerializer.Deserialize<JsonElement>(json);
            var tables = new List<TableDefinition>();
            
            if (config.TryGetProperty("tables", out var tablesArray))
            {
                foreach (var table in tablesArray.EnumerateArray())
                {
                    var tableDefinition = new TableDefinition
                    {
                        Name = table.GetProperty("name").GetString() ?? "",
                        DisplayName = table.GetProperty("displayName").GetString() ?? "",
                        Description = table.TryGetProperty("description", out var desc) ? desc.GetString() : ""
                    };
                    
                    tables.Add(tableDefinition);
                }
                
                Console.WriteLine($"[ConfigService] Loaded {tables.Count} tables");
            }
            else
            {
                Console.WriteLine($"[ConfigService] WARNING: No 'tables' property found in {fileName}");
            }
            
            return tables;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ConfigService] ERROR loading tables: {ex.Message}");
            Console.WriteLine($"[ConfigService] Stack trace: {ex.StackTrace}");
            return new List<TableDefinition>();
        }
    }

    public async Task SaveDatabaseConfigurationAsync(DatabaseConfiguration configuration)
    {
        _databaseConfiguration = configuration;
        var dbConfigPath = Path.Combine(_baseConfigPath, "database.json");
        
        var config = new
        {
            server = configuration.Server,
            database = configuration.Database,
            username = configuration.Username,
            password = configuration.Password,
            useWindowsAuthentication = configuration.UseWindowsAuthentication,
            connectionTimeout = configuration.ConnectionTimeout,
            trustServerCertificate = configuration.TrustServerCertificate
        };
        
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(dbConfigPath, json);
    }

    public async Task<bool> ValidateConfigurationAsync()
    {
        try
        {
            // Check if config directory exists
            if (!Directory.Exists(_baseConfigPath))
            {
                return false;
            }

            // Check for required config files
            var requiredFiles = new[] 
            { 
                "database.json", 
                "appsettings.json", 
                "export_procedures.json", 
                "export_tables.json"
            };

            foreach (var file in requiredFiles)
            {
                var filePath = Path.Combine(_baseConfigPath, file);
                if (!File.Exists(filePath))
                {
                    return false;
                }
            }

            // Try to load and validate each configuration
            await GetApplicationSettingsAsync();
            await GetDatabaseConfigurationAsync();
            await GetStoredProceduresAsync(OperationMode.Export);
            await GetTablesAsync(OperationMode.Export);

            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private static string ResolveConfigPath()
    {
        // Probe from the application base directory and the current working directory.
        var probeRoots = new[]
        {
            AppDomain.CurrentDomain.BaseDirectory,
            Directory.GetCurrentDirectory(),
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ""
        };

        Console.WriteLine($"[ConfigService] Attempting to resolve config path...");
        
        foreach (var root in probeRoots)
        {
            Console.WriteLine($"[ConfigService] Probing from root: {root}");
            var located = FindConfigDirectory(root);
            if (!string.IsNullOrEmpty(located))
            {
                Console.WriteLine($"[ConfigService] Found config directory: {located}");
                return located;
            }
        }

        // Fall back to a config directory next to the executable.
        var fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
        Console.WriteLine($"[ConfigService] Using fallback config directory: {fallback}");
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    private static string? FindConfigDirectory(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "config");
            Console.WriteLine($"[ConfigService] Checking: {candidate}");
            if (Directory.Exists(candidate))
            {
                Console.WriteLine($"[ConfigService] ✓ Config directory found at: {candidate}");
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// Get animation configuration with defaults
    /// </summary>
    public async Task<AnimationConfiguration> GetAnimationConfigurationAsync()
    {
        if (_animationConfiguration == null)
        {
            await LoadAnimationConfigurationAsync();
        }
        return _animationConfiguration!;
    }

    /// <summary>
    /// Save animation configuration
    /// </summary>
    public async Task SaveAnimationConfigurationAsync(AnimationConfiguration config)
    {
        _animationConfiguration = config ?? throw new ArgumentNullException(nameof(config));
        
        var animationConfigPath = Path.Combine(_baseConfigPath, "animations.json");
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var configData = new
        {
            quality = config.Quality.ToString(),
            enabledEffects = config.EnabledEffects.ToString(),
            particleDensity = config.ParticleDensity,
            animationSpeed = config.AnimationSpeed,
            effectIntensity = config.EffectIntensity,
            adaptivePerformance = config.AdaptivePerformance,
            targetFrameRate = config.TargetFrameRate,
            showPerformanceMetrics = config.ShowPerformanceMetrics
        };

        var json = JsonSerializer.Serialize(configData, options);
        await File.WriteAllTextAsync(animationConfigPath, json);
    }

    /// <summary>
    /// Load animation configuration from file or create defaults
    /// </summary>
    private async Task LoadAnimationConfigurationAsync()
    {
        var animationConfigPath = Path.Combine(_baseConfigPath, "animations.json");
        
        _animationConfiguration = new AnimationConfiguration(); // Start with defaults

        if (File.Exists(animationConfigPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(animationConfigPath);
                var config = JsonSerializer.Deserialize<JsonElement>(json);

                if (config.TryGetProperty("quality", out var quality) && 
                    Enum.TryParse<AnimationQuality>(quality.GetString(), out var qualityValue))
                {
                    _animationConfiguration.Quality = qualityValue;
                }

                if (config.TryGetProperty("enabledEffects", out var effects) && 
                    Enum.TryParse<BackgroundEffects>(effects.GetString(), out var effectsValue))
                {
                    _animationConfiguration.EnabledEffects = effectsValue;
                }

                if (config.TryGetProperty("particleDensity", out var density))
                {
                    _animationConfiguration.ParticleDensity = density.GetDouble();
                }

                if (config.TryGetProperty("animationSpeed", out var speed))
                {
                    _animationConfiguration.AnimationSpeed = speed.GetDouble();
                }

                if (config.TryGetProperty("effectIntensity", out var intensity))
                {
                    _animationConfiguration.EffectIntensity = intensity.GetDouble();
                }

                if (config.TryGetProperty("adaptivePerformance", out var adaptive))
                {
                    _animationConfiguration.AdaptivePerformance = adaptive.GetBoolean();
                }

                if (config.TryGetProperty("targetFrameRate", out var frameRate))
                {
                    _animationConfiguration.TargetFrameRate = frameRate.GetInt32();
                }

                if (config.TryGetProperty("showPerformanceMetrics", out var showMetrics))
                {
                    _animationConfiguration.ShowPerformanceMetrics = showMetrics.GetBoolean();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AnimationConfig] Error loading configuration, using defaults: {ex.Message}");
            }
        }
    }
}