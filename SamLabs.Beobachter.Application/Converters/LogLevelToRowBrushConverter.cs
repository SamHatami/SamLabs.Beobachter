using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SamLabs.Beobachter.Core.Enums;
using AvaloniaApp = Avalonia.Application;

namespace SamLabs.Beobachter.Application.Converters;

public sealed class LogLevelToRowBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value switch
        {
            LogLevel.Trace => "RowTraceBrush",
            LogLevel.Debug => "RowDebugBrush",
            LogLevel.Info => "RowInfoBrush",
            LogLevel.Warn => "RowWarnBrush",
            LogLevel.Error => "RowErrorBrush",
            LogLevel.Fatal => "RowFatalBrush",
            _ => "RowFallbackBrush"
        };

        var app = AvaloniaApp.Current;
        if (app?.Resources.TryGetResource(key, app.ActualThemeVariant, out var resource) == true &&
            resource is IBrush brush)
        {
            return brush;
        }

        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
