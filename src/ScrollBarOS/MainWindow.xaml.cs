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

        try
        {
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

            // Create system tray icon (uses its own message window)
            SetupTrayIcon();

            Closed += MainWindow_Closed;
        }
        catch (Exception ex)
        {
            // Log crash to file for diagnosis
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ScrollBarOS", "crash.log");
            try { File.WriteAllText(logPath, $"{DateTime.Now}\n{ex}"); } catch { }
            throw;
        }
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
/// System tray icon using a dedicated message-only window (safe for WinUI 3)
/// </summary>
public class NotifyIconHelper : IDisposable
{
    private nint _messageHwnd;
    private nint _mainHwnd;
    private readonly uint _callbackMessage = 0x0400 + 1; // WM_APP + 1
    private const string MESSAGE_WINDOW_CLASS = "ScrollBarOS_TrayMsgWnd";

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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASS
    {
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
    }

    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;
    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_DELETE = 0x00000002;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_DESTROY = 0x0002;
    private const nint HWND_MESSAGE = -3;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint LoadIcon(nint hInstance, nint lpIconName);

    [DllImport("user32.dll")]
    private static extern nint GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClass(ref WNDCLASS lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint CreateWindowEx(
        uint dwExStyle, string lpClassName, string? lpWindowName,
        uint dwStyle, int X, int Y, int nWidth, int nHeight,
        nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern nint DefWindowProc(nint hWnd, uint Msg, nint wParam, nint lParam);

    private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);
    private WndProcDelegate? _wndProcDelegate;

    public NotifyIconHelper(nint mainHwnd)
    {
        _mainHwnd = mainHwnd;
    }

    public void Create()
    {
        nint hInstance = GetModuleHandle(null);

        // Keep delegate alive to prevent GC
        _wndProcDelegate = MessageWndProc;

        var wc = new WNDCLASS
        {
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
            hInstance = hInstance,
            lpszClassName = MESSAGE_WINDOW_CLASS
        };
        RegisterClass(ref wc);

        // Create a message-only window (no visible UI)
        _messageHwnd = CreateWindowEx(
            0, MESSAGE_WINDOW_CLASS, null, 0,
            0, 0, 0, 0,
            HWND_MESSAGE, nint.Zero, hInstance, nint.Zero);

        // Add tray icon
        var nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _messageHwnd,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = _callbackMessage,
            hIcon = LoadIcon(hInstance, 32512), // IDI_APPLICATION
            szTip = "ScrollBar OS"
        };

        Shell_NotifyIcon(NIM_ADD, ref nid);
    }

    private nint MessageWndProc(nint hWnd, uint msg, nint wParam, nint lParam)
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
                OnToggleTaskbar?.Invoke();
            }
            return nint.Zero;
        }

        if (msg == WM_DESTROY)
        {
            return nint.Zero;
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_messageHwnd != nint.Zero)
        {
            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _messageHwnd,
                uID = 1
            };
            Shell_NotifyIcon(NIM_DELETE, ref nid);
            DestroyWindow(_messageHwnd);
            _messageHwnd = nint.Zero;
        }
    }
}
