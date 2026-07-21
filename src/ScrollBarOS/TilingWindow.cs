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
/// Independent tiling configuration window.
/// Shows a grid preview of selected windows, supports drag-in from capsule,
/// per-item delete, and a confirm button to execute the tiling layout.
/// </summary>
public partial class TilingWindow : Window
{
    private readonly WindowService _windowService;
    private readonly TilingService _tilingService;
    private nint _hwnd;

    private readonly ObservableCollection<WindowInfo> _selectedWindows = new();

    public TilingWindow(WindowService windowService, TilingService tilingService)
    {
        _windowService = windowService;
        _tilingService = tilingService;

        InitializeComponent();

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

        InitializeControls();

        // Pre-populate with currently visible windows
        foreach (var win in _windowService.GetVisibleWindows(true))
        {
            _selectedWindows.Add(win);
        }
        UpdateCount();

        // Allow drag-drop of window icons into this window
        AllowDropThrough();
    }

    private void InitializeControls()
    {
        // Set up ItemsRepeater with data source and template
        ItemsRepeater.ItemsSource = _selectedWindows;
        ItemsRepeater.ItemTemplate = new WindowTileFactory(this);

        // Set up drag and drop
        ScrollViewer.AllowDrop = true;
        ScrollViewer.DragOver += (s, e) => { e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy; };
        ScrollViewer.Drop += ScrollArea_Drop;

        // Set up confirm button
        ConfirmButton.Click += ConfirmBtn_Click;
    }

    private void AllowDropThrough()
    {
        // The window itself accepts drops
    }

    private async void ScrollArea_Drop(object sender, DragEventArgs e)
    {
        try
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
            {
                var text = await e.DataView.GetTextAsync();
                // Expect window handle as text
                if (long.TryParse(text, out long hwndValue))
                {
                    var windows = _windowService.GetVisibleWindows(true);
                    var target = windows.FirstOrDefault(w => w.Handle == (nint)hwndValue);
                    if (target != null && !_selectedWindows.Any(w => w.Handle == target.Handle))
                    {
                        _selectedWindows.Add(target);
                        UpdateCount();
                    }
                }
            }
        }
        catch { }
    }

    internal void RemoveWindow(WindowInfo window)
    {
        var item = _selectedWindows.FirstOrDefault(w => w.Handle == window.Handle);
        if (item != null)
        {
            _selectedWindows.Remove(item);
            UpdateCount();
        }
    }

    private void UpdateCount()
    {
        CountText.Text = $"{_selectedWindows.Count} window(s) selected";
    }

    private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedWindows.Count > 0)
        {
            _tilingService.TileWindows(_selectedWindows.ToList());
        }
        Close();
    }
}

/// <summary>
/// Element factory for each window tile in the tiling grid.
/// Shows icon + title with a delete (X) button in the top-right corner.
/// </summary>
internal class WindowTileFactory : IElementFactory
{
    private readonly TilingWindow _owner;

    public WindowTileFactory(TilingWindow owner)
    {
        _owner = owner;
    }

    public UIElement GetElement(ElementFactoryGetArgs args)
    {
        var window = (WindowInfo)args.Data;

        var tile = new Grid
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            MinHeight = 100
        };

        var content = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (window.Icon != null)
        {
            content.Children.Add(new Image
            {
                Source = window.Icon,
                Width = 36,
                Height = 36,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }
        else
        {
            content.Children.Add(new FontIcon
            {
                Glyph = "\uE737",
                FontSize = 28,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xAA, 0xFF, 0xFF, 0xFF)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }

        content.Children.Add(new TextBlock
        {
            Text = window.Title,
            FontSize = 11,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xDD, 0xFF, 0xFF, 0xFF)),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1
        });

        tile.Children.Add(content);

        // Delete (X) button in top-right corner
        var deleteBtn = new Button
        {
            Content = new FontIcon
            {
                Glyph = "\uE711",
                FontSize = 10,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF))
            },
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0x44, 0xFF, 0x44, 0x44)),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4),
            Width = 22,
            Height = 22,
            CornerRadius = new CornerRadius(11),
            Tag = window
        };
        deleteBtn.Click += (s, e) =>
        {
            if (s is Button btn && btn.Tag is WindowInfo wi)
            {
                _owner.RemoveWindow(wi);
            }
        };
        tile.Children.Add(deleteBtn);

        return tile;
    }

    public void RecycleElement(ElementFactoryRecycleArgs args)
    {
        // No recycling needed for this simple factory
    }
}
