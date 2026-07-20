using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutCatalog.Services;

namespace WorkoutCatalog.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ExerciseService _exerciseService;
    private readonly FolderScanService _folderScanService;
    private readonly SettingsService _settingsService;
    private readonly PlaylistService _playlistService;
    private readonly VlcLaunchService _vlcLaunchService;
    private readonly ThumbnailService _thumbnailService;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _activeNav = "Browse";

    private ExerciseListViewModel? _exerciseListViewModel;
    private ExerciseDetailViewModel? _exerciseDetailViewModel;
    private TopTenViewModel? _topTenViewModel;
    private HistoryViewModel? _historyViewModel;
    private PlaylistViewModel? _playlistViewModel;

    public MainViewModel(
        ExerciseService exerciseService,
        FolderScanService folderScanService,
        SettingsService settingsService,
        PlaylistService playlistService,
        VlcLaunchService vlcLaunchService,
        ThumbnailService thumbnailService)
    {
        _exerciseService = exerciseService;
        _folderScanService = folderScanService;
        _settingsService = settingsService;
        _playlistService = playlistService;
        _vlcLaunchService = vlcLaunchService;
        _thumbnailService = thumbnailService;
    }

    public async Task InitialLoadAsync()
    {
        _exerciseListViewModel = new ExerciseListViewModel(_exerciseService, _settingsService,
            (id, autoPlay) => NavigateToDetail(id, autoPlay));
        _topTenViewModel = new TopTenViewModel(_exerciseService, id => NavigateToDetail(id));
        _historyViewModel = new HistoryViewModel(_exerciseService, id => NavigateToDetail(id));
        _playlistViewModel = new PlaylistViewModel(_playlistService, _exerciseService, _settingsService, id => NavigateToDetail(id));

        NavigateToList();

        // Auto-scan if database is empty
        var exercises = await _exerciseService.GetAllAsync();
        if (exercises.Count == 0 && _settingsService.ValidateVideoRoot())
        {
            await RescanFolders();
        }
    }

    [RelayCommand]
    private void NavigateToList()
    {
        ActiveNav = "Browse";
        CurrentView = _exerciseListViewModel;
        _exerciseListViewModel?.LoadExercisesCommand.Execute(null);
    }

    [RelayCommand]
    private void NavigateToTopTen()
    {
        ActiveNav = "TopTen";
        CurrentView = _topTenViewModel;
        _topTenViewModel?.LoadTopTenCommand.Execute(null);
    }

    [RelayCommand]
    private void NavigateToHistory()
    {
        ActiveNav = "History";
        CurrentView = _historyViewModel;
        _historyViewModel?.LoadHistoryCommand.Execute(null);
    }

    [RelayCommand]
    private void NavigateToPlaylists()
    {
        ActiveNav = "Playlists";
        CurrentView = _playlistViewModel;
        _playlistViewModel?.LoadPlaylistsCommand.Execute(null);
    }

    private void NavigateToDetail(int exerciseId, string? autoPlay = null)
    {
        try
        {
            _exerciseDetailViewModel = new ExerciseDetailViewModel(
                _exerciseService, _settingsService, _vlcLaunchService, _thumbnailService, _folderScanService, exerciseId, NavigateBackToList, _playlistService, autoPlay);
            CurrentView = _exerciseDetailViewModel;
            _exerciseDetailViewModel.LoadExerciseCommand.Execute(null);
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"NavigateToDetail failed for exerciseId={exerciseId}", ex);
            StatusMessage = "Failed to open exercise details. See app log.";
            NavigateToList();
        }
    }

    private void NavigateBackToList()
    {
        NavigateToList();
    }

    [RelayCommand(CanExecute = nameof(CanRescan))]
    private async Task RescanFolders()
    {
        if (!_settingsService.ValidateVideoRoot())
        {
            StatusMessage = "Video root folder not configured or does not exist.";
            return;
        }

        IsScanning = true;
        StatusMessage = "Scanning folders...";

        try
        {
            var progress = new Progress<string>(msg => StatusMessage = msg);
            var count = await _folderScanService.ScanFoldersAsync(
                _settingsService.VideoRootFolder, progress);
            StatusMessage = $"Scan complete. {count} exercises found.";

            // Refresh the current view
            if (CurrentView == _exerciseListViewModel)
                _exerciseListViewModel?.LoadExercisesCommand.Execute(null);
            else if (CurrentView == _topTenViewModel)
                _topTenViewModel?.LoadTopTenCommand.Execute(null);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    private bool CanRescan() => !IsScanning;

    partial void OnIsScanningChanged(bool value)
    {
        RescanFoldersCommand.NotifyCanExecuteChanged();
    }
}
