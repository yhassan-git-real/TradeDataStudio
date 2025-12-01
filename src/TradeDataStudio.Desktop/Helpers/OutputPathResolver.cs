using System;
using System.IO;
using System.Threading.Tasks;
using TradeDataStudio.Core.Interfaces;

namespace TradeDataStudio.Desktop.Helpers;

/// <summary>
/// Resolves output paths for export operations, handling custom locations and default paths from configuration.
/// </summary>
public class OutputPathResolver
{
    private readonly IConfigurationService _configurationService;

    public OutputPathResolver(IConfigurationService configurationService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
    }

    /// <summary>
    /// Resolves the output path based on custom location or application settings.
    /// </summary>
    /// <param name="useCustomLocation">Whether to use a custom location.</param>
    /// <param name="customOutputLocation">The custom output location path.</param>
    /// <param name="currentMode">The current operation mode (Export/Import).</param>
    /// <returns>The resolved absolute output path with mode-specific subdirectory.</returns>
    public async Task<string> ResolveOutputPathAsync(
        bool useCustomLocation, 
        string customOutputLocation, 
        Core.Models.OperationMode currentMode)
    {
        string outputPath;
        
        if (useCustomLocation && !string.IsNullOrEmpty(customOutputLocation))
        {
            outputPath = customOutputLocation;
        }
        else
        {
            // Read from appsettings.json
            var appSettings = await _configurationService.GetApplicationSettingsAsync();
            var configuredPath = currentMode == Core.Models.OperationMode.Export 
                ? appSettings.Paths.Exports 
                : appSettings.Paths.Imports;
            
            // Convert to absolute path if needed
            if (!Path.IsPathRooted(configuredPath))
            {
                outputPath = Path.GetFullPath(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    configuredPath.Replace("/", Path.DirectorySeparatorChar.ToString())));
            }
            else
            {
                outputPath = configuredPath.Replace("/", Path.DirectorySeparatorChar.ToString());
            }
        }
        
        // Ensure directory exists
        Directory.CreateDirectory(outputPath);
        
        return outputPath;
    }

    /// <summary>
    /// Gets the default export path from configuration.
    /// </summary>
    public async Task<string> GetDefaultExportPathAsync()
    {
        var appSettings = await _configurationService.GetApplicationSettingsAsync();
        var configuredExportPath = appSettings.Paths.Exports;
        
        if (!Path.IsPathRooted(configuredExportPath))
        {
            return Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                configuredExportPath.Replace("/", Path.DirectorySeparatorChar.ToString())));
        }
        
        return configuredExportPath.Replace("/", Path.DirectorySeparatorChar.ToString());
    }
    
    /// <summary>
    /// Gets the default import path from configuration.
    /// </summary>
    public async Task<string> GetDefaultImportPathAsync()
    {
        var appSettings = await _configurationService.GetApplicationSettingsAsync();
        var configuredImportPath = appSettings.Paths.Imports;
        
        if (!Path.IsPathRooted(configuredImportPath))
        {
            return Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                configuredImportPath.Replace("/", Path.DirectorySeparatorChar.ToString())));
        }
        
        return configuredImportPath.Replace("/", Path.DirectorySeparatorChar.ToString());
    }
}