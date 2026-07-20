using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WorkoutCatalog.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter is string s && s == "Invert";
        bool isNull = value == null;

        if (value is string str)
            isNull = string.IsNullOrWhiteSpace(str);

        if (invert)
            return isNull ? Visibility.Visible : Visibility.Collapsed;

        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
