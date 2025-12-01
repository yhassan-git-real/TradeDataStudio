using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Threading;
using TradeDataStudio.Core.Models;

namespace TradeDataStudio.Desktop.Services;

/// <summary>
/// Monitors and manages animation performance to ensure smooth 60fps operation
/// </summary>
public class AnimationPerformanceManager
{
    private readonly DispatcherTimer _performanceTimer;
    private readonly Queue<double> _frameTimes = new();
    private readonly AnimationConfiguration _config;
    private readonly Stopwatch _frameStopwatch = new();
    
    private int _frameCount = 0;
    private double _currentFPS = 60.0;
    private double _averageFrameTime = 16.67; // Target: 16.67ms for 60fps
    private bool _performanceDegradationDetected = false;
    
    // Performance thresholds
    private const double TARGET_FRAME_TIME_60FPS = 16.67; // milliseconds
    private const double TARGET_FRAME_TIME_30FPS = 33.33; // milliseconds
    private const double PERFORMANCE_WARNING_THRESHOLD = 1.2; // 20% over target
    private const double PERFORMANCE_CRITICAL_THRESHOLD = 1.5; // 50% over target
    private const int FRAME_SAMPLE_SIZE = 30; // Number of frames to average
    
    public event EventHandler<PerformanceEventArgs>? PerformanceChanged;
    public event EventHandler<PerformanceEventArgs>? PerformanceWarning;

    public AnimationPerformanceManager(AnimationConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        
        // Monitor performance every 500ms
        _performanceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _performanceTimer.Tick += OnPerformanceCheck;
        _performanceTimer.Start();
        
        _frameStopwatch.Start();
    }

    /// <summary>
    /// Call this at the start of each animation frame
    /// </summary>
    public void StartFrame()
    {
        _frameStopwatch.Restart();
    }

    /// <summary>
    /// Call this at the end of each animation frame
    /// </summary>
    public void EndFrame()
    {
        var frameTime = _frameStopwatch.Elapsed.TotalMilliseconds;
        _frameTimes.Enqueue(frameTime);
        
        // Keep only recent frames for rolling average
        if (_frameTimes.Count > FRAME_SAMPLE_SIZE)
        {
            _frameTimes.Dequeue();
        }
        
        _frameCount++;
    }

    /// <summary>
    /// Get current performance metrics
    /// </summary>
    public PerformanceMetrics GetMetrics()
    {
        return new PerformanceMetrics
        {
            CurrentFPS = _currentFPS,
            AverageFrameTime = _averageFrameTime,
            TargetFrameTime = GetTargetFrameTime(),
            PerformanceDegradationDetected = _performanceDegradationDetected,
            FrameCount = _frameCount,
            IsPerformingWell = _averageFrameTime <= GetTargetFrameTime() * PERFORMANCE_WARNING_THRESHOLD
        };
    }

    /// <summary>
    /// Check if animations should be reduced for better performance
    /// </summary>
    public bool ShouldReduceQuality()
    {
        if (!_config.AdaptivePerformance)
            return false;
            
        var targetFrameTime = GetTargetFrameTime();
        return _averageFrameTime > targetFrameTime * PERFORMANCE_CRITICAL_THRESHOLD;
    }

    /// <summary>
    /// Get recommended quality level based on current performance
    /// </summary>
    public AnimationQuality GetRecommendedQuality()
    {
        if (!_config.AdaptivePerformance)
            return _config.Quality;
            
        var targetFrameTime = GetTargetFrameTime();
        
        if (_averageFrameTime <= targetFrameTime)
            return AnimationQuality.High;
        else if (_averageFrameTime <= targetFrameTime * PERFORMANCE_WARNING_THRESHOLD)
            return AnimationQuality.Medium;
        else if (_averageFrameTime <= targetFrameTime * PERFORMANCE_CRITICAL_THRESHOLD)
            return AnimationQuality.Low;
        else
            return AnimationQuality.Off;
    }

    private void OnPerformanceCheck(object? sender, EventArgs e)
    {
        if (_frameTimes.Count == 0)
            return;

        // Calculate current metrics
        _averageFrameTime = _frameTimes.Average();
        _currentFPS = 1000.0 / _averageFrameTime;
        
        var targetFrameTime = GetTargetFrameTime();
        var previousDegradation = _performanceDegradationDetected;
        _performanceDegradationDetected = _averageFrameTime > targetFrameTime * PERFORMANCE_WARNING_THRESHOLD;

        var metrics = GetMetrics();
        PerformanceChanged?.Invoke(this, new PerformanceEventArgs(metrics));

        // Raise warning if performance degraded
        if (_performanceDegradationDetected && !previousDegradation)
        {
            PerformanceWarning?.Invoke(this, new PerformanceEventArgs(metrics));
        }

        // Auto-adjust quality if adaptive performance is enabled
        if (_config.AdaptivePerformance)
        {
            var recommendedQuality = GetRecommendedQuality();
            if (recommendedQuality != _config.Quality)
            {
                // Only auto-reduce quality, don't auto-increase to avoid oscillation
                if (recommendedQuality < _config.Quality)
                {
                    _config.Quality = recommendedQuality;
                }
            }
        }
    }

    private double GetTargetFrameTime()
    {
        return _config.TargetFrameRate switch
        {
            30 => TARGET_FRAME_TIME_30FPS,
            120 => TARGET_FRAME_TIME_60FPS / 2, // 8.33ms for 120fps
            _ => TARGET_FRAME_TIME_60FPS
        };
    }

    public void Dispose()
    {
        _performanceTimer?.Stop();
        _frameStopwatch?.Stop();
    }
}

/// <summary>
/// Performance metrics for animation system
/// </summary>
public class PerformanceMetrics
{
    public double CurrentFPS { get; init; }
    public double AverageFrameTime { get; init; }
    public double TargetFrameTime { get; init; }
    public bool PerformanceDegradationDetected { get; init; }
    public int FrameCount { get; init; }
    public bool IsPerformingWell { get; init; }
    
    public double PerformanceRatio => AverageFrameTime / TargetFrameTime;
    public string PerformanceStatus => IsPerformingWell ? "Good" : 
                                     PerformanceRatio < 1.5 ? "Warning" : "Critical";
}

/// <summary>
/// Event args for performance notifications
/// </summary>
public class PerformanceEventArgs : EventArgs
{
    public PerformanceMetrics Metrics { get; }
    
    public PerformanceEventArgs(PerformanceMetrics metrics)
    {
        Metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }
}