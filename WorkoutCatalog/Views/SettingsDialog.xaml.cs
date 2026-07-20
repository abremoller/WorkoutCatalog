using System.Windows;
using WorkoutCatalog.Helpers;
using WorkoutCatalog.Services;
using WorkoutCatalog.ViewModels;

namespace WorkoutCatalog.Views;

public partial class SettingsDialog : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsDialog(SettingsService settingsService)
    {
        InitializeComponent();
        _viewModel = new SettingsViewModel(settingsService);
        DataContext = _viewModel;
        SourceInitialized += (_, _) => WindowHelper.ApplyDarkMode(this);
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Save())
        {
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("Video root folder is required and must exist.",
                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
