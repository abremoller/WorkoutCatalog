using System.Windows.Controls;
using System.Windows.Input;
using WorkoutCatalog.Models;
using WorkoutCatalog.ViewModels;

namespace WorkoutCatalog.Views;

public partial class TopTenView : UserControl
{
    public TopTenView()
    {
        InitializeComponent();
    }

    private void OnListDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListView lv && lv.SelectedItem is Exercise exercise &&
            DataContext is TopTenViewModel vm)
        {
            vm.ViewDetailCommand.Execute(exercise);
        }
    }
}
