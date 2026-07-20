using System.Diagnostics;
using ScrollBarOS.Models;

namespace ScrollBarOS.Services;

/// <summary>
/// Scroll state machine that determines slow vs fast scrolling behavior
/// </summary>
public class ScrollStateMachine
{
    private readonly WindowService _windowService;
    private readonly ConfigService _configService;

    private readonly List<(DateTime timestamp, int delta)> _scrollHistory = new();
    private int _currentFocusIndex = 0;
    private ScrollMode _currentMode = ScrollMode.Idle;
    private Timer? _modeResetTimer;
    private Timer? _listExitTimer;

    public event EventHandler<ScrollModeChangedEventArgs>? ModeChanged;
    public event EventHandler<WindowFocusedEventArgs>? WindowFocused;
    public event EventHandler<List<int>>? WindowListRequested;

    public ScrollMode CurrentMode => _currentMode;
    public int CurrentFocusIndex => _currentFocusIndex;

    public ScrollStateMachine(WindowService windowService)
    {
        _windowService = windowService;
        _configService = ConfigService.Instance;
    }

    /// <summary>
    /// Processes a mouse wheel scroll event
    /// </summary>
    public void ProcessScroll(int delta)
    {
        var now = DateTime.Now;
        _scrollHistory.Add((now, delta));

        // Remove entries older than 200ms
        _scrollHistory.RemoveAll(entry => (now - entry.timestamp).TotalMilliseconds > 200);

        int totalTicks = _scrollHistory.Count;
        int threshold = _configService.Config.ScrollThreshold;

        if (totalTicks > threshold)
        {
            if (_currentMode != ScrollMode.FastScroll)
            {
                SetMode(ScrollMode.FastScroll);
            }
            HandleFastScroll(delta);
        }
        else if (_currentMode == ScrollMode.FastScroll)
        {
            ResetListExitTimer();
        }
        else
        {
            if (_currentMode != ScrollMode.SlowScroll)
            {
                SetMode(ScrollMode.SlowScroll);
            }
            HandleSlowScroll(delta);
        }

        ResetModeTimer();
    }

    /// <summary>
    /// Handles slow scroll - directly switches focused window
    /// </summary>
    private void HandleSlowScroll(int delta)
    {
        var windows = _windowService.GetVisibleWindows();
        if (windows.Count == 0) return;

        int direction = GetDirection(delta);

        // Update focus index
        _currentFocusIndex += direction;
        if (_currentFocusIndex < 0) _currentFocusIndex = windows.Count - 1;
        if (_currentFocusIndex >= windows.Count) _currentFocusIndex = 0;

        // Focus the window
        var targetWindow = windows[_currentFocusIndex];
        _windowService.FocusWindow(targetWindow);

        WindowFocused?.Invoke(this, new WindowFocusedEventArgs(targetWindow, _currentFocusIndex));
    }

    private static int GetDirection(int delta) => delta > 0 ? -1 : 1;

    /// <summary>
    /// Handles fast scroll - updates list selection
    /// </summary>
    private void HandleFastScroll(int delta)
    {
        var windows = _windowService.GetVisibleWindows();
        if (windows.Count == 0) return;

        int direction = GetDirection(delta);
        _currentFocusIndex += direction;
        _currentFocusIndex = Math.Clamp(_currentFocusIndex, 0, windows.Count - 1);

        WindowFocused?.Invoke(this, new WindowFocusedEventArgs(windows[_currentFocusIndex], _currentFocusIndex));
    }

    /// <summary>
    /// Called when scrolling stops in fast mode - focuses the selected window
    /// </summary>
    public void ConfirmSelection()
    {
        var windows = _windowService.GetVisibleWindows();
        if (windows.Count > 0 && _currentFocusIndex < windows.Count)
        {
            _windowService.FocusWindow(windows[_currentFocusIndex]);
        }

        SetMode(ScrollMode.Idle);
    }

    /// <summary>
    /// Sets the scroll mode and raises the event
    /// </summary>
    private void SetMode(ScrollMode mode)
    {
        if (_currentMode == mode) return;

        var oldMode = _currentMode;
        _currentMode = mode;

        if (mode == ScrollMode.FastScroll)
        {
            // Request window list display
            var windows = _windowService.GetVisibleWindows(true);
            WindowListRequested?.Invoke(this, windows.Select((_, i) => i).ToList());
        }

        ModeChanged?.Invoke(this, new ScrollModeChangedEventArgs(oldMode, mode));
    }

    /// <summary>
    /// Resets the mode to idle after timeout
    /// </summary>
    private void ResetModeTimer()
    {
        _modeResetTimer?.Dispose();
        _modeResetTimer = new Timer(_ =>
        {
            if (_currentMode == ScrollMode.SlowScroll)
            {
                SetMode(ScrollMode.Idle);
            }
            else if (_currentMode == ScrollMode.FastScroll)
            {
                // Auto-confirm and exit list mode after 1.5s
                ConfirmSelection();
            }
        }, null, 1500, Timeout.Infinite);
    }

    /// <summary>
    /// Resets the list exit timer (called during fast scroll)
    /// </summary>
    private void ResetListExitTimer()
    {
        _listExitTimer?.Dispose();
        _listExitTimer = new Timer(_ =>
        {
            ConfirmSelection();
        }, null, 1500, Timeout.Infinite);
    }

    /// <summary>
    /// Resets the state machine
    /// </summary>
    public void Reset()
    {
        _scrollHistory.Clear();
        _currentFocusIndex = 0;
        SetMode(ScrollMode.Idle);
        _modeResetTimer?.Dispose();
        _listExitTimer?.Dispose();
    }
}

/// <summary>
/// Scroll mode enumeration
/// </summary>
public enum ScrollMode
{
    Idle,
    SlowScroll,
    FastScroll
}

/// <summary>
/// Event args for mode changes
/// </summary>
public class ScrollModeChangedEventArgs : EventArgs
{
    public ScrollMode OldMode { get; }
    public ScrollMode NewMode { get; }

    public ScrollModeChangedEventArgs(ScrollMode oldMode, ScrollMode newMode)
    {
        OldMode = oldMode;
        NewMode = newMode;
    }
}

/// <summary>
/// Event args for window focus changes
/// </summary>
public class WindowFocusedEventArgs : EventArgs
{
    public WindowInfo Window { get; }
    public int Index { get; }

    public WindowFocusedEventArgs(WindowInfo window, int index)
    {
        Window = window;
        Index = index;
    }
}
