using Microsoft.UI.Xaml;

namespace ScrollBarOS;

public partial class App : Application
{
    private static Mutex? _mutex;
    private const string MutexName = "ScrollBarOS_SingleInstance_Mutex";

    public static Window? MainWindow { get; private set; }

    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScrollBarOS");
    private static readonly string LogPath = Path.Combine(LogDir, "crash.log");

    public App()
    {
        WriteLog("App constructor started");
        InitializeComponent();
        WriteLog("App InitializeComponent done");
        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        WriteLog($"UnhandledException: {e.Exception}");
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            WriteLog("App.OnLaunched started");

            // Single instance check
            _mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
            {
                _mutex.Dispose();
                _mutex = null;
                WriteLog("Another instance running, exiting");
                System.Diagnostics.Process.GetCurrentProcess().Kill();
                return;
            }

            WriteLog("Creating MainWindow...");
            MainWindow = new MainWindow();
            WriteLog("MainWindow created, activating...");
            MainWindow.Closed += (s, e) =>
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
                _mutex = null;
            };
            MainWindow.Activate();
            WriteLog("MainWindow activated successfully");
        }
        catch (Exception ex)
        {
            WriteLog($"FATAL in OnLaunched: {ex}");
            throw;
        }
    }

    public static void WriteLog(string message)
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
        }
        catch { }
    }
}
