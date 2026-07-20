using System.Globalization;
using System.Windows.Data;

namespace WorkoutCatalog.Converters;

public class RatingToStarsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int rating && rating >= 1 && rating <= 10)
        {
            return new string('\u2605', rating) + new string('\u2606', 10 - rating);
        }

        return new string('\u2606', 10);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
