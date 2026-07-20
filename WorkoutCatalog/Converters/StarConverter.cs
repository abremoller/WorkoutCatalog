using System.Globalization;
using System.Windows.Data;

namespace WorkoutCatalog.Converters;

// Converts (CurrentRating, ConverterParameter=starNumber) → "★" if rating >= star, else "☆"
public class StarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int rating = value is int r ? r : 0;
        int star = parameter is string s && int.TryParse(s, out int n) ? n : 0;
        return rating >= star ? "\u2605" : "\u2606";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
