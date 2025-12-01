using System.ComponentModel;

namespace TradeDataStudio.Core.Models;

/// <summary>
/// Animation quality levels for performance optimization
/// </summary>
public enum AnimationQuality
{
    Off = 0,
    Low = 1,
    Medium = 2,
    High = 3
}

/// <summary>
/// Background effect types that can be individually controlled
/// </summary>
[Flags]
public enum BackgroundEffects
{
    None = 0,
    Particles = 1,
    FlowingLights = 2,
    TwinklingStars = 4,
    PulsingGlows = 8,
    All = Particles | FlowingLights | TwinklingStars | PulsingGlows
}

/// <summary>
/// Configuration model for Ocean & Teal Glass theme animations and visual effects
/// </summary>
public class AnimationConfiguration : INotifyPropertyChanged
{
    private AnimationQuality _quality = AnimationQuality.Medium;
    private BackgroundEffects _enabledEffects = BackgroundEffects.All;
    private double _particleDensity = 0.5;
    private double _animationSpeed = 1.0;
    private double _effectIntensity = 0.7;
    private bool _adaptivePerformance = true;
    private int _targetFrameRate = 60;
    private bool _showPerformanceMetrics = false;

    /// <summary>
    /// Overall animation quality level
    /// </summary>
    public AnimationQuality Quality
    {
        get => _quality;
        set
        {
            if (_quality != value)
            {
                _quality = value;
                OnPropertyChanged();
                ApplyQualityPreset();
            }
        }
    }

    /// <summary>
    /// Enabled background effects (can be combined)
    /// </summary>
    public BackgroundEffects EnabledEffects
    {
        get => _enabledEffects;
        set
        {
            if (_enabledEffects != value)
            {
                _enabledEffects = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Particle density (0.0 = None, 1.0 = Maximum)
    /// </summary>
    public double ParticleDensity
    {
        get => _particleDensity;
        set
        {
            var clampedValue = Math.Clamp(value, 0.0, 1.0);
            if (Math.Abs(_particleDensity - clampedValue) > 0.001)
            {
                _particleDensity = clampedValue;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Animation speed multiplier (0.1 = Very slow, 2.0 = Fast)
    /// </summary>
    public double AnimationSpeed
    {
        get => _animationSpeed;
        set
        {
            var clampedValue = Math.Clamp(value, 0.1, 2.0);
            if (Math.Abs(_animationSpeed - clampedValue) > 0.001)
            {
                _animationSpeed = clampedValue;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Overall effect intensity (0.0 = Subtle, 1.0 = Full)
    /// </summary>
    public double EffectIntensity
    {
        get => _effectIntensity;
        set
        {
            var clampedValue = Math.Clamp(value, 0.0, 1.0);
            if (Math.Abs(_effectIntensity - clampedValue) > 0.001)
            {
                _effectIntensity = clampedValue;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Whether to automatically adjust quality based on performance
    /// </summary>
    public bool AdaptivePerformance
    {
        get => _adaptivePerformance;
        set
        {
            if (_adaptivePerformance != value)
            {
                _adaptivePerformance = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Target frame rate for animations (30, 60, 120)
    /// </summary>
    public int TargetFrameRate
    {
        get => _targetFrameRate;
        set
        {
            var allowedRates = new[] { 30, 60, 120 };
            var validRate = allowedRates.OrderBy(r => Math.Abs(r - value)).First();
            if (_targetFrameRate != validRate)
            {
                _targetFrameRate = validRate;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Whether to display performance metrics overlay
    /// </summary>
    public bool ShowPerformanceMetrics
    {
        get => _showPerformanceMetrics;
        set
        {
            if (_showPerformanceMetrics != value)
            {
                _showPerformanceMetrics = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Apply preset values based on quality level
    /// </summary>
    private void ApplyQualityPreset()
    {
        switch (Quality)
        {
            case AnimationQuality.Off:
                EnabledEffects = BackgroundEffects.None;
                break;
                
            case AnimationQuality.Low:
                EnabledEffects = BackgroundEffects.PulsingGlows | BackgroundEffects.TwinklingStars;
                ParticleDensity = 0.2;
                AnimationSpeed = 0.7;
                EffectIntensity = 0.4;
                TargetFrameRate = 30;
                break;
                
            case AnimationQuality.Medium:
                EnabledEffects = BackgroundEffects.All & ~BackgroundEffects.FlowingLights; // All except flowing lights
                ParticleDensity = 0.5;
                AnimationSpeed = 1.0;
                EffectIntensity = 0.7;
                TargetFrameRate = 60;
                break;
                
            case AnimationQuality.High:
                EnabledEffects = BackgroundEffects.All;
                ParticleDensity = 0.8;
                AnimationSpeed = 1.2;
                EffectIntensity = 1.0;
                TargetFrameRate = 60;
                break;
        }
    }

    /// <summary>
    /// Get animation update interval based on target frame rate
    /// </summary>
    public TimeSpan GetUpdateInterval() => TimeSpan.FromMilliseconds(1000.0 / TargetFrameRate);

    /// <summary>
    /// Check if a specific effect is enabled
    /// </summary>
    public bool IsEffectEnabled(BackgroundEffects effect) => EnabledEffects.HasFlag(effect);

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Ocean & Teal Glass theme color constants for animations
/// </summary>
public static class OceanThemeColors
{
    // Primary accent colors
    public const string ElectricBlue = "#00E5FF";
    public const string AzureBlue = "#26C6DA";
    public const string SkyBlue = "#00B8D4";
    public const string CyanAccent = "#84FFFF";
    
    // Background colors
    public const string DeepNavy = "#02111B";
    public const string RichDarkBlue = "#052633";
    public const string MidnightBlue = "#083D4F";
    public const string SlateBlue = "#13566B";
    
    // Effect colors with opacity variants
    public static readonly string[] ParticleColors = 
    {
        "#1000E5FF", "#1526C6DA", "#1000B8D4", "#1284FFFF"
    };
    
    public static readonly string[] GlowColors = 
    {
        "#0800E5FF", "#0826C6DA", "#0A00B8D4", "#0684FFFF"
    };
    
    public static readonly string[] StarColors = 
    {
        "#FF00E5FF", "#FF26C6DA", "#FF84FFFF"
    };
}