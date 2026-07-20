using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Composition.SystemBackdrops;
using System.Runtime.InteropServices;
using Windows.Graphics;
using ScrollBarOS.Services;
using ScrollBarOS.Helpers;
using ScrollBarOS.Views;

namespace ScrollBarOS;

public sealed partial class MainWindow : Window
{
    private readonly ConfigService _configService;
    private readonly WindowService _windowService;
    private readonly ScrollStateMachine _scrollStateMachine;
    private readonly TaskbarService _taskbarService;
    private readonly HardwareMonitorService _hardwareMonitor;
    private readonly HotkeyService _hotkeyService;
    private readonly TilingService _tilingService;
    private readonly TrayService _trayService;

    private nint _hwnd;
    private bool _isClickThrough = false;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize services
        _configService = ConfigService.Instance;
        _windowService = new WindowService();
        _scrollStateMachine = new ScrollStateMachine(_windowService);
        _taskbarService = new TaskbarService();
        _hardwareMonitor = new HardwareMonitorService();
        _hotkeyService = new HotkeyService();
        _tilingService = new TilingService(_windowService);
        _trayService = new TrayService(_windowService);

        // Set window properties
        SetupWindow();

        // Apply initial configuration
        ApplyConfiguration();

        // Register hotkeys
        RegisterHotkeys();

        // Start hardware monitoring
        _hardwareMonitor.Start();

        // Subscribe to config changes
        _configService.ConfigChanged += OnConfigChanged;

        // Wire up capsule events
        Capsule.SettingsRequested += Capsule_SettingsRequested;

        Closed += MainWindow_Closed;
    }

    private void SetupWindow()
    {
        // Get the window handle
        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        // Set window to always on top, no activation, tool window
        Win32Helper.SetWindowStyle(_hwnd, isTopmost: true, isToolWindow: true);

        // Make window transparent and click-through
        Win32Helper.SetWindowTransparent(_hwnd);
        SetClickThrough(true);

        // Set window to cover the full screen work area
        PositionWindow();

        // Disable window resizing and title bar
        Title = "ScrollBar OS";
        ExtendsContentIntoTitleBar = true;
    }

    private void PositionWindow()
    {
        var workArea = Win32Helper.GetPrimaryMonitorWorkArea();
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(
            Win32Interop.GetWindowIdFromWindow(_hwnd));

        appWindow.MoveAndResize(new RectInt32(
            workArea.X, workArea.Y,
            workArea.Width, workArea.Height));
    }

    private void SetClickThrough(bool clickThrough)
    {
        _isClickThrough = clickThrough;
        Win32Helper.SetClickThrough(_hwnd, clickThrough);
    }

    public void DisableClickThrough()
    {
        SetClickThrough(false);
    }

    public void EnableClickThrough()
    {
        SetClickThrough(true);
    }

    private void ApplyConfiguration()
    {
        var config = _configService.Config;

        // Apply taskbar hiding
        if (config.HideTaskbar)
        {
            _taskbarService.Hide();
        }

        // Update capsule position
        Capsule.UpdatePosition(config.CapsulePosition);
        Capsule.UpdateAppearance(config);
    }

    private void RegisterHotkeys()
    {
        // Win+T to restore taskbar
        _hotkeyService.Register(_hwnd, HotkeyService.MOD_WIN, 0x54, () =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _taskbarService.Toggle();
                _configService.Config.HideTaskbar = !_taskbarService.IsHidden;
                _configService.Save();
            });
        });

        // Ctrl+Z to undo tiling
        _hotkeyService.Register(_hwnd, HotkeyService.MOD_CONTROL, 0x5A, () =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _tilingService.UndoLastTiling();
            });
        });
    }

    private void OnConfigChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ApplyConfiguration();
        });
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        _hotkeyService.UnregisterAll();
        _hardwareMonitor.Stop();
        _taskbarService.Restore();
        _configService.ConfigChanged -= OnConfigChanged;
    }

    private void Capsule_SettingsRequested(object? sender, EventArgs e)
    {
        SettingsPanel.Show();
    }

    /// <summary>
    /// Shows the tray menu
    /// </summary>
    public void ShowTrayMenu()
    {
        TrayMenu.Show(_trayService, _windowService);
    }

    /// <summary>
    /// Shows the tiling grid
    /// </summary>
    public void ShowTilingGrid()
    {
        TilingGridOverlay.Show(_tilingService, _windowService);
    }

    // Expose services to child controls
    public ConfigService ConfigServiceInstance => _configService;
    public WindowService WindowServiceInstance => _windowService;
    public ScrollStateMachine ScrollStateMachineInstance => _scrollStateMachine;
    public TilingService TilingServiceInstance => _tilingService;
    public HardwareMonitorService HardwareMonitorInstance => _hardwareMonitor;
    public TrayService TrayServiceInstance => _trayService;
}
