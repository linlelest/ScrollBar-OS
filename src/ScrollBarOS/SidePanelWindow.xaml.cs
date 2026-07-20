using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
// Note: avoid using Microsoft.UI.Win32Interop as a using alias because it's a type in some SDKs
using Windows.Graphics;
using ScrollBarOS.Services;
using ScrollBarOS.Models;
using ScrollBarOS.Helpers;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Linq;

namespace ScrollBarOS;

public partial class SidePanelWindow : Window
{
    public enum PanelMode { TrayIcons, SystemMenu }

    private readonly PanelMode _mode;
    private readonly WindowService _windowService;
    private readonly TrayService _trayService;
    private nint _hwnd;

    public SidePanelWindow(PanelMode mode, WindowService windowService, TrayService trayService, RectInt32 anchorRect)
    {
        _mode = mode;
        _windowService = windowService;
        _trayService = trayService;

        InitializeComponent();

        Title = mode == PanelMode.TrayIcons ? "Tray" : "Quick Settings";

        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        // Borderless topmost tool window
        Win32Helper.RemoveWindowBorder(_hwnd);
        Win32Helper.SetWindowStyle(_hwnd, isTopmost: true, isToolWindow: true);

        // Header text
        HeaderText.Text = _mode == PanelMode.TrayIcons ? "System Tray" : "Quick Settings";

        // Populate dynamic content
        if (_mode == PanelMode.TrayIcons)
        {
            PopulateTrayIcons();
        }
        else
        {
            PopulateSystemMenu();
        }

        // Current date/time
        DateTimeText.Text = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");

        // Position next to the capsule (to its left)
        var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(_hwnd));
        int panelWidth = 220;
        int panelHeight = mode == PanelMode.TrayIcons ? 320 : 360;
        int x = anchorRect.X - panelWidth - 8;
        int y = anchorRect.Y + (anchorRect.Height - panelHeight) / 2;
        appWindow.MoveAndResize(new RectInt32(x, y, panelWidth, panelHeight));

        Closed += (s, e) => { };
    }

    private void PopulateTrayIcons()
    {
        var windows = _windowService.GetVisibleWindows(true);
        var minimized = windows.Where(w => w.IsMinimized).ToList();

        if (minimized.Count == 0)
        {
            ItemsPanel.Children.Add(new TextBlock
            {
                Text = "No minimized apps",
                FontSize = 11,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF)),
                Margin = new Thickness(0, 8, 0, 0)
            });
            return;
        }

        foreach (var window in minimized)
        {
            var btn = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6, 5, 6, 5),
                Margin = new Thickness(0, 2, 0, 2),
                Tag = window
            };

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            if (window.Icon != null)
            {
                row.Children.Add(new Image { Source = window.Icon, Width = 16, Height = 16, Margin = new Thickness(0, 0, 8, 0) });
            }
            row.Children.Add(new TextBlock
            {
                Text = window.Title,
                FontSize = 11,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0xDD, 0xFF, 0xFF, 0xFF)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 150
            });
            btn.Content = row;

            btn.Click += (s, e) =>
            {
                if (s is Button b && b.Tag is WindowInfo wi)
                {
                    _windowService.FocusWindow(wi);
                    Close();
                }
            };

            ItemsPanel.Children.Add(btn);
        }
    }

    private void PopulateSystemMenu()
    {
        var items = new (string glyph, string label)[]
        {
            ("\uE767", "Volume"),
            ("\uE706", "Brightness"),
            ("\uE701", "Network"),
            ("\uE765", "Bluetooth"),
            ("\uE776", "Focus Assist"),
            ("\uE793", "Date & Time"),
        };

        foreach (var (glyph, label) in items)
        {
            var btn = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 7, 8, 7),
                Margin = new Thickness(0, 3, 0, 3),
                CornerRadius = new CornerRadius(6)
            };

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(new FontIcon
            {
                Glyph = glyph,
                FontSize = 13,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)),
                Margin = new Thickness(0, 0, 10, 0)
            });
            row.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 11,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0xDD, 0xFF, 0xFF, 0xFF)),
                VerticalAlignment = VerticalAlignment.Center
            });
            btn.Content = row;

            ItemsPanel.Children.Add(btn);
        }

        // Date/time is set in constructor
    }
}
