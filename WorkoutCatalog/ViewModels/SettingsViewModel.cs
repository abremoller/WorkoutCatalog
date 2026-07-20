using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutCatalog.Services;

namespace WorkoutCatalog.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private string _videoRootFolder = string.Empty;

    [ObservableProperty]
    private string _ffmpegPath = string.Empty;

    [ObservableProperty]
    private string _vlcPath = string.Empty;

    [ObservableProperty]
    private string _databasePath = string.Empty;

    public bool Confirmed { get; private set; }

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        VideoRootFolder = settingsService.VideoRootFolder;
        FfmpegPath = settingsService.FFmpegPath;
        VlcPath = settingsService.VlcPath;
        DatabasePath = settingsService.DatabasePath;
    }

    [RelayCommand]
    private void BrowseVideoRoot()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Video Root Folder",
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "Folder Selection"
        };

        if (dialog.ShowDialog() == true)
        {
            VideoRootFolder = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
        }
    }

    [RelayCommand]
    private void BrowseFFmpeg()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select FFmpeg Executable",
            Filter = "FFmpeg (ffmpeg.exe)|ffmpeg.exe|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
            FfmpegPath = dialog.FileName;
    }

    [RelayCommand]
    private void BrowseVlc()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select VLC Executable",
            Filter = "VLC (vlc.exe)|vlc.exe|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
            VlcPath = dialog.FileName;
    }

    public bool Save()
    {
        if (string.IsNullOrWhiteSpace(VideoRootFolder) || !Directory.Exists(VideoRootFolder))
            return false;

        _settingsService.VideoRootFolder = VideoRootFolder;

        if (!string.IsNullOrWhiteSpace(FfmpegPath))
            _settingsService.FFmpegPath = FfmpegPath;

        if (!string.IsNullOrWhiteSpace(VlcPath))
            _settingsService.VlcPath = VlcPath;

        Confirmed = true;
        return true;
    }
}
