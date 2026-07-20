using System.Windows;
using System.Windows.Controls;
using VideoAudioMerger.ViewModels;

namespace VideoAudioMerger;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Set up drag and drop for file textboxes
        SetupDragDrop(FullVideoTextBox, nameof(MainViewModel.FullVideoPath));
        SetupDragDrop(FullAudioTextBox, nameof(MainViewModel.FullAudioPath));
        SetupDragDrop(PreviewVideoTextBox, nameof(MainViewModel.PreviewVideoPath));
        SetupDragDrop(PreviewAudioTextBox, nameof(MainViewModel.PreviewAudioPath));
    }

    private void SetupDragDrop(TextBox textBox, string propertyName)
    {
        textBox.PreviewDragOver += (s, e) =>
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) 
                ? DragDropEffects.Copy 
                : DragDropEffects.None;
            e.Handled = true;
        };

        textBox.PreviewDrop += (s, e) =>
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files?.Length > 0 && DataContext is MainViewModel viewModel)
                {
                    viewModel.HandleFileDrop(propertyName, files[0]);
                }
            }
            e.Handled = true;
        };
    }
}