using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed class NullableColorToBrushConverter : IValueConverter
{
    public static readonly NullableColorToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return new SolidColorBrush(color);
        }

        return new SolidColorBrush(Color.Parse("#808080"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
