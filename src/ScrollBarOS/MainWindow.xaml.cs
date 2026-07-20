using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Windowing;
using System.Runtime.InteropServices;
using Windows.Graphics;
using ScrollBarOS.Services;
using ScrollBarOS.Helpers;
using ScrollBarOS.Models;

namespace ScrollBarOS;

public sealed partial class MainWindow : Window
{
    private readonly ConfigService _configService;
    private readonly WindowService _windowService;
    private readonly ScrollStateMachine _scrollStateMachine;
    private readonly TaskbarService _taskbarService;
    private readonly HardwareMonitorService _hardwareMonitor;
    private readonly TilingService _tilingService;
    private readonly TrayService _trayService;

    private nint _hwnd;
    private NotifyIconHelper? _notifyIcon;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize services
        _configService = ConfigService.Instance;
        _windowService = new WindowService();
        _scrollStateMachine = new ScrollStateMachine(_windowService);
        _taskbarService = new TaskbarService();
        _hardwareMonitor = new HardwareMonitorService();
        _tilingService = new TilingService(_windowService);
        _trayService = new TrayService(_windowService);

        // Set window properties
        SetupWindow();

        // Apply initial configuration
        ApplyConfiguration();

        // Start hardware monitoring
        _hardwareMonitor.Start();

        // Subscribe to config changes
        _configService.ConfigChanged += OnConfigChanged;

        // Wire up capsule events
        Capsule.SettingsRequested += Capsule_SettingsRequested;

        // Wire up scroll state machine
        _scrollStateMachine.ModeChanged += ScrollStateMachine_ModeChanged;

        // Create system tray icon
        SetupTrayIcon();

        Closed += MainWindow_Closed;
    }

    private void SetupWindow()
    {
        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        Title = "ScrollBar OS";
        ExtendsContentIntoTitleBar = true;

        // Set window style: topmost + tool window (no taskbar entry)
        Win32Helper.SetWindowStyle(_hwnd, isTopmost: true, isToolWindow: true);

        // Position the window as a capsule at the screen edge
        PositionCapsuleWindow();
    }

    /// <summary>
    /// Positions the window as a small capsule at the right/left edge of the screen
    /// </summary>
    private void PositionCapsuleWindow()
    {
        var config = _configService.Config;
        var workArea = Win32Helper.GetPrimaryMonitorWorkArea();

        int capsuleWidth = config.CapsuleWidth + 16; // padding
        int capsuleHeight = (int)(workArea.Height * config.CapsuleHeightPercent);
        int x, y;

        // Vertically centered
        y = workArea.Y + (workArea.Height - capsuleHeight) / 2;

        // Horizontally at edge
        if (config.CapsulePosition == CapsulePosition.Right)
        {
            x = workArea.X + workArea.Width - capsuleWidth - 4;
        }
        else
        {
            x = workArea.X + 4;
        }

        var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(_hwnd));
        appWindow.MoveAndResize(new RectInt32(x, y, capsuleWidth, capsuleHeight));
    }

    private void SetupTrayIcon()
    {
        _notifyIcon = new NotifyIconHelper(_hwnd);
        _notifyIcon.OnShowSettings += () =>
        {
            DispatcherQueue.TryEnqueue(() => Capsule_SettingsRequested(this, EventArgs.Empty));
        };
        _notifyIcon.OnExit += () =>
        {
            DispatcherQueue.TryEnqueue(() => Close());
        };
        _notifyIcon.OnToggleTaskbar += () =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _taskbarService.Toggle();
                _configService.Config.HideTaskbar = _taskbarService.IsHidden;
                _configService.Save();
            });
        };
        _notifyIcon.Create();
    }

    private void ScrollStateMachine_ModeChanged(object? sender, ScrollModeChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            // Mode change handling - capsule updates internally
        });
    }

    private void ApplyConfiguration()
    {
        var config = _configService.Config;

        if (config.HideTaskbar)
        {
            _taskbarService.Hide();
        }

        Capsule.UpdatePosition(config.CapsulePosition);
        Capsule.UpdateAppearance(config);
    }

    private void OnConfigChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ApplyConfiguration();
            PositionCapsuleWindow();
        });
    }

    private void Capsule_SettingsRequested(object? sender, EventArgs e)
    {
        // Open settings as a new window
        var settingsWindow = new SettingsWindow();
        settingsWindow.Activate();
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        _notifyIcon?.Dispose();
        _hardwareMonitor.Stop();
        _taskbarService.Restore();
        _configService.ConfigChanged -= OnConfigChanged;
    }

    // Expose services to child controls
    public ConfigService ConfigServiceInstance => _configService;
    public WindowService WindowServiceInstance => _windowService;
    public ScrollStateMachine ScrollStateMachineInstance => _scrollStateMachine;
    public TilingService TilingServiceInstance => _tilingService;
    public HardwareMonitorService HardwareMonitorInstance => _hardwareMonitor;
    public TrayService TrayServiceInstance => _trayService;
}

/// <summary>
/// Helper class for system tray icon using Shell_NotifyIcon
/// </summary>
public class NotifyIconHelper : IDisposable
{
    private readonly nint _hwnd;
    private readonly uint _callbackMessage = 0x0400 + 1; // WM_APP + 1

    public event Action? OnShowSettings;
    public event Action? OnExit;
    public event Action? OnToggleTaskbar;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public nint hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public nint hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
    }

    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;
    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_DELETE = 0x00000002;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_RBUTTONUP = 0x0205;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint LoadIcon(nint hInstance, string lpIconName);

    [DllImport("user32.dll")]
    private static extern nint GetModuleHandle(string? lpModuleName);

    private nint _oldWndProc;
    private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);
    private WndProcDelegate? _wndProcDelegate;

    [DllImport("user32.dll")]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll")]
    private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern nint CallWindowProc(nint lpPrevWndFunc, nint hWnd, uint Msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern nint DefWindowProc(nint hWnd, uint Msg, nint wParam, nint lParam);

    private const int GWLP_WNDPROC = -4;

    public NotifyIconHelper(nint hwnd)
    {
        _hwnd = hwnd;
    }

    public void Create()
    {
        // Subclass the window to receive tray icon messages
        _wndProcDelegate = WndProc;
        _oldWndProc = GetWindowLongPtr(_hwnd, GWLP_WNDPROC);
        SetWindowLongPtr(_hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));

        var nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = _callbackMessage,
            hIcon = LoadDefaultIcon(),
            szTip = "ScrollBar OS"
        };

        Shell_NotifyIcon(NIM_ADD, ref nid);
    }

    private nint LoadDefaultIcon()
    {
        // Use the default application icon
        nint hModule = GetModuleHandle(null);
        nint icon = LoadIcon(hModule, "32512"); // IDI_APPLICATION
        return icon;
    }

    private nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == _callbackMessage)
        {
            uint mouseMsg = (uint)(lParam.ToInt64() & 0xFFFF);
            if (mouseMsg == WM_LBUTTONUP)
            {
                OnShowSettings?.Invoke();
            }
            else if (mouseMsg == WM_RBUTTONUP)
            {
                ShowContextMenu();
            }
            return nint.Zero;
        }

        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    private void ShowContextMenu()
    {
        // Simple approach: toggle taskbar on right-click for now
        OnToggleTaskbar?.Invoke();
    }

    public void Dispose()
    {
        var nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = 1
        };
        Shell_NotifyIcon(NIM_DELETE, ref nid);

        if (_oldWndProc != nint.Zero)
        {
            SetWindowLongPtr(_hwnd, GWLP_WNDPROC, _oldWndProc);
        }
    }
}
