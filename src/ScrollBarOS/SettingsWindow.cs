using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using ScrollBarOS.Services;
using ScrollBarOS.Models;
using Microsoft.Win32;

namespace ScrollBarOS;

/// <summary>
/// Settings window that opens as a separate window
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
        ExtendsContentIntoTitleBar = true;

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var appWindow = AppWindow.GetFromWindowId(
            Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd));
        appWindow.Resize(new SizeInt32(380, 600));

        Content = CreateSettingsUI();
    }

    private UIElement CreateSettingsUI()
    {
        var config = _configService.Config;

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(20)
        };

        var panel = new StackPanel { Spacing = 16 };

        // Title
        panel.Children.Add(new TextBlock
        {
            Text = "Settings",
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold
        });

        // 1. Capsule Position
        var positionPanel = new StackPanel { Spacing = 8 };
        positionPanel.Children.Add(new TextBlock { Text = "Capsule Position", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        var positionCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
        positionCombo.Items.Add(new ComboBoxItem { Content = "Left", Tag = "Left" });
        positionCombo.Items.Add(new ComboBoxItem { Content = "Right", Tag = "Right" });
        positionCombo.SelectedIndex = config.CapsulePosition == CapsulePosition.Left ? 0 : 1;
        positionCombo.SelectionChanged += (s, e) =>
        {
            var pos = positionCombo.SelectedIndex == 0 ? CapsulePosition.Left : CapsulePosition.Right;
            _configService.Update(c => c.CapsulePosition = pos);
        };
        positionPanel.Children.Add(positionCombo);
        panel.Children.Add(positionPanel);

        // 2. Background Material
        var materialPanel = new StackPanel { Spacing = 8 };
        materialPanel.Children.Add(new TextBlock { Text = "Background Material", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        var materialCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
        materialCombo.Items.Add(new ComboBoxItem { Content = "Solid", Tag = "Solid" });
        materialCombo.Items.Add(new ComboBoxItem { Content = "Acrylic", Tag = "Acrylic" });
        materialCombo.Items.Add(new ComboBoxItem { Content = "Mica", Tag = "Mica" });
        materialCombo.SelectedIndex = (int)config.Material;
        materialCombo.SelectionChanged += (s, e) =>
        {
            _configService.Update(c => c.Material = (BackgroundMaterial)materialCombo.SelectedIndex);
        };
        materialPanel.Children.Add(materialCombo);
        panel.Children.Add(materialPanel);

        // 3. Opacity
        var opacityPanel = new StackPanel { Spacing = 8 };
        opacityPanel.Children.Add(new TextBlock { Text = $"Opacity: {config.BackgroundOpacity:P0}", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        var opacitySlider = new Slider
        {
            Minimum = 20,
            Maximum = 100,
            Value = config.BackgroundOpacity * 100,
            StepFrequency = 5
        };
        opacitySlider.ValueChanged += (s, e) =>
        {
            _configService.Update(c => c.BackgroundOpacity = e.NewValue / 100.0);
        };
        opacityPanel.Children.Add(opacitySlider);
        panel.Children.Add(opacityPanel);

        // 4. Capsule Width
        var widthPanel = new StackPanel { Spacing = 8 };
        widthPanel.Children.Add(new TextBlock { Text = $"Capsule Width: {config.CapsuleWidth}px", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        var widthSlider = new Slider
        {
            Minimum = 48,
            Maximum = 96,
            Value = config.CapsuleWidth,
            StepFrequency = 4
        };
        widthSlider.ValueChanged += (s, e) =>
        {
            _configService.Update(c => c.CapsuleWidth = (int)e.NewValue);
        };
        widthPanel.Children.Add(widthSlider);
        panel.Children.Add(widthPanel);

        // 5. Icon Size
        var iconPanel = new StackPanel { Spacing = 8 };
        iconPanel.Children.Add(new TextBlock { Text = $"Icon Size: {config.IconSize}px", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        var iconSlider = new Slider
        {
            Minimum = 24,
            Maximum = 48,
            Value = config.IconSize,
            StepFrequency = 4
        };
        iconSlider.ValueChanged += (s, e) =>
        {
            _configService.Update(c => c.IconSize = (int)e.NewValue);
        };
        iconPanel.Children.Add(iconSlider);
        panel.Children.Add(iconPanel);

        // 6. Widgets
        var widgetPanel = new StackPanel { Spacing = 8 };
        widgetPanel.Children.Add(new TextBlock { Text = "Widgets", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });

        var hwToggle = new ToggleSwitch { Header = "Hardware Monitor", IsOn = config.ShowHardwareWidget };
        hwToggle.Toggled += (s, e) => _configService.Update(c => c.ShowHardwareWidget = hwToggle.IsOn);
        widgetPanel.Children.Add(hwToggle);

        var dtToggle = new ToggleSwitch { Header = "Date & Time", IsOn = config.ShowDateTimeWidget };
        dtToggle.Toggled += (s, e) => _configService.Update(c => c.ShowDateTimeWidget = dtToggle.IsOn);
        widgetPanel.Children.Add(dtToggle);

        panel.Children.Add(widgetPanel);

        // 7. System
        var sysPanel = new StackPanel { Spacing = 8 };
        sysPanel.Children.Add(new TextBlock { Text = "System", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });

        var taskbarToggle = new ToggleSwitch { Header = "Hide Taskbar", IsOn = config.HideTaskbar };
        taskbarToggle.Toggled += (s, e) =>
        {
            _configService.Update(c => c.HideTaskbar = taskbarToggle.IsOn);
            if (taskbarToggle.IsOn) _taskbarService.Hide();
            else _taskbarService.Restore();
        };
        sysPanel.Children.Add(taskbarToggle);

        var startupToggle = new ToggleSwitch { Header = "Start with Windows", IsOn = config.StartWithWindows };
        startupToggle.Toggled += (s, e) =>
        {
            _configService.Update(c => c.StartWithWindows = startupToggle.IsOn);
            SetStartup(startupToggle.IsOn);
        };
        sysPanel.Children.Add(startupToggle);

        panel.Children.Add(sysPanel);

        // 8. Language
        var langPanel = new StackPanel { Spacing = 8 };
        langPanel.Children.Add(new TextBlock { Text = "Language", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        var langCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
        langCombo.Items.Add(new ComboBoxItem { Content = "简体中文" });
        langCombo.Items.Add(new ComboBoxItem { Content = "English" });
        langCombo.SelectedIndex = config.Language == AppLanguage.Chinese ? 0 : 1;
        langCombo.SelectionChanged += (s, e) =>
        {
            var lang = langCombo.SelectedIndex == 0 ? AppLanguage.Chinese : AppLanguage.English;
            _configService.Update(c => c.Language = lang);
        };
        langPanel.Children.Add(langCombo);
        panel.Children.Add(langPanel);

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
