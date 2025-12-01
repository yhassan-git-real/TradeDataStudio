using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using TradeDataStudio.Core.Models;
using TradeDataStudio.Desktop.Services;
using System;
using System.Collections.Generic;

namespace TradeDataStudio.Desktop.Views.Components;

public partial class FlowingLightsBackground : UserControl
{
    private readonly AnimationConfiguration _config;
    private readonly AnimationPerformanceManager _performanceManager;
    private readonly List<LightTrail> _lightTrails = new();
    private readonly Random _random = new();
    
    private Canvas? _canvas;
    private DispatcherTimer? _animationTimer;
    private double _elapsedTime = 0;
    private bool _isDisposed = false;

    // Performance and visual settings
    private const int MAX_LIGHT_TRAILS = 8;
    private const double TRAIL_WIDTH = 2.0;
    private const double TRAIL_MIN_LENGTH = 50;
    private const double TRAIL_MAX_LENGTH = 200;
    private const double LIGHT_SPEED_BASE = 30.0; // pixels per second
    
    public FlowingLightsBackground(AnimationConfiguration? config = null)
    {
        _config = config ?? new AnimationConfiguration();
        _performanceManager = new AnimationPerformanceManager(_config);
        
        InitializeComponent();
        SetupLightSystem();
        
        _config.PropertyChanged += OnConfigurationChanged;
        _performanceManager.PerformanceChanged += OnPerformanceChanged;
    }

    private void InitializeComponent()
    {
        Background = Brushes.Transparent;
        IsHitTestVisible = false;
        
        _canvas = new Canvas
        {
            Background = Brushes.Transparent,
            ClipToBounds = true
        };
        
        Content = _canvas;
    }

    private void SetupLightSystem()
    {
        if (!_config.IsEffectEnabled(BackgroundEffects.FlowingLights))
            return;

        CreateLightTrails();
        StartAnimation();
    }

    private void CreateLightTrails()
    {
        if (_canvas == null) return;

        _lightTrails.Clear();
        _canvas.Children.Clear();

        // Calculate trail count based on configuration
        var baseCount = MAX_LIGHT_TRAILS;
        var trailCount = _config.Quality switch
        {
            AnimationQuality.Off => 0,
            AnimationQuality.Low => Math.Max(1, baseCount / 4),
            AnimationQuality.Medium => Math.Max(2, baseCount / 2),
            AnimationQuality.High => baseCount,
            _ => baseCount / 2
        };

        // Adjust by effect intensity
        trailCount = (int)(trailCount * _config.EffectIntensity);

        for (int i = 0; i < trailCount; i++)
        {
            CreateLightTrail();
        }
    }

    private void CreateLightTrail()
    {
        if (_canvas == null) return;

        // Random trail properties
        var trailLength = TRAIL_MIN_LENGTH + _random.NextDouble() * (TRAIL_MAX_LENGTH - TRAIL_MIN_LENGTH);
        var speed = LIGHT_SPEED_BASE * (0.3 + _random.NextDouble() * 0.7) * _config.AnimationSpeed;
        var angle = _random.NextDouble() * Math.PI * 2; // Random direction
        
        // Create gradient for light trail effect
        var gradientBrush = CreateTrailGradient();
        
        // Create trail visual as a thin rectangle
        var trailElement = new Rectangle
        {
            Width = trailLength,
            Height = TRAIL_WIDTH,
            Fill = gradientBrush,
            Opacity = 0.4 * _config.EffectIntensity,
            RenderTransform = new RotateTransform(angle * 180 / Math.PI)
        };

        // Random starting position (off-screen)
        var margin = 100;
        var startX = _random.NextDouble() * (Bounds.Width + 2 * margin) - margin;
        var startY = _random.NextDouble() * (Bounds.Height + 2 * margin) - margin;

        // Create light trail data
        var lightTrail = new LightTrail
        {
            Element = trailElement,
            X = startX,
            Y = startY,
            VelocityX = Math.Cos(angle) * speed,
            VelocityY = Math.Sin(angle) * speed,
            Length = trailLength,
            Angle = angle,
            LifeTime = 20 + _random.NextDouble() * 30, // 20-50 seconds
            Age = 0,
            PulsePhase = _random.NextDouble() * Math.PI * 2 // Random pulse phase
        };

        _lightTrails.Add(lightTrail);
        _canvas.Children.Add(trailElement);
        
        // Set initial position
        Canvas.SetLeft(trailElement, startX);
        Canvas.SetTop(trailElement, startY);
    }

    private LinearGradientBrush CreateTrailGradient()
    {
        var colors = OceanThemeColors.GlowColors;
        var baseColor = colors[_random.Next(colors.Length)];
        var color = Color.Parse(baseColor);
        
        // Create gradient that fades from transparent to color to transparent
        var gradient = new LinearGradientBrush
        {
            StartPoint = RelativePoint.Parse("0%,50%"),
            EndPoint = RelativePoint.Parse("100%,50%")
        };

        gradient.GradientStops.Add(new GradientStop(Colors.Transparent, 0.0));
        gradient.GradientStops.Add(new GradientStop(color, 0.2));
        gradient.GradientStops.Add(new GradientStop(color, 0.8));
        gradient.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));

        return gradient;
    }

    private void StartAnimation()
    {
        if (_animationTimer != null) return;

        _animationTimer = new DispatcherTimer
        {
            Interval = _config.GetUpdateInterval()
        };
        _animationTimer.Tick += OnAnimationTick;
        _animationTimer.Start();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        if (_isDisposed || _canvas == null) return;

        _performanceManager.StartFrame();

        var deltaTime = _config.GetUpdateInterval().TotalSeconds;
        _elapsedTime += deltaTime;

        UpdateLightTrails(deltaTime);

        _performanceManager.EndFrame();
    }

    private void UpdateLightTrails(double deltaTime)
    {
        if (_canvas == null) return;

        var canvasWidth = Bounds.Width;
        var canvasHeight = Bounds.Height;
        
        for (int i = _lightTrails.Count - 1; i >= 0; i--)
        {
            var trail = _lightTrails[i];
            
            // Update position
            trail.X += trail.VelocityX * deltaTime;
            trail.Y += trail.VelocityY * deltaTime;
            trail.Age += deltaTime;
            trail.PulsePhase += deltaTime * 2 * Math.PI; // One pulse per second

            // Apply gentle wave motion for organic movement
            var waveAmplitude = 5 * _config.EffectIntensity;
            var waveFrequency = 0.5;
            var lateralOffset = Math.Sin(trail.PulsePhase * waveFrequency) * waveAmplitude;
            
            // Calculate perpendicular direction for lateral movement
            var perpAngle = trail.Angle + Math.PI / 2;
            var lateralX = Math.Cos(perpAngle) * lateralOffset;
            var lateralY = Math.Sin(perpAngle) * lateralOffset;
            
            var displayX = trail.X + lateralX;
            var displayY = trail.Y + lateralY;
            
            // Update visual position
            Canvas.SetLeft(trail.Element, displayX);
            Canvas.SetTop(trail.Element, displayY);

            // Update opacity with pulsing effect
            var baseopacity = 0.4 * _config.EffectIntensity;
            var pulseIntensity = 0.3 * _config.EffectIntensity;
            var pulse = Math.Sin(trail.PulsePhase) * 0.5 + 0.5; // Normalize to 0-1
            var currentOpacity = baseopacity + pulse * pulseIntensity;
            
            // Apply fade in/out based on age
            var normalizedAge = trail.Age / trail.LifeTime;
            var fadeMultiplier = normalizedAge < 0.1 ? normalizedAge / 0.1 : // Fade in
                               normalizedAge > 0.9 ? (1.0 - normalizedAge) / 0.1 : // Fade out
                               1.0; // Full opacity

            trail.Element.Opacity = currentOpacity * fadeMultiplier;

            // Check if trail is out of bounds or too old
            var margin = trail.Length + 50; // Consider trail length for bounds checking
            if (displayX < -margin || displayX > canvasWidth + margin ||
                displayY < -margin || displayY > canvasHeight + margin ||
                trail.Age >= trail.LifeTime)
            {
                // Remove old trail
                _canvas.Children.Remove(trail.Element);
                _lightTrails.RemoveAt(i);
                
                // Create new trail to maintain count
                if (_config.IsEffectEnabled(BackgroundEffects.FlowingLights))
                {
                    CreateLightTrail();
                }
            }
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
                    if (!_config.IsEffectEnabled(BackgroundEffects.FlowingLights))
                    {
                        ClearLightTrails();
                        StopAnimation();
                    }
                    else
                    {
                        SetupLightSystem();
                    }
                    break;
                    
                case nameof(AnimationConfiguration.Quality):
                case nameof(AnimationConfiguration.EffectIntensity):
                    CreateLightTrails();
                    break;
                    
                case nameof(AnimationConfiguration.TargetFrameRate):
                    if (_animationTimer != null)
                    {
                        _animationTimer.Interval = _config.GetUpdateInterval();
                    }
                    break;
            }
        });
    }

    private void OnPerformanceChanged(object? sender, PerformanceEventArgs e)
    {
        // Auto-adjust trail count if performance is poor
        if (e.Metrics.PerformanceRatio > 1.4 && _lightTrails.Count > 2)
        {
            Dispatcher.UIThread.Post(() =>
            {
                // Remove some trails to improve performance
                var trailsToRemove = Math.Min(2, _lightTrails.Count / 3);
                for (int i = 0; i < trailsToRemove; i++)
                {
                    var lastIndex = _lightTrails.Count - 1;
                    if (lastIndex >= 0 && _canvas != null)
                    {
                        _canvas.Children.Remove(_lightTrails[lastIndex].Element);
                        _lightTrails.RemoveAt(lastIndex);
                    }
                }
            });
        }
    }

    private void ClearLightTrails()
    {
        if (_canvas == null) return;

        _canvas.Children.Clear();
        _lightTrails.Clear();
    }

    private void StopAnimation()
    {
        _animationTimer?.Stop();
        _animationTimer = null;
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
        
        StopAnimation();
        ClearLightTrails();
        _performanceManager.Dispose();
    }
}

/// <summary>
/// Represents a single animated light trail
/// </summary>
internal class LightTrail
{
    public Rectangle Element { get; set; } = null!;
    public double X { get; set; }
    public double Y { get; set; }
    public double VelocityX { get; set; }
    public double VelocityY { get; set; }
    public double Length { get; set; }
    public double Angle { get; set; }
    public double LifeTime { get; set; }
    public double Age { get; set; }
    public double PulsePhase { get; set; }
}