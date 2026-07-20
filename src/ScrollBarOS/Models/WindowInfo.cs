using Microsoft.UI.Xaml.Media.Imaging;

namespace ScrollBarOS.Models;

/// <summary>
/// Represents information about a running window
/// </summary>
public class WindowInfo
{
    /// <summary>Window handle (HWND)</summary>
    public nint Handle { get; set; }

    /// <summary>Window title</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Process name</summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>Process ID</summary>
    public int ProcessId { get; set; }

    /// <summary>Window icon as bitmap</summary>
    public BitmapImage? Icon { get; set; }

    /// <summary>Path to the executable</summary>
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>Whether the window is currently visible</summary>
    public bool IsVisible { get; set; }

    /// <summary>Whether the window is minimized</summary>
    public bool IsMinimized { get; set; }

    /// <summary>Whether the window is maximized</summary>
    public bool IsMaximized { get; set; }

    /// <summary>Window position and size</summary>
    public WindowRect Position { get; set; }

    public override string ToString() => Title;
}

/// <summary>
/// Represents a window rectangle
/// </summary>
public struct WindowRect
{
    public int Left { get; set; }
    public int Top { get; set; }
    public int Right { get; set; }
    public int Bottom { get; set; }

    public int X => Left;
    public int Y => Top;
    public int Width => Right - Left;
    public int Height => Bottom - Top;

    public WindowRect(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }
}
