using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScrollBarOS.Models;
using ScrollBarOS.Services;
using System.Collections.ObjectModel;

namespace ScrollBarOS.Views;

public sealed partial class TilingGrid : UserControl
{
    private TilingService? _tilingService;
    private WindowService? _windowService;

    public ObservableCollection<WindowInfo> SelectedWindows { get; } = new();

    public event EventHandler? TilingApplied;
    public event EventHandler? TilingCancelled;

    public TilingGrid()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows the tiling grid with current windows
    /// </summary>
    public void Show(TilingService tilingService, WindowService windowService)
    {
        _tilingService = tilingService;
        _windowService = windowService;

        // Load all visible windows
        var windows = windowService.GetVisibleWindows(true);
        SelectedWindows.Clear();
        foreach (var window in windows)
        {
            SelectedWindows.Add(window);
        }

        TilingItemsRepeater.ItemsSource = SelectedWindows;
        TilingRoot.Visibility = Visibility.Visible;

        // Disable click-through

    }

    /// <summary>
    /// Hides the tiling grid
    /// </summary>
    public void Hide()
    {
        TilingRoot.Visibility = Visibility.Collapsed;

    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_tilingService != null && SelectedWindows.Count > 0)
        {
            _tilingService.TileWindows(SelectedWindows.ToList());
            TilingApplied?.Invoke(this, EventArgs.Empty);
        }
        Hide();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        TilingCancelled?.Invoke(this, EventArgs.Empty);
        Hide();
    }
}
