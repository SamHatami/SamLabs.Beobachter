using Avalonia.Styling;
using Avalonia.Threading;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Services;
using AvaloniaApp = Avalonia.Application;

namespace SamLabs.Beobachter.Application.Services;

public sealed class ThemeService : IThemeService
{
    public AppThemeMode CurrentMode { get; private set; } = AppThemeMode.System;

    public void SetTheme(AppThemeMode mode)
    {
        CurrentMode = mode;

        if (AvaloniaApp.Current is not { } app)
        {
            return;
        }

        ThemeVariant variant = mode switch
        {
            AppThemeMode.Light => ThemeVariant.Light,
            AppThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

        if (Dispatcher.UIThread.CheckAccess())
        {
            app.RequestedThemeVariant = variant;
            return;
        }

        Dispatcher.UIThread.Post(() => app.RequestedThemeVariant = variant);
    }
}
