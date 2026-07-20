using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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
    private readonly TaskbarGuard _taskbarGuard;

    private nint _hwnd;
    private NotifyIconHelper? _notifyIcon;
    private DispatcherTimer? _refreshTimer;
    private SidePanelWindow? _sidePanel;
    private TilingWindow? _tilingWindow;

    // Non-visual state
    private DispatcherTimer? _dateTimeTimer;
    private bool _isListMode = false;
    private int _listSelectedIndex = 0;
    private List<WindowInfo> _currentWindows = new();

    public MainWindow()
    {
        // Initialize XAML-defined UI
        InitializeComponent();

        // Ensure pinned app slots exist (3 slots)
        for (int i = 0; i < 3; i++)
        {
            var pinBtn = new Button
            {
                Width = 20,
                Height = 20,
                Padding = new Thickness(0),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(2, 0, 2, 0),
                Tag = i
            };
            pinBtn.Click += PinnedApp_Click;
            _pinnedAppsPanel.Children.Add(pinBtn);
        }

        // Initialize services
        _configService = ConfigService.Instance;
        _windowService = new WindowService();
        _scrollStateMachine = new ScrollStateMachine(_windowService);
        _taskbarService = new TaskbarService();
        _hardwareMonitor = new HardwareMonitorService();
        _tilingService = new TilingService(_windowService);
        _trayService = new TrayService(_windowService);
        _taskbarGuard = new TaskbarGuard(_taskbarService);

        // Set window properties
        SetupWindow();

        // Apply initial configuration
        ApplyConfiguration();

        // Start hardware monitoring
        _hardwareMonitor.Start();
        _hardwareMonitor.InfoUpdated += HardwareMonitor_InfoUpdated;

        // Subscribe to config changes
        _configService.ConfigChanged += OnConfigChanged;

        // Wire up scroll state machine
        _scrollStateMachine.ModeChanged += ScrollStateMachine_ModeChanged;
        _scrollStateMachine.WindowFocused += ScrollStateMachine_WindowFocused;

        // Create system tray icon (uses its own message window)
        SetupTrayIcon();

        // Populate window list
        RefreshWindowList();

        // Refresh pinned apps UI
        RefreshPinnedApps();

        // Refresh timer
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _refreshTimer.Tick += (s, e) => RefreshWindowList();
        _refreshTimer.Start();

        // Date/Time timer
        _dateTimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _dateTimeTimer.Tick += (s, e) => _dateTimeText.Text = DateTime.Now.ToString("HH:mm\nMM/dd");
        _dateTimeTimer.Start();
        _dateTimeText.Text = DateTime.Now.ToString("HH:mm\nMM/dd");

        Closed += MainWindow_Closed;
        App.WriteLog("MainWindow constructor completed successfully");
    }

    // BuildUI removed: UI is defined in XAML (MainWindow.xaml). Dynamic parts (pinned apps) are created at runtime.

    private void SetupWindow()
    {
        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        Title = "ScrollBar OS";
        ExtendsContentIntoTitleBar = true;

        // Remove window border/titlebar - make it a pure capsule shape
        Win32Helper.RemoveWindowBorder(_hwnd);

        // Set window style: topmost + tool window (no taskbar entry)
        Win32Helper.SetWindowStyle(_hwnd, isTopmost: true, isToolWindow: true);

        // Position the window as a capsule at the screen edge (DPI-aware)
        PositionCapsuleWindow();

        // Add scroll handling
        _rootGrid.PointerWheelChanged += RootGrid_PointerWheelChanged;
    }

    private void RootGrid_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_rootGrid);
        int delta = point.Properties.MouseWheelDelta;
        if (delta != 0)
        {
            _scrollStateMachine.ProcessScroll(delta);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Positions the window as a small capsule at the right/left edge of the screen (DPI-aware)
    /// </summary>
    private void PositionCapsuleWindow()
    {
        var config = _configService.Config;
        var workArea = Win32Helper.GetPrimaryMonitorWorkArea();

        // Get DPI scale factor for proper high-DPI adaptation
        double dpiScale = DpiHelper.GetScaleFactor(_hwnd);

        int capsuleWidth = (int)((config.CapsuleWidth) * dpiScale); // DPI-scaled
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

    private void RefreshWindowList()
    {
        _currentWindows = _windowService.GetVisibleWindows(true);
        _appIconsPanel.Children.Clear();

        foreach (var window in _currentWindows)
        {
            _appIconsPanel.Children.Add(CreateWindowButton(window));
        }
    }

    private Button CreateWindowButton(WindowInfo window)
    {
        var button = new Button
        {
            Width = 44,
            Height = 44,
            Padding = new Thickness(4),
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
            CornerRadius = new CornerRadius(8),
            HorizontalAlignment = HorizontalAlignment.Center,
            Tag = window,
            RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
            RenderTransform = new ScaleTransform { ScaleX = 1.0, ScaleY = 1.0 }
        };

        button.Content = CreateWindowButtonContent(window);
        button.Click += WindowButton_Click;
        button.ContextFlyout = CreateWindowFlyout(window);
        button.PointerEntered += (_, _) => SetButtonScale(button, 1.15);
        button.PointerExited += (_, _) => SetButtonScale(button, 1.0);
        ToolTipService.SetToolTip(button, window.Title);
        return button;
    }

    private void WindowButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not WindowInfo wi) return;

        var foreground = _windowService.GetForegroundWindow();
        if (foreground is { Handle: var handle } && handle == wi.Handle)
        {
            Win32Helper.MinimizeWindow(wi.Handle);
        }
        else
        {
            _windowService.FocusWindow(wi);
        }
    }

    private UIElement CreateWindowButtonContent(WindowInfo window)
    {
        return window.Icon != null
            ? new Image { Source = window.Icon, Width = 32, Height = 32 }
            : new FontIcon
            {
                Glyph = "\uE737",
                FontSize = 18,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0xAA, 0xFF, 0xFF, 0xFF))
            };
    }

    private MenuFlyout CreateWindowFlyout(WindowInfo window)
    {
        var flyout = new MenuFlyout();
        var closeItem = new MenuFlyoutItem { Text = "Close", Tag = window };
        closeItem.Click += (_, _) =>
        {
            if (closeItem.Tag is WindowInfo wi)
            {
                Win32Helper.CloseWindow(wi.Handle);
            }
        };

        var minimizeItem = new MenuFlyoutItem { Text = "Minimize", Tag = window };
        minimizeItem.Click += (_, _) =>
        {
            if (minimizeItem.Tag is WindowInfo wi)
            {
                Win32Helper.MinimizeWindow(wi.Handle);
            }
        };

        flyout.Items.Add(minimizeItem);
        flyout.Items.Add(closeItem);
        AddPinOption(flyout, window);
        return flyout;
    }

    private static void SetButtonScale(Button button, double scale)
    {
        if (button.RenderTransform is ScaleTransform transform)
        {
            transform.ScaleX = scale;
            transform.ScaleY = scale;
        }
    }


    private void HardwareMonitor_InfoUpdated(object? sender, HardwareInfo info)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _cpuText.Text = $"CPU: {info.CpuUsage:F0}%";
            _memText.Text = $"RAM: {info.MemoryUsage:F0}%";
            _diskText.Text = $"Disk: {info.DiskUsage:F0}%";
            _netText.Text = $"\u2191{info.NetworkUploadKBps:F0} \u2193{info.NetworkDownloadKBps:F0} KB/s";
        });
    }

    private void SetupTrayIcon()
    {
        _notifyIcon = new NotifyIconHelper(_hwnd);
        _notifyIcon.OnShowSettings += () =>
        {
            DispatcherQueue.TryEnqueue(() => OpenSettings());
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
        _notifyIcon.OnUndoTiling += () =>
        {
            DispatcherQueue.TryEnqueue(() => _tilingService.UndoLastTiling());
        };
        _notifyIcon.Create();

        // Register global hotkeys on the message window
        _notifyIcon.RegisterHotKey(HotkeyService.MOD_WIN, 0x54); // Win+T = toggle taskbar
        _notifyIcon.RegisterHotKey(HotkeyService.MOD_CONTROL, 0x5A); // Ctrl+Z = undo tiling
    }

    private void ScrollStateMachine_ModeChanged(object? sender, ScrollModeChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (e.NewMode == Services.ScrollMode.FastScroll)
            {
                // Switch to list mode
                _isListMode = true;
                _appIconsPanel.Visibility = Visibility.Collapsed;
                _windowListPanel.Visibility = Visibility.Visible;
                RebuildWindowList();
            }
            else if (e.NewMode == Services.ScrollMode.Idle && _isListMode)
            {
                // Exit list mode
                _isListMode = false;
                _windowListPanel.Visibility = Visibility.Collapsed;
                _appIconsPanel.Visibility = Visibility.Visible;
            }
        });
    }

    private void ScrollStateMachine_WindowFocused(object? sender, WindowFocusedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_isListMode)
            {
                _listSelectedIndex = e.Index;
                UpdateListHighlights();
            }
        });
    }

    private void RebuildWindowList()
    {
        _windowListPanel.Children.Clear();
        _listSelectedIndex = _scrollStateMachine.CurrentFocusIndex;

        for (int i = 0; i < _currentWindows.Count; i++)
        {
            var tb = new TextBlock
            {
                Text = _currentWindows[i].Title,
                FontSize = 11,
                Padding = new Thickness(6, 4, 6, 4),
                Foreground = new SolidColorBrush(i == _listSelectedIndex
                    ? Windows.UI.Color.FromArgb(0xFF, 0x4C, 0xC2, 0xFF)
                    : Windows.UI.Color.FromArgb(0xDD, 0xFF, 0xFF, 0xFF))
            };
            _windowListPanel.Children.Add(tb);
        }
    }

    private void UpdateListHighlights()
    {
        for (int i = 0; i < _windowListPanel.Children.Count; i++)
        {
            if (_windowListPanel.Children[i] is TextBlock tb)
            {
                tb.Foreground = new SolidColorBrush(i == _listSelectedIndex
                    ? Windows.UI.Color.FromArgb(0xFF, 0x4C, 0xC2, 0xFF)
                    : Windows.UI.Color.FromArgb(0xDD, 0xFF, 0xFF, 0xFF));
            }
        }
    }

    private void ApplyConfiguration()
    {
        var config = _configService.Config;

        if (config.HideTaskbar)
        {
            _taskbarService.Hide();
            _taskbarGuard.MarkHidden();
        }
        else
        {
            _taskbarService.Restore();
            _taskbarGuard.MarkRestored();
        }

        // Widget visibility
        _cpuText.Visibility = config.ShowHardwareWidget ? Visibility.Visible : Visibility.Collapsed;
        _memText.Visibility = config.ShowHardwareWidget ? Visibility.Visible : Visibility.Collapsed;
        _diskText.Visibility = config.ShowHardwareWidget ? Visibility.Visible : Visibility.Collapsed;
        _netText.Visibility = config.ShowHardwareWidget ? Visibility.Visible : Visibility.Collapsed;
        _dateTimeText.Visibility = config.ShowDateTimeWidget ? Visibility.Visible : Visibility.Collapsed;

        // Background color/opacity
        try
        {
            byte alpha = (byte)(config.BackgroundOpacity * 255);
            var bgColor = Windows.UI.Color.FromArgb(alpha, 0x1E, 0x1E, 0x2E);
            _rootBorder.Background = new SolidColorBrush(bgColor);
        }
        catch { }
    }

    private void OnConfigChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ApplyConfiguration();
            PositionCapsuleWindow();
        });
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSettings();
    }

    private void TilingButton_Click(object sender, RoutedEventArgs e)
    {
        // Open the independent tiling configuration window
        if (_tilingWindow == null)
        {
            _tilingWindow = new TilingWindow(_windowService, _tilingService);
            _tilingWindow.Closed += (s, args) => _tilingWindow = null;
        }
        _tilingWindow.Activate();
    }

    private void TrayExpandButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleSidePanel(SidePanelWindow.PanelMode.TrayIcons);
    }

    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleSidePanel(SidePanelWindow.PanelMode.SystemMenu);
    }

    private void ToggleSidePanel(SidePanelWindow.PanelMode mode)
    {
        if (_sidePanel != null)
        {
            _sidePanel.Close();
            _sidePanel = null;
            return;
        }

        // Get the capsule window's position to anchor the panel
        var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(_hwnd));
        var bounds = appWindow.Position;
        var size = appWindow.Size;
        var anchorRect = new RectInt32(bounds.X, bounds.Y, size.Width, size.Height);

        _sidePanel = new SidePanelWindow(mode, _windowService, _trayService, anchorRect);
        _sidePanel.Closed += (s, args) => _sidePanel = null;
        _sidePanel.Activate();
    }

    private void PinnedApp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int index)
        {
            var pinnedApps = _trayService.PinnedApps;
            if (index < pinnedApps.Count)
            {
                _trayService.LaunchPinnedApp(pinnedApps[index]);
            }
        }
    }

    /// <summary>
    /// Adds a "Pin to quick launch" option to an app icon's context menu
    /// </summary>
    private void AddPinOption(MenuFlyout flyout, WindowInfo window)
    {
        var pinItem = new MenuFlyoutItem { Text = "Pin to Quick Launch", Tag = window };
        pinItem.Click += (s, e) =>
        {
            if (s is MenuFlyoutItem item && item.Tag is WindowInfo wi && !string.IsNullOrEmpty(wi.ExecutablePath))
            {
                _trayService.PinApp(wi.ExecutablePath);
                RefreshPinnedApps();
            }
        };
        flyout.Items.Add(pinItem);
    }

    /// <summary>
    /// Refreshes the pinned apps panel UI
    /// </summary>
    private void RefreshPinnedApps()
    {
        var pinnedApps = _trayService.PinnedApps;
        for (int i = 0; i < _pinnedAppsPanel.Children.Count; i++)
        {
            if (_pinnedAppsPanel.Children[i] is not Button btn) continue;

            if (i < pinnedApps.Count)
            {
                btn.Content = CreatePinnedAppContent(pinnedApps[i]);
                ToolTipService.SetToolTip(btn, pinnedApps[i].Name);
            }
            else
            {
                btn.Content = CreateEmptyPinnedAppContent();
                ToolTipService.SetToolTip(btn, "Empty slot");
            }
        }
    }

    private object CreatePinnedAppContent(PinnedAppInfo app)
    {
        return app.Icon != null
            ? new Image { Source = app.Icon, Width = 16, Height = 16 }
            : new FontIcon
            {
                Glyph = "\uE718",
                FontSize = 10,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF))
            };
    }

    private object CreateEmptyPinnedAppContent()
    {
        return new FontIcon
        {
            Glyph = "\uE718",
            FontSize = 10,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0x44, 0xFF, 0xFF, 0xFF))
        };
    }

    private void OpenSettings()
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.Activate();
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        _refreshTimer?.Stop();
        _dateTimeTimer?.Stop();
        _notifyIcon?.Dispose();
        _hardwareMonitor.Stop();
        _taskbarService.Restore();
        _taskbarGuard.Dispose();
        _configService.ConfigChanged -= OnConfigChanged;
    }

    // Expose services
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
    public event Action? OnUndoTiling;

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
    private const uint WM_HOTKEY = 0x0312;
    private const nint HWND_MESSAGE = -3;

    // Context menu IDs
    private const int IDM_SETTINGS = 40001;
    private const int IDM_TOGGLE_TASKBAR = 40002;
    private const int IDM_UNDO_TILING = 40003;
    private const int IDM_EXIT = 40004;
    private const uint TPM_RIGHTALIGN = 0x0008;
    private const uint TPM_BOTTOMALIGN = 0x0020;

    private int _hotkeyId = 1;
    private readonly Dictionary<int, Action> _hotkeys = new();

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint LoadIcon(nint hInstance, nint lpIconName);

    [DllImport("kernel32.dll")]
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

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    [DllImport("user32.dll")]
    private static extern nint CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenu(nint hMenu, uint uFlags, nint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenuEx(nint hMenu, uint uFlags, int x, int y, nint hWnd, nint tpmParams);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(nint hMenu);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    private const uint MF_STRING = 0x00000000;
    private const uint WM_COMMAND = 0x0111;

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

    /// <summary>
    /// Registers a global hotkey on the message window
    /// </summary>
    public void RegisterHotKey(int modifiers, int virtualKey)
    {
        if (_messageHwnd == nint.Zero) return;
        int id = _hotkeyId++;
        if (RegisterHotKey(_messageHwnd, id, (uint)modifiers, (uint)virtualKey))
        {
            // Map hotkey to action based on virtual key
            if (virtualKey == 0x54) // T = toggle taskbar
                _hotkeys[id] = () => OnToggleTaskbar?.Invoke();
            else if (virtualKey == 0x5A) // Z = undo tiling
                _hotkeys[id] = () => OnUndoTiling?.Invoke();
        }
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
                ShowContextMenu();
            }
            return nint.Zero;
        }

        if (msg == WM_HOTKEY)
        {
            int id = (int)wParam;
            if (_hotkeys.TryGetValue(id, out var action))
                action();
            return nint.Zero;
        }

        if (msg == WM_COMMAND)
        {
            int cmd = (int)(wParam.ToInt64() & 0xFFFF);
            switch (cmd)
            {
                case IDM_SETTINGS: OnShowSettings?.Invoke(); break;
                case IDM_TOGGLE_TASKBAR: OnToggleTaskbar?.Invoke(); break;
                case IDM_UNDO_TILING: OnUndoTiling?.Invoke(); break;
                case IDM_EXIT: OnExit?.Invoke(); break;
            }
            return nint.Zero;
        }

        if (msg == WM_DESTROY)
        {
            return nint.Zero;
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private void ShowContextMenu()
    {
        GetCursorPos(out POINT pt);
        nint menu = CreatePopupMenu();
        AppendMenu(menu, MF_STRING, IDM_SETTINGS, "Settings");
        AppendMenu(menu, MF_STRING, IDM_TOGGLE_TASKBAR, "Toggle Taskbar");
        AppendMenu(menu, MF_STRING, IDM_UNDO_TILING, "Undo Tiling");
        AppendMenu(menu, MF_STRING, IDM_EXIT, "Exit");
        SetForegroundWindow(_messageHwnd);
        TrackPopupMenuEx(menu, TPM_RIGHTALIGN | TPM_BOTTOMALIGN, pt.X, pt.Y, _messageHwnd, nint.Zero);
        DestroyMenu(menu);
    }

    public void Dispose()
    {
        if (_messageHwnd != nint.Zero)
        {
            // Unregister hotkeys
            foreach (var id in _hotkeys.Keys)
                UnregisterHotKey(_messageHwnd, id);
            _hotkeys.Clear();

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
