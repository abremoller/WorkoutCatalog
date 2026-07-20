using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WorkoutCatalog.Models;

namespace WorkoutCatalog.Converters;

public class UnknownLevelToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ExerciseLevel level)
            return level == ExerciseLevel.Unknown ? Visibility.Visible : Visibility.Collapsed;

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
