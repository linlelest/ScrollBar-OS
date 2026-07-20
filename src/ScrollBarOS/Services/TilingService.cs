using ScrollBarOS.Helpers;
using ScrollBarOS.Models;

namespace ScrollBarOS.Services;

/// <summary>
/// Service for tiling windows in a grid layout
/// </summary>
public class TilingService
{
    private readonly WindowService _windowService;
    private readonly Stack<List<(nint hwnd, WindowRect originalRect)>> _undoStack = new();

    public TilingService(WindowService windowService)
    {
        _windowService = windowService;
    }

    /// <summary>
    /// Tiles the specified windows in a grid layout across the current monitor work area
    /// </summary>
    public void TileWindows(List<WindowInfo> windows)
    {
        if (windows.Count == 0) return;

        // Get the current monitor work area
        var workArea = Win32Helper.GetCurrentMonitorWorkArea();

        // Save original positions for undo
        var originalPositions = windows.Select(w => (w.Handle, w.Position)).ToList();
        _undoStack.Push(originalPositions);

        // Calculate grid dimensions (golden ratio approximation)
        int count = windows.Count;
        int cols = (int)Math.Ceiling(Math.Sqrt(count));
        int rows = (int)Math.Ceiling((double)count / cols);

        // Calculate cell dimensions
        int cellWidth = workArea.Width / cols;
        int cellHeight = workArea.Height / rows;

        // Position each window
        for (int i = 0; i < windows.Count; i++)
        {
            int row = i / cols;
            int col = i % cols;

            int x = workArea.Left + col * cellWidth;
            int y = workArea.Top + row * cellHeight;

            // Handle last row with fewer items - center them
            int itemsInThisRow = Math.Min(cols, count - row * cols);
            if (itemsInThisRow < cols && row == rows - 1)
            {
                int totalWidth = itemsInThisRow * cellWidth;
                int offsetX = (workArea.Width - totalWidth) / 2;
                x = workArea.Left + offsetX + col * cellWidth;
            }

            Win32Helper.SetWindowPosition(windows[i].Handle, x, y, cellWidth, cellHeight);
        }
    }

    /// <summary>
    /// Tiles windows using a specific layout pattern
    /// </summary>
    public void TileWindows(List<WindowInfo> windows, TilingLayout layout)
    {
        if (windows.Count == 0) return;

        var workArea = Win32Helper.GetCurrentMonitorWorkArea();

        // Save original positions
        var originalPositions = windows.Select(w => (w.Handle, w.Position)).ToList();
        _undoStack.Push(originalPositions);

        switch (layout)
        {
            case TilingLayout.Grid:
                TileGrid(windows, workArea);
                break;
            case TilingLayout.Horizontal:
                TileHorizontal(windows, workArea);
                break;
            case TilingLayout.Vertical:
                TileVertical(windows, workArea);
                break;
            case TilingLayout.MasterSlave:
                TileMasterSlave(windows, workArea);
                break;
        }
    }

    private void TileGrid(List<WindowInfo> windows, WindowRect workArea)
    {
        int count = windows.Count;
        int cols = (int)Math.Ceiling(Math.Sqrt(count));
        int rows = (int)Math.Ceiling((double)count / cols);

        int cellWidth = workArea.Width / cols;
        int cellHeight = workArea.Height / rows;

        for (int i = 0; i < count; i++)
        {
            int row = i / cols;
            int col = i % cols;
            int x = workArea.Left + col * cellWidth;
            int y = workArea.Top + row * cellHeight;

            Win32Helper.SetWindowPosition(windows[i].Handle, x, y, cellWidth, cellHeight);
        }
    }

    private void TileHorizontal(List<WindowInfo> windows, WindowRect workArea)
    {
        int count = windows.Count;
        int cellWidth = workArea.Width / count;

        for (int i = 0; i < count; i++)
        {
            int x = workArea.Left + i * cellWidth;
            Win32Helper.SetWindowPosition(windows[i].Handle, x, workArea.Top, cellWidth, workArea.Height);
        }
    }

    private void TileVertical(List<WindowInfo> windows, WindowRect workArea)
    {
        int count = windows.Count;
        int cellHeight = workArea.Height / count;

        for (int i = 0; i < count; i++)
        {
            int y = workArea.Top + i * cellHeight;
            Win32Helper.SetWindowPosition(windows[i].Handle, workArea.Left, y, workArea.Width, cellHeight);
        }
    }

    private void TileMasterSlave(List<WindowInfo> windows, WindowRect workArea)
    {
        if (windows.Count == 1)
        {
            Win32Helper.SetWindowPosition(windows[0].Handle,
                workArea.Left, workArea.Top, workArea.Width, workArea.Height);
            return;
        }

        // Master window takes left half
        int masterWidth = workArea.Width / 2;
        Win32Helper.SetWindowPosition(windows[0].Handle,
            workArea.Left, workArea.Top, masterWidth, workArea.Height);

        // Slave windows share right half
        int slaveCount = windows.Count - 1;
        int slaveWidth = workArea.Width - masterWidth;
        int slaveHeight = workArea.Height / slaveCount;

        for (int i = 1; i < windows.Count; i++)
        {
            int y = workArea.Top + (i - 1) * slaveHeight;
            Win32Helper.SetWindowPosition(windows[i].Handle,
                workArea.Left + masterWidth, y, slaveWidth, slaveHeight);
        }
    }

    /// <summary>
    /// Undoes the last tiling operation
    /// </summary>
    public bool UndoLastTiling()
    {
        if (_undoStack.Count == 0) return false;

        var originalPositions = _undoStack.Pop();
        foreach (var (hwnd, rect) in originalPositions)
        {
            Win32Helper.SetWindowPosition(hwnd, rect.Left, rect.Top, rect.Width, rect.Height);
        }

        return true;
    }

    /// <summary>
    /// Gets whether undo is available
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;
}

/// <summary>
/// Tiling layout patterns
/// </summary>
public enum TilingLayout
{
    Grid,
    Horizontal,
    Vertical,
    MasterSlave
}
