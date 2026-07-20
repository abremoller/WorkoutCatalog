using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WorkoutCatalog.Models;
using WorkoutCatalog.Services;

namespace WorkoutCatalog.ViewModels;

public partial class ExerciseDetailViewModel : ObservableObject
{
    private readonly ExerciseService _exerciseService;
    private readonly PlaylistService? _playlistService;
    private readonly SettingsService _settingsService;
    private readonly VlcLaunchService _vlcLaunchService;
    private readonly ThumbnailService _thumbnailService;
    private readonly FolderScanService _folderScanService;
    private readonly int _exerciseId;
    private readonly Action _navigateBack;

    [ObservableProperty]
    private Exercise? _exercise;

    [ObservableProperty]
    private ObservableCollection<ExerciseComment> _comments = [];

    [ObservableProperty]
    private string _newCommentText = string.Empty;

    [ObservableProperty]
    private int _currentRating;

    [ObservableProperty]
    private ExerciseType _currentType;

    [ObservableProperty]
    private ExerciseLevel _currentLevel;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _showImage;

    [ObservableProperty]
    private string? _imagePath;

    [ObservableProperty]
    private BitmapImage? _thumbnailBitmap;

    [ObservableProperty]
    private int _captureTimeSeconds = 30;

    [ObservableProperty]
    private bool _isRescanningFolder;

    [ObservableProperty]
    private string _rescanStatusMessage = string.Empty;

    // Playlist properties
    [ObservableProperty]
    private ObservableCollection<Playlist> _exercisePlaylists = [];

    [ObservableProperty]
    private ObservableCollection<Playlist> _allPlaylists = [];

    [ObservableProperty]
    private Playlist? _selectedPlaylistToAdd;

    private readonly string? _autoPlay;

    public ExerciseDetailViewModel(
        ExerciseService exerciseService,
        SettingsService settingsService,
        VlcLaunchService vlcLaunchService,
        ThumbnailService thumbnailService,
        FolderScanService folderScanService,
        int exerciseId,
        Action navigateBack,
        PlaylistService? playlistService = null,
        string? autoPlay = null)
    {
        _exerciseService = exerciseService;
        _settingsService = settingsService;
        _vlcLaunchService = vlcLaunchService;
        _thumbnailService = thumbnailService;
        _folderScanService = folderScanService;
        _exerciseId = exerciseId;
        _navigateBack = navigateBack;
        _playlistService = playlistService;
        _autoPlay = autoPlay;
    }

    [RelayCommand]
    private async Task LoadExercise()
    {
        try
        {
            var exercise = await _exerciseService.GetByIdAsync(_exerciseId);
            if (exercise == null) return;

            Exercise = exercise;
            CurrentRating = exercise.Rating ?? 0;
            CurrentType = exercise.Type;
            CurrentLevel = exercise.Level;
            Comments = new ObservableCollection<ExerciseComment>(exercise.Comments);

            // Resolve image path
            if (exercise.HasImage && exercise.ImageFileName != null)
            {
                var rootPath = _settingsService.VideoRootFolder;
                var fullPath = Path.Combine(rootPath, exercise.FolderRelativePath, exercise.ImageFileName);
                if (File.Exists(fullPath))
                    ImagePath = fullPath;
            }

            // Resolve thumbnail
            if (exercise.ThumbnailFileName != null)
            {
                var rootPath = _settingsService.VideoRootFolder;
                var thumbPath = Path.Combine(rootPath, exercise.FolderRelativePath, exercise.ThumbnailFileName);
                ThumbnailBitmap = LoadBitmap(thumbPath);
            }

            // Load playlists this exercise belongs to
            await LoadPlaylistsAsync();

            if (_autoPlay != null && Enum.TryParse<VideoKind>(_autoPlay, out var autoPlayKind))
            {
                var path = GetVideoPath(autoPlayKind);
                if (path != null)
                {
                    await _exerciseService.RecordViewAsync(_exerciseId, autoPlayKind);
                    _vlcLaunchService.Play(path);
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"LoadExercise failed for exerciseId={_exerciseId}", ex);
            RescanStatusMessage = "Failed to load exercise details. See app log.";
        }
    }

    private async Task LoadPlaylistsAsync()
    {
        if (_playlistService == null) return;

        var exercisePlaylists = await _playlistService.GetPlaylistsForExerciseAsync(_exerciseId);
        ExercisePlaylists = new ObservableCollection<Playlist>(exercisePlaylists);

        var allPlaylists = await _playlistService.GetAllAsync();
        var exercisePlaylistIds = exercisePlaylists.Select(p => p.Id).ToHashSet();
        var available = allPlaylists.Where(p => !exercisePlaylistIds.Contains(p.Id)).ToList();
        AllPlaylists = new ObservableCollection<Playlist>(available);
    }

    [RelayCommand]
    private async Task AddToPlaylist()
    {
        if (_playlistService == null || SelectedPlaylistToAdd == null) return;

        await _playlistService.AddExerciseAsync(SelectedPlaylistToAdd.Id, _exerciseId);
        SelectedPlaylistToAdd = null;
        await LoadPlaylistsAsync();
    }

    [RelayCommand]
    private async Task RemoveFromPlaylist(int playlistId)
    {
        if (_playlistService == null) return;

        await _playlistService.RemoveExerciseAsync(playlistId, _exerciseId);
        await LoadPlaylistsAsync();
    }

    [RelayCommand]
    private async Task SetRating(string ratingStr)
    {
        if (!int.TryParse(ratingStr, out var rating)) return;

        if (CurrentRating == rating)
        {
            CurrentRating = 0;
            await _exerciseService.UpdateRatingAsync(_exerciseId, null);
        }
        else
        {
            CurrentRating = rating;
            await _exerciseService.UpdateRatingAsync(_exerciseId, rating);
        }
    }

    [RelayCommand]
    private void Edit() => IsEditing = true;

    [RelayCommand]
    private async Task SaveMetadata()
    {
        await _exerciseService.UpdateMetadataAsync(_exerciseId, CurrentType, CurrentLevel);
        IsEditing = false;
    }

    [RelayCommand(CanExecute = nameof(CanRescanFolder))]
    private async Task RescanFolder()
    {
        if (Exercise == null)
            return;

        var rootPath = _settingsService.VideoRootFolder;
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            RescanStatusMessage = "Video root is not configured.";
            return;
        }

        var folderPath = Path.Combine(rootPath, Exercise.FolderRelativePath);
        if (!Directory.Exists(folderPath))
        {
            RescanStatusMessage = "Exercise folder no longer exists.";
            return;
        }

        IsRescanningFolder = true;
        RescanStatusMessage = "Rescanning folder...";

        try
        {
            var success = await _folderScanService.RescanSingleFolderAsync(rootPath, folderPath);
            if (!success)
            {
                RescanStatusMessage = "Rescan failed for this folder.";
                return;
            }

            await LoadExercise();
            RescanStatusMessage = "Folder rescanned.";
        }
        catch (Exception ex)
        {
            RescanStatusMessage = $"Rescan error: {ex.Message}";
        }
        finally
        {
            IsRescanningFolder = false;
        }
    }

    private bool CanRescanFolder() => !IsRescanningFolder;

    partial void OnIsRescanningFolderChanged(bool value)
    {
        RescanFolderCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task AddComment()
    {
        if (string.IsNullOrWhiteSpace(NewCommentText)) return;

        await _exerciseService.AddCommentAsync(_exerciseId, NewCommentText.Trim());
        NewCommentText = string.Empty;

        await LoadExercise();
    }

    [RelayCommand]
    private async Task DeleteComment(int commentId)
    {
        await _exerciseService.DeleteCommentAsync(commentId);
        await LoadExercise();
    }

    [RelayCommand]
    private async Task PlayPreview()
    {
        var path = GetVideoPath(VideoKind.Preview);
        if (path != null)
        {
            await _exerciseService.RecordViewAsync(_exerciseId, VideoKind.Preview);
            _vlcLaunchService.Play(path);
        }
    }

    [RelayCommand]
    private async Task PlayFull()
    {
        var path = GetVideoPath(VideoKind.Full);
        if (path != null)
        {
            await _exerciseService.RecordViewAsync(_exerciseId, VideoKind.Full);
            _vlcLaunchService.Play(path);
        }
    }

    [RelayCommand]
    private async Task SetThumbnailFromFile()
    {
        if (Exercise == null) return;

        var dialog = new OpenFileDialog
        {
            Title = "Select Thumbnail Image",
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files|*.*"
        };

        if (dialog.ShowDialog() != true) return;

        var rootPath = _settingsService.VideoRootFolder;
        var exerciseFolder = Path.Combine(rootPath, Exercise.FolderRelativePath);
        var destPath = Path.Combine(exerciseFolder, "thumb.jpg");

        File.Copy(dialog.FileName, destPath, overwrite: true);
        await _exerciseService.UpdateThumbnailAsync(_exerciseId, "thumb.jpg");
        ThumbnailBitmap = LoadBitmap(destPath);
    }

    [RelayCommand]
    private async Task CaptureThumbnail()
    {
        if (Exercise == null) return;

        var videoPath = GetVideoPath(VideoKind.Full) ?? GetVideoPath(VideoKind.Preview);
        if (videoPath == null) return;

        var rootPath = _settingsService.VideoRootFolder;
        var exerciseFolder = Path.Combine(rootPath, Exercise.FolderRelativePath);

        var fileName = await _thumbnailService.CaptureFrameAtAsync(videoPath, exerciseFolder, CaptureTimeSeconds);
        if (fileName == null) return;

        await _exerciseService.UpdateThumbnailAsync(_exerciseId, fileName);
        ThumbnailBitmap = LoadBitmap(Path.Combine(exerciseFolder, fileName));
    }

    [RelayCommand]
    private async Task StepCaptureTime(string? deltaSeconds)
    {
        if (!int.TryParse(deltaSeconds, out var delta))
            return;

        CaptureTimeSeconds = Math.Max(0, CaptureTimeSeconds + delta);
        await CaptureThumbnail();
    }

    private static BitmapImage? LoadBitmap(string? path)
    {
        if (path == null || !File.Exists(path)) return null;

        var bi = new BitmapImage();
        bi.BeginInit();
        bi.UriSource = new Uri(path);
        bi.CacheOption = BitmapCacheOption.OnLoad;
        bi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        bi.EndInit();
        bi.Freeze();
        return bi;
    }

    [RelayCommand]
    private void GoBack() => _navigateBack();

    [RelayCommand]
    private void ToggleImage()
    {
        ShowImage = !ShowImage;
    }

    public string? GetVideoPath(VideoKind kind)
    {
        if (Exercise == null) return null;

        var fileName = kind == VideoKind.Preview
            ? Exercise.PreviewVideoFileName
            : Exercise.FullVideoFileName;

        if (fileName == null) return null;

        var rootPath = _settingsService.VideoRootFolder;
        var fullPath = Path.Combine(rootPath, Exercise.FolderRelativePath, fileName);
        return File.Exists(fullPath) ? fullPath : null;
    }
}
