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

public partial class ParticleSystemBackground : UserControl
{
    private readonly AnimationConfiguration _config;
    private readonly AnimationPerformanceManager _performanceManager;
    private readonly List<Particle> _particles = new();
    private readonly Random _random = new();
    
    private Canvas? _canvas;
    private DispatcherTimer? _animationTimer;
    private double _elapsedTime = 0;
    private bool _isDisposed = false;

    // Performance settings
    private const int MAX_PARTICLES = 50;
    private const double PARTICLE_MIN_SIZE = 1.0;
    private const double PARTICLE_MAX_SIZE = 3.0;
    private const double PARTICLE_SPEED_RANGE = 20.0; // pixels per second
    
    public ParticleSystemBackground(AnimationConfiguration? config = null)
    {
        _config = config ?? new AnimationConfiguration();
        _performanceManager = new AnimationPerformanceManager(_config);
        
        InitializeComponent();
        SetupParticleSystem();
        
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

    private void SetupParticleSystem()
    {
        if (!_config.IsEffectEnabled(BackgroundEffects.Particles))
            return;

        CreateParticles();
        StartAnimation();
    }

    private void CreateParticles()
    {
        if (_canvas == null) return;

        _particles.Clear();
        _canvas.Children.Clear();

        // Calculate particle count based on configuration
        var baseCount = (int)(MAX_PARTICLES * _config.ParticleDensity);
        var particleCount = _config.Quality switch
        {
            AnimationQuality.Off => 0,
            AnimationQuality.Low => Math.Max(1, baseCount / 3),
            AnimationQuality.Medium => Math.Max(1, baseCount / 2),
            AnimationQuality.High => baseCount,
            _ => baseCount
        };

        for (int i = 0; i < particleCount; i++)
        {
            CreateParticle();
        }
    }

    private void CreateParticle()
    {
        if (_canvas == null) return;

        // Random properties for natural movement
        var size = PARTICLE_MIN_SIZE + _random.NextDouble() * (PARTICLE_MAX_SIZE - PARTICLE_MIN_SIZE);
        var speed = (0.3 + _random.NextDouble() * 0.7) * PARTICLE_SPEED_RANGE * _config.AnimationSpeed;
        var direction = _random.NextDouble() * Math.PI * 2; // Random direction in radians
        
        // Start from random position
        var startX = _random.NextDouble() * (Bounds.Width + 100) - 50; // Allow off-screen spawn
        var startY = _random.NextDouble() * (Bounds.Height + 100) - 50;

        // Create visual element
        var ellipse = new Ellipse
        {
            Width = size,
            Height = size,
            Fill = GetRandomParticleBrush(),
            Opacity = 0.3 + _random.NextDouble() * 0.4, // Vary opacity for depth
        };

        // Create particle data
        var particle = new Particle
        {
            Element = ellipse,
            X = startX,
            Y = startY,
            VelocityX = Math.Cos(direction) * speed,
            VelocityY = Math.Sin(direction) * speed,
            Size = size,
            LifeTime = 10 + _random.NextDouble() * 20, // 10-30 seconds
            Age = 0,
            RotationSpeed = (_random.NextDouble() - 0.5) * 60 * _config.AnimationSpeed // degrees per second
        };

        _particles.Add(particle);
        _canvas.Children.Add(ellipse);
        
        // Set initial position
        Canvas.SetLeft(ellipse, startX);
        Canvas.SetTop(ellipse, startY);
    }

    private IBrush GetRandomParticleBrush()
    {
        var colors = OceanThemeColors.ParticleColors;
        var baseColor = colors[_random.Next(colors.Length)];
        
        // Adjust opacity based on effect intensity
        var alpha = (int)(255 * _config.EffectIntensity * (0.1 + _random.NextDouble() * 0.2));
        var color = Color.Parse(baseColor);
        var adjustedColor = Color.FromArgb((byte)alpha, color.R, color.G, color.B);
        
        return new SolidColorBrush(adjustedColor);
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

        UpdateParticles(deltaTime);

        _performanceManager.EndFrame();
    }

    private void UpdateParticles(double deltaTime)
    {
        if (_canvas == null) return;

        var canvasWidth = Bounds.Width;
        var canvasHeight = Bounds.Height;
        
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];
            
            // Update position
            particle.X += particle.VelocityX * deltaTime;
            particle.Y += particle.VelocityY * deltaTime;
            particle.Age += deltaTime;

            // Apply gentle sine wave for organic movement
            var waveOffset = Math.Sin(_elapsedTime * 0.5 + i * 0.3) * 10 * _config.EffectIntensity;
            var displayX = particle.X + waveOffset;
            
            // Update visual position
            Canvas.SetLeft(particle.Element, displayX);
            Canvas.SetTop(particle.Element, particle.Y);

            // Update opacity based on age for fade in/out effect
            var normalizedAge = particle.Age / particle.LifeTime;
            var opacity = normalizedAge < 0.1 ? normalizedAge / 0.1 : // Fade in
                         normalizedAge > 0.9 ? (1.0 - normalizedAge) / 0.1 : // Fade out
                         1.0; // Full opacity

            particle.Element.Opacity = opacity * _config.EffectIntensity * 0.6;

            // Check if particle is out of bounds or too old
            var margin = 100; // Allow some off-screen movement
            if (displayX < -margin || displayX > canvasWidth + margin ||
                particle.Y < -margin || particle.Y > canvasHeight + margin ||
                particle.Age >= particle.LifeTime)
            {
                // Remove old particle
                _canvas.Children.Remove(particle.Element);
                _particles.RemoveAt(i);
                
                // Create new particle to maintain count
                if (_config.IsEffectEnabled(BackgroundEffects.Particles))
                {
                    CreateParticle();
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
                    if (!_config.IsEffectEnabled(BackgroundEffects.Particles))
                    {
                        ClearParticles();
                        StopAnimation();
                    }
                    else
                    {
                        SetupParticleSystem();
                    }
                    break;
                    
                case nameof(AnimationConfiguration.Quality):
                case nameof(AnimationConfiguration.ParticleDensity):
                    CreateParticles();
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
        // Auto-adjust particle count if performance is poor
        if (e.Metrics.PerformanceRatio > 1.3 && _particles.Count > 5)
        {
            Dispatcher.UIThread.Post(() =>
            {
                // Remove some particles to improve performance
                var particlesToRemove = Math.Min(5, _particles.Count / 4);
                for (int i = 0; i < particlesToRemove; i++)
                {
                    var lastIndex = _particles.Count - 1;
                    if (lastIndex >= 0 && _canvas != null)
                    {
                        _canvas.Children.Remove(_particles[lastIndex].Element);
                        _particles.RemoveAt(lastIndex);
                    }
                }
            });
        }
    }

    private void ClearParticles()
    {
        if (_canvas == null) return;

        _canvas.Children.Clear();
        _particles.Clear();
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
        ClearParticles();
        _performanceManager.Dispose();
    }
}

/// <summary>
/// Represents a single animated particle
/// </summary>
internal class Particle
{
    public Ellipse Element { get; set; } = null!;
    public double X { get; set; }
    public double Y { get; set; }
    public double VelocityX { get; set; }
    public double VelocityY { get; set; }
    public double Size { get; set; }
    public double LifeTime { get; set; }
    public double Age { get; set; }
    public double RotationSpeed { get; set; }
}