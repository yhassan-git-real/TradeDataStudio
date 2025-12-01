using Avalonia;
using Avalonia.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;
using TradeDataStudio.Desktop.ViewModels;

namespace TradeDataStudio.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly IResponsiveUIService? _responsiveUIService;
    private readonly ILoggingService? _loggingService;

    public MainWindow()
    {
        InitializeComponent();
        
        // Set default resources (will be overridden by responsive service)
        SetScreenSizeResources(ScreenSizeCategory.Large);
    }
    
    public MainWindow(MainWindowViewModel viewModel, IResponsiveUIService? responsiveUIService = null, ILoggingService? loggingService = null) : this()
    {
        if (viewModel == null)
            throw new ArgumentNullException(nameof(viewModel));
            
        DataContext = viewModel;
        _responsiveUIService = responsiveUIService;
        _loggingService = loggingService;
        
        // Initialize responsive UI
        _ = InitializeResponsiveUIAsync();
    }

    /// <summary>
    /// Initialize responsive UI system
    /// </summary>
    private async Task InitializeResponsiveUIAsync()
    {
        try
        {
            if (_responsiveUIService != null)
            {
                // Subscribe to screen size changes
                _responsiveUIService.ScreenSizeChanged += OnScreenSizeChanged;
                
                // Perform initial screen detection
                await _responsiveUIService.DetectScreenSizeAsync();
                
                // Apply initial responsive configuration
                ApplyResponsiveConfiguration(_responsiveUIService.CurrentScreenCategory);
                
                if (_loggingService != null)
                {
                    await _loggingService.LogMainAsync($"Responsive UI initialized for {_responsiveUIService.CurrentScreenCategory} screen");
                }
            }
        }
        catch (Exception ex)
        {
            if (_loggingService != null)
            {
                await _loggingService.LogErrorAsync($"Failed to initialize responsive UI: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Handle screen size changes
    /// </summary>
    private async void OnScreenSizeChanged(object? sender, ScreenSizeChangedEventArgs e)
    {
        try
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                ApplyResponsiveConfiguration(e.NewCategory);
            });
            
            if (_loggingService != null)
            {
                await _loggingService.LogMainAsync($"Screen size changed from {e.PreviousCategory} to {e.NewCategory}");
            }
        }
        catch (Exception ex)
        {
            if (_loggingService != null)
            {
                await _loggingService.LogErrorAsync($"Error handling screen size change: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Apply responsive configuration based on screen size category
    /// </summary>
    private void ApplyResponsiveConfiguration(ScreenSizeCategory category)
    {
        try
        {
            // Update resource dictionary based on screen size
            SetScreenSizeResources(category);
            
            // Update window size constraints if needed
            UpdateWindowConstraints(category);
            
            // Trigger layout update
            InvalidateVisual();
            InvalidateMeasure();
            InvalidateArrange();
        }
        catch (Exception ex)
        {
            _loggingService?.LogErrorAsync($"Error applying responsive configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Set appropriate resource dictionary based on screen size
    /// </summary>
    private void SetScreenSizeResources(ScreenSizeCategory category)
    {
        try
        {
            // For now, we'll just apply the responsive configuration directly
            // This avoids complex resource dictionary loading issues
            // The responsive resources are loaded via the AXAML file
            
            var resourceKey = category switch
            {
                ScreenSizeCategory.Small => "SmallScreenResources",
                ScreenSizeCategory.Medium => "MediumScreenResources", 
                ScreenSizeCategory.Large => "LargeScreenResources",
                ScreenSizeCategory.ExtraLarge => "ExtraLargeScreenResources",
                _ => "LargeScreenResources"
            };
            
            // Resources will be applied automatically through the responsive AXAML
            // Just log the current configuration
            if (_loggingService != null)
            {
                _ = Task.Run(async () => await _loggingService.LogMainAsync($"Applied {resourceKey} for {category} screen"));
            }
        }
        catch (Exception ex)
        {
            if (_loggingService != null)
            {
                _ = Task.Run(async () => await _loggingService.LogErrorAsync($"Error setting screen size resources: {ex.Message}", ex));
            }
        }
    }

    /// <summary>
    /// Update window size constraints based on screen category
    /// </summary>
    private void UpdateWindowConstraints(ScreenSizeCategory category)
    {
        try
        {
            // Only update if window is not maximized
            if (WindowState == WindowState.Normal)
            {
                // Get current screen size to avoid making window larger than screen
                var screenBounds = Screens.Primary?.Bounds ?? new Avalonia.PixelRect(0, 0, 1920, 1080);
                var maxWidth = screenBounds.Width * 0.9; // 90% of screen width
                var maxHeight = screenBounds.Height * 0.9; // 90% of screen height

                // Apply category-specific constraints
                var constraints = GetWindowConstraints(category);
                
                // Ensure constraints don't exceed screen size
                MinWidth = Math.Min(constraints.MinWidth, maxWidth);
                MinHeight = Math.Min(constraints.MinHeight, maxHeight);
                
                // Adjust current window size if needed
                if (Width < MinWidth) Width = MinWidth;
                if (Height < MinHeight) Height = MinHeight;
                if (Width > maxWidth) Width = maxWidth;
                if (Height > maxHeight) Height = maxHeight;
            }
        }
        catch (Exception ex)
        {
            _loggingService?.LogErrorAsync($"Error updating window constraints: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get window constraints for a screen category
    /// </summary>
    private (double MinWidth, double MinHeight) GetWindowConstraints(ScreenSizeCategory category)
    {
        return category switch
        {
            ScreenSizeCategory.Small => (720, 480),
            ScreenSizeCategory.Medium => (750, 520),
            ScreenSizeCategory.Large => (800, 550),
            ScreenSizeCategory.ExtraLarge => (900, 600),
            _ => (800, 550)
        };
    }

    /// <summary>
    /// Clean up resources
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        try
        {
            if (_responsiveUIService != null)
            {
                _responsiveUIService.ScreenSizeChanged -= OnScreenSizeChanged;
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
        
        base.OnClosed(e);
    }
}