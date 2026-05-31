using System.Globalization;
using System.Windows.Data;

namespace MidiControllerFrontend.Converters;

/// <summary>Kehrt einen bool-Wert um: true → false, false → true.</summary>
[ValueConversion(typeof(bool), typeof(bool))]
public sealed class BoolNegateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;
}
