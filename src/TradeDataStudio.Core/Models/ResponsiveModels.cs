using System.ComponentModel;

namespace TradeDataStudio.Core.Models;

/// <summary>
/// Defines screen size categories for responsive design
/// </summary>
public enum ScreenSizeCategory
{
    Small,      // ≤ 1366×768 (laptops, older monitors)
    Medium,     // 1440×900 to 1600×1200
    Large,      // 1920×1080 (standard desktop)
    ExtraLarge  // ≥ 2560×1440 (4K, ultrawide)
}

/// <summary>
/// Event args for screen size change notifications
/// </summary>
public class ScreenSizeChangedEventArgs : EventArgs
{
    public ScreenSizeCategory PreviousCategory { get; set; }
    public ScreenSizeCategory NewCategory { get; set; }
    public double PreviousWidth { get; set; }
    public double PreviousHeight { get; set; }
    public double NewWidth { get; set; }
    public double NewHeight { get; set; }
}

/// <summary>
/// Defines responsive layout parameters for different screen sizes
/// </summary>
public class ResponsiveLayoutDefinition
{
    public ScreenSizeCategory Category { get; set; }
    public string ColumnDefinitions { get; set; } = "*";
    public string RowDefinitions { get; set; } = "*";
    public double ConfigurationPanelRatio { get; set; } = 0.7;
    public double ActionsPanelRatio { get; set; } = 0.3;
    public bool UseStackedLayout { get; set; } = false;
    public ResponsiveSpacing Spacing { get; set; } = new();
}

/// <summary>
/// Defines spacing and margin values for responsive design
/// </summary>
public class ResponsiveSpacing
{
    public double CardMargin { get; set; } = 12;
    public double CardPadding { get; set; } = 14;
    public double ElementSpacing { get; set; } = 8;
    public double SectionSpacing { get; set; } = 16;
    public double ComponentMargin { get; set; } = 6;
}

/// <summary>
/// Typography scaling configuration for different screen sizes
/// </summary>
public class ResponsiveTypography
{
    public double FontScaleFactor { get; set; } = 1.0;
    public double TitleFontSize { get; set; } = 20;
    public double SubtitleFontSize { get; set; } = 12;
    public double BodyFontSize { get; set; } = 12;
    public double CaptionFontSize { get; set; } = 11;
    public double IconFontSize { get; set; } = 14;

    public static ResponsiveTypography CreateForCategory(ScreenSizeCategory category)
    {
        return category switch
        {
            ScreenSizeCategory.Small => new ResponsiveTypography
            {
                FontScaleFactor = 0.85,
                TitleFontSize = 17,
                SubtitleFontSize = 10,
                BodyFontSize = 10,
                CaptionFontSize = 9,
                IconFontSize = 12
            },
            ScreenSizeCategory.Medium => new ResponsiveTypography
            {
                FontScaleFactor = 0.95,
                TitleFontSize = 19,
                SubtitleFontSize = 11,
                BodyFontSize = 11,
                CaptionFontSize = 10,
                IconFontSize = 13
            },
            ScreenSizeCategory.Large => new ResponsiveTypography
            {
                FontScaleFactor = 1.0,
                TitleFontSize = 20,
                SubtitleFontSize = 12,
                BodyFontSize = 12,
                CaptionFontSize = 11,
                IconFontSize = 14
            },
            ScreenSizeCategory.ExtraLarge => new ResponsiveTypography
            {
                FontScaleFactor = 1.15,
                TitleFontSize = 23,
                SubtitleFontSize = 14,
                BodyFontSize = 14,
                CaptionFontSize = 12,
                IconFontSize = 16
            },
            _ => new ResponsiveTypography()
        };
    }
}

/// <summary>
/// Animation performance settings for different screen sizes
/// </summary>
public class ResponsiveAnimationSettings
{
    public bool EnableBackgroundEffects { get; set; } = true;
    public bool EnableParticleSystem { get; set; } = true;
    public bool EnableFlowingLights { get; set; } = true;
    public bool EnableTwinklingStars { get; set; } = true;
    public int ParticleCount { get; set; } = 50;
    public double AnimationSpeed { get; set; } = 1.0;
    public int FrameRate { get; set; } = 60;

    public static ResponsiveAnimationSettings CreateForCategory(ScreenSizeCategory category)
    {
        return category switch
        {
            ScreenSizeCategory.Small => new ResponsiveAnimationSettings
            {
                EnableBackgroundEffects = true,
                EnableParticleSystem = false,
                EnableFlowingLights = false,
                EnableTwinklingStars = true,
                ParticleCount = 15,
                AnimationSpeed = 0.8,
                FrameRate = 30
            },
            ScreenSizeCategory.Medium => new ResponsiveAnimationSettings
            {
                EnableBackgroundEffects = true,
                EnableParticleSystem = true,
                EnableFlowingLights = false,
                EnableTwinklingStars = true,
                ParticleCount = 30,
                AnimationSpeed = 0.9,
                FrameRate = 45
            },
            ScreenSizeCategory.Large => new ResponsiveAnimationSettings
            {
                EnableBackgroundEffects = true,
                EnableParticleSystem = true,
                EnableFlowingLights = true,
                EnableTwinklingStars = true,
                ParticleCount = 50,
                AnimationSpeed = 1.0,
                FrameRate = 60
            },
            ScreenSizeCategory.ExtraLarge => new ResponsiveAnimationSettings
            {
                EnableBackgroundEffects = true,
                EnableParticleSystem = true,
                EnableFlowingLights = true,
                EnableTwinklingStars = true,
                ParticleCount = 75,
                AnimationSpeed = 1.1,
                FrameRate = 60
            },
            _ => new ResponsiveAnimationSettings()
        };
    }
}

/// <summary>
/// Complete responsive configuration for a screen category
/// </summary>
public class ResponsiveConfiguration
{
    public ScreenSizeCategory Category { get; set; }
    public ResponsiveLayoutDefinition Layout { get; set; } = new();
    public ResponsiveTypography Typography { get; set; } = new();
    public ResponsiveAnimationSettings Animation { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public static ResponsiveConfiguration CreateForCategory(ScreenSizeCategory category)
    {
        var layout = CreateLayoutDefinition(category);
        var typography = ResponsiveTypography.CreateForCategory(category);
        var animation = ResponsiveAnimationSettings.CreateForCategory(category);

        return new ResponsiveConfiguration
        {
            Category = category,
            Layout = layout,
            Typography = typography,
            Animation = animation
        };
    }

    private static ResponsiveLayoutDefinition CreateLayoutDefinition(ScreenSizeCategory category)
    {
        return category switch
        {
            ScreenSizeCategory.Small => new ResponsiveLayoutDefinition
            {
                Category = category,
                ColumnDefinitions = "6*,4*",
                ConfigurationPanelRatio = 0.6,
                ActionsPanelRatio = 0.4,
                UseStackedLayout = false,
                Spacing = new ResponsiveSpacing
                {
                    CardMargin = 8,
                    CardPadding = 10,
                    ElementSpacing = 6,
                    SectionSpacing = 12,
                    ComponentMargin = 4
                }
            },
            ScreenSizeCategory.Medium => new ResponsiveLayoutDefinition
            {
                Category = category,
                ColumnDefinitions = "65*,35*",
                ConfigurationPanelRatio = 0.65,
                ActionsPanelRatio = 0.35,
                UseStackedLayout = false,
                Spacing = new ResponsiveSpacing
                {
                    CardMargin = 10,
                    CardPadding = 12,
                    ElementSpacing = 7,
                    SectionSpacing = 14,
                    ComponentMargin = 5
                }
            },
            ScreenSizeCategory.Large => new ResponsiveLayoutDefinition
            {
                Category = category,
                ColumnDefinitions = "7*,3*",
                ConfigurationPanelRatio = 0.7,
                ActionsPanelRatio = 0.3,
                UseStackedLayout = false,
                Spacing = new ResponsiveSpacing
                {
                    CardMargin = 12,
                    CardPadding = 14,
                    ElementSpacing = 8,
                    SectionSpacing = 16,
                    ComponentMargin = 6
                }
            },
            ScreenSizeCategory.ExtraLarge => new ResponsiveLayoutDefinition
            {
                Category = category,
                ColumnDefinitions = "75*,25*",
                ConfigurationPanelRatio = 0.75,
                ActionsPanelRatio = 0.25,
                UseStackedLayout = false,
                Spacing = new ResponsiveSpacing
                {
                    CardMargin = 16,
                    CardPadding = 18,
                    ElementSpacing = 10,
                    SectionSpacing = 20,
                    ComponentMargin = 8
                }
            },
            _ => new ResponsiveLayoutDefinition()
        };
    }
}