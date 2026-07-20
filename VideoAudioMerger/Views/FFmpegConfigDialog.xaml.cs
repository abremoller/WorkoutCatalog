using System.IO;
using System.Windows;

namespace VideoAudioMerger.Views;

public partial class FFmpegConfigDialog : Window
{
    public string? FFmpegPath { get; private set; }

    public FFmpegConfigDialog()
    {
        InitializeComponent();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select FFmpeg Executable",
            Filter = "FFmpeg Executable (ffmpeg.exe)|ffmpeg.exe|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            PathTextBox.Text = dialog.FileName;
        }
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        var path = PathTextBox.Text?.Trim();
        
        if (string.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show("Please specify a path to ffmpeg.exe", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!File.Exists(path))
        {
            MessageBox.Show("The specified file does not exist", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!path.EndsWith("ffmpeg.exe", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Please select ffmpeg.exe", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        FFmpegPath = path;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
