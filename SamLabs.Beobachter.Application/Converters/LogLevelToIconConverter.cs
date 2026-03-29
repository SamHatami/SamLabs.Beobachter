using System;
using System.Globalization;
using Avalonia.Data.Converters;
using SamLabs.Beobachter.Core.Enums;

namespace SamLabs.Beobachter.Application.Converters;

public sealed class LogLevelToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            LogLevel.Trace => "fa-solid fa-wave-square",
            LogLevel.Debug => "fa-solid fa-bug",
            LogLevel.Info => "fa-solid fa-circle-info",
            LogLevel.Warn => "fa-solid fa-triangle-exclamation",
            LogLevel.Error => "fa-solid fa-circle-xmark",
            LogLevel.Fatal => "fa-solid fa-skull-crossbones",
            _ => "fa-solid fa-circle"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
