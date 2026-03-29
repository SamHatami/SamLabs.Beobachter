using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SamLabs.Beobachter.Core.Enums;
using AvaloniaApp = Avalonia.Application;

namespace SamLabs.Beobachter.Application.Converters;

public sealed class LogLevelToMessageBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value switch
        {
            LogLevel.Trace => "MessageTraceBrush",
            LogLevel.Debug => "MessageDebugBrush",
            LogLevel.Info => "MessageInfoBrush",
            LogLevel.Warn => "MessageWarnBrush",
            LogLevel.Error => "MessageErrorBrush",
            LogLevel.Fatal => "MessageFatalBrush",
            _ => "MessageFallbackBrush"
        };

        var app = AvaloniaApp.Current;
        if (app?.Resources.TryGetResource(key, app.ActualThemeVariant, out var resource) == true &&
            resource is IBrush brush)
        {
            return brush;
        }

        return Brushes.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
