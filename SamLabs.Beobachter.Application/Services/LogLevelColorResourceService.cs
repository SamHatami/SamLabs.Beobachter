using System;
using Avalonia.Media;
using SamLabs.Beobachter.Core.Settings;
using AvaloniaApp = Avalonia.Application;

namespace SamLabs.Beobachter.Application.Services;

public sealed class LogLevelColorResourceService
{
    public void ApplyOverrides(LogLevelColorOverrides overrides)
    {
        ArgumentNullException.ThrowIfNull(overrides);

        TrySetBrush("RowTraceBrush", overrides.Trace.Row);
        TrySetBrush("RowDebugBrush", overrides.Debug.Row);
        TrySetBrush("RowInfoBrush", overrides.Info.Row);
        TrySetBrush("RowWarnBrush", overrides.Warn.Row);
        TrySetBrush("RowErrorBrush", overrides.Error.Row);
        TrySetBrush("RowFatalBrush", overrides.Fatal.Row);

        TrySetBrush("BadgeTraceBrush", overrides.Trace.Badge);
        TrySetBrush("BadgeDebugBrush", overrides.Debug.Badge);
        TrySetBrush("BadgeInfoBrush", overrides.Info.Badge);
        TrySetBrush("BadgeWarnBrush", overrides.Warn.Badge);
        TrySetBrush("BadgeErrorBrush", overrides.Error.Badge);
        TrySetBrush("BadgeFatalBrush", overrides.Fatal.Badge);

        TrySetBrush("MessageTraceBrush", overrides.Trace.Message);
        TrySetBrush("MessageDebugBrush", overrides.Debug.Message);
        TrySetBrush("MessageInfoBrush", overrides.Info.Message);
        TrySetBrush("MessageWarnBrush", overrides.Warn.Message);
        TrySetBrush("MessageErrorBrush", overrides.Error.Message);
        TrySetBrush("MessageFatalBrush", overrides.Fatal.Message);
    }

    private static void TrySetBrush(string resourceKey, string? colorText)
    {
        if (string.IsNullOrWhiteSpace(colorText) || AvaloniaApp.Current is null)
        {
            return;
        }

        if (!Color.TryParse(colorText.Trim(), out var parsedColor))
        {
            return;
        }

        AvaloniaApp.Current.Resources[resourceKey] = new SolidColorBrush(parsedColor);
    }
}
