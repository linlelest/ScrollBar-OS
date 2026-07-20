using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScrollBarOS.Models;
using ScrollBarOS.Services;
using System.Collections.ObjectModel;

namespace ScrollBarOS.Views;

public sealed partial class WindowListOverlay : UserControl
{
    private WindowService? _windowService;

    public ObservableCollection<WindowInfo> Windows { get; } = new();

    public event EventHandler<WindowInfo>? WindowSelected;

    public WindowListOverlay()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows the window list overlay
    /// </summary>
    public void Show(WindowService windowService, int selectedIndex = 0)
    {
        _windowService = windowService;

        var windows = windowService.GetVisibleWindows(true);
        Windows.Clear();
        foreach (var window in windows)
        {
            Windows.Add(window);
        }

        WindowListView.ItemsSource = Windows;

        if (selectedIndex >= 0 && selectedIndex < Windows.Count)
        {
            WindowListView.SelectedIndex = selectedIndex;
        }

        OverlayRoot.Visibility = Visibility.Visible;
        (App.MainWindow as MainWindow)?.DisableClickThrough();
    }

    /// <summary>
    /// Hides the overlay
    /// </summary>
    public void Hide()
    {
        OverlayRoot.Visibility = Visibility.Collapsed;
        (App.MainWindow as MainWindow)?.EnableClickThrough();
    }

    /// <summary>
    /// Updates the selected index (for scroll navigation)
    /// </summary>
    public void UpdateSelection(int index)
    {
        if (index >= 0 && index < Windows.Count)
        {
            WindowListView.SelectedIndex = index;
            WindowListView.ScrollIntoView(Windows[index]);
        }
    }

    private void WindowListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is WindowInfo window)
        {
            _windowService?.FocusWindow(window);
            WindowSelected?.Invoke(this, window);
            Hide();
        }
    }
}
