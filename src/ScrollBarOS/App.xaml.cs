using Microsoft.UI.Xaml;

namespace ScrollBarOS;

public partial class App : Application
{
    private static Mutex? _mutex;
    private const string MutexName = "ScrollBarOS_SingleInstance_Mutex";

    public static Window? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        System.Diagnostics.Debug.WriteLine($"Unhandled exception: {e.Exception}");
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Single instance check
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            // Another instance is already running, exit
            _mutex.Dispose();
            _mutex = null;
            System.Diagnostics.Process.GetCurrentProcess().Kill();
            return;
        }

        MainWindow = new MainWindow();
        MainWindow.Closed += (s, e) =>
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            _mutex = null;
        };
        MainWindow.Activate();
    }
}
