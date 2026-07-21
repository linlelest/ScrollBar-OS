using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using ScrollBarOS.Models;
using ScrollBarOS.Services;
using Windows.Graphics;

namespace ScrollBarOS;

/// <summary>
/// Settings window with XAML UI
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly ConfigService _configService;
    private readonly TaskbarService _taskbarService;

    public SettingsWindow()
    {
        _configService = ConfigService.Instance;
        _taskbarService = new TaskbarService();

        Title = "ScrollBar OS - Settings";

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var appWindow = AppWindow.GetFromWindowId(
            Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd));
        appWindow.Resize(new SizeInt32(380, 620));

        // Initialize UI controls from config
        InitializeControls();

        App.WriteLog("SettingsWindow created successfully");
    }

    private void InitializeControls()
    {
        var config = _configService.Config;

        // Position
        PositionCombo.SelectedIndex = config.CapsulePosition == CapsulePosition.Left ? 0 : 1;

        // Material
        MaterialCombo.SelectedIndex = (int)config.Material;

        // Opacity
        OpacitySlider.Value = config.BackgroundOpacity * 100;
        OpacityText.Text = $"Opacity: {config.BackgroundOpacity:P0}";

        // Width
        WidthSlider.Value = config.CapsuleWidth;
        WidthText.Text = $"Capsule Width: {config.CapsuleWidth}px";

        // Icon
        IconSlider.Value = config.IconSize;
        IconText.Text = $"Icon Size: {config.IconSize}px";

        // Widgets
        HardwareToggle.IsOn = config.ShowHardwareWidget;
        DateTimeToggle.IsOn = config.ShowDateTimeWidget;

        // System
        TaskbarToggle.IsOn = config.HideTaskbar;
        ScrollToggle.IsOn = config.StartWithWindows;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        var config = _configService.Config;

        // Update config from controls
        config.CapsulePosition = PositionCombo.SelectedIndex == 0 ? CapsulePosition.Left : CapsulePosition.Right;
        config.Material = (BackgroundMaterial)MaterialCombo.SelectedIndex;
        config.BackgroundOpacity = OpacitySlider.Value / 100.0;
        config.CapsuleWidth = (int)WidthSlider.Value;
        config.IconSize = (int)IconSlider.Value;
        config.ShowHardwareWidget = HardwareToggle.IsOn;
        config.ShowDateTimeWidget = DateTimeToggle.IsOn;
        config.HideTaskbar = TaskbarToggle.IsOn;
        config.StartWithWindows = ScrollToggle.IsOn;

        // Save config
        _configService.Save();

        // Apply taskbar changes
        if (config.HideTaskbar)
            _taskbarService.Hide();
        else
            _taskbarService.Restore();

        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

