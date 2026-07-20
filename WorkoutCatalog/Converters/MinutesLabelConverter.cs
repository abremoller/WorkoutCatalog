using System.Globalization;
using System.Windows.Data;

namespace WorkoutCatalog.Converters;

// Converts int minutes → "Any" if 0, else "N min"
public class MinutesLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int minutes && minutes > 0)
            return $"{minutes} min";
        return "Any";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
