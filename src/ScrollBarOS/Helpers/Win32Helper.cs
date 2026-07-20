using ScrollBarOS.Models;
using System.Runtime.InteropServices;

namespace ScrollBarOS.Helpers;

/// <summary>
/// Win32 API helper methods for window management
/// </summary>
public static class Win32Helper
{
    #region Constants

    private const int GWL_EXSTYLE = -20;
    private const int GWL_STYLE = -16;
    private const uint WS_EX_LAYERED = 0x00080000;
    private const uint WS_EX_TRANSPARENT = 0x00000020;
    private const uint WS_EX_TOPMOST = 0x00000008;
    private const uint WS_EX_TOOLWINDOW = 0x00000080;
    private const uint WS_EX_NOACTIVATE = 0x08000000;
    private const uint WS_VISIBLE = 0x10000000;
    private const uint WS_MINIMIZE = 0x20000000;
    private const uint WS_MAXIMIZE = 0x01000000;
    private const uint LWA_ALPHA = 0x00000002;
    private const uint LWA_COLORKEY = 0x00000001;

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SWP_ASYNCWINDOWPOS = 0x4000;

    private static readonly nint HWND_TOPMOST = new nint(-1);
    private static readonly nint HWND_NOTOPMOST = new nint(-2);
    private static readonly nint HWND_TOP = new nint(0);

    private const int GWL_HWNDPARENT = -8;
    private const int WM_GETICON = 0x007F;
    private const int ICON_SMALL = 0;
    private const int ICON_BIG = 1;
    private const int ICON_SMALL2 = 2;

    #endregion

    #region DllImports

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetLayeredWindowAttributes(nint hwnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsZoomed(nint hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BringWindowToTop(nint hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern nint SendMessage(nint hWnd, uint Msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("kernel32.dll")]
    private static extern uint GetLastError();

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X, Y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;
    private const uint SPI_GETWORKAREA = 0x0030;

    #endregion

    #region Window Style Methods

    /// <summary>
    /// Sets window to topmost and/or tool window style
    /// </summary>
    public static void SetWindowStyle(nint hwnd, bool isTopmost = false, bool isToolWindow = false)
    {
        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

        if (isTopmost)
            exStyle |= (int)WS_EX_TOPMOST;
        else
            exStyle &= ~(int)WS_EX_TOPMOST;

        if (isToolWindow)
            exStyle |= (int)WS_EX_TOOLWINDOW | (int)WS_EX_NOACTIVATE;

        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);

        if (isTopmost)
        {
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }
    }

    /// <summary>
    /// Makes a window transparent using layered window attributes
    /// </summary>
    public static void SetWindowTransparent(nint hwnd)
    {
        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        exStyle |= (int)WS_EX_LAYERED;
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
        SetLayeredWindowAttributes(hwnd, 0, 255, LWA_ALPHA);
    }

    /// <summary>
    /// Sets or removes click-through (transparent to mouse) on a window
    /// </summary>
    public static void SetClickThrough(nint hwnd, bool clickThrough)
    {
        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

        if (clickThrough)
            exStyle |= (int)WS_EX_TRANSPARENT;
        else
            exStyle &= ~(int)WS_EX_TRANSPARENT;

        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
    }

    #endregion

    #region Window Enumeration

    /// <summary>
    /// Enumerates all visible top-level windows
    /// </summary>
    public static List<nint> EnumerateVisibleWindows()
    {
        var windows = new List<nint>();

        EnumWindows((hwnd, lParam) =>
        {
            if (IsWindowVisible(hwnd) && GetWindowTextLength(hwnd) > 0)
            {
                // Filter out tool windows and the desktop
                int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                if ((exStyle & (int)WS_EX_TOOLWINDOW) == 0)
                {
                    windows.Add(hwnd);
                }
            }
            return true;
        }, nint.Zero);

        return windows;
    }

    /// <summary>
    /// Gets the title of a window
    /// </summary>
    public static string GetWindowTitle(nint hwnd)
    {
        int length = GetWindowTextLength(hwnd);
        if (length == 0) return string.Empty;

        var sb = new System.Text.StringBuilder(length + 1);
        GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    /// <summary>
    /// Gets the process ID for a window
    /// </summary>
    public static uint GetWindowProcessId(nint hwnd)
    {
        GetWindowThreadProcessId(hwnd, out uint processId);
        return processId;
    }

    /// <summary>
    /// Gets the window rectangle
    /// </summary>
    public static WindowRect GetWindowRect(nint hwnd)
    {
        GetWindowRect(hwnd, out RECT rect);
        return new WindowRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
    }

    /// <summary>
    /// Checks if a window is minimized
    /// </summary>
    public static bool IsWindowMinimized(nint hwnd) => IsIconic(hwnd);

    /// <summary>
    /// Checks if a window is maximized
    /// </summary>
    public static bool IsWindowMaximized(nint hwnd) => IsZoomed(hwnd);

    #endregion

    #region Window Operations

    /// <summary>
    /// Brings a window to the foreground
    /// </summary>
    public static bool BringToForeground(nint hwnd)
    {
        if (IsIconic(hwnd))
        {
            ShowWindow(hwnd, SW_RESTORE);
        }

        // Use the AttachThreadInput trick to allow SetForegroundWindow to work
        nint foreWnd = GetForegroundWindow();
        uint foreThread = GetWindowThreadProcessId(foreWnd, out _);
        uint appThread = GetCurrentThreadId();

        if (foreThread != appThread)
        {
            AttachThreadInput(foreThread, appThread, true);
            bool result = SetForegroundWindow(hwnd);
            BringWindowToTop(hwnd);
            AttachThreadInput(foreThread, appThread, false);
            return result;
        }

        return SetForegroundWindow(hwnd);
    }

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    /// <summary>
    /// Sets window position and size
    /// </summary>
    public static void SetWindowPosition(nint hwnd, int x, int y, int width, int height)
    {
        SetWindowPos(hwnd, HWND_TOP, x, y, width, height, SWP_SHOWWINDOW | SWP_ASYNCWINDOWPOS);
    }

    /// <summary>
    /// Hides a window
    /// </summary>
    public static void HideWindow(nint hwnd) => ShowWindow(hwnd, SW_HIDE);

    /// <summary>
    /// Shows a window
    /// </summary>
    public static void ShowWindow(nint hwnd) => ShowWindow(hwnd, SW_SHOW);

    private const int SW_MINIMIZE = 6;
    private const uint WM_CLOSE = 0x0010;
    private const uint WS_CAPTION = 0x00C00000;
    private const uint WS_THICKFRAME = 0x00040000;
    private const uint WS_BORDER = 0x00800000;
    private const uint WS_DLGFRAME = 0x00400000;
    private const uint WS_SYSMENU = 0x00080000;
    private const uint WS_MINIMIZEBOX = 0x00020000;
    private const uint WS_MAXIMIZEBOX = 0x00010000;

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hWnd, uint Msg, nint wParam, nint lParam);

    /// <summary>
    /// Minimizes a window
    /// </summary>
    public static void MinimizeWindow(nint hwnd) => ShowWindow(hwnd, SW_MINIMIZE);

    /// <summary>
    /// Closes a window by posting WM_CLOSE
    /// </summary>
    public static void CloseWindow(nint hwnd) => PostMessage(hwnd, WM_CLOSE, nint.Zero, nint.Zero);

    /// <summary>
    /// Removes window border/titlebar to make it borderless
    /// </summary>
    public static void RemoveWindowBorder(nint hwnd)
    {
        int style = GetWindowLong(hwnd, GWL_STYLE);
        style &= ~(int)(WS_CAPTION | WS_THICKFRAME | WS_BORDER | WS_DLGFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
        SetWindowLong(hwnd, GWL_STYLE, style);

        // Also add WS_EX_LAYERED for transparent background support
        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        exStyle |= (int)WS_EX_LAYERED;
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
        SetLayeredWindowAttributes(hwnd, 0, 255, LWA_ALPHA);
    }

    #endregion

    #region Taskbar

    /// <summary>
    /// Finds the taskbar window handle
    /// </summary>
    public static nint FindTaskbar()
    {
        return FindWindow("Shell_TrayWnd", null);
    }

    /// <summary>
    /// Hides the taskbar
    /// </summary>
    public static void HideTaskbar()
    {
        nint taskbar = FindTaskbar();
        if (taskbar != nint.Zero)
        {
            ShowWindow(taskbar, SW_HIDE);
        }

        // Also hide the secondary taskbar (for multi-monitor)
        nint secondaryTaskbar = FindWindow("Shell_SecondaryTrayWnd", null);
        while (secondaryTaskbar != nint.Zero)
        {
            ShowWindow(secondaryTaskbar, SW_HIDE);
            secondaryTaskbar = FindWindow("Shell_SecondaryTrayWnd", null);
        }
    }

    /// <summary>
    /// Shows the taskbar
    /// </summary>
    public static void ShowTaskbar()
    {
        nint taskbar = FindTaskbar();
        if (taskbar != nint.Zero)
        {
            ShowWindow(taskbar, SW_SHOW);
        }

        nint secondaryTaskbar = FindWindow("Shell_SecondaryTrayWnd", null);
        while (secondaryTaskbar != nint.Zero)
        {
            ShowWindow(secondaryTaskbar, SW_SHOW);
            secondaryTaskbar = FindWindow("Shell_SecondaryTrayWnd", null);
        }
    }

    #endregion

    #region Monitor Info

    /// <summary>
    /// Gets the primary monitor work area
    /// </summary>
    public static WindowRect GetPrimaryMonitorWorkArea()
    {
        RECT rect = new RECT();
        SystemParametersInfo(SPI_GETWORKAREA, 0, ref rect, 0);
        return new WindowRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
    }

    /// <summary>
    /// Gets the work area of the monitor where the cursor is located
    /// </summary>
    public static WindowRect GetCurrentMonitorWorkArea()
    {
        GetCursorPos(out POINT pt);
        nint monitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);

        MONITORINFO mi = new MONITORINFO();
        mi.cbSize = (uint)Marshal.SizeOf<MONITORINFO>();

        if (GetMonitorInfo(monitor, ref mi))
        {
            return new WindowRect(mi.rcWork.Left, mi.rcWork.Top, mi.rcWork.Right, mi.rcWork.Bottom);
        }

        return GetPrimaryMonitorWorkArea();
    }

    /// <summary>
    /// Gets the full monitor rectangle (including taskbar area)
    /// </summary>
    public static WindowRect GetCurrentMonitorRect()
    {
        GetCursorPos(out POINT pt);
        nint monitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);

        MONITORINFO mi = new MONITORINFO();
        mi.cbSize = (uint)Marshal.SizeOf<MONITORINFO>();

        if (GetMonitorInfo(monitor, ref mi))
        {
            return new WindowRect(mi.rcMonitor.Left, mi.rcMonitor.Top, mi.rcMonitor.Right, mi.rcMonitor.Bottom);
        }

        return GetPrimaryMonitorWorkArea();
    }

    #endregion

    #region Window Icon

    /// <summary>
    /// Gets the icon handle for a window
    /// </summary>
    public static nint GetWindowIcon(nint hwnd)
    {
        nint icon = SendMessage(hwnd, WM_GETICON, (nint)ICON_SMALL, nint.Zero);
        if (icon == nint.Zero)
        {
            icon = SendMessage(hwnd, WM_GETICON, (nint)ICON_BIG, nint.Zero);
        }
        if (icon == nint.Zero)
        {
            icon = SendMessage(hwnd, WM_GETICON, (nint)ICON_SMALL2, nint.Zero);
        }
        return icon;
    }

    #endregion
}
