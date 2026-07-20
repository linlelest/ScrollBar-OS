using System.Text.Json.Serialization;

namespace ScrollBarOS.Models;

/// <summary>
/// Application configuration model
/// </summary>
public class AppConfig
{
    /// <summary>Capsule background color (ARGB hex string, e.g. "#CC1E1E2E")</summary>
    public string BackgroundColor { get; set; } = "#CC1E1E2E";

    /// <summary>Capsule background material type</summary>
    public BackgroundMaterial Material { get; set; } = BackgroundMaterial.Acrylic;

    /// <summary>Capsule position on screen</summary>
    public CapsulePosition CapsulePosition { get; set; } = CapsulePosition.Right;

    /// <summary>Capsule width in pixels</summary>
    public int CapsuleWidth { get; set; } = 64;

    /// <summary>Capsule height as percentage of screen height</summary>
    public double CapsuleHeightPercent { get; set; } = 0.33;

    /// <summary>Icon size in pixels</summary>
    public int IconSize { get; set; } = 36;

    /// <summary>Font size for text elements</summary>
    public int FontSize { get; set; } = 12;

    /// <summary>Whether to show date/time widget</summary>
    public bool ShowDateTimeWidget { get; set; } = false;

    /// <summary>Whether to show hardware info widget</summary>
    public bool ShowHardwareWidget { get; set; } = true;

    /// <summary>Whether to hide the system taskbar</summary>
    public bool HideTaskbar { get; set; } = false;

    /// <summary>Application language</summary>
    public AppLanguage Language { get; set; } = AppLanguage.Chinese;

    /// <summary>Pinned application paths</summary>
    public List<string> PinnedApps { get; set; } = new();

    /// <summary>Whether to start with Windows</summary>
    public bool StartWithWindows { get; set; } = false;

    /// <summary>Scroll speed threshold (number of scroll ticks in 200ms to trigger fast mode)</summary>
    public int ScrollThreshold { get; set; } = 3;

    /// <summary>Corner radius of the capsule</summary>
    public int CornerRadius { get; set; } = 20;

    /// <summary>Background opacity (0.0 - 1.0)</summary>
    public double BackgroundOpacity { get; set; } = 0.8;
}

/// <summary>
/// Background material types for the capsule
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BackgroundMaterial
{
    Solid,
    Gradient,
    Mica,
    Acrylic
}

/// <summary>
/// Capsule screen position
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CapsulePosition
{
    Left,
    Right
}

/// <summary>
/// Application language
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppLanguage
{
    Chinese,
    English
}
