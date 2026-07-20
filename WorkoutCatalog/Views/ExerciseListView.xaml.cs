using System.Windows.Controls;
using System.Windows.Input;
using WorkoutCatalog.Models;
using WorkoutCatalog.ViewModels;

namespace WorkoutCatalog.Views;

public partial class ExerciseListView : UserControl
{
    public ExerciseListView()
    {
        InitializeComponent();
    }

    private void OnListDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ExerciseListViewModel vm && vm.SelectedExercise is Exercise exercise)
        {
            vm.OnExerciseDoubleClick(exercise);
        }
    }
}
