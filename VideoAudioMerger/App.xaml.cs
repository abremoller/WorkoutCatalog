using System.Configuration;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using VideoAudioMerger.Services;
using VideoAudioMerger.ViewModels;
using VideoAudioMerger.Views;

namespace VideoAudioMerger;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Set shutdown mode first thing to prevent auto-shutdown
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            // Get or configure FFmpeg path
            var ffmpegPath = ConfigurationManager.AppSettings["FFmpegPath"];
            
            if (string.IsNullOrWhiteSpace(ffmpegPath) || !File.Exists(ffmpegPath))
            {
                var configDialog = new FFmpegConfigDialog();
                if (configDialog.ShowDialog() != true)
                {
                    MessageBox.Show("FFmpeg path is required to run this application.", 
                        "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                    return;
                }

                ffmpegPath = configDialog.FFmpegPath!;
                SaveFFmpegPath(ffmpegPath);
            }

            // Validate FFmpeg
            var ffmpegService = new FFmpegService(ffmpegPath);
            var isValid = Task.Run(async () => await ffmpegService.ValidateFFmpegPath()).Result;
            if (!isValid)
            {
                MessageBox.Show("The specified FFmpeg path is invalid. Please reconfigure.", 
                    "Invalid FFmpeg", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // Initialize services
            var trackingFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VideoAudioMerger",
                "processed_names.txt");
            
            var nameTrackingService = new NameTrackingService(trackingFilePath);
            var autoPopulationService = new FileAutoPopulationService(ffmpegService);

            // Create ViewModel
            var viewModel = new MainViewModel(ffmpegService, nameTrackingService, autoPopulationService);

            // Load default paths from config
            var defaultSourceFolder = ConfigurationManager.AppSettings["DefaultSourceFolder"];
            var defaultOutputFolder = ConfigurationManager.AppSettings["DefaultOutputFolder"];
            
            if (!string.IsNullOrWhiteSpace(defaultSourceFolder))
            {
                viewModel.FolderPath = defaultSourceFolder;
            }
            
            if (!string.IsNullOrWhiteSpace(defaultOutputFolder))
            {
                viewModel.OutputFolder = defaultOutputFolder;
            }

            // Create and show main window
            var mainWindow = new MainWindow
            {
                DataContext = viewModel
            };
            
            // Set as the main application window and switch to normal shutdown mode
            this.MainWindow = mainWindow;
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;
            
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup Error:\n{ex.Message}\n\nStack:\n{ex.StackTrace}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void SaveFFmpegPath(string path)
    {
        try
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            
            if (config.AppSettings.Settings["FFmpegPath"] == null)
            {
                config.AppSettings.Settings.Add("FFmpegPath", path);
            }
            else
            {
                config.AppSettings.Settings["FFmpegPath"].Value = path;
            }
            
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not save configuration: {ex.Message}", 
                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}


