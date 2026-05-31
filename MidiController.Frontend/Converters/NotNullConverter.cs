using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MidiControllerFrontend.Converters;

/// <summary>null → false; nicht-null → true</summary>
[ValueConversion(typeof(object), typeof(bool))]
public sealed class NotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        => value is not null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>null → Collapsed; nicht-null → Visible</summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public sealed class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        => value is not null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
