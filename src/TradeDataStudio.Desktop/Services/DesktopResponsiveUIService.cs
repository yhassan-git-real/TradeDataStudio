using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;
using TradeDataStudio.Core.Services;
using System;
using System.Threading.Tasks;

namespace TradeDataStudio.Desktop.Services;

/// <summary>
/// Desktop-specific implementation of ResponsiveUIService using Avalonia screen detection
/// </summary>
public class DesktopResponsiveUIService : ResponsiveUIService
{
    private readonly ILoggingService? _loggingService;

    public DesktopResponsiveUIService(ILoggingService? loggingService = null)
    {
        _loggingService = loggingService;
    }

    /// <summary>
    /// Detects current screen size using Avalonia's platform APIs
    /// </summary>
    public new async Task<ScreenSizeCategory> DetectScreenSizeAsync()
    {
        try
        {
            var screenSize = await GetAvaloniaScreenSizeAsync();
            var category = ClassifyScreenSize(screenSize.Width, screenSize.Height);
            
            if (CurrentScreenCategory != category)
            {
                // Update screen category silently
            }

            await UpdateCurrentCategoryAsync(category);
            
            return category;
        }
        catch (Exception ex)
        {
            if (_loggingService != null)
            {
                await _loggingService.LogErrorAsync($"Failed to detect screen size: {ex.Message}", ex);
            }
            
            // Fallback to base implementation
            return await base.DetectScreenSizeAsync();
        }
    }

    /// <summary>
    /// Gets actual screen size from Avalonia platform
    /// </summary>
    private async Task<(double Width, double Height)> GetAvaloniaScreenSizeAsync()
    {
        await Task.Yield(); // Ensure we're not blocking

        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow?.Screens?.Primary != null)
                {
                    var screen = mainWindow.Screens.Primary;
                    var bounds = screen.Bounds;
                    
                    // Return actual screen resolution
                    return (bounds.Width, bounds.Height);
                }
            }

            // Try to get screen info from platform
            var platformScreen = GetPlatformScreenInfo();
            if (platformScreen.HasValue)
            {
                return platformScreen.Value;
            }
        }
        catch (Exception ex)
        {
            if (_loggingService != null)
            {
                await _loggingService.LogErrorAsync($"Avalonia screen detection failed: {ex.Message}", ex);
            }
        }

        // Fallback to common resolution
        return (1920, 1080);
    }

    /// <summary>
    /// Gets platform-specific screen information
    /// </summary>
    private (double Width, double Height)? GetPlatformScreenInfo()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                return GetWindowsScreenSize();
            }
            else if (OperatingSystem.IsLinux())
            {
                return GetLinuxScreenSize();
            }
            else if (OperatingSystem.IsMacOS())
            {
                return GetMacOSScreenSize();
            }
        }
        catch
        {
            // Platform detection failed
        }

        return null;
    }

    private (double Width, double Height)? GetWindowsScreenSize()
    {
        // Windows screen detection placeholder
        return null;
    }

    private (double Width, double Height)? GetLinuxScreenSize()
    {
        return null;
    }

    private (double Width, double Height)? GetMacOSScreenSize()
    {
        return null;
    }

    // Disabled for performance - screen detection only happens at startup
    private void InitializeScreenChangeDetection() { }
    private void SetupPeriodicScreenCheck() { }

    private async Task UpdateCurrentCategoryAsync(ScreenSizeCategory category)
    {
        await RefreshScreenDetectionAsync();
    }

    private ScreenSizeCategory ClassifyScreenSize(double width, double height)
    {
        var effectiveResolution = Math.Max(width, height);
        var scaledResolution = ApplyDPIScaling(effectiveResolution);

        return scaledResolution switch
        {
            <= 1366 => ScreenSizeCategory.Small,
            <= 1600 => ScreenSizeCategory.Medium,
            <= 2000 => ScreenSizeCategory.Large,
            _ => ScreenSizeCategory.ExtraLarge
        };
    }

    private double ApplyDPIScaling(double resolution)
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow?.Screens?.Primary != null)
                {
                    var screen = mainWindow.Screens.Primary;
                    var scaling = screen.Scaling;
                    return resolution / scaling;
                }
            }
        }
        catch
        {
            // Fallback to raw resolution
        }

        return resolution;
    }

    /// <summary>
    /// Gets detailed screen information for diagnostics
    /// </summary>
    public async Task<string> GetScreenDiagnosticsAsync()
    {
        try
        {
            var screenSize = await GetAvaloniaScreenSizeAsync();
            var category = ClassifyScreenSize(screenSize.Width, screenSize.Height);
            
            var diagnostics = $"Screen Resolution: {screenSize.Width}×{screenSize.Height}\n";
            diagnostics += $"Screen Category: {category}\n";
            
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow?.Screens?.Primary != null)
                {
                    var screen = mainWindow.Screens.Primary;
                    diagnostics += $"DPI Scaling: {screen.Scaling:F2}\n";
                    diagnostics += $"Working Area: {screen.WorkingArea}\n";
                }
            }
            
            return diagnostics;
        }
        catch (Exception ex)
        {
            return $"Screen diagnostics failed: {ex.Message}";
        }
    }
}