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
public class TilingWindow : Window
{
    private readonly WindowService _windowService;
    private readonly TilingService _tilingService;
    private nint _hwnd;

    private readonly ObservableCollection<WindowInfo> _selectedWindows = new();
    private UniformGridLayout? _gridLayout;
    private ItemsRepeater? _itemsRepeater;
    private TextBlock? _countText;

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

        Content = BuildUI();

        // Pre-populate with currently visible windows
        foreach (var win in _windowService.GetVisibleWindows(true))
        {
            _selectedWindows.Add(win);
        }
        UpdateCount();

        // Allow drag-drop of window icons into this window
        AllowDropThrough();
    }

    private UIElement BuildUI()
    {
        var rootBorder = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xF5, 0x1A, 0x1A, 0x28)),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(20)
        };

        var rootGrid = new Grid();
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Header
        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };
        headerPanel.Children.Add(new TextBlock
        {
            Text = "Window Tiling",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)),
            VerticalAlignment = VerticalAlignment.Center
        });
        _countText = new TextBlock
        {
            Text = "",
            FontSize = 12,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0)
        };
        headerPanel.Children.Add(_countText);
        rootGrid.Children.Add(headerPanel);

        // Grid preview area
        _gridLayout = new UniformGridLayout
        {
            MinItemWidth = 140,
            MinItemHeight = 100,
            MinColumnSpacing = 10,
            MinRowSpacing = 10,
            ItemsStretch = UniformGridLayoutItemsStretch.Fill
        };

        _itemsRepeater = new ItemsRepeater
        {
            Layout = _gridLayout,
            ItemsSource = _selectedWindows
        };
        _itemsRepeater.ItemTemplate = new WindowTileFactory(this);

        var scrollArea = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _itemsRepeater,
            AllowDrop = true
        };
        scrollArea.DragOver += (s, e) => { e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy; };
        scrollArea.Drop += ScrollArea_Drop;
        Grid.SetRow(scrollArea, 1);
        rootGrid.Children.Add(scrollArea);

        // Bottom bar: hint + confirm button
        var bottomBar = new Grid { Margin = new Thickness(0, 14, 0, 0) };
        bottomBar.Children.Add(new TextBlock
        {
            Text = "Drag app icons here, then confirm to tile.",
            FontSize = 11,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0x77, 0xFF, 0xFF, 0xFF)),
            VerticalAlignment = VerticalAlignment.Center
        });

        var confirmBtn = new Button
        {
            Content = "Confirm Tiling",
            HorizontalAlignment = HorizontalAlignment.Right,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x4C, 0xC2, 0xFF)),
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)),
            Padding = new Thickness(18, 8, 18, 8),
            CornerRadius = new CornerRadius(6),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        confirmBtn.Click += ConfirmBtn_Click;
        bottomBar.Children.Add(confirmBtn);
        Grid.SetRow(bottomBar, 2);
        rootGrid.Children.Add(bottomBar);

        rootBorder.Child = rootGrid;
        return rootBorder;
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
        if (_countText != null)
        {
            _countText.Text = $"{_selectedWindows.Count} window(s) selected";
        }
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

