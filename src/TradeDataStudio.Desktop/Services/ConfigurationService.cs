using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TradeDataStudio.Core.Constants;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;

namespace TradeDataStudio.Desktop.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly string _configPath;
    private ApplicationSettings? _cachedAppSettings;
    private DatabaseConfiguration? _cachedDbConfig;

    public ConfigurationService()
    {
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConstants.ConfigDirectory);
        
        // Create config directory if it doesn't exist
        if (!Directory.Exists(_configPath))
        {
            Directory.CreateDirectory(_configPath);
        }
    }

    public async Task<ApplicationSettings> GetApplicationSettingsAsync()
    {
        if (_cachedAppSettings != null)
            return _cachedAppSettings;

        var filePath = Path.Combine(_configPath, AppConstants.AppSettingsFile);
        
        if (!File.Exists(filePath))
        {
            _cachedAppSettings = new ApplicationSettings();
            await SaveApplicationSettingsAsync(_cachedAppSettings);
            return _cachedAppSettings;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            _cachedAppSettings = JsonSerializer.Deserialize<ApplicationSettings>(json, options) ?? new ApplicationSettings();
            return _cachedAppSettings;
        }
        catch (Exception)
        {
            _cachedAppSettings = new ApplicationSettings();
            return _cachedAppSettings;
        }
    }

    public async Task<DatabaseConfiguration> GetDatabaseConfigurationAsync()
    {
        if (_cachedDbConfig != null)
            return _cachedDbConfig;

        var filePath = Path.Combine(_configPath, AppConstants.DatabaseConfigFile);
        
        if (!File.Exists(filePath))
        {
            _cachedDbConfig = new DatabaseConfiguration();
            await SaveDatabaseConfigurationAsync(_cachedDbConfig);
            return _cachedDbConfig;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            _cachedDbConfig = JsonSerializer.Deserialize<DatabaseConfiguration>(json, options) ?? new DatabaseConfiguration();
            return _cachedDbConfig;
        }
        catch (Exception)
        {
            _cachedDbConfig = new DatabaseConfiguration();
            return _cachedDbConfig;
        }
    }

    public async Task<List<StoredProcedureDefinition>> GetStoredProceduresAsync(OperationMode mode)
    {
        var fileName = mode == OperationMode.Export 
            ? AppConstants.ExportProceduresFile 
            : AppConstants.ImportProceduresFile;
            
        var filePath = Path.Combine(_configPath, fileName);
        
        if (!File.Exists(filePath))
        {
            return new List<StoredProcedureDefinition>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var proceduresContainer = JsonSerializer.Deserialize<ProceduresContainer>(json, options);
            return proceduresContainer?.Procedures ?? new List<StoredProcedureDefinition>();
        }
        catch (Exception)
        {
            return new List<StoredProcedureDefinition>();
        }
    }

    public async Task<List<TableDefinition>> GetTablesAsync(OperationMode mode)
    {
        var fileName = mode == OperationMode.Export 
            ? AppConstants.ExportTablesFile 
            : AppConstants.ImportTablesFile;
            
        var filePath = Path.Combine(_configPath, fileName);
        
        if (!File.Exists(filePath))
        {
            return new List<TableDefinition>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var tablesContainer = JsonSerializer.Deserialize<TablesContainer>(json, options);
            return tablesContainer?.Tables ?? new List<TableDefinition>();
        }
        catch (Exception)
        {
            return new List<TableDefinition>();
        }
    }

    public async Task SaveDatabaseConfigurationAsync(DatabaseConfiguration config)
    {
        var filePath = Path.Combine(_configPath, AppConstants.DatabaseConfigFile);
        var options = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true 
        };
        
        var json = JsonSerializer.Serialize(config, options);
        await File.WriteAllTextAsync(filePath, json);
        
        // Clear cache to force reload
        _cachedDbConfig = null;
    }

    private async Task SaveApplicationSettingsAsync(ApplicationSettings settings)
    {
        var filePath = Path.Combine(_configPath, AppConstants.AppSettingsFile);
        var options = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true 
        };
        
        var json = JsonSerializer.Serialize(settings, options);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<bool> ValidateConfigurationAsync()
    {
        try
        {
            // Check if all required configuration files exist
            var requiredFiles = new[]
            {
                AppConstants.AppSettingsFile,
                AppConstants.DatabaseConfigFile,
                AppConstants.ExportProceduresFile,
                AppConstants.ImportProceduresFile,
                AppConstants.ExportTablesFile,
                AppConstants.ImportTablesFile
            };

            foreach (var file in requiredFiles)
            {
                var filePath = Path.Combine(_configPath, file);
                if (!File.Exists(filePath))
                {
                    return false;
                }
            }

            // Try to load and parse each configuration file
            await GetApplicationSettingsAsync();
            await GetDatabaseConfigurationAsync();
            await GetStoredProceduresAsync(OperationMode.Export);
            await GetStoredProceduresAsync(OperationMode.Import);
            await GetTablesAsync(OperationMode.Export);
            await GetTablesAsync(OperationMode.Import);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // Helper classes for JSON deserialization
    private class ProceduresContainer
    {
        public List<StoredProcedureDefinition> Procedures { get; set; } = new();
    }

    private class TablesContainer
    {
        public List<TableDefinition> Tables { get; set; } = new();
    }
}