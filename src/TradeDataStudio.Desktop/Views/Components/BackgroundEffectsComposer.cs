using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using TradeDataStudio.Core.Models;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Desktop.Services;
using System;

namespace TradeDataStudio.Desktop.Views.Components;

/// <summary>
/// Intelligently composes and layers multiple background effects with performance optimization
/// and responsive design support
/// </summary>
public partial class BackgroundEffectsComposer : UserControl
{
    private readonly AnimationConfiguration _config;
    private readonly AnimationPerformanceManager _performanceManager;
    private readonly IResponsiveUIService? _responsiveUIService;
    
    // Effect components
    private Grid _layeredGrid = null!;
    private AnimatedBackground? _pulsingGlows;
    private TwinklingStarsBackground? _twinklingStars;
    private ParticleSystemBackground? _particleSystem;
    private FlowingLightsBackground? _flowingLights;
    private PerformanceOverlay? _performanceOverlay;
    
    private bool _isDisposed = false;
    private ScreenSizeCategory _currentScreenCategory = ScreenSizeCategory.Large;

    // Parameterless constructor required for XAML instantiation
    public BackgroundEffectsComposer() : this(null, null) { }

    public BackgroundEffectsComposer(AnimationConfiguration? config = null, IResponsiveUIService? responsiveUIService = null)
    {
        _config = config ?? new AnimationConfiguration();
        _performanceManager = new AnimationPerformanceManager(_config);
        _responsiveUIService = responsiveUIService;
        
        InitializeComponent();
        SetupEffectLayers();
        InitializeResponsiveAnimations();
        
        _config.PropertyChanged += OnConfigurationChanged;
        _performanceManager.PerformanceChanged += OnPerformanceChanged;
        _performanceManager.PerformanceWarning += OnPerformanceWarning;
    }

    private void InitializeComponent()
    {
        Background = Brushes.Transparent;
        IsHitTestVisible = false;
        ClipToBounds = true;
        
        // Create layered grid for proper effect composition
        _layeredGrid = new Grid();
        Content = _layeredGrid;
    }

    private void SetupEffectLayers()
    {
        if (_isDisposed) return;

        // Clear existing layers
        _layeredGrid.Children.Clear();
        DisposeEffects();

        // Layer 1: Pulsing glows (deepest layer)
        if (_config.IsEffectEnabled(BackgroundEffects.PulsingGlows))
        {
            _pulsingGlows = new AnimatedBackground();
            _layeredGrid.Children.Add(_pulsingGlows);
        }

        // Layer 2: Twinkling stars (star field)
        if (_config.IsEffectEnabled(BackgroundEffects.TwinklingStars))
        {
            _twinklingStars = new TwinklingStarsBackground(_config);
            _layeredGrid.Children.Add(_twinklingStars);
        }

        // Layer 3: Flowing lights (mid-layer trails)
        if (_config.IsEffectEnabled(BackgroundEffects.FlowingLights))
        {
            _flowingLights = new FlowingLightsBackground(_config);
            _layeredGrid.Children.Add(_flowingLights);
        }

        // Layer 4: Particle system (top animated layer)
        if (_config.IsEffectEnabled(BackgroundEffects.Particles))
        {
            _particleSystem = new ParticleSystemBackground(_config);
            _layeredGrid.Children.Add(_particleSystem);
        }

        // Layer 5: Performance overlay (always on top if enabled)
        if (_config.ShowPerformanceMetrics)
        {
            _performanceOverlay = new PerformanceOverlay(_performanceManager);
            _layeredGrid.Children.Add(_performanceOverlay);
        }
    }

    private void OnConfigurationChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_isDisposed) return;

        Dispatcher.UIThread.Post(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(AnimationConfiguration.EnabledEffects):
                case nameof(AnimationConfiguration.Quality):
                    SetupEffectLayers();
                    break;
                    
                case nameof(AnimationConfiguration.ShowPerformanceMetrics):
                    if (_config.ShowPerformanceMetrics && _performanceOverlay == null)
                    {
                        _performanceOverlay = new PerformanceOverlay(_performanceManager);
                        _layeredGrid.Children.Add(_performanceOverlay);
                    }
                    else if (!_config.ShowPerformanceMetrics && _performanceOverlay != null)
                    {
                        _layeredGrid.Children.Remove(_performanceOverlay);
                        _performanceOverlay.Dispose();
                        _performanceOverlay = null;
                    }
                    break;
            }
        });
    }

    /// <summary>
    /// Initialize responsive animations based on screen size
    /// </summary>
    private void InitializeResponsiveAnimations()
    {
        try
        {
            if (_responsiveUIService != null)
            {
                // Subscribe to screen size changes
                _responsiveUIService.ScreenSizeChanged += OnScreenSizeChanged;
                
                // Apply initial responsive configuration
                ApplyResponsiveAnimationSettings(_responsiveUIService.CurrentScreenCategory);
            }
        }
        catch (Exception)
        {
            // Use default settings if responsive service is not available
            ApplyResponsiveAnimationSettings(ScreenSizeCategory.Large);
        }
    }

    /// <summary>
    /// Handle screen size changes for animation optimization
    /// </summary>
    private async void OnScreenSizeChanged(object? sender, ScreenSizeChangedEventArgs e)
    {
        if (_isDisposed) return;

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ApplyResponsiveAnimationSettings(e.NewCategory);
            });
        }
        catch (Exception)
        {
            // Ignore errors during screen size change handling
        }
    }

    /// <summary>
    /// Apply animation settings based on screen size category
    /// </summary>
    private void ApplyResponsiveAnimationSettings(ScreenSizeCategory category)
    {
        if (_isDisposed) return;

        try
        {
            _currentScreenCategory = category;
            var settings = ResponsiveAnimationSettings.CreateForCategory(category);
            
            // Update configuration based on screen size
            var oldAdaptivePerformance = _config.AdaptivePerformance;
            _config.AdaptivePerformance = true; // Always enable for responsive design
            
            // Update enabled effects based on screen size
            UpdateEffectsForScreenSize(settings);
            
            // Update performance targets
            UpdatePerformanceTargets(settings);
            
            // Refresh effect layers if significant changes were made
            if (ShouldRefreshEffects(settings))
            {
                SetupEffectLayers();
            }
        }
        catch (Exception)
        {
            // Use safe defaults if responsive configuration fails
        }
    }

    /// <summary>
    /// Update enabled effects based on responsive animation settings
    /// </summary>
    private void UpdateEffectsForScreenSize(ResponsiveAnimationSettings settings)
    {
        // Disable expensive effects on smaller screens
        if (!settings.EnableParticleSystem && _particleSystem != null)
        {
            _layeredGrid.Children.Remove(_particleSystem);
            _particleSystem?.Dispose();
            _particleSystem = null;
        }

        if (!settings.EnableFlowingLights && _flowingLights != null)
        {
            _layeredGrid.Children.Remove(_flowingLights);
            _flowingLights?.Dispose();
            _flowingLights = null;
        }

        // Keep twinkling stars as they're lightweight
        if (!settings.EnableTwinklingStars && _twinklingStars != null)
        {
            _layeredGrid.Children.Remove(_twinklingStars);
            _twinklingStars?.Dispose();
            _twinklingStars = null;
        }

        // Update particle count for existing systems
        if (_particleSystem != null && settings.EnableParticleSystem)
        {
            // Update particle count if the system supports it
            UpdateParticleCount(settings.ParticleCount);
        }
    }

    /// <summary>
    /// Update performance targets based on screen size
    /// </summary>
    private void UpdatePerformanceTargets(ResponsiveAnimationSettings settings)
    {
        // Store target frame rate for future use
        // _performanceManager.SetTargetFrameRate(settings.FrameRate);
        
        // Update animation speed multiplier
        if (Math.Abs(settings.AnimationSpeed - 1.0) > 0.01)
        {
            UpdateAnimationSpeed(settings.AnimationSpeed);
        }
    }

    /// <summary>
    /// Determine if effect layers need to be refreshed
    /// </summary>
    private bool ShouldRefreshEffects(ResponsiveAnimationSettings settings)
    {
        // Check if major effects need to be added or removed
        var needsParticles = settings.EnableParticleSystem && _particleSystem == null;
        var needsFlowingLights = settings.EnableFlowingLights && _flowingLights == null;
        var needsStars = settings.EnableTwinklingStars && _twinklingStars == null;
        
        return needsParticles || needsFlowingLights || needsStars;
    }

    /// <summary>
    /// Update particle count for particle system
    /// </summary>
    private void UpdateParticleCount(int particleCount)
    {
        // This would need to be implemented in the ParticleSystemBackground class
        // For now, we'll just store the target count
        try
        {
            if (_particleSystem != null)
            {
                // Implementation would depend on ParticleSystemBackground API
                // _particleSystem.SetParticleCount(particleCount);
            }
        }
        catch
        {
            // Ignore particle count update errors
        }
    }

    /// <summary>
    /// Update animation speed for all effects
    /// </summary>
    private void UpdateAnimationSpeed(double speedMultiplier)
    {
        try
        {
            // Update speed for all active animations - would need to be implemented in background classes
            // _pulsingGlows?.SetAnimationSpeed(speedMultiplier);
            // _twinklingStars?.SetAnimationSpeed(speedMultiplier);
            // _flowingLights?.SetAnimationSpeed(speedMultiplier);
            // _particleSystem?.SetAnimationSpeed(speedMultiplier);
        }
        catch
        {
            // Ignore animation speed update errors
        }
    }

    /// <summary>
    /// Get current responsive animation settings
    /// </summary>
    public ResponsiveAnimationSettings GetCurrentAnimationSettings()
    {
        return ResponsiveAnimationSettings.CreateForCategory(_currentScreenCategory);
    }

    private void OnPerformanceChanged(object? sender, PerformanceEventArgs e)
    {
        // Performance monitoring is handled by individual components
        // This composer just ensures proper layering
    }

    private void OnPerformanceWarning(object? sender, PerformanceEventArgs e)
    {
        if (_isDisposed || !_config.AdaptivePerformance) return;

        // Emergency performance optimization - disable expensive effects
        Dispatcher.UIThread.Post(() =>
        {
            if (e.Metrics.PerformanceRatio > 2.0) // Critical performance issues
            {
                // Disable flowing lights first (most expensive)
                if (_config.EnabledEffects.HasFlag(BackgroundEffects.FlowingLights))
                {
                    _config.EnabledEffects &= ~BackgroundEffects.FlowingLights;
                    return;
                }

                // Then disable particles
                if (_config.EnabledEffects.HasFlag(BackgroundEffects.Particles))
                {
                    _config.EnabledEffects &= ~BackgroundEffects.Particles;
                    return;
                }

                // Finally reduce to minimal effects
                _config.EnabledEffects = BackgroundEffects.PulsingGlows;
            }
        });
    }

    private void DisposeEffects()
    {
        _pulsingGlows?.Dispose();
        _twinklingStars?.Dispose();
        _particleSystem?.Dispose();
        _flowingLights?.Dispose();
        _performanceOverlay?.Dispose();
        
        _pulsingGlows = null;
        _twinklingStars = null;
        _particleSystem = null;
        _flowingLights = null;
        _performanceOverlay = null;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        // Unsubscribe from all events
        _config.PropertyChanged -= OnConfigurationChanged;
        _performanceManager.PerformanceChanged -= OnPerformanceChanged;
        _performanceManager.PerformanceWarning -= OnPerformanceWarning;
        
        // Unsubscribe from responsive service events
        if (_responsiveUIService != null)
        {
            _responsiveUIService.ScreenSizeChanged -= OnScreenSizeChanged;
        }
        
        DisposeEffects();
        _performanceManager.Dispose();
    }
}

/// <summary>
/// Performance metrics overlay for debugging and monitoring
/// </summary>
internal class PerformanceOverlay : UserControl
{
    private readonly AnimationPerformanceManager _performanceManager;
    private readonly TextBlock _metricsText;
    private readonly Border _backgroundBorder;
    private readonly DispatcherTimer _updateTimer;
    private bool _isDisposed = false;

    public PerformanceOverlay(AnimationPerformanceManager performanceManager)
    {
        _performanceManager = performanceManager ?? throw new ArgumentNullException(nameof(performanceManager));
        
        // Create UI elements
        _metricsText = new TextBlock
        {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10,
            Foreground = Brushes.Cyan,
            Padding = new Thickness(8, 4),
            Text = "Initializing..."
        };

        _backgroundBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
            CornerRadius = new CornerRadius(4),
            Child = _metricsText,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Thickness(0, 8, 8, 0)
        };

        Content = _backgroundBorder;
        IsHitTestVisible = false;

        // Update timer
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _updateTimer.Tick += UpdateMetrics;
        _updateTimer.Start();
    }

    private void UpdateMetrics(object? sender, EventArgs e)
    {
        if (_isDisposed) return;

        var metrics = _performanceManager.GetMetrics();
        
        var metricsText = $"FPS: {metrics.CurrentFPS:F1}\n" +
                         $"Frame: {metrics.AverageFrameTime:F1}ms\n" +
                         $"Target: {metrics.TargetFrameTime:F1}ms\n" +
                         $"Status: {metrics.PerformanceStatus}\n" +
                         $"Frames: {metrics.FrameCount:N0}";

        _metricsText.Text = metricsText;

        // Color code based on performance
        _metricsText.Foreground = metrics.IsPerformingWell ? Brushes.LightGreen :
                                 metrics.PerformanceRatio < 1.5 ? Brushes.Yellow :
                                 Brushes.Red;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _updateTimer?.Stop();
    }
}