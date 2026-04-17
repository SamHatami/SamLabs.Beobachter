using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SamLabs.Beobachter.Application.ViewModels;
using AvaloniaApp = Avalonia.Application;

namespace SamLabs.Beobachter.Application.Converters;

public sealed class ExceptionLineKindToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string key = value switch
        {
            ExceptionLineKind.Header => "MessageErrorBrush",
            ExceptionLineKind.Frame => "ShellForegroundBrush",
            ExceptionLineKind.Separator => "ShellMutedForegroundBrush",
            _ => "ShellForegroundBrush"
        };

        var app = AvaloniaApp.Current;
        if (app?.Resources.TryGetResource(key, app.ActualThemeVariant, out var resource) == true &&
            resource is IBrush brush)
        {
            return brush;
        }

        return Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
