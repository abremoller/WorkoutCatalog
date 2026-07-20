using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WorkoutCatalog.ViewModels;

namespace WorkoutCatalog.Views;

public partial class ExerciseDetailView : UserControl
{
    public ExerciseDetailView()
    {
        InitializeComponent();
    }

    private void InfoImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount < 2)
            return;

        if (DataContext is not ExerciseDetailViewModel vm || string.IsNullOrWhiteSpace(vm.ImagePath))
            return;

        if (!File.Exists(vm.ImagePath))
            return;

        var source = new BitmapImage();
        source.BeginInit();
        source.CacheOption = BitmapCacheOption.OnLoad;
        source.UriSource = new Uri(vm.ImagePath);
        source.EndInit();
        source.Freeze();

        var image = new Image
        {
            Source = source,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var root = new Grid
        {
            Background = Brushes.Black
        };

        root.Children.Add(image);

        var hint = new TextBlock
        {
            Text = "Press Esc to close",
            Foreground = Brushes.White,
            Margin = new Thickness(16),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Opacity = 0.75
        };
        root.Children.Add(hint);

        var fullscreenWindow = new Window
        {
            Title = "Info Image",
            Content = root,
            WindowStyle = WindowStyle.None,
            WindowState = WindowState.Maximized,
            ResizeMode = ResizeMode.NoResize,
            Background = Brushes.Black,
            Topmost = true
        };

        fullscreenWindow.KeyDown += (_, args) =>
        {
            if (args.Key == Key.Escape)
                fullscreenWindow.Close();
        };

        root.MouseLeftButtonDown += (_, args) =>
        {
            if (args.ClickCount >= 2)
                fullscreenWindow.Close();
        };

        fullscreenWindow.ShowDialog();
        e.Handled = true;
    }
}
