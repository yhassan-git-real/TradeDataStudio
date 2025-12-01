using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Timers;

namespace TradeDataStudio.Desktop.Views.Components;

public partial class AnimatedBackground : UserControl
{
    private DispatcherTimer? _animationTimer;
    private double _elapsedSeconds = 0;
    private Ellipse? _leftGlow;
    private Ellipse? _rightGlow;

    public AnimatedBackground()
    {
        InitializeComponent();
        SetupAnimations();
    }

    private void SetupAnimations()
    {
        // Cache references to animated elements
        if (this.FindControl<Canvas>("AnimationCanvas") is Canvas canvas)
        {
            if (canvas.Children.Count >= 2)
            {
                _leftGlow = canvas.Children[0] as Ellipse;
                _rightGlow = canvas.Children[1] as Ellipse;
            }
        }

        // Create a lightweight animation timer
        _animationTimer = new DispatcherTimer();
        _animationTimer.Interval = TimeSpan.FromMilliseconds(50); // 20 FPS for efficiency
        _animationTimer.Tick += OnAnimationTick;
        _animationTimer.Start();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        _elapsedSeconds += 0.05; // 50ms increment

        // Smooth pulsing effect using sine wave for natural rhythm
        if (_leftGlow != null)
        {
            // Pulsates between 0.08 and 0.15 with period of 4 seconds
            double opacityLeft = 0.08 + (0.07 * (Math.Sin(_elapsedSeconds * Math.PI / 2) * 0.5 + 0.5));
            _leftGlow.Opacity = opacityLeft;
        }

        if (_rightGlow != null)
        {
            // Pulsates between 0.06 and 0.12 with period of 5 seconds (offset for variation)
            double opacityRight = 0.06 + (0.06 * (Math.Sin((_elapsedSeconds + 1) * Math.PI / 2.5) * 0.5 + 0.5));
            _rightGlow.Opacity = opacityRight;
        }

        // Reset timer periodically to prevent float overflow (every 20 seconds)
        if (_elapsedSeconds > 20)
        {
            _elapsedSeconds = 0;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _animationTimer?.Stop();
        _animationTimer = null;
    }
}
