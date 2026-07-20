namespace ScrollBarOS.Models;

/// <summary>
/// Hardware monitoring information
/// </summary>
public class HardwareInfo
{
    /// <summary>CPU usage percentage (0-100)</summary>
    public float CpuUsage { get; set; }

    /// <summary>Memory usage percentage (0-100)</summary>
    public float MemoryUsage { get; set; }

    /// <summary>Disk usage percentage (0-100)</summary>
    public float DiskUsage { get; set; }

    /// <summary>Network upload speed in KB/s</summary>
    public float NetworkUploadKBps { get; set; }

    /// <summary>Network download speed in KB/s</summary>
    public float NetworkDownloadKBps { get; set; }

    /// <summary>Timestamp of last update</summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}
