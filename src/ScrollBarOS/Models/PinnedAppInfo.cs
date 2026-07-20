using Microsoft.UI.Xaml.Media.Imaging;

namespace ScrollBarOS.Models;

/// <summary>
/// Represents a pinned application shortcut
/// </summary>
public class PinnedAppInfo
{
    /// <summary>Application display name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Path to the executable</summary>
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>Application icon</summary>
    public BitmapImage? Icon { get; set; }

    /// <summary>Command line arguments (optional)</summary>
    public string Arguments { get; set; } = string.Empty;

    /// <summary>Whether this is a system app</summary>
    public bool IsSystemApp { get; set; }

    public PinnedAppInfo() { }
}
