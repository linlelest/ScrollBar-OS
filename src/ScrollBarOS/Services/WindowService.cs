using Microsoft.UI.Xaml.Media.Imaging;
using ScrollBarOS.Helpers;
using ScrollBarOS.Models;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ScrollBarOS.Services;

/// <summary>
/// Service for enumerating and managing windows
/// </summary>
public class WindowService
{
    private List<WindowInfo> _cachedWindows = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromMilliseconds(500);

    [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
    private static extern nint NativeGetForegroundWindow();

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern nint ExtractIcon(nint hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(nint hIcon);

    /// <summary>
    /// Gets the list of visible windows (with caching)
    /// </summary>
    public List<WindowInfo> GetVisibleWindows(bool forceRefresh = false)
    {
        if (!forceRefresh && DateTime.Now - _lastRefresh < _refreshInterval && _cachedWindows.Count > 0)
        {
            return _cachedWindows;
        }

        var windows = new List<WindowInfo>();
        var handles = Win32Helper.EnumerateVisibleWindows();

        foreach (var hwnd in handles)
        {
            try
            {
                var title = Win32Helper.GetWindowTitle(hwnd);
                if (string.IsNullOrWhiteSpace(title)) continue;

                var processId = Win32Helper.GetWindowProcessId(hwnd);
                string processName = string.Empty;
                string exePath = string.Empty;

                try
                {
                    using var process = Process.GetProcessById((int)processId);
                    processName = process.ProcessName;
                    exePath = process.MainModule?.FileName ?? string.Empty;
                }
                catch
                {
                    // Process may have exited or access denied
                    processName = "Unknown";
                }

                // Skip our own window
                if (processName == "ScrollBarOS") continue;

                var windowInfo = new WindowInfo
                {
                    Handle = hwnd,
                    Title = title,
                    ProcessName = processName,
                    ProcessId = (int)processId,
                    ExecutablePath = exePath,
                    IsVisible = true,
                    IsMinimized = Win32Helper.IsWindowMinimized(hwnd),
                    IsMaximized = Win32Helper.IsWindowMaximized(hwnd),
                    Position = Win32Helper.GetWindowRect(hwnd),
                    Icon = GetWindowIcon(hwnd, exePath)
                };

                windows.Add(windowInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enumerating window: {ex.Message}");
            }
        }

        _cachedWindows = windows;
        _lastRefresh = DateTime.Now;
        return windows;
    }

    /// <summary>
    /// Gets the currently focused window
    /// </summary>
    public WindowInfo? GetForegroundWindow()
    {
        nint hwnd = NativeGetForegroundWindow();
        if (hwnd == nint.Zero) return null;

        var title = Win32Helper.GetWindowTitle(hwnd);
        var processId = Win32Helper.GetWindowProcessId(hwnd);

        return new WindowInfo
        {
            Handle = hwnd,
            Title = title,
            ProcessId = (int)processId,
            IsVisible = true,
            Position = Win32Helper.GetWindowRect(hwnd)
        };
    }

    /// <summary>
    /// Brings a window to the foreground
    /// </summary>
    public bool FocusWindow(nint hwnd)
    {
        return Win32Helper.BringToForeground(hwnd);
    }

    /// <summary>
    /// Brings a window to the foreground by WindowInfo
    /// </summary>
    public bool FocusWindow(WindowInfo window)
    {
        return FocusWindow(window.Handle);
    }

    /// <summary>
    /// Gets the icon for a window
    /// </summary>
    private BitmapImage? GetWindowIcon(nint hwnd, string exePath)
    {
        try
        {
            nint iconHandle = Win32Helper.GetWindowIcon(hwnd);

            if (iconHandle == nint.Zero && !string.IsNullOrEmpty(exePath))
            {
                iconHandle = ExtractIcon(nint.Zero, exePath, 0);
            }

            return iconHandle == nint.Zero ? null : CreateBitmapImageFromIcon(iconHandle);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets icon for an executable path
    /// </summary>
    public BitmapImage? GetIconForPath(string exePath)
    {
        try
        {
            nint iconHandle = ExtractIcon(nint.Zero, exePath, 0);
            return iconHandle == nint.Zero ? null : CreateBitmapImageFromIcon(iconHandle, destroyHandle: true);
        }
        catch
        {
            return null;
        }
    }

    private static BitmapImage? CreateBitmapImageFromIcon(nint iconHandle, bool destroyHandle = false)
    {
        try
        {
            using var icon = Icon.FromHandle(iconHandle);
            using var bitmap = icon.ToBitmap();
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.SetSource(memoryStream.AsRandomAccessStream());

            if (destroyHandle)
            {
                DestroyIcon(iconHandle);
            }

            return bitmapImage;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Launches an application
    /// </summary>
    public void LaunchApp(string path, string arguments = "")
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                Arguments = arguments,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to launch app: {ex.Message}");
        }
    }

    /// <summary>
    /// Invalidates the window cache
    /// </summary>
    public void InvalidateCache()
    {
        _lastRefresh = DateTime.MinValue;
    }
}
