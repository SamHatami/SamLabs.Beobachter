using System;
using Avalonia.Media;
using Avalonia.Threading;
using SamLabs.Beobachter.Core.Settings;
using AvaloniaApp = Avalonia.Application;

namespace SamLabs.Beobachter.Application.Services;

public sealed class LogLevelColorResourceService
{
    public void ApplyOverrides(LogLevelColorOverrides overrides)
    {
        ArgumentNullException.ThrowIfNull(overrides);

        if (AvaloniaApp.Current is not { } app)
        {
            return;
        }

        void Apply()
        {
            TrySetBrush(app, "RowTraceBrush", overrides.Trace.Row);
            TrySetBrush(app, "RowDebugBrush", overrides.Debug.Row);
            TrySetBrush(app, "RowInfoBrush", overrides.Info.Row);
            TrySetBrush(app, "RowWarnBrush", overrides.Warn.Row);
            TrySetBrush(app, "RowErrorBrush", overrides.Error.Row);
            TrySetBrush(app, "RowFatalBrush", overrides.Fatal.Row);

            TrySetBrush(app, "BadgeTraceBrush", overrides.Trace.Badge);
            TrySetBrush(app, "BadgeDebugBrush", overrides.Debug.Badge);
            TrySetBrush(app, "BadgeInfoBrush", overrides.Info.Badge);
            TrySetBrush(app, "BadgeWarnBrush", overrides.Warn.Badge);
            TrySetBrush(app, "BadgeErrorBrush", overrides.Error.Badge);
            TrySetBrush(app, "BadgeFatalBrush", overrides.Fatal.Badge);

            TrySetBrush(app, "MessageTraceBrush", overrides.Trace.Message);
            TrySetBrush(app, "MessageDebugBrush", overrides.Debug.Message);
            TrySetBrush(app, "MessageInfoBrush", overrides.Info.Message);
            TrySetBrush(app, "MessageWarnBrush", overrides.Warn.Message);
            TrySetBrush(app, "MessageErrorBrush", overrides.Error.Message);
            TrySetBrush(app, "MessageFatalBrush", overrides.Fatal.Message);
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            Apply();
            return;
        }

        Dispatcher.UIThread.Post(Apply);
    }

    private static void TrySetBrush(AvaloniaApp app, string resourceKey, string? colorText)
    {
        if (string.IsNullOrWhiteSpace(colorText))
        {
            return;
        }

        if (!Color.TryParse(colorText.Trim(), out var parsedColor))
        {
            return;
        }

        app.Resources[resourceKey] = new SolidColorBrush(parsedColor);
    }
}
