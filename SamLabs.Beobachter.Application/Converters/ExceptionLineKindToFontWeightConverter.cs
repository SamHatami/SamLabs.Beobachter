using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SamLabs.Beobachter.Application.ViewModels;

namespace SamLabs.Beobachter.Application.Converters;

public sealed class ExceptionLineKindToFontWeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is ExceptionLineKind.Header ? FontWeight.SemiBold : FontWeight.Normal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
