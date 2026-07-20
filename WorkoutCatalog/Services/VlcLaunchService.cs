using System.Diagnostics;
using System.Windows;

namespace WorkoutCatalog.Services;

public class VlcLaunchService
{
    private readonly SettingsService _settingsService;

    public VlcLaunchService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public bool IsConfigured()
    {
        var path = _settingsService.VlcPath;
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
    }

    public void Play(string videoPath)
    {
        var vlcExe = _settingsService.VlcPath;
        if (!EnsureVlcExists(vlcExe)) return;

        Process.Start(new ProcessStartInfo
        {
            FileName = vlcExe,
            Arguments = $"\"{videoPath}\"",
            UseShellExecute = false
        });
    }

    public void PlayPlaylist(IEnumerable<string> videoPaths)
    {
        var vlcExe = _settingsService.VlcPath;
        if (!EnsureVlcExists(vlcExe)) return;

        var paths = videoPaths.ToList();
        if (paths.Count == 0) return;

        if (paths.Count == 1)
        {
            Play(paths[0]);
            return;
        }

        var tempFile = Path.Combine(Path.GetTempPath(), $"workout_{Guid.NewGuid():N}.m3u");
        File.WriteAllLines(tempFile, paths);

        Process.Start(new ProcessStartInfo
        {
            FileName = vlcExe,
            Arguments = $"\"{tempFile}\"",
            UseShellExecute = false
        });
    }

    private static bool EnsureVlcExists(string vlcExe)
    {
        if (!string.IsNullOrWhiteSpace(vlcExe) && File.Exists(vlcExe))
            return true;

        MessageBox.Show(
            "VLC not found. Please configure the VLC path in Settings.",
            "VLC Not Configured",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return false;
    }
}
