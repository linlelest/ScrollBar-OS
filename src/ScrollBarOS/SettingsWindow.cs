using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using ScrollBarOS.Models;
using ScrollBarOS.Services;
using Windows.Graphics;

namespace ScrollBarOS;

/// <summary>
/// Settings window that opens as a separate window
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly ConfigService _configService;
    private readonly TaskbarService _taskbarService;

    public SettingsWindow()
    {
        _configService = ConfigService.Instance;
        _taskbarService = new TaskbarService();

        InitializeComponent();

        Title = "ScrollBar OS - Settings";

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var appWindow = AppWindow.GetFromWindowId(
            Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd));
        appWindow.Resize(new SizeInt32(380, 620));

        InitializeControls();

        App.WriteLog("SettingsWindow created successfully");
    }

    private void InitializeControls()
    {
        var config = _configService.Config;

        // Initialize position combo box
        PositionComboBox.SelectedIndex = config.CapsulePosition == CapsulePosition.Left ? 0 : 1;
        PositionComboBox.SelectionChanged += (s, e) =>
        {
            var pos = PositionComboBox.SelectedIndex == 0 ? CapsulePosition.Left : CapsulePosition.Right;
            _configService.Update(c => c.CapsulePosition = pos);
        };

        // Initialize material combo box
        MaterialComboBox.SelectedIndex = (int)config.Material;
        MaterialComboBox.SelectionChanged += (s, e) =>
        {
            _configService.Update(c => c.Material = (BackgroundMaterial)MaterialComboBox.SelectedIndex);
        };

        // Initialize opacity slider
        OpacitySlider.Value = config.BackgroundOpacity * 100;
        OpacityLabel.Text = $"Opacity: {config.BackgroundOpacity:P0}";
        OpacitySlider.ValueChanged += (s, e) =>
        {
            _configService.Update(c => c.BackgroundOpacity = e.NewValue / 100.0);
            OpacityLabel.Text = $"Opacity: {e.NewValue / 100.0:P0}";
        };

        // Initialize width slider
        WidthSlider.Value = config.CapsuleWidth;
        WidthLabel.Text = $"Capsule Width: {config.CapsuleWidth}px";
        WidthSlider.ValueChanged += (s, e) =>
        {
            _configService.Update(c => c.CapsuleWidth = (int)e.NewValue);
            WidthLabel.Text = $"Capsule Width: {(int)e.NewValue}px";
        };

        // Initialize icon size slider
        IconSizeSlider.Value = config.IconSize;
        IconSizeLabel.Text = $"Icon Size: {config.IconSize}px";
        IconSizeSlider.ValueChanged += (s, e) =>
        {
            _configService.Update(c => c.IconSize = (int)e.NewValue);
            IconSizeLabel.Text = $"Icon Size: {(int)e.NewValue}px";
        };

        // Initialize widget toggles
        HardwareWidgetToggle.IsOn = config.ShowHardwareWidget;
        HardwareWidgetToggle.Toggled += (s, e) => _configService.Update(c => c.ShowHardwareWidget = HardwareWidgetToggle.IsOn);

        DateTimeWidgetToggle.IsOn = config.ShowDateTimeWidget;
        DateTimeWidgetToggle.Toggled += (s, e) => _configService.Update(c => c.ShowDateTimeWidget = DateTimeWidgetToggle.IsOn);

        // Initialize system toggles
        HideTaskbarToggle.IsOn = config.HideTaskbar;
        HideTaskbarToggle.Toggled += (s, e) =>
        {
            _configService.Update(c => c.HideTaskbar = HideTaskbarToggle.IsOn);
        };

        StartupToggle.IsOn = config.StartWithWindows;
        StartupToggle.Toggled += (s, e) =>
        {
            _configService.Update(c => c.StartWithWindows = StartupToggle.IsOn);
            SetStartup(StartupToggle.IsOn);
        };
    }

    private void SetStartup(bool enable)
    {
        try
        {
            const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            const string appName = "ScrollBarOS";
            using var key = Registry.CurrentUser.OpenSubKey(keyPath, true);
            if (key == null) return;

            if (enable)
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                    key.SetValue(appName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(appName, false);
            }
        }
        catch { }
    }
}
