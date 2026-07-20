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

        panel.Children.Add(new TextBlock
        {
            Text = "Settings",
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 16)
        });

        panel.Children.Add(CreateSectionTitle("Capsule Position"));
        var positionCombo = CreateComboBox("Left", "Right");
        positionCombo.SelectedIndex = config.CapsulePosition == CapsulePosition.Left ? 0 : 1;
        positionCombo.SelectionChanged += (s, e) =>
        {
            var pos = positionCombo.SelectedIndex == 0 ? CapsulePosition.Left : CapsulePosition.Right;
            _configService.Update(c => c.CapsulePosition = pos);
        };
        panel.Children.Add(positionCombo);

        panel.Children.Add(CreateSectionTitle("Background Material"));
        var materialCombo = CreateComboBox("Solid", "Acrylic", "Mica");
        materialCombo.SelectedIndex = (int)config.Material;
        materialCombo.SelectionChanged += (s, e) =>
        {
            _configService.Update(c => c.Material = (BackgroundMaterial)materialCombo.SelectedIndex);
        };
        panel.Children.Add(materialCombo);

        panel.Children.Add(CreateSectionTitle($"Opacity: {config.BackgroundOpacity:P0}"));
        var opacitySlider = CreateSlider(config.BackgroundOpacity * 100, 20, 100, 5);
        opacitySlider.ValueChanged += (s, e) => _configService.Update(c => c.BackgroundOpacity = e.NewValue / 100.0);
        panel.Children.Add(opacitySlider);

        panel.Children.Add(CreateSectionTitle($"Capsule Width: {config.CapsuleWidth}px"));
        var widthSlider = CreateSlider(config.CapsuleWidth, 48, 96, 4);
        widthSlider.ValueChanged += (s, e) => _configService.Update(c => c.CapsuleWidth = (int)e.NewValue);
        panel.Children.Add(widthSlider);

        panel.Children.Add(CreateSectionTitle($"Icon Size: {config.IconSize}px"));
        var iconSlider = CreateSlider(config.IconSize, 24, 48, 4);
        iconSlider.ValueChanged += (s, e) => _configService.Update(c => c.IconSize = (int)e.NewValue);
        panel.Children.Add(iconSlider);

        panel.Children.Add(CreateSectionTitle("Widgets"));
        var hwToggle = CreateToggle("Hardware Monitor", config.ShowHardwareWidget);
        hwToggle.Toggled += (s, e) => _configService.Update(c => c.ShowHardwareWidget = hwToggle.IsOn);
        panel.Children.Add(hwToggle);

        var dtToggle = CreateToggle("Date & Time", config.ShowDateTimeWidget, 16);
        dtToggle.Toggled += (s, e) => _configService.Update(c => c.ShowDateTimeWidget = dtToggle.IsOn);
        panel.Children.Add(dtToggle);

        panel.Children.Add(CreateSectionTitle("System"));
        var taskbarToggle = CreateToggle("Hide Taskbar", config.HideTaskbar);
        taskbarToggle.Toggled += (s, e) =>
        {
            _configService.Update(c => c.HideTaskbar = taskbarToggle.IsOn);
            //if (taskbarToggle.IsOn) _taskbarService.Hide();
            //else _taskbarService.Restore();
        };
        panel.Children.Add(taskbarToggle);

        var startupToggle = CreateToggle("Start with Windows", config.StartWithWindows, 16);
        startupToggle.Toggled += (s, e) =>
        {
            _configService.Update(c => c.StartWithWindows = startupToggle.IsOn);
            SetStartup(startupToggle.IsOn);
        };
        panel.Children.Add(startupToggle);

        panel.Children.Add(CreateSectionTitle("Language"));
        var langCombo = CreateComboBox("简体中文", "English");
        langCombo.SelectedIndex = config.Language == AppLanguage.Chinese ? 0 : 1;
        langCombo.SelectionChanged += (s, e) =>
        {
            var lang = langCombo.SelectedIndex == 0 ? AppLanguage.Chinese : AppLanguage.English;
            _configService.Update(c => c.Language = lang);
        };
        panel.Children.Add(langCombo);

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

    private static TextBlock CreateSectionTitle(string text)
    {
        return new TextBlock { Text = text, FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) };
    }

    private static ComboBox CreateComboBox(params string[] items)
    {
        var combo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 16) };
        foreach (var item in items)
        {
            combo.Items.Add(new ComboBoxItem { Content = item });
        }
        return combo;
    }

    private static Slider CreateSlider(double value, double minimum, double maximum, double step)
    {
        return new Slider { Minimum = minimum, Maximum = maximum, Value = value, StepFrequency = step, Margin = new Thickness(0, 0, 0, 16) };
    }

    private static ToggleSwitch CreateToggle(string header, bool isOn, int bottomMargin = 8)
    {
        return new ToggleSwitch { Header = header, IsOn = isOn, Margin = new Thickness(0, 0, 0, bottomMargin) };
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
