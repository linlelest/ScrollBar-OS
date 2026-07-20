using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ScrollBarOS.Models;
using ScrollBarOS.Services;

namespace ScrollBarOS.Views;

public sealed partial class TrayMenu : UserControl
{
    private TrayService? _trayService;
    private WindowService? _windowService;

    public TrayMenu()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows the tray menu
    /// </summary>
    public void Show(TrayService trayService, WindowService windowService)
    {
        _trayService = trayService;
        _windowService = windowService;

        // Load minimized windows (tray items)
        var trayWindows = trayService.GetTrayWindows();
        TrayItemsRepeater.ItemsSource = trayWindows;

        // Load pinned apps
        PinnedItemsRepeater.ItemsSource = trayService.PinnedApps;

        TrayMenuRoot.Visibility = Visibility.Visible;

    }

    /// <summary>
    /// Hides the tray menu
    /// </summary>
    public void Hide()
    {
        TrayMenuRoot.Visibility = Visibility.Collapsed;

    }

    private void Backdrop_Tapped(object sender, TappedRoutedEventArgs e)
    {
        Hide();
    }

    private void HideButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void TrayItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is WindowInfo window)
        {
            _windowService?.FocusWindow(window);
            Hide();
        }
    }

    private void PinnedItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is PinnedAppInfo app)
        {
            _trayService?.LaunchPinnedApp(app);
        }
    }

    private void PinnedItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is PinnedAppInfo app)
        {
            var menu = new MenuFlyout();
            var unpinItem = new MenuFlyoutItem { Text = "Unpin" };
            unpinItem.Click += (s, args) =>
            {
                _trayService?.UnpinApp(app.ExecutablePath);
                PinnedItemsRepeater.ItemsSource = _trayService?.PinnedApps;
            };
            menu.Items.Add(unpinItem);
            menu.ShowAt(button);
        }
    }
}
