using System.Globalization;
using System.Windows.Data;

namespace WorkoutCatalog.Converters;

public class DurationToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.TotalHours >= 1
                ? ts.ToString(@"h\:mm\:ss")
                : ts.ToString(@"m\:ss");
        }

        if (value is double?)
        {
            var nullable = (double?)value;
            if (nullable.HasValue)
            {
                var ts = TimeSpan.FromSeconds(nullable.Value);
                return ts.TotalHours >= 1
                    ? ts.ToString(@"h\:mm\:ss")
                    : ts.ToString(@"m\:ss");
            }
        }

        return "--:--";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
