using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutCatalog.Models;
using WorkoutCatalog.Services;

namespace WorkoutCatalog.ViewModels;

public partial class ExerciseListViewModel : ObservableObject
{
    private readonly ExerciseService _exerciseService;
    private readonly SettingsService _settingsService;
    private readonly Action<int, string?> _navigateToDetail;

    [ObservableProperty]
    private ObservableCollection<Exercise> _exercises = [];

    [ObservableProperty]
    private Exercise? _selectedExercise;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ExerciseType? _filterType;

    [ObservableProperty]
    private ExerciseLevel? _filterLevel;

    [ObservableProperty]
    private int _minRating;

    [ObservableProperty]
    private int _minDurationMinutes;

    [ObservableProperty]
    private int _maxDurationMinutes;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _exerciseCount;

    [ObservableProperty]
    private bool _isIconView;

    public string VideoRootFolder => _settingsService.VideoRootFolder;

    public ExerciseListViewModel(
        ExerciseService exerciseService,
        SettingsService settingsService,
        Action<int, string?> navigateToDetail)
    {
        _exerciseService = exerciseService;
        _settingsService = settingsService;
        _navigateToDetail = navigateToDetail;
    }

    [RelayCommand]
    private async Task LoadExercises()
    {
        IsLoading = true;
        try
        {
            var results = await _exerciseService.SearchAsync(
                keyword: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                type: FilterType,
                level: FilterLevel,
                minDurationSeconds: MinDurationMinutes > 0 ? (double?)(MinDurationMinutes * 60) : null,
                maxDurationSeconds: MaxDurationMinutes > 0 ? (double?)(MaxDurationMinutes * 60) : null,
                minRating: MinRating > 0 ? (int?)MinRating : null);

            Exercises = new ObservableCollection<Exercise>(results);
            ExerciseCount = results.Count;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ApplyFilters()
    {
        await LoadExercises();
    }

    [RelayCommand]
    private async Task ClearFilters()
    {
        SearchText = string.Empty;
        FilterType = null;
        FilterLevel = null;
        MinRating = 0;
        MinDurationMinutes = 0;
        MaxDurationMinutes = 0;
        await LoadExercises();
    }

    [RelayCommand]
    private void SetListView() => IsIconView = false;

    [RelayCommand]
    private void SetIconView() => IsIconView = true;

    [RelayCommand]
    private void OpenExercise(Exercise exercise) => _navigateToDetail(exercise.Id, null);

    [RelayCommand]
    private void ViewDetail()
    {
        if (SelectedExercise != null)
            _navigateToDetail(SelectedExercise.Id, null);
    }

    [RelayCommand]
    private void PlayFull(Exercise exercise) => _navigateToDetail(exercise.Id, nameof(VideoKind.Full));

    [RelayCommand]
    private void PlayPreview(Exercise exercise)
    {
        if (exercise.HasPreviewVideo)
            _navigateToDetail(exercise.Id, nameof(VideoKind.Preview));
    }

    public void OnExerciseDoubleClick(Exercise exercise)
    {
        _navigateToDetail(exercise.Id, null);
    }
}
