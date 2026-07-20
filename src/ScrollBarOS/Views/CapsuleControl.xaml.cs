using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using ScrollBarOS.Models;
using ScrollBarOS.Services;
using System.Collections.ObjectModel;

namespace ScrollBarOS.Views;

public sealed partial class CapsuleControl : UserControl
{
    private readonly ConfigService _configService;
    private WindowService? _windowService;
    private HardwareMonitorService? _hardwareMonitor;
    private ScrollStateMachine? _scrollStateMachine;
    private TrayService? _trayService;
    private Timer? _refreshTimer;
    private Timer? _dateTimeTimer;

    public ObservableCollection<WindowInfo> WindowList { get; } = new();

    public event EventHandler? SettingsRequested;
    public event EventHandler<WindowInfo>? WindowDragStarted;
    public event EventHandler<WindowInfo>? WindowSelected;
    public event EventHandler<int>? ScrollEvent;

    public CapsuleControl()
    {
        InitializeComponent();
        _configService = ConfigService.Instance;

        Loaded += CapsuleControl_Loaded;
        Unloaded += CapsuleControl_Unloaded;
    }

    private void CapsuleControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Get services from parent window
        var mainWindow = App.MainWindow as MainWindow;
        if (mainWindow != null)
        {
            _windowService = mainWindow.WindowServiceInstance;
            _hardwareMonitor = mainWindow.HardwareMonitorInstance;
            _scrollStateMachine = mainWindow.ScrollStateMachineInstance;
            _trayService = mainWindow.TrayServiceInstance;

            if (_hardwareMonitor != null)
            {
                _hardwareMonitor.InfoUpdated += HardwareMonitor_InfoUpdated;
            }
        }

        _trayService ??= new TrayService(_windowService ?? new WindowService());

        // Initial load
        RefreshWindowList();
        ApplyConfig();
        UpdateCapsuleHeight();

        // Start refresh timer
        _refreshTimer = new Timer(_ =>
        {
            DispatcherQueue.TryEnqueue(() => RefreshWindowList());
        }, null, 2000, 2000);

        // DateTime timer
        _dateTimeTimer = new Timer(_ =>
        {
            DispatcherQueue.TryEnqueue(() => UpdateDateTime());
        }, null, 0, 1000);
    }

    private void CapsuleControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _refreshTimer?.Dispose();
        _dateTimeTimer?.Dispose();
        if (_hardwareMonitor != null)
        {
            _hardwareMonitor.InfoUpdated -= HardwareMonitor_InfoUpdated;
        }
    }

    /// <summary>
    /// Refreshes the list of visible windows
    /// </summary>
    private void RefreshWindowList()
    {
        if (_windowService == null) return;

        var windows = _windowService.GetVisibleWindows(true);
        WindowList.Clear();
        foreach (var window in windows)
        {
            WindowList.Add(window);
        }
        AppIconsRepeater.ItemsSource = WindowList;
    }

    /// <summary>
    /// Updates capsule position - window positioning is handled by MainWindow
    /// </summary>
    public void UpdatePosition(CapsulePosition position)
    {
        // Window positioning is managed by MainWindow.PositionCapsuleWindow()
        // No internal layout change needed
    }

    /// <summary>
    /// Updates capsule appearance based on configuration
    /// </summary>
    public void UpdateAppearance(AppConfig config)
    {
        CapsuleRoot.CornerRadius = new CornerRadius(config.CornerRadius);

        // Parse background color
        try
        {
            var color = Microsoft.UI.ColorHelper.FromArgb(
                (byte)(config.BackgroundOpacity * 255),
                Convert.ToByte(config.BackgroundColor.Substring(3, 2), 16),
                Convert.ToByte(config.BackgroundColor.Substring(5, 2), 16),
                Convert.ToByte(config.BackgroundColor.Substring(7, 2), 16));
            CapsuleRoot.Background = new SolidColorBrush(color);
        }
        catch
        {
            CapsuleRoot.Background = new SolidColorBrush(Microsoft.UI.Colors.DarkSlateGray);
        }

        // Widget visibility
        HardwareButton.Visibility = config.ShowHardwareWidget ? Visibility.Visible : Visibility.Collapsed;
        DateTimeText.Visibility = config.ShowDateTimeWidget ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyConfig()
    {
        var config = _configService.Config;
        UpdatePosition(config.CapsulePosition);
        UpdateAppearance(config);
    }

    /// <summary>
    /// Capsule fills the window - height is managed by MainWindow positioning
    /// </summary>
    private void UpdateCapsuleHeight()
    {
        // Window is already sized to capsule dimensions by MainWindow
        // Capsule fills the entire window
        CapsuleRoot.VerticalAlignment = VerticalAlignment.Stretch;
        CapsuleRoot.HorizontalAlignment = HorizontalAlignment.Stretch;
        CapsuleRoot.Margin = new Thickness(0);
    }

    private void UpdateDateTime()
    {
        DateTimeText.Text = DateTime.Now.ToString("HH:mm\nMM/dd");
    }

    /// <summary>
    /// Handles mouse wheel scroll over the capsule - core interaction
    /// </summary>
    public void HandlePointerWheelChanged(int delta)
    {
        _scrollStateMachine?.ProcessScroll(delta);
        ScrollEvent?.Invoke(this, delta);
    }

    #region Event Handlers

    private void CapsuleRoot_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        // Animate capsule width on hover
        var animation = new DoubleAnimation
        {
            To = _configService.Config.CapsuleWidth + 8,
            Duration = new Duration(TimeSpan.FromMilliseconds(150)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var storyboard = new Storyboard();
        Storyboard.SetTarget(animation, CapsuleRoot);
        Storyboard.SetTargetProperty(animation, "Width");
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private void CapsuleRoot_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        // Animate back to normal width
        var animation = new DoubleAnimation
        {
            To = _configService.Config.CapsuleWidth,
            Duration = new Duration(TimeSpan.FromMilliseconds(150)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var storyboard = new Storyboard();
        Storyboard.SetTarget(animation, CapsuleRoot);
        Storyboard.SetTargetProperty(animation, "Width");
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private void AppIcon_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is WindowInfo window)
        {
            // Check for drag
            if (e.GetCurrentPoint(element).Properties.IsLeftButtonPressed)
            {
                _windowService?.FocusWindow(window);
                WindowSelected?.Invoke(this, window);
            }
        }
    }

    private void AppIcon_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var scaleTransform = new ScaleTransform { ScaleX = 1.15, ScaleY = 1.15 };
            element.RenderTransform = scaleTransform;
            element.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        }
    }

    private void AppIcon_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            element.RenderTransform = new ScaleTransform { ScaleX = 1.0, ScaleY = 1.0 };
        }
    }

    private void AppIcon_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is WindowInfo window)
        {
            // Show context menu for pinning
            var menu = new MenuFlyout();
            var pinItem = new MenuFlyoutItem { Text = "Pin to Quick Launch" };
            pinItem.Click += (s, args) =>
            {
                if (!string.IsNullOrEmpty(window.ExecutablePath))
                {
                    _trayService?.PinApp(window.ExecutablePath);
                }
            };
            menu.Items.Add(pinItem);
            menu.ShowAt(element);
        }
    }

    private void HardwareButton_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        HardwarePopup.IsOpen = true;
    }

    private void HardwareButton_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        // Delay closing to allow moving mouse to popup
    }

    private void HardwareButton_Click(object sender, RoutedEventArgs e)
    {
        HardwarePopup.IsOpen = !HardwarePopup.IsOpen;
    }

    private void HardwareMonitor_InfoUpdated(object? sender, HardwareInfo info)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            CpuLabel.Text = $"CPU: {info.CpuUsage:F0}%";
            CpuBar.Value = info.CpuUsage;

            MemLabel.Text = $"RAM: {info.MemoryUsage:F0}% ({info.UsedMemoryMB / 1024.0:F1}GB)";
            MemBar.Value = info.MemoryUsage;

            DiskLabel.Text = $"Disk: {info.DiskUsage:F0}% ({info.FreeDiskGB:F0}GB free)";
            DiskBar.Value = info.DiskUsage;

            NetLabel.Text = $"Net: ↑{info.NetworkUploadKBps:F0} ↓{info.NetworkDownloadKBps:F0} KB/s";
        });
    }

    private void PinnedApp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tagStr && int.TryParse(tagStr, out int index))
        {
            if (_trayService != null && index < _trayService.PinnedApps.Count)
            {
                _trayService.LaunchPinnedApp(_trayService.PinnedApps[index]);
            }
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CapsuleRoot_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(CapsuleRoot);
        int delta = point.Properties.MouseWheelDelta;
        if (delta != 0)
        {
            HandlePointerWheelChanged(delta);
            e.Handled = true;
        }
    }

    private void TilingMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = App.MainWindow as MainWindow;
        if (mainWindow != null && _windowService != null)
        {
            var windows = _windowService.GetVisibleWindows(true);
            if (windows.Count > 0)
            {
                mainWindow.TilingServiceInstance.TileWindows(windows);
            }
        }
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        App.MainWindow?.Close();
    }

    #endregion
}
