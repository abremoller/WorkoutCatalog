using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using VideoAudioMerger.Services;

namespace VideoAudioMerger.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly FFmpegService _ffmpegService;
    private readonly NameTrackingService _nameTrackingService;
    private readonly FileAutoPopulationService _autoPopulationService;

    [ObservableProperty]
    private string _projectName = string.Empty;

    [ObservableProperty]
    private bool _isNameDuplicate;

    [ObservableProperty]
    private string _folderPath = string.Empty;

    [ObservableProperty]
    private string _fullVideoPath = string.Empty;

    [ObservableProperty]
    private string _fullAudioPath = string.Empty;

    [ObservableProperty]
    private string _previewVideoPath = string.Empty;

    [ObservableProperty]
    private string _previewAudioPath = string.Empty;

    [ObservableProperty]
    private string _outputFolder = string.Empty;

    [ObservableProperty]
    private string _infoText = string.Empty;

    [ObservableProperty]
    private BitmapSource? _infoImageSource;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isProcessing;

    public MainViewModel(FFmpegService ffmpegService, NameTrackingService nameTrackingService, FileAutoPopulationService autoPopulationService)
    {
        _ffmpegService = ffmpegService;
        _nameTrackingService = nameTrackingService;
        _autoPopulationService = autoPopulationService;
    }

    partial void OnProjectNameChanged(string value)
    {
        IsNameDuplicate = _nameTrackingService.IsNameProcessed(value);

        // Auto-suggest output folder only if it's currently empty (remove everything from '(' onwards)
        if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(OutputFolder))
        {
            var bracketIndex = value.IndexOf('(');
            OutputFolder = bracketIndex > 0 ? value.Substring(0, bracketIndex).Trim() : value.Trim();
        }

        ProcessCommand.NotifyCanExecuteChanged();
    }

    partial void OnFullVideoPathChanged(string value)
    {
        ProcessCommand.NotifyCanExecuteChanged();
    }

    partial void OnFullAudioPathChanged(string value)
    {
        ProcessCommand.NotifyCanExecuteChanged();
    }

    partial void OnPreviewVideoPathChanged(string value)
    {
        ProcessCommand.NotifyCanExecuteChanged();
    }

    partial void OnPreviewAudioPathChanged(string value)
    {
        ProcessCommand.NotifyCanExecuteChanged();
    }

    partial void OnOutputFolderChanged(string value)
    {
        ProcessCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsProcessingChanged(bool value)
    {
        ProcessCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task BrowseFolder()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Folder with MP4 Files",
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "Folder Selection"
        };

        if (dialog.ShowDialog() == true)
        {
            FolderPath = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
            await AutoPopulateFiles();
        }
    }

    [RelayCommand]
    private async Task AutoPopulateFiles()
    {
        if (string.IsNullOrWhiteSpace(FolderPath))
        {
            StatusMessage = "Please select a folder first";
            return;
        }

        IsProcessing = true;
        StatusMessage = "Auto-populating files...";

        try
        {
            var progress = new Progress<string>(msg => StatusMessage = msg);
            var result = await _autoPopulationService.AutoPopulateFromFolder(FolderPath, progress);

            if (result != null)
            {
                FullVideoPath = result.FullVideoPath;
                FullAudioPath = result.FullAudioPath;
                PreviewVideoPath = result.PreviewVideoPath;
                PreviewAudioPath = result.PreviewAudioPath;

                // Auto-populate project name if detected
                if (!string.IsNullOrWhiteSpace(result.ProjectName))
                {
                    ProjectName = result.ProjectName;
                }

                // Load info.txt if available
                if (!string.IsNullOrWhiteSpace(result.InfoText))
                {
                    InfoText = result.InfoText;
                }

                // Load info.png if available
                if (!string.IsNullOrWhiteSpace(result.InfoImagePath) && File.Exists(result.InfoImagePath))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(result.InfoImagePath);
                        bitmap.EndInit();
                        InfoImageSource = bitmap;
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Warning: Could not load info image - {ex.Message}";
                    }
                }

                StatusMessage = "Files auto-populated successfully!";
            }
            else
            {
                StatusMessage = "Could not auto-populate files. Please select manually.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void BrowseFullVideo()
    {
        FullVideoPath = BrowseForFile("Select Full Video File");
    }

    [RelayCommand]
    private void BrowseFullAudio()
    {
        FullAudioPath = BrowseForFile("Select Full Audio File");
    }

    [RelayCommand]
    private void BrowsePreviewVideo()
    {
        PreviewVideoPath = BrowseForFile("Select Preview Video File");
    }

    [RelayCommand]
    private void BrowsePreviewAudio()
    {
        PreviewAudioPath = BrowseForFile("Select Preview Audio File");
    }

    [RelayCommand]
    private void BrowseOutputFolder()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Select Output Folder",
            CheckPathExists = false,
            FileName = "Folder Selection"
        };

        if (dialog.ShowDialog() == true)
        {
            OutputFolder = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
        }
    }

    [RelayCommand]
    private void PasteImage()
    {
        try
        {
            if (Clipboard.ContainsImage())
            {
                InfoImageSource = Clipboard.GetImage();
                StatusMessage = "Image pasted from clipboard";
            }
            else
            {
                StatusMessage = "No image found in clipboard";
                MessageBox.Show("No image found in clipboard. Please copy an image first.", 
                    "No Image", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error pasting image: {ex.Message}";
            MessageBox.Show($"Error pasting image:\n{ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string BrowseForFile(string title)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            Filter = "MP4 Files (*.mp4)|*.mp4|All Files (*.*)|*.*"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanProcess))]
    private async Task Process()
    {
        if (IsNameDuplicate)
        {
            var result = MessageBox.Show(
                $"The name '{ProjectName}' has already been processed. Continue anyway?",
                "Duplicate Name",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;
        }

        IsProcessing = true;
        StatusMessage = "Processing...";

        try
        {
            var outputPath = Path.Combine(OutputFolder, ProjectName);
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            var progress = new Progress<string>(msg => StatusMessage = msg);

            // Process Full video
            var fullOutputPath = Path.Combine(outputPath, "Full.mp4");
            var fullResult = await _ffmpegService.MergeVideoAudio(FullVideoPath, FullAudioPath, fullOutputPath, progress);

            if (!fullResult.Success)
            {
                StatusMessage = $"Full video failed: {fullResult.Message}";
                MessageBox.Show($"Error processing Full video:\n{fullResult.ErrorDetails}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Process Preview video
            var previewOutputPath = Path.Combine(outputPath, "Preview.mp4");
            var previewResult = await _ffmpegService.MergeVideoAudio(PreviewVideoPath, PreviewAudioPath, previewOutputPath, progress);

            if (!previewResult.Success)
            {
                StatusMessage = $"Preview video failed: {previewResult.Message}";
                MessageBox.Show($"Error processing Preview video:\n{previewResult.ErrorDetails}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Save Info.txt if provided
            if (!string.IsNullOrWhiteSpace(InfoText))
            {
                var infoTextPath = Path.Combine(outputPath, "Info.txt");
                await File.WriteAllTextAsync(infoTextPath, InfoText);
                StatusMessage = "Saved Info.txt";
            }

            // Save Info.png if provided
            if (InfoImageSource != null)
            {
                var infoPngPath = Path.Combine(outputPath, "Info.png");
                SaveImageToPng(InfoImageSource, infoPngPath);
                StatusMessage = "Saved Info.png";
            }

            // Add name to tracking
            _nameTrackingService.AddName(ProjectName);

            StatusMessage = "Processing completed successfully!";

            // Verify output files exist
            bool fullExists = File.Exists(fullOutputPath);
            bool previewExists = File.Exists(previewOutputPath);

            if (!fullExists || !previewExists)
            {
                var missingFiles = new System.Text.StringBuilder("Warning: Some output files were not created:\n");
                if (!fullExists) missingFiles.AppendLine($"- {fullOutputPath}");
                if (!previewExists) missingFiles.AppendLine($"- {previewOutputPath}");

                MessageBox.Show(missingFiles.ToString(), "Output Verification", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"Videos processed successfully!\n\nOutput:\n{fullOutputPath}\n{previewOutputPath}",
                "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            // Ask if user wants to delete source files
            var deleteResult = MessageBox.Show(
                "Output files verified successfully!\n\nWould you like to delete the 4 source files?\n\n" +
                $"- {Path.GetFileName(FullVideoPath)}\n" +
                $"- {Path.GetFileName(FullAudioPath)}\n" +
                $"- {Path.GetFileName(PreviewVideoPath)}\n" +
                $"- {Path.GetFileName(PreviewAudioPath)}",
                "Delete Source Files?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (deleteResult == MessageBoxResult.Yes)
            {
                try
                {
                    File.Delete(FullVideoPath);
                    File.Delete(FullAudioPath);
                    File.Delete(PreviewVideoPath);
                    File.Delete(PreviewAudioPath);

                    StatusMessage = "Source files deleted successfully!";
                    MessageBox.Show("Source files have been deleted.", "Files Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception deleteEx)
                {
                    MessageBox.Show($"Error deleting source files:\n{deleteEx.Message}", "Deletion Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Clear fields for next operation
            ClearFields();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show($"An error occurred:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private bool CanProcess()
    {
        return !IsProcessing
            && !string.IsNullOrWhiteSpace(ProjectName)
            && !string.IsNullOrWhiteSpace(FullVideoPath)
            && !string.IsNullOrWhiteSpace(FullAudioPath)
            && !string.IsNullOrWhiteSpace(PreviewVideoPath)
            && !string.IsNullOrWhiteSpace(PreviewAudioPath)
            && !string.IsNullOrWhiteSpace(OutputFolder);
    }

    private void ClearFields()
    {
        ProjectName = string.Empty;
        FolderPath = string.Empty;
        FullVideoPath = string.Empty;
        FullAudioPath = string.Empty;
        PreviewVideoPath = string.Empty;
        PreviewAudioPath = string.Empty;
        InfoText = string.Empty;
        InfoImageSource = null;
    }

    private void SaveImageToPng(BitmapSource image, string filePath)
    {
        using var fileStream = new FileStream(filePath, FileMode.Create);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        encoder.Save(fileStream);
    }

    public void HandleFileDrop(string propertyName, string filePath)
    {
        if (!File.Exists(filePath) || !filePath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "Please drop a valid MP4 file";
            return;
        }

        switch (propertyName)
        {
            case nameof(FullVideoPath):
                FullVideoPath = filePath;
                break;
            case nameof(FullAudioPath):
                FullAudioPath = filePath;
                break;
            case nameof(PreviewVideoPath):
                PreviewVideoPath = filePath;
                break;
            case nameof(PreviewAudioPath):
                PreviewAudioPath = filePath;
                break;
        }

        StatusMessage = $"File loaded: {Path.GetFileName(filePath)}";
    }
}
