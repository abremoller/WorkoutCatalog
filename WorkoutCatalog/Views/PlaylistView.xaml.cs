using System.Windows.Controls;
using System.Windows.Input;
using WorkoutCatalog.Models;
using WorkoutCatalog.ViewModels;

namespace WorkoutCatalog.Views;

public partial class PlaylistView : UserControl
{
    public PlaylistView()
    {
        InitializeComponent();
    }

    private void OnExerciseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListView lv && lv.SelectedItem is PlaylistExercise entry &&
            DataContext is PlaylistViewModel vm)
        {
            vm.ViewExerciseDetailCommand.Execute(entry);
        }
    }
}
