using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScrollBarOS.Helpers;
using ScrollBarOS.Models;
using ScrollBarOS.Services;
using System.Collections.ObjectModel;
using Windows.Graphics;

namespace ScrollBarOS;

/// <summary>
/// Tiling window with XAML UI
/// </summary>
public partial class TilingWindow : Window
{
    private readonly WindowService _windowService;
    private readonly TilingService _tilingService;
    private nint _hwnd;

    public ObservableCollection<WindowInfo> SelectedWindows { get; } = new();

    public TilingWindow(WindowService windowService, TilingService tilingService)
    {
        _windowService = windowService;
        _tilingService = tilingService;

        Title = "Window Tiling";

        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Win32Helper.SetWindowStyle(_hwnd, isTopmost: true, isToolWindow: false);

        var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(_hwnd));
        var workArea = Win32Helper.GetPrimaryMonitorWorkArea();
        int w = (int)(workArea.Width * 0.6);
        int h = (int)(workArea.Height * 0.6);
        int x = workArea.X + (workArea.Width - w) / 2;
        int y = workArea.Y + (workArea.Height - h) / 2;
        appWindow.MoveAndResize(new RectInt32(x, y, w, h));

        // Initialize data context
        DataContext = this;

        // Pre-populate with currently visible windows
        foreach (var win in _windowService.GetVisibleWindows(true))
        {
            SelectedWindows.Add(win);
        }
        UpdateCount();

        // Allow drag-drop of window icons into this window
        AllowDropThrough();
    }

    private void UpdateCount()
    {
        CountText.Text = $"({SelectedWindows.Count} windows)";
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is WindowInfo window)
        {
            SelectedWindows.Remove(window);
            UpdateCount();
        }
    }

    private void TileButton_Click(object sender, RoutedEventArgs e)
    {
        _tilingService.TileWindows(SelectedWindows.ToList());
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AllowDropThrough()
    {
        AllowDrop = true;
        DragEnter += (s, e) =>
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            }
        };
        Drop += async (s, e) =>
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                foreach (var item in items)
                {
                    if (item is Windows.Storage.IStorageFile file)
                    {
                        var path = file.Path;
                        if (File.Exists(path))
                        {
                            var windowInfo = _windowService.GetVisibleWindows()
                                .FirstOrDefault(w => w.ExecutablePath == path);
                            if (windowInfo != null && !SelectedWindows.Contains(windowInfo))
                            {
                                SelectedWindows.Add(windowInfo);
                                UpdateCount();
                            }
                        }
                    }
                }
            }
        };
    }
}

        return tile;
    }

    public void RecycleElement(ElementFactoryRecycleArgs args)
    {
        // No recycling needed for this simple factory
    }
}

