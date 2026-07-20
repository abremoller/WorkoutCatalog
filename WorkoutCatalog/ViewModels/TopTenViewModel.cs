using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutCatalog.Models;
using WorkoutCatalog.Services;

namespace WorkoutCatalog.ViewModels;

public partial class TopTenViewModel : ObservableObject
{
    private readonly ExerciseService _exerciseService;
    private readonly Action<int> _navigateToDetail;

    [ObservableProperty]
    private ObservableCollection<Exercise> _topExercises = [];

    [ObservableProperty]
    private bool _isLoading;

    public TopTenViewModel(ExerciseService exerciseService, Action<int> navigateToDetail)
    {
        _exerciseService = exerciseService;
        _navigateToDetail = navigateToDetail;
    }

    [RelayCommand]
    private async Task LoadTopTen()
    {
        IsLoading = true;
        try
        {
            var results = await _exerciseService.GetTopRatedAsync(10);
            TopExercises = new ObservableCollection<Exercise>(results);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ViewDetail(Exercise? exercise)
    {
        if (exercise != null)
            _navigateToDetail(exercise.Id);
    }
}
