using System.Runtime.InteropServices;

namespace ScrollBarOS.Helpers;

/// <summary>
/// DPI scaling helper methods
/// </summary>
public static class DpiHelper
{
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hWnd);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(nint hMonitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromPoint(POINT pt, uint dwFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X, Y;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;
    private const int MDT_EFFECTIVE_DPI = 0;
    private const uint DEFAULT_DPI = 96;

    /// <summary>
    /// Gets the DPI scale factor for a window
    /// </summary>
    public static double GetScaleFactor(nint hwnd)
    {
        uint dpi = GetDpiForWindow(hwnd);
        return dpi / (double)DEFAULT_DPI;
    }

    /// <summary>
    /// Gets the DPI scale factor for the monitor at a specific point
    /// </summary>
    public static double GetScaleFactorForPoint(int x, int y)
    {
        POINT pt = new POINT { X = x, Y = y };
        nint monitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);

        if (GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out uint dpiX, out _) == 0)
        {
            return dpiX / (double)DEFAULT_DPI;
        }

        return 1.0;
    }

    /// <summary>
    /// Scales a value based on current DPI
    /// </summary>
    public static int Scale(int value, double scaleFactor)
    {
        return (int)(value * scaleFactor);
    }

    /// <summary>
    /// Gets the current system DPI
    /// </summary>
    public static uint GetSystemDpi()
    {
        return GetDpiForWindow(nint.Zero);
    }
}
