using System.Text.Json;

namespace WorkoutCatalog.Services;

public class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WorkoutCatalog",
        "settings.json");

    private Dictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    public string VideoRootFolder
    {
        get => GetSetting("VideoRootFolder");
        set => SaveSetting("VideoRootFolder", value);
    }

    public string FFmpegPath
    {
        get => GetSetting("FFmpegPath");
        set => SaveSetting("FFmpegPath", value);
    }

    public string DatabasePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WorkoutCatalog",
        "catalog.db");

    public bool ValidateVideoRoot()
    {
        var root = VideoRootFolder;
        return !string.IsNullOrWhiteSpace(root) && Directory.Exists(root);
    }

    public string VlcPath
    {
        get
        {
            var configured = GetSetting("VlcPath");
            return string.IsNullOrWhiteSpace(configured) ? AutoDetectVlc() : configured;
        }
        set => SaveSetting("VlcPath", value);
    }

    public bool ValidateFFmpegPath()
    {
        var path = FFmpegPath;
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
    }

    private string GetSetting(string key)
    {
        EnsureLoaded();
        return _cache.TryGetValue(key, out var value) ? value : string.Empty;
    }

    private void SaveSetting(string key, string value)
    {
        EnsureLoaded();
        _cache[key] = value;

        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Could not save settings: {ex.Message}");
        }
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                         ?? new(StringComparer.OrdinalIgnoreCase);
            }
        }
        catch
        {
            _cache = new(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string AutoDetectVlc()
    {
        var candidates = new[]
        {
            @"C:\Program Files\VideoLAN\VLC\vlc.exe",
            @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe"
        };
        return candidates.FirstOrDefault(File.Exists) ?? string.Empty;
    }
}
