using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using ScrollBarOS.Services;
using ScrollBarOS.Models;
using ScrollBarOS.Helpers;

namespace ScrollBarOS;

/// <summary>
/// A side panel window that expands next to the capsule.
/// Used for both tray icons and system quick-settings.
/// </summary>
public class SidePanelWindow : Window
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

        Title = mode == PanelMode.TrayIcons ? "Tray" : "Quick Settings";

        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        // Borderless topmost tool window
        Win32Helper.RemoveWindowBorder(_hwnd);
        Win32Helper.SetWindowStyle(_hwnd, isTopmost: true, isToolWindow: true);

        Content = BuildContent();

        // Position next to the capsule (to its left)
        var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(_hwnd));
        int panelWidth = 220;
        int panelHeight = mode == PanelMode.TrayIcons ? 320 : 360;
        int x = anchorRect.X - panelWidth - 8;
        int y = anchorRect.Y + (anchorRect.Height - panelHeight) / 2;
        appWindow.MoveAndResize(new RectInt32(x, y, panelWidth, panelHeight));

        Closed += (s, e) => { };
    }

    private UIElement BuildContent()
    {
        var border = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xEE, 0x1E, 0x1E, 0x2E)),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12)
        };

        var panel = new StackPanel();

        // Header
        var header = new TextBlock
        {
            Text = _mode == PanelMode.TrayIcons ? "System Tray" : "Quick Settings",
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)),
            Margin = new Thickness(0, 0, 0, 10)
        };
        panel.Children.Add(header);

        if (_mode == PanelMode.TrayIcons)
        {
            BuildTrayIcons(panel);
        }
        else
        {
            BuildSystemMenu(panel);
        }

        border.Child = panel;
        return border;
    }

    private void BuildTrayIcons(StackPanel panel)
    {
        // Show minimized windows (tray-like items)
        var windows = _windowService.GetVisibleWindows(true);
        var minimized = windows.Where(w => w.IsMinimized).ToList();

        if (minimized.Count == 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "No minimized apps",
                FontSize = 11,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF)),
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
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
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
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xDD, 0xFF, 0xFF, 0xFF)),
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

            panel.Children.Add(btn);
        }
    }

    private void BuildSystemMenu(StackPanel panel)
    {
        // System quick settings: volume, brightness, date, network placeholders
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
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)),
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
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)),
                Margin = new Thickness(0, 0, 10, 0)
            });
            row.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 11,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0xDD, 0xFF, 0xFF, 0xFF)),
                VerticalAlignment = VerticalAlignment.Center
            });
            btn.Content = row;

            panel.Children.Add(btn);
        }

        // Current date/time at bottom
        panel.Children.Add(new TextBlock
        {
            Text = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss"),
            FontSize = 10,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 12, 0, 0)
        });
    }
}
