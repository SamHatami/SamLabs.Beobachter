using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SamLabs.Beobachter.Core.Enums;
using AvaloniaApp = Avalonia.Application;

namespace SamLabs.Beobachter.Application.Converters;

public sealed class LogLevelToBadgeBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value switch
        {
            LogLevel.Trace => "BadgeTraceBrush",
            LogLevel.Debug => "BadgeDebugBrush",
            LogLevel.Info => "BadgeInfoBrush",
            LogLevel.Warn => "BadgeWarnBrush",
            LogLevel.Error => "BadgeErrorBrush",
            LogLevel.Fatal => "BadgeFatalBrush",
            _ => "BadgeFallbackBrush"
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
