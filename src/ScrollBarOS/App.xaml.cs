using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;

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
        // Log the exception for debugging
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
            Exit();
            return;
        }

        MainWindow = new MainWindow();
        MainWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs args)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(args);
    }
}
