using Avalonia;
using Avalonia.Styling;
using AvaloniaApp = Avalonia.Application;

namespace SamLabs.Beobachter.Application.Services;

public sealed class ThemeService : IThemeService
{
    public AppThemeMode CurrentMode { get; private set; } = AppThemeMode.System;

    public void SetTheme(AppThemeMode mode)
    {
        CurrentMode = mode;

        if (AvaloniaApp.Current is null)
        {
            return;
        }

        AvaloniaApp.Current.RequestedThemeVariant = mode switch
        {
            AppThemeMode.Light => ThemeVariant.Light,
            AppThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
}
