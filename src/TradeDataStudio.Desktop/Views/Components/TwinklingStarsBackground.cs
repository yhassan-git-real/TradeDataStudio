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

public partial class TwinklingStarsBackground : UserControl
{
    private readonly AnimationConfiguration _config;
    private readonly AnimationPerformanceManager _performanceManager;
    private readonly List<Star> _stars = new();
    private readonly Random _random = new();
    
    private Canvas? _canvas;
    private DispatcherTimer? _animationTimer;
    private double _elapsedTime = 0;
    private bool _isDisposed = false;

    // Star pattern settings
    private const int STARS_PER_TILE = 8; // Based on existing StarPatternBrush
    private const double TILE_SIZE = 120; // Based on existing DestinationRect
    private const double STAR_MIN_SIZE = 0.8; // Increased for better visibility
    private const double STAR_MAX_SIZE = 3.5; // Increased for better visibility
    private const double TWINKLE_SPEED_BASE = 2.5; // Increased frequency for more blinking
    
    public TwinklingStarsBackground(AnimationConfiguration? config = null)
    {
        _config = config ?? new AnimationConfiguration();
        _performanceManager = new AnimationPerformanceManager(_config);
        
        InitializeComponent();
        SetupStarField();
        
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

    private void SetupStarField()
    {
        if (!_config.IsEffectEnabled(BackgroundEffects.TwinklingStars))
            return;

        CreateStarField();
        StartAnimation();
    }

    private void CreateStarField()
    {
        if (_canvas == null) return;

        _stars.Clear();
        _canvas.Children.Clear();

        var canvasWidth = Bounds.Width > 0 ? Bounds.Width : 1200;
        var canvasHeight = Bounds.Height > 0 ? Bounds.Height : 700;

        // Calculate number of tiles needed to cover the canvas
        var tilesX = (int)Math.Ceiling(canvasWidth / TILE_SIZE) + 1;
        var tilesY = (int)Math.Ceiling(canvasHeight / TILE_SIZE) + 1;

        // Quality-based star density
        var densityMultiplier = _config.Quality switch
        {
            AnimationQuality.Off => 0,
            AnimationQuality.Low => 0.3,
            AnimationQuality.Medium => 0.7,
            AnimationQuality.High => 1.0,
            _ => 0.7
        };

        var starsPerTile = (int)(STARS_PER_TILE * densityMultiplier * _config.EffectIntensity);

        // Create stars in a tiled pattern to match the original StarPatternBrush
        for (int tileX = 0; tileX < tilesX; tileX++)
        {
            for (int tileY = 0; tileY < tilesY; tileY++)
            {
                CreateStarsInTile(tileX, tileY, starsPerTile);
            }
        }
    }

    private void CreateStarsInTile(int tileX, int tileY, int starCount)
    {
        if (_canvas == null) return;

        var tileOffsetX = tileX * TILE_SIZE;
        var tileOffsetY = tileY * TILE_SIZE;

        // Predefined positions to match the original pattern where possible
        var positions = new[]
        {
            new Point(60, 30),   // Center-top
            new Point(30, 70),   // Left-bottom
            new Point(90, 70),   // Right-bottom
            new Point(15, 15),   // Top-left
            new Point(105, 105), // Bottom-right
            new Point(75, 85),   // Additional positions for higher density
            new Point(45, 50),
            new Point(100, 20)
        };

        for (int i = 0; i < starCount && i < positions.Length; i++)
        {
            var pos = positions[i];
            CreateStar(tileOffsetX + pos.X, tileOffsetY + pos.Y, i);
        }

        // Add random stars if we need more than predefined positions
        for (int i = positions.Length; i < starCount; i++)
        {
            var randomX = tileOffsetX + _random.NextDouble() * TILE_SIZE;
            var randomY = tileOffsetY + _random.NextDouble() * TILE_SIZE;
            CreateStar(randomX, randomY, i);
        }
    }

    private void CreateStar(double x, double y, int index)
    {
        if (_canvas == null) return;

        // Random star properties for natural variation
        var size = STAR_MIN_SIZE + _random.NextDouble() * (STAR_MAX_SIZE - STAR_MIN_SIZE);
        var twinkleSpeed = TWINKLE_SPEED_BASE * (0.3 + _random.NextDouble() * 0.7) * _config.AnimationSpeed; // Slower range
        var twinklePhase = _random.NextDouble() * Math.PI * 2; // Random starting phase
        var baseOpacity = 0.4 + _random.NextDouble() * 0.5; // Higher base brightness for visibility
        
        // Create star visual element
        var starElement = new Ellipse
        {
            Width = size,
            Height = size,
            Fill = GetRandomStarBrush(),
            Opacity = baseOpacity * _config.EffectIntensity,
        };

        // Create star data
        var star = new Star
        {
            Element = starElement,
            X = x,
            Y = y,
            Size = size,
            TwinkleSpeed = twinkleSpeed,
            TwinklePhase = twinklePhase,
            BaseOpacity = baseOpacity,
            TwinkleIntensity = 0.5 + _random.NextDouble() * 0.4 // Stronger twinkle effect
        };

        _stars.Add(star);
        _canvas.Children.Add(starElement);
        
        // Set position
        Canvas.SetLeft(starElement, x - size / 2); // Center the star
        Canvas.SetTop(starElement, y - size / 2);
    }

    private IBrush GetRandomStarBrush()
    {
        var colors = OceanThemeColors.StarColors;
        var colorString = colors[_random.Next(colors.Length)];
        var color = Color.Parse(colorString);
        
        // Apply effect intensity to alpha with minimum visibility
        var minAlpha = (byte)(color.A * 0.6); // Ensure minimum 60% visibility
        var adjustedAlpha = (byte)Math.Max(minAlpha, color.A * _config.EffectIntensity);
        var adjustedColor = Color.FromArgb(adjustedAlpha, color.R, color.G, color.B);
        
        return new SolidColorBrush(adjustedColor);
    }

    private void StartAnimation()
    {
        if (_animationTimer != null) return;

        // Use a faster update interval for smoother animation
        var smoothInterval = TimeSpan.FromMilliseconds(33); // ~30fps for smooth motion
        _animationTimer = new DispatcherTimer
        {
            Interval = smoothInterval
        };
        _animationTimer.Tick += OnAnimationTick;
        _animationTimer.Start();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        if (_isDisposed || _canvas == null) return;

        _performanceManager.StartFrame();

        // Use consistent deltaTime for smooth animation
        var deltaTime = 0.033; // 30fps consistent timing
        _elapsedTime += deltaTime;

        UpdateStars(deltaTime);

        _performanceManager.EndFrame();
    }

    private void UpdateStars(double deltaTime)
    {
        if (_canvas == null) return;

        for (int i = 0; i < _stars.Count; i++)
        {
            var star = _stars[i];
            
            // Update twinkle phase with slower motion
            star.TwinklePhase += star.TwinkleSpeed * deltaTime * Math.PI * 1.5; // Slower phase increment
            
            // Calculate twinkling opacity using sine wave with enhanced visibility
            var twinkle = Math.Sin(star.TwinklePhase) * 0.6 + 0.4; // More pronounced twinkle range (0.4-1.0)
            var twinkleOpacity = star.BaseOpacity + (twinkle * star.TwinkleIntensity);
            
            // Apply global effect intensity
            var finalOpacity = twinkleOpacity * _config.EffectIntensity;
            
            // Add subtle breathing effect for groups of stars (slower and more gentle)
            var groupBreathing = Math.Sin(_elapsedTime * 0.15 + i * 0.05) * 0.08 + 1.0; // Slower group pulsing
            finalOpacity *= groupBreathing;
            
            // Add individual slow drift motion for more organic feel
            var driftX = Math.Sin(_elapsedTime * 0.1 + i * 0.3) * 0.5; // Very slow horizontal drift
            var driftY = Math.Cos(_elapsedTime * 0.08 + i * 0.4) * 0.3; // Very slow vertical drift
            
            // Apply position with drift (very subtle)
            Canvas.SetLeft(star.Element, star.X - star.Size / 2 + driftX);
            Canvas.SetTop(star.Element, star.Y - star.Size / 2 + driftY);
            
            // Clamp opacity
            finalOpacity = Math.Clamp(finalOpacity, 0.0, 1.0);
            
            star.Element.Opacity = finalOpacity;
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
                    if (!_config.IsEffectEnabled(BackgroundEffects.TwinklingStars))
                    {
                        ClearStars();
                        StopAnimation();
                    }
                    else
                    {
                        SetupStarField();
                    }
                    break;
                    
                case nameof(AnimationConfiguration.Quality):
                case nameof(AnimationConfiguration.EffectIntensity):
                    CreateStarField();
                    break;
                    
                case nameof(AnimationConfiguration.AnimationSpeed):
                    // Update twinkle speeds for existing stars
                    foreach (var star in _stars)
                    {
                        star.TwinkleSpeed = TWINKLE_SPEED_BASE * (0.5 + _random.NextDouble()) * _config.AnimationSpeed;
                    }
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
        // Auto-adjust star count if performance is poor
        if (e.Metrics.PerformanceRatio > 1.3 && _stars.Count > 20)
        {
            Dispatcher.UIThread.Post(() =>
            {
                // Remove some stars to improve performance
                var starsToRemove = Math.Min(10, _stars.Count / 4);
                for (int i = 0; i < starsToRemove; i++)
                {
                    var lastIndex = _stars.Count - 1;
                    if (lastIndex >= 0 && _canvas != null)
                    {
                        _canvas.Children.Remove(_stars[lastIndex].Element);
                        _stars.RemoveAt(lastIndex);
                    }
                }
            });
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Recreate stars when size changes significantly
        if (Math.Abs(Bounds.Width - availableSize.Width) > 50 || 
            Math.Abs(Bounds.Height - availableSize.Height) > 50)
        {
            Dispatcher.UIThread.Post(CreateStarField);
        }
        
        return base.MeasureOverride(availableSize);
    }

    private void ClearStars()
    {
        if (_canvas == null) return;

        _canvas.Children.Clear();
        _stars.Clear();
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
        ClearStars();
        _performanceManager.Dispose();
    }
}

/// <summary>
/// Represents a single twinkling star
/// </summary>
internal class Star
{
    public Ellipse Element { get; set; } = null!;
    public double X { get; set; }
    public double Y { get; set; }
    public double Size { get; set; }
    public double TwinkleSpeed { get; set; }
    public double TwinklePhase { get; set; }
    public double BaseOpacity { get; set; }
    public double TwinkleIntensity { get; set; }
}