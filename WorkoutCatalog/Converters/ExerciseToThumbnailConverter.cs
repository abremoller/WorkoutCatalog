using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace WorkoutCatalog.Converters;

/// <summary>
/// Converts exercise media fields to a BitmapImage.
/// Expects values: FolderRelativePath, ThumbnailFileName, ImageFileName, VideoRootFolder.
/// </summary>
public class ExerciseToThumbnailConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 4) return null;

        var folderRelativePath = values[0] as string;
        var thumbnailFileName = values[1] as string;
        var imageFileName = values[2] as string;
        var videoRoot = values[3] as string;

        if (string.IsNullOrEmpty(videoRoot) || string.IsNullOrEmpty(folderRelativePath))
            return null;

        // Try thumbnail first.
        if (!string.IsNullOrEmpty(thumbnailFileName))
        {
            var thumbPath = Path.Combine(videoRoot, folderRelativePath, thumbnailFileName);
            if (File.Exists(thumbPath))
                return LoadImage(thumbPath);
        }

        // Fall back to the folder image when no thumbnail exists.
        if (!string.IsNullOrEmpty(imageFileName))
        {
            var imagePath = Path.Combine(videoRoot, folderRelativePath, imageFileName);
            if (File.Exists(imagePath))
                return LoadImage(imagePath);
        }

        return null;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static BitmapImage? LoadImage(string path)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 640;
            bitmap.UriSource = new Uri(path);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
