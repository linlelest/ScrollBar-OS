using System.Runtime.CompilerServices;

namespace ScrollBarOS;

/// <summary>
/// Module initializer - runs before ANY other code in the assembly.
/// Used to diagnose startup crashes at the native level.
/// </summary>
internal static class StartupDiagnostics
{
    [ModuleInitializer]
    internal static void Init()
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScrollBarOS");
            Directory.CreateDirectory(logDir);
            File.AppendAllText(
                Path.Combine(logDir, "crash.log"),
                $"[{DateTime.Now:HH:mm:ss.fff}] Module initializer reached\n");
        }
        catch { }
    }
}
