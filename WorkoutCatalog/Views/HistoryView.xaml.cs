using System.Windows.Controls;
using System.Windows.Input;
using WorkoutCatalog.Models;
using WorkoutCatalog.ViewModels;

namespace WorkoutCatalog.Views;

public partial class HistoryView : UserControl
{
    public HistoryView()
    {
        InitializeComponent();
    }

    private void OnListDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListView lv && lv.SelectedItem is ViewHistory entry &&
            DataContext is HistoryViewModel vm)
        {
            vm.ViewDetailCommand.Execute(entry);
        }
    }
}
