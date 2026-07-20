using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using ScrollBarOS.Services;
using ScrollBarOS.Models;
using Microsoft.Win32;
using System.IO;

namespace ScrollBarOS;

/// <summary>
/// Settings window that opens as a separate window (XAML-backed)
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly ConfigService _configService;
    private readonly TaskbarService _taskbarService;

    // References to named controls loaded from XAML
    private ComboBox? PositionCombo;
    private ComboBox? MaterialCombo;
    private Slider? OpacitySlider;
    private TextBlock? OpacityLabel;
    private Slider? WidthSlider;
    private TextBlock? WidthLabel;
    private Slider? IconSlider;
    private TextBlock? IconLabel;
    private ToggleSwitch? HwToggle;
    private ToggleSwitch? DtToggle;
    private ToggleSwitch? TaskbarToggle;
    private ToggleSwitch? StartupToggle;
    private ComboBox? LangCombo;

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

        // Try to load XAML from the copied SettingsWindow.xaml at runtime
        var xamlPath = Path.Combine(AppContext.BaseDirectory, "SettingsWindow.xaml");
        if (File.Exists(xamlPath))
        {
            try
            {
                var xamlText = File.ReadAllText(xamlPath);
                if (!string.IsNullOrWhiteSpace(xamlText))
                {
                    var root = (FrameworkElement)XamlReader.Load(xamlText);
                    Content = root;

                    // Resolve named controls
                    PositionCombo = root.FindName("PositionCombo") as ComboBox;
                    MaterialCombo = root.FindName("MaterialCombo") as ComboBox;
                    OpacitySlider = root.FindName("OpacitySlider") as Slider;
                    OpacityLabel = root.FindName("OpacityLabel") as TextBlock;
                    WidthSlider = root.FindName("WidthSlider") as Slider;
                    WidthLabel = root.FindName("WidthLabel") as TextBlock;
                    IconSlider = root.FindName("IconSlider") as Slider;
                    IconLabel = root.FindName("IconLabel") as TextBlock;
                    HwToggle = root.FindName("HwToggle") as ToggleSwitch;
                    DtToggle = root.FindName("DtToggle") as ToggleSwitch;
                    TaskbarToggle = root.FindName("TaskbarToggle") as ToggleSwitch;
                    StartupToggle = root.FindName("StartupToggle") as ToggleSwitch;
                    LangCombo = root.FindName("LangCombo") as ComboBox;

                    // Attach handlers
                    if (PositionCombo != null) PositionCombo.SelectionChanged += PositionCombo_SelectionChanged;
                    if (MaterialCombo != null) MaterialCombo.SelectionChanged += MaterialCombo_SelectionChanged;
                    if (OpacitySlider != null) OpacitySlider.ValueChanged += OpacitySlider_ValueChanged;
                    if (WidthSlider != null) WidthSlider.ValueChanged += WidthSlider_ValueChanged;
                    if (IconSlider != null) IconSlider.ValueChanged += IconSlider_ValueChanged;
                    if (HwToggle != null) HwToggle.Toggled += HwToggle_Toggled;
                    if (DtToggle != null) DtToggle.Toggled += DtToggle_Toggled;
                    if (TaskbarToggle != null) TaskbarToggle.Toggled += TaskbarToggle_Toggled;
                    if (StartupToggle != null) StartupToggle.Toggled += StartupToggle_Toggled;
                    if (LangCombo != null) LangCombo.SelectionChanged += LangCombo_SelectionChanged;

                    // Initialize values
                    InitializeFromConfig();
                    return;
                }
            }
            catch
            {
                // fall through to programmatic fallback
            }
        }

        // Fallback: create a minimal programmatic UI if XAML load failed
        Content = new TextBlock { Text = "Settings UI unavailable", Padding = new Thickness(20) };
    }

    private void InitializeFromConfig()
    {
        var config = _configService.Config;

        if (PositionCombo != null) PositionCombo.SelectedIndex = config.CapsulePosition == CapsulePosition.Left ? 0 : 1;
        if (MaterialCombo != null) MaterialCombo.SelectedIndex = (int)config.Material;
        if (OpacitySlider != null) OpacitySlider.Value = config.BackgroundOpacity * 100;
        if (OpacityLabel != null) OpacityLabel.Text = $"Opacity: {config.BackgroundOpacity:P0}";
        if (WidthSlider != null) WidthSlider.Value = config.CapsuleWidth;
        if (WidthLabel != null) WidthLabel.Text = $"Capsule Width: {config.CapsuleWidth}px";
        if (IconSlider != null) IconSlider.Value = config.IconSize;
        if (IconLabel != null) IconLabel.Text = $"Icon Size: {config.IconSize}px";

        if (HwToggle != null) HwToggle.IsOn = config.ShowHardwareWidget;
        if (DtToggle != null) DtToggle.IsOn = config.ShowDateTimeWidget;

        if (TaskbarToggle != null) TaskbarToggle.IsOn = config.HideTaskbar;
        if (StartupToggle != null) StartupToggle.IsOn = config.StartWithWindows;

        if (LangCombo != null) LangCombo.SelectedIndex = config.Language == AppLanguage.Chinese ? 0 : 1;
    }

    private void PositionCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (PositionCombo == null) return;
        var pos = PositionCombo.SelectedIndex == 0 ? CapsulePosition.Left : CapsulePosition.Right;
        _configService.Update(c => c.CapsulePosition = pos);
    }

    private void MaterialCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (MaterialCombo == null) return;
        _configService.Update(c => c.Material = (BackgroundMaterial)MaterialCombo.SelectedIndex);
    }

    private void OpacitySlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        _configService.Update(c => c.BackgroundOpacity = e.NewValue / 100.0);
        if (OpacityLabel != null) OpacityLabel.Text = $"Opacity: {e.NewValue / 100.0:P0}";
    }

    private void WidthSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        _configService.Update(c => c.CapsuleWidth = (int)e.NewValue);
        if (WidthLabel != null) WidthLabel.Text = $"Capsule Width: {(int)e.NewValue}px";
    }

    private void IconSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        _configService.Update(c => c.IconSize = (int)e.NewValue);
        if (IconLabel != null) IconLabel.Text = $"Icon Size: {(int)e.NewValue}px";
    }

    private void HwToggle_Toggled(object? sender, RoutedEventArgs e)
    {
        if (HwToggle == null) return;
        _configService.Update(c => c.ShowHardwareWidget = HwToggle.IsOn);
    }

    private void DtToggle_Toggled(object? sender, RoutedEventArgs e)
    {
        if (DtToggle == null) return;
        _configService.Update(c => c.ShowDateTimeWidget = DtToggle.IsOn);
    }

    private void TaskbarToggle_Toggled(object? sender, RoutedEventArgs e)
    {
        if (TaskbarToggle == null) return;
        _configService.Update(c => c.HideTaskbar = TaskbarToggle.IsOn);
        if (TaskbarToggle.IsOn) _taskbarService.Hide();
        else _taskbarService.Restore();
    }

    private void StartupToggle_Toggled(object? sender, RoutedEventArgs e)
    {
        if (StartupToggle == null) return;
        _configService.Update(c => c.StartWithWindows = StartupToggle.IsOn);
        SetStartup(StartupToggle.IsOn);
    }

    private void LangCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (LangCombo == null) return;
        var lang = LangCombo.SelectedIndex == 0 ? AppLanguage.Chinese : AppLanguage.English;
        _configService.Update(c => c.Language = lang);
    }

    private void SetStartup(bool enable)
    {
        try
        {
            const string keyPath = @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
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
