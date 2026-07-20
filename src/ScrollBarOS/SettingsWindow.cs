using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using ScrollBarOS.Services;
using ScrollBarOS.Models;
using Microsoft.Win32;

namespace ScrollBarOS;

/// <summary>
/// Settings window that opens as a separate window (pure code UI, no XAML)
/// </summary>
public class SettingsWindow : Window
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

        Content = CreateSettingsUI();

        App.WriteLog("SettingsWindow created successfully");
    }

    private UIElement CreateSettingsUI()
    {
        var config = _configService.Config;

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var panel = new StackPanel { Margin = new Thickness(20) };

        // Title
        panel.Children.Add(new TextBlock
        {
            Text = "Settings",
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 16)
        });

        // 1. Capsule Position
        panel.Children.Add(new TextBlock { Text = "Capsule Position", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        var positionCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 16) };
        positionCombo.Items.Add(new ComboBoxItem { Content = "Left" });
        positionCombo.Items.Add(new ComboBoxItem { Content = "Right" });
        positionCombo.SelectedIndex = config.CapsulePosition == CapsulePosition.Left ? 0 : 1;
        positionCombo.SelectionChanged += (s, e) =>
        {
            var pos = positionCombo.SelectedIndex == 0 ? CapsulePosition.Left : CapsulePosition.Right;
            _configService.Update(c => c.CapsulePosition = pos);
        };
        panel.Children.Add(positionCombo);

        // 2. Background Material
        panel.Children.Add(new TextBlock { Text = "Background Material", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        var materialCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 16) };
        materialCombo.Items.Add(new ComboBoxItem { Content = "Solid" });
        materialCombo.Items.Add(new ComboBoxItem { Content = "Acrylic" });
        materialCombo.Items.Add(new ComboBoxItem { Content = "Mica" });
        materialCombo.SelectedIndex = (int)config.Material;
        materialCombo.SelectionChanged += (s, e) =>
        {
            _configService.Update(c => c.Material = (BackgroundMaterial)materialCombo.SelectedIndex);
        };
        panel.Children.Add(materialCombo);

        // 3. Opacity
        panel.Children.Add(new TextBlock { Text = $"Opacity: {config.BackgroundOpacity:P0}", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        var opacitySlider = new Slider { Minimum = 20, Maximum = 100, Value = config.BackgroundOpacity * 100, StepFrequency = 5, Margin = new Thickness(0, 0, 0, 16) };
        opacitySlider.ValueChanged += (s, e) => _configService.Update(c => c.BackgroundOpacity = e.NewValue / 100.0);
        panel.Children.Add(opacitySlider);

        // 4. Capsule Width
        panel.Children.Add(new TextBlock { Text = $"Capsule Width: {config.CapsuleWidth}px", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        var widthSlider = new Slider { Minimum = 48, Maximum = 96, Value = config.CapsuleWidth, StepFrequency = 4, Margin = new Thickness(0, 0, 0, 16) };
        widthSlider.ValueChanged += (s, e) => _configService.Update(c => c.CapsuleWidth = (int)e.NewValue);
        panel.Children.Add(widthSlider);

        // 5. Icon Size
        panel.Children.Add(new TextBlock { Text = $"Icon Size: {config.IconSize}px", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        var iconSlider = new Slider { Minimum = 24, Maximum = 48, Value = config.IconSize, StepFrequency = 4, Margin = new Thickness(0, 0, 0, 16) };
        iconSlider.ValueChanged += (s, e) => _configService.Update(c => c.IconSize = (int)e.NewValue);
        panel.Children.Add(iconSlider);

        // 6. Widgets
        panel.Children.Add(new TextBlock { Text = "Widgets", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        var hwToggle = new ToggleSwitch { Header = "Hardware Monitor", IsOn = config.ShowHardwareWidget, Margin = new Thickness(0, 0, 0, 8) };
        hwToggle.Toggled += (s, e) => _configService.Update(c => c.ShowHardwareWidget = hwToggle.IsOn);
        panel.Children.Add(hwToggle);

        var dtToggle = new ToggleSwitch { Header = "Date & Time", IsOn = config.ShowDateTimeWidget, Margin = new Thickness(0, 0, 0, 16) };
        dtToggle.Toggled += (s, e) => _configService.Update(c => c.ShowDateTimeWidget = dtToggle.IsOn);
        panel.Children.Add(dtToggle);

        // 7. System
        panel.Children.Add(new TextBlock { Text = "System", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        var taskbarToggle = new ToggleSwitch { Header = "Hide Taskbar", IsOn = config.HideTaskbar, Margin = new Thickness(0, 0, 0, 8) };
        taskbarToggle.Toggled += (s, e) =>
        {
            _configService.Update(c => c.HideTaskbar = taskbarToggle.IsOn);
            if (taskbarToggle.IsOn) _taskbarService.Hide();
            else _taskbarService.Restore();
        };
        panel.Children.Add(taskbarToggle);

        var startupToggle = new ToggleSwitch { Header = "Start with Windows", IsOn = config.StartWithWindows, Margin = new Thickness(0, 0, 0, 16) };
        startupToggle.Toggled += (s, e) =>
        {
            _configService.Update(c => c.StartWithWindows = startupToggle.IsOn);
            SetStartup(startupToggle.IsOn);
        };
        panel.Children.Add(startupToggle);

        // 8. Language
        panel.Children.Add(new TextBlock { Text = "Language", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        var langCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 16) };
        langCombo.Items.Add(new ComboBoxItem { Content = "简体中文" });
        langCombo.Items.Add(new ComboBoxItem { Content = "English" });
        langCombo.SelectedIndex = config.Language == AppLanguage.Chinese ? 0 : 1;
        langCombo.SelectionChanged += (s, e) =>
        {
            var lang = langCombo.SelectedIndex == 0 ? AppLanguage.Chinese : AppLanguage.English;
            _configService.Update(c => c.Language = lang);
        };
        panel.Children.Add(langCombo);

        // About
        panel.Children.Add(new TextBlock
        {
            Text = "ScrollBar OS v1.0.0",
            FontSize = 11,
            Opacity = 0.5,
            Margin = new Thickness(0, 16, 0, 0)
        });

        scrollViewer.Content = panel;
        return scrollViewer;
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
