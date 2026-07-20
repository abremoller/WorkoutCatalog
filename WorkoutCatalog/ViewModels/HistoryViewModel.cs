using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutCatalog.Models;
using WorkoutCatalog.Services;

namespace WorkoutCatalog.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly ExerciseService _exerciseService;
    private readonly Action<int> _navigateToDetail;

    [ObservableProperty]
    private ObservableCollection<ViewHistory> _recentHistory = [];

    [ObservableProperty]
    private bool _isLoading;

    public HistoryViewModel(ExerciseService exerciseService, Action<int> navigateToDetail)
    {
        _exerciseService = exerciseService;
        _navigateToDetail = navigateToDetail;
    }

    [RelayCommand]
    private async Task LoadHistory()
    {
        IsLoading = true;
        try
        {
            var results = await _exerciseService.GetRecentHistoryAsync(50);
            RecentHistory = new ObservableCollection<ViewHistory>(results);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ViewDetail(ViewHistory? entry)
    {
        if (entry != null)
            _navigateToDetail(entry.ExerciseId);
    }
}
