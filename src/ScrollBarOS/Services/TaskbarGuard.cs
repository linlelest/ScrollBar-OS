using System.Runtime.InteropServices;

namespace ScrollBarOS.Services;

/// <summary>
/// Ensures the system taskbar is restored even if the app is force-killed.
/// Uses a lock file: if the app crashes/is force-quit, the lock file remains,
/// and the next launch (or the watchdog) restores the taskbar.
/// </summary>
public class TaskbarGuard : IDisposable
{
    private readonly TaskbarService _taskbarService;
    private readonly string _lockFilePath;
    private Timer? _watchdogTimer;

    public TaskbarGuard(TaskbarService taskbarService)
    {
        _taskbarService = taskbarService;
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScrollBarOS");
        Directory.CreateDirectory(dir);
        _lockFilePath = Path.Combine(dir, "taskbar_hidden.lock");

        // On startup: if a previous instance was force-killed while taskbar was hidden, restore it
        if (File.Exists(_lockFilePath))
        {
            _taskbarService.Restore();
            try { File.Delete(_lockFilePath); } catch { }
        }

        // Hook process exit events for graceful cleanup
        AppDomain.CurrentDomain.ProcessExit += (s, e) => Cleanup();
        Console.CancelKeyPress += (s, e) => Cleanup();

        // Watchdog: periodically verify taskbar state matches our intent
        _watchdogTimer = new Timer(_ =>
        {
            try
            {
                bool shouldBeHidden = File.Exists(_lockFilePath);
                if (!shouldBeHidden && _taskbarService.IsHidden)
                {
                    _taskbarService.Restore();
                }
            }
            catch { }
        }, null, 5000, 5000);
    }

    /// <summary>
    /// Call when taskbar is intentionally hidden
    /// </summary>
    public void MarkHidden()
    {
        try { File.WriteAllText(_lockFilePath, DateTime.Now.ToString("o")); } catch { }
    }

    /// <summary>
    /// Call when taskbar is intentionally restored
    /// </summary>
    public void MarkRestored()
    {
        try { if (File.Exists(_lockFilePath)) File.Delete(_lockFilePath); } catch { }
    }

    private void Cleanup()
    {
        try
        {
            _taskbarService.Restore();
            if (File.Exists(_lockFilePath)) File.Delete(_lockFilePath);
        }
        catch { }
    }

    public void Dispose()
    {
        _watchdogTimer?.Dispose();
        _watchdogTimer = null;
        Cleanup();
    }
}
