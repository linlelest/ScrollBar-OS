using System.Text.Json;
using ScrollBarOS.Models;

namespace ScrollBarOS.Services;

/// <summary>
/// Configuration service with JSON persistence and debounced saving
/// </summary>
public class ConfigService
{
    private static readonly Lazy<ConfigService> _instance = new(() => new ConfigService());
    public static ConfigService Instance => _instance.Value;

    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private Timer? _saveTimer;
    private readonly object _lock = new();

    public AppConfig Config { get; private set; }

    public event EventHandler? ConfigChanged;

    private ConfigService()
    {
        // Store config in AppData
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ScrollBarOS");
        Directory.CreateDirectory(appDataPath);
        _configPath = Path.Combine(appDataPath, "config.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        Config = Load();
    }

    /// <summary>
    /// Loads configuration from disk, or creates default if not exists
    /// </summary>
    private AppConfig Load()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);
                if (config != null)
                {
                    return config;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
        }

        return new AppConfig();
    }

    /// <summary>
    /// Saves configuration immediately
    /// </summary>
    public void Save()
    {
        lock (_lock)
        {
            try
            {
                var json = JsonSerializer.Serialize(Config, _jsonOptions);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Debounced save - waits 500ms after last change before writing to disk
    /// </summary>
    public void SaveDebounced()
    {
        _saveTimer?.Dispose();
        _saveTimer = new Timer(_ =>
        {
            Save();
        }, null, 500, Timeout.Infinite);
    }

    /// <summary>
    /// Updates configuration and notifies listeners
    /// </summary>
    public void Update(Action<AppConfig> updateAction)
    {
        updateAction(Config);
        SaveDebounced();
        ConfigChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Reloads configuration from disk
    /// </summary>
    public void Reload()
    {
        Config = Load();
        ConfigChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the configuration file path
    /// </summary>
    public string ConfigFilePath => _configPath;
}
