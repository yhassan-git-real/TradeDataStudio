using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using TradeDataStudio.Core.Models;
using TradeDataStudio.Desktop.Services;
using System;

namespace TradeDataStudio.Desktop.Views.Components;

/// <summary>
/// Intelligently composes and layers multiple background effects with performance optimization
/// </summary>
public partial class BackgroundEffectsComposer : UserControl
{
    private readonly AnimationConfiguration _config;
    private readonly AnimationPerformanceManager _performanceManager;
    
    // Effect components
    private Grid _layeredGrid = null!;
    private AnimatedBackground? _pulsingGlows;
    private TwinklingStarsBackground? _twinklingStars;
    private ParticleSystemBackground? _particleSystem;
    private FlowingLightsBackground? _flowingLights;
    private PerformanceOverlay? _performanceOverlay;
    
    private bool _isDisposed = false;

    // Parameterless constructor required for XAML instantiation
    public BackgroundEffectsComposer() : this(null) { }

    public BackgroundEffectsComposer(AnimationConfiguration? config = null)
    {
        _config = config ?? new AnimationConfiguration();
        _performanceManager = new AnimationPerformanceManager(_config);
        
        InitializeComponent();
        SetupEffectLayers();
        
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

        _config.PropertyChanged -= OnConfigurationChanged;
        _performanceManager.PerformanceChanged -= OnPerformanceChanged;
        _performanceManager.PerformanceWarning -= OnPerformanceWarning;
        
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