using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutCatalog.Models;
using WorkoutCatalog.Services;

namespace WorkoutCatalog.ViewModels;

public partial class PlaylistViewModel : ObservableObject
{
    private readonly PlaylistService _playlistService;
    private readonly ExerciseService _exerciseService;
    private readonly SettingsService _settingsService;
    private readonly Action<int> _navigateToDetail;

    [ObservableProperty]
    private ObservableCollection<Playlist> _playlists = [];

    [ObservableProperty]
    private Playlist? _selectedPlaylist;

    [ObservableProperty]
    private ObservableCollection<PlaylistExercise> _playlistExercises = [];

    [ObservableProperty]
    private string _newPlaylistName = string.Empty;

    [ObservableProperty]
    private string _newPlaylistDescription = string.Empty;

    [ObservableProperty]
    private bool _isCreating;

    [ObservableProperty]
    private bool _isLoading;

    public PlaylistViewModel(
        PlaylistService playlistService,
        ExerciseService exerciseService,
        SettingsService settingsService,
        Action<int> navigateToDetail)
    {
        _playlistService = playlistService;
        _exerciseService = exerciseService;
        _settingsService = settingsService;
        _navigateToDetail = navigateToDetail;
    }

    [RelayCommand]
    private async Task LoadPlaylists()
    {
        IsLoading = true;
        try
        {
            var results = await _playlistService.GetAllAsync();
            Playlists = new ObservableCollection<Playlist>(results);
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedPlaylistChanged(Playlist? value)
    {
        if (value != null)
            LoadPlaylistExercisesCommand.Execute(value.Id);
    }

    [RelayCommand]
    private async Task LoadPlaylistExercises(int playlistId)
    {
        var playlist = await _playlistService.GetByIdAsync(playlistId);
        if (playlist == null) return;

        var sorted = playlist.PlaylistExercises
            .OrderBy(pe => pe.SortOrder)
            .ToList();
        PlaylistExercises = new ObservableCollection<PlaylistExercise>(sorted);
    }

    [RelayCommand]
    private void ToggleCreate()
    {
        IsCreating = !IsCreating;
        if (!IsCreating)
        {
            NewPlaylistName = string.Empty;
            NewPlaylistDescription = string.Empty;
        }
    }

    [RelayCommand]
    private async Task CreatePlaylist()
    {
        if (string.IsNullOrWhiteSpace(NewPlaylistName)) return;

        await _playlistService.CreateAsync(NewPlaylistName.Trim(), NewPlaylistDescription.Trim());
        NewPlaylistName = string.Empty;
        NewPlaylistDescription = string.Empty;
        IsCreating = false;
        await LoadPlaylists();
    }

    [RelayCommand]
    private async Task DeletePlaylist(int playlistId)
    {
        await _playlistService.DeleteAsync(playlistId);
        SelectedPlaylist = null;
        PlaylistExercises = [];
        await LoadPlaylists();
    }

    [RelayCommand]
    private async Task RemoveExercise(PlaylistExercise? entry)
    {
        if (entry == null || SelectedPlaylist == null) return;

        await _playlistService.RemoveExerciseAsync(SelectedPlaylist.Id, entry.ExerciseId);
        await LoadPlaylistExercises(SelectedPlaylist.Id);
    }

    [RelayCommand]
    private void ViewExerciseDetail(PlaylistExercise? entry)
    {
        if (entry != null)
            _navigateToDetail(entry.ExerciseId);
    }
}
