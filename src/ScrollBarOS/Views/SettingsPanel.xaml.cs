using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using ScrollBarOS.Models;
using ScrollBarOS.Services;
using Microsoft.Win32;

namespace ScrollBarOS.Views;

public sealed partial class SettingsPanel : UserControl
{
    private readonly ConfigService _configService;
    private readonly TaskbarService _taskbarService;

    public SettingsPanel()
    {
        InitializeComponent();
        _configService = ConfigService.Instance;
        _taskbarService = new TaskbarService();

        Loaded += SettingsPanel_Loaded;
    }

    private void SettingsPanel_Loaded(object sender, RoutedEventArgs e)
    {
        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        var config = _configService.Config;

        // Material
        foreach (var item in MaterialCombo.Items.Cast<ComboBoxItem>())
        {
            if (item.Tag?.ToString() == config.Material.ToString())
            {
                MaterialCombo.SelectedItem = item;
                break;
            }
        }

        // Opacity
        OpacitySlider.Value = config.BackgroundOpacity;

        // Position
        PositionRadio.SelectedIndex = config.CapsulePosition == CapsulePosition.Left ? 0 : 1;

        // Widgets
        DateTimeToggle.IsOn = config.ShowDateTimeWidget;
        HardwareToggle.IsOn = config.ShowHardwareWidget;

        // Taskbar
        TaskbarToggle.IsOn = config.HideTaskbar;

        // Startup
        StartupToggle.IsOn = config.StartWithWindows;

        // Language
        LanguageCombo.SelectedIndex = config.Language == AppLanguage.Chinese ? 0 : 1;

        // Size
        CapsuleWidthSlider.Value = config.CapsuleWidth;
        IconSizeSlider.Value = config.IconSize;
    }

    /// <summary>
    /// Shows the settings panel with slide animation
    /// </summary>
    public void Show()
    {
        SettingsRoot.Visibility = Visibility.Visible;
        (App.MainWindow as MainWindow)?.DisableClickThrough();

        // Slide in animation
        var animation = new DoubleAnimation
        {
            From = 400,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(180)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var transform = new Microsoft.UI.Xaml.Media.TranslateTransform();
        SettingsBorder.RenderTransform = transform;

        var storyboard = new Storyboard();
        Storyboard.SetTarget(animation, transform);
        Storyboard.SetTargetProperty(animation, "X");
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    /// <summary>
    /// Hides the settings panel
    /// </summary>
    public void Hide()
    {
        SettingsRoot.Visibility = Visibility.Collapsed;
        (App.MainWindow as MainWindow)?.EnableClickThrough();
    }

    #region Event Handlers

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void Backdrop_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        Hide();
    }

    private void MaterialCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MaterialCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            if (Enum.TryParse<BackgroundMaterial>(tag, out var material))
            {
                _configService.Update(c => c.Material = material);
            }
        }
    }

    private void OpacitySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        _configService.Update(c => c.BackgroundOpacity = e.NewValue);
    }

    private void PositionRadio_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var position = PositionRadio.SelectedIndex == 0 ? CapsulePosition.Left : CapsulePosition.Right;
        _configService.Update(c => c.CapsulePosition = position);
    }

    private void DateTimeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        _configService.Update(c => c.ShowDateTimeWidget = DateTimeToggle.IsOn);
    }

    private void HardwareToggle_Toggled(object sender, RoutedEventArgs e)
    {
        _configService.Update(c => c.ShowHardwareWidget = HardwareToggle.IsOn);
    }

    private void TaskbarToggle_Toggled(object sender, RoutedEventArgs e)
    {
        _configService.Update(c => c.HideTaskbar = TaskbarToggle.IsOn);

        if (TaskbarToggle.IsOn)
            _taskbarService.Hide();
        else
            _taskbarService.Restore();
    }

    private void StartupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        _configService.Update(c => c.StartWithWindows = StartupToggle.IsOn);
        SetStartup(StartupToggle.IsOn);
    }

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var language = LanguageCombo.SelectedIndex == 0 ? AppLanguage.Chinese : AppLanguage.English;
        _configService.Update(c => c.Language = language);
    }

    private void CapsuleWidthSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        _configService.Update(c => c.CapsuleWidth = (int)e.NewValue);
    }

    private void IconSizeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        _configService.Update(c => c.IconSize = (int)e.NewValue);
    }

    #endregion

    /// <summary>
    /// Sets or removes the app from Windows startup
    /// </summary>
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
                {
                    key.SetValue(appName, $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue(appName, false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set startup: {ex.Message}");
        }
    }
}
