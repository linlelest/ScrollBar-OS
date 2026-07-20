using ScrollBarOS.Models;

namespace ScrollBarOS.Services;

/// <summary>
/// Service for managing system tray icons and pinned applications
/// </summary>
public class TrayService
{
    private readonly ConfigService _configService;
    private readonly WindowService _windowService;

    public List<PinnedAppInfo> PinnedApps { get; private set; } = new();

    public event EventHandler? PinnedAppsChanged;

    public TrayService(WindowService windowService)
    {
        _configService = ConfigService.Instance;
        _windowService = windowService;
        LoadPinnedApps();
    }

    /// <summary>
    /// Loads pinned apps from configuration
    /// </summary>
    private void LoadPinnedApps()
    {
        PinnedApps.Clear();

        foreach (var path in _configService.Config.PinnedApps)
        {
            if (File.Exists(path))
            {
                PinnedApps.Add(CreatePinnedAppInfo(path));
            }
        }
    }

    /// <summary>
    /// Pins an application
    /// </summary>
    public void PinApp(string executablePath)
    {
        if (string.IsNullOrEmpty(executablePath) || !File.Exists(executablePath))
            return;

        if (_configService.Config.PinnedApps.Contains(executablePath))
            return;

        _configService.Config.PinnedApps.Add(executablePath);
        _configService.SaveDebounced();

        PinnedApps.Add(CreatePinnedAppInfo(executablePath));

        PinnedAppsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Unpins an application
    /// </summary>
    public void UnpinApp(string executablePath)
    {
        _configService.Config.PinnedApps.Remove(executablePath);
        _configService.SaveDebounced();

        var app = PinnedApps.FirstOrDefault(a => a.ExecutablePath == executablePath);
        if (app != null)
        {
            PinnedApps.Remove(app);
        }

        PinnedAppsChanged?.Invoke(this, EventArgs.Empty);
    }

    private PinnedAppInfo CreatePinnedAppInfo(string path)
    {
        return new PinnedAppInfo
        {
            Name = Path.GetFileNameWithoutExtension(path),
            ExecutablePath = path,
            Icon = _windowService.GetIconForPath(path)
        };
    }

    /// <summary>
    /// Launches a pinned application
    /// </summary>
    public void LaunchPinnedApp(PinnedAppInfo app)
    {
        _windowService.LaunchApp(app.ExecutablePath, app.Arguments);
    }

    /// <summary>
    /// Gets running processes that have tray icons (approximation)
    /// </summary>
    public List<WindowInfo> GetTrayWindows()
    {
        // This is a simplified implementation
        // Full tray icon enumeration requires Shell_NotifyIcon which has restrictions
        var windows = _windowService.GetVisibleWindows(true);
        return windows.Where(w => w.IsMinimized).ToList();
    }
}
