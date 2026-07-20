using System.Windows;
using WorkoutCatalog.Helpers;
using WorkoutCatalog.Services;
using WorkoutCatalog.ViewModels;

namespace WorkoutCatalog.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowHelper.ApplyDarkMode(this);
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var mainVm = DataContext as MainViewModel;
        if (mainVm == null) return;

        // Access the SettingsService from the App
        var settingsService = (Application.Current as App)?.SettingsService;
        if (settingsService == null) return;

        var dialog = new SettingsDialog(settingsService)
        {
            Owner = this
        };
        dialog.ShowDialog();
    }
}
