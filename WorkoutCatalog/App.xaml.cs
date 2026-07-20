using System.Windows;
using Microsoft.EntityFrameworkCore;
using WorkoutCatalog.Data;
using WorkoutCatalog.Services;
using WorkoutCatalog.ViewModels;
using WorkoutCatalog.Views;

namespace WorkoutCatalog;

public partial class App : Application
{
    public SettingsService? SettingsService { get; private set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        RegisterGlobalExceptionHandlers();
        AppLogger.LogInfo("Application startup.");
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            // 1. Load settings
            SettingsService = new SettingsService();

            // 2. If video root not configured, show settings dialog
            if (!SettingsService.ValidateVideoRoot())
            {
                var settingsDialog = new SettingsDialog(SettingsService);
                if (settingsDialog.ShowDialog() != true)
                {
                    MessageBox.Show("Video root folder is required to run this application.",
                        "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                    return;
                }
            }

            // 3. Initialize database (auto-migrate)
            using (var db = new CatalogDbContext())
            {
                await db.Database.EnsureCreatedAsync();
                await db.EnsurePlaylistSchemaAsync();
            }

            // 4. Create services
            var dbContext = new CatalogDbContext();
            var probeService = new FFmpegProbeService(SettingsService.FFmpegPath);
            var thumbnailService = new ThumbnailService(SettingsService.FFmpegPath);
            var exerciseService = new ExerciseService(dbContext);
            var folderScanService = new FolderScanService(dbContext, probeService, thumbnailService);
            var playlistService = new PlaylistService(dbContext);
            var vlcLaunchService = new VlcLaunchService(SettingsService);

            // 5. Create main ViewModel
            var mainViewModel = new MainViewModel(
                exerciseService, folderScanService, SettingsService, playlistService, vlcLaunchService, thumbnailService);

            // 6. Show main window
            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
            MainWindow = mainWindow;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();

            // 7. Initial load
            await mainViewModel.InitialLoadAsync();
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Unhandled startup exception", ex);
            MessageBox.Show($"Startup Error:\n{ex.Message}\n\n{ex.StackTrace}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += (_, args) =>
        {
            AppLogger.LogError("DispatcherUnhandledException", args.Exception);
            args.Handled = true;
            MessageBox.Show(
                $"Unexpected error. Details were written to:\n{AppLogger.LogPath}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception
                     ?? new Exception($"Non-exception object: {args.ExceptionObject}");
            AppLogger.LogError("AppDomain.UnhandledException", ex);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            AppLogger.LogError("TaskScheduler.UnobservedTaskException", args.Exception);
            args.SetObserved();
        };
    }
}
