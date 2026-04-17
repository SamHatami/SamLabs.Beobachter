using System;
using System.Globalization;
using System.Text.Json;
using Avalonia.Data.Converters;
using Avalonia.Media;
using AvaloniaApp = Avalonia.Application;

namespace SamLabs.Beobachter.Application.Converters;

public sealed class JsonValueKindToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string key = value switch
        {
            JsonValueKind.String => "JsonStringBrush",
            JsonValueKind.Number => "JsonNumberBrush",
            JsonValueKind.True or JsonValueKind.False => "JsonBooleanBrush",
            JsonValueKind.Null => "JsonNullBrush",
            JsonValueKind.Object or JsonValueKind.Array => "JsonPunctuationBrush",
            _ => "ShellMutedForegroundBrush"
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
