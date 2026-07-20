using ScrollBarOS.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ScrollBarOS.Services;

/// <summary>
/// Service for monitoring hardware metrics (CPU, Memory, Disk, Network)
/// </summary>
public class HardwareMonitorService
{
    private Timer? _monitorTimer;
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _networkUploadCounter;
    private PerformanceCounter? _networkDownloadCounter;
    private readonly object _lock = new();

    public HardwareInfo CurrentInfo { get; private set; } = new();
    public event EventHandler<HardwareInfo>? InfoUpdated;

    private const int MONITOR_INTERVAL_MS = 2000;

    public HardwareMonitorService()
    {
        InitializeCounters();
    }

    private void InitializeCounters()
    {
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue(); // First call always returns 0

            _networkUploadCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", GetNetworkInstance());
            _networkDownloadCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", GetNetworkInstance());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize performance counters: {ex.Message}");
        }
    }

    private string GetNetworkInstance()
    {
        try
        {
            var category = new PerformanceCounterCategory("Network Interface");
            var instances = category.GetInstanceNames();
            // Return the first non-loopback interface
            foreach (var instance in instances)
            {
                if (!instance.Contains("Loopback", StringComparison.OrdinalIgnoreCase) &&
                    !instance.Contains("isatap", StringComparison.OrdinalIgnoreCase))
                {
                    return instance;
                }
            }
            return instances.Length > 0 ? instances[0] : "_Total";
        }
        catch
        {
            return "_Total";
        }
    }

    /// <summary>
    /// Starts monitoring hardware metrics
    /// </summary>
    public void Start()
    {
        _monitorTimer?.Dispose();
        _monitorTimer = new Timer(CollectMetrics, null, 0, MONITOR_INTERVAL_MS);
    }

    /// <summary>
    /// Stops monitoring
    /// </summary>
    public void Stop()
    {
        _monitorTimer?.Dispose();
        _monitorTimer = null;
    }

    /// <summary>
    /// Collects all hardware metrics
    /// </summary>
    private void CollectMetrics(object? state)
    {
        try
        {
            var info = new HardwareInfo
            {
                LastUpdated = DateTime.Now
            };

            // CPU Usage
            info.CpuUsage = GetCpuUsage();

            // Memory Usage
            info.MemoryUsage = GetMemoryUsage();

            // Disk Usage
            info.DiskUsage = GetDiskUsage();

            // Network
            info.NetworkUploadKBps = GetNetworkUpload();
            info.NetworkDownloadKBps = GetNetworkDownload();

            lock (_lock)
            {
                CurrentInfo = info;
            }

            InfoUpdated?.Invoke(this, info);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error collecting metrics: {ex.Message}");
        }
    }

    private float GetCpuUsage()
    {
        try
        {
            return _cpuCounter?.NextValue() ?? 0f;
        }
        catch
        {
            return 0f;
        }
    }

    private float GetMemoryUsage()
    {
        try
        {
            var status = new MEMORYSTATUSEX();
            status.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();

            if (GlobalMemoryStatusEx(ref status))
            {
                long totalBytes = (long)status.ullTotalPhys;
                long usedBytes = totalBytes - (long)status.ullAvailPhys;
                return (float)usedBytes / totalBytes * 100;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting memory info: {ex.Message}");
        }

        return 0f;
    }

    private float GetDiskUsage()
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\");
            if (drive.IsReady)
            {
                return (float)((drive.TotalSize - drive.TotalFreeSpace) / (double)drive.TotalSize * 100);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting disk info: {ex.Message}");
        }

        return 0f;
    }

    private float GetNetworkUpload()
    {
        try
        {
            return (_networkUploadCounter?.NextValue() ?? 0f) / 1024f;
        }
        catch
        {
            return 0f;
        }
    }

    private float GetNetworkDownload()
    {
        try
        {
            return (_networkDownloadCounter?.NextValue() ?? 0f) / 1024f;
        }
        catch
        {
            return 0f;
        }
    }

    #region Native Methods

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    #endregion

    public void Dispose()
    {
        Stop();
        _cpuCounter?.Dispose();
        _networkUploadCounter?.Dispose();
        _networkDownloadCounter?.Dispose();
    }
}
