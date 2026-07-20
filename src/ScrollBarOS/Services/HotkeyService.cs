using System.Runtime.InteropServices;

namespace ScrollBarOS.Services;

/// <summary>
/// Service for registering and handling global hotkeys
/// </summary>
public class HotkeyService
{
    public const int MOD_ALT = 0x0001;
    public const int MOD_CONTROL = 0x0002;
    public const int MOD_SHIFT = 0x0004;
    public const int MOD_WIN = 0x0008;

    private const int WM_HOTKEY = 0x0312;

    private readonly Dictionary<int, Action> _hotkeys = new();
    private int _nextId = 1;
    private nint _hwnd;
    private nint _wndProcSubclass;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    [DllImport("user32.dll")]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll")]
    private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern nint CallWindowProc(nint lpPrevWndFunc, nint hWnd, uint Msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern nint DefWindowProc(nint hWnd, uint Msg, nint wParam, nint lParam);

    private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);
    private WndProcDelegate? _wndProcDelegate;
    private nint _originalWndProc;

    private const int GWLP_WNDPROC = -4;

    /// <summary>
    /// Registers a global hotkey
    /// </summary>
    public bool Register(nint hwnd, int modifiers, int virtualKey, Action callback)
    {
        _hwnd = hwnd;

        // Subclass the window to receive hotkey messages
        if (_wndProcDelegate == null)
        {
            _wndProcDelegate = WndProc;
            _originalWndProc = GetWindowLongPtr(hwnd, GWLP_WNDPROC);
            SetWindowLongPtr(hwnd, GWLP_WNDPROC,
                Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
        }

        int id = _nextId++;
        if (RegisterHotKey(hwnd, id, (uint)modifiers, (uint)virtualKey))
        {
            _hotkeys[id] = callback;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Unregisters all hotkeys
    /// </summary>
    public void UnregisterAll()
    {
        foreach (var id in _hotkeys.Keys)
        {
            UnregisterHotKey(_hwnd, id);
        }
        _hotkeys.Clear();

        // Restore original window proc
        if (_originalWndProc != nint.Zero)
        {
            SetWindowLongPtr(_hwnd, GWLP_WNDPROC, _originalWndProc);
            _originalWndProc = nint.Zero;
        }
    }

    /// <summary>
    /// Window procedure to handle hotkey messages
    /// </summary>
    private nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == WM_HOTKEY)
        {
            int id = (int)wParam;
            if (_hotkeys.TryGetValue(id, out var callback))
            {
                callback();
                return nint.Zero;
            }
        }

        return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
    }
}
