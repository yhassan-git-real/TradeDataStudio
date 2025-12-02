using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;
using System;
using System.Threading.Tasks;

namespace TradeDataStudio.Core.Services;

/// <summary>
/// Service for managing responsive UI adaptations based on screen size detection
/// </summary>
public class ResponsiveUIService : IResponsiveUIService
{
    private ScreenSizeCategory _currentScreenCategory = ScreenSizeCategory.Large;
    private ResponsiveConfiguration? _currentConfiguration;
    private readonly object _lockObject = new();
    private DateTime _lastDetectionTime = DateTime.MinValue;
    private const int DETECTION_THROTTLE_MS = 250; // Prevent excessive recalculations

    public ScreenSizeCategory CurrentScreenCategory 
    { 
        get 
        { 
            lock (_lockObject)
            {
                return _currentScreenCategory;
            }
        }
        private set
        {
            lock (_lockObject)
            {
                if (_currentScreenCategory != value)
                {
                    var previous = _currentScreenCategory;
                    _currentScreenCategory = value;
                    
                    // Update configuration when category changes
                    _currentConfiguration = ResponsiveConfiguration.CreateForCategory(value);
                    
                    // Trigger event on background thread to avoid blocking UI
                    Task.Run(() => OnScreenSizeChanged(previous, value));
                }
            }
        }
    }

    public event EventHandler<ScreenSizeChangedEventArgs>? ScreenSizeChanged;

    /// <summary>
    /// Detects current screen size category based on available screen resolution
    /// </summary>
    public async Task<ScreenSizeCategory> DetectScreenSizeAsync()
    {
        // Throttle detection calls to prevent performance issues
        if (DateTime.UtcNow - _lastDetectionTime < TimeSpan.FromMilliseconds(DETECTION_THROTTLE_MS))
        {
            return CurrentScreenCategory;
        }

        _lastDetectionTime = DateTime.UtcNow;

        try
        {
            // This will be implemented with platform-specific screen detection
            // For now, we'll use a default detection based on common screen sizes
            var screenSize = await GetPrimaryScreenSizeAsync();
            var category = ClassifyScreenSize(screenSize.Width, screenSize.Height);
            
            CurrentScreenCategory = category;
            return category;
        }
        catch (Exception)
        {
            // Fallback to Large if detection fails
            CurrentScreenCategory = ScreenSizeCategory.Large;
            return ScreenSizeCategory.Large;
        }
    }

    /// <summary>
    /// Gets the optimal layout definition for a given screen category
    /// </summary>
    public ResponsiveLayoutDefinition GetOptimalLayout(ScreenSizeCategory category)
    {
        var config = GetOrCreateConfiguration(category);
        return config.Layout;
    }

    /// <summary>
    /// Calculates scaled font size based on screen category
    /// </summary>
    public double GetScaledFontSize(double baseFontSize, ScreenSizeCategory category)
    {
        var config = GetOrCreateConfiguration(category);
        return baseFontSize * config.Typography.FontScaleFactor;
    }

    /// <summary>
    /// Gets responsive spacing configuration for a screen category
    /// </summary>
    public ResponsiveSpacing GetResponsiveSpacing(ScreenSizeCategory category)
    {
        var config = GetOrCreateConfiguration(category);
        return config.Layout.Spacing;
    }

    /// <summary>
    /// Applies a responsive configuration to the service
    /// </summary>
    public void ApplyResponsiveConfiguration(ResponsiveConfiguration config)
    {
        lock (_lockObject)
        {
            _currentConfiguration = config;
            CurrentScreenCategory = config.Category;
        }
    }

    /// <summary>
    /// Gets or creates configuration for a given category
    /// </summary>
    private ResponsiveConfiguration GetOrCreateConfiguration(ScreenSizeCategory category)
    {
        lock (_lockObject)
        {
            if (_currentConfiguration?.Category == category)
            {
                return _currentConfiguration;
            }

            var config = ResponsiveConfiguration.CreateForCategory(category);
            
            // Cache if it's the current category
            if (category == _currentScreenCategory)
            {
                _currentConfiguration = config;
            }

            return config;
        }
    }

    /// <summary>
    /// Platform-specific screen size detection (will be implemented in Desktop project)
    /// </summary>
    private async Task<(double Width, double Height)> GetPrimaryScreenSizeAsync()
    {
        // This is a placeholder - actual implementation will be in the Desktop project
        // where we have access to Avalonia's screen detection APIs
        await Task.Delay(1); // Simulate async operation
        
        // Default to 1920x1080 for now
        return (1920, 1080);
    }

    /// <summary>
    /// Classifies screen size into appropriate category
    /// </summary>
    private ScreenSizeCategory ClassifyScreenSize(double width, double height)
    {
        var effectiveResolution = Math.Min(width, height * 1.6); // Approximate 16:10 normalization

        return effectiveResolution switch
        {
            <= 1366 => ScreenSizeCategory.Small,    // 1366×768, 1280×720, etc.
            <= 1600 => ScreenSizeCategory.Medium,   // 1440×900, 1600×1200, etc.
            <= 2000 => ScreenSizeCategory.Large,    // 1920×1080, 1920×1200, etc.
            _ => ScreenSizeCategory.ExtraLarge       // 2560×1440, 3840×2160, etc.
        };
    }

    /// <summary>
    /// Raises the ScreenSizeChanged event
    /// </summary>
    private void OnScreenSizeChanged(ScreenSizeCategory previous, ScreenSizeCategory current)
    {
        try
        {
            var eventArgs = new ScreenSizeChangedEventArgs
            {
                PreviousCategory = previous,
                NewCategory = current,
                // Screen dimensions would be filled by actual detection
                PreviousWidth = 0,
                PreviousHeight = 0,
                NewWidth = 0,
                NewHeight = 0
            };

            ScreenSizeChanged?.Invoke(this, eventArgs);
        }
        catch (Exception)
        {
            // Prevent event handler exceptions from crashing the service
        }
    }

    /// <summary>
    /// Gets the current responsive configuration
    /// </summary>
    public ResponsiveConfiguration GetCurrentConfiguration()
    {
        return GetOrCreateConfiguration(CurrentScreenCategory);
    }

    /// <summary>
    /// Utility method to get animation settings for current screen
    /// </summary>
    public ResponsiveAnimationSettings GetCurrentAnimationSettings()
    {
        var config = GetCurrentConfiguration();
        return config.Animation;
    }

    /// <summary>
    /// Utility method to get typography settings for current screen
    /// </summary>
    public ResponsiveTypography GetCurrentTypography()
    {
        var config = GetCurrentConfiguration();
        return config.Typography;
    }

    /// <summary>
    /// Force refresh of screen detection (useful for testing or manual refresh)
    /// </summary>
    public async Task RefreshScreenDetectionAsync()
    {
        _lastDetectionTime = DateTime.MinValue; // Reset throttle
        await DetectScreenSizeAsync();
    }
}