using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SamLabs.Beobachter.Application.Composition;
using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Services;
using MainWindow = SamLabs.Beobachter.Application.Views.MainWindow;
using MainWindowViewModel = SamLabs.Beobachter.Application.ViewModels.MainWindowViewModel;

namespace SamLabs.Beobachter.Application;

public partial class App : Avalonia.Application
{
    private IServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _services = Root.Build();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settingsStore = _services.GetRequiredService<ISettingsStore>();
            var appSettings = settingsStore.LoadAppSettingsAsync().GetAwaiter().GetResult();

            var themeService = _services.GetRequiredService<IThemeService>();
            themeService.SetTheme(ParseThemeMode(appSettings.ThemeMode));

            var logLevelColorResourceService = _services.GetRequiredService<LogLevelColorResourceService>();
            logLevelColorResourceService.ApplyOverrides(appSettings.LogLevelColors);

            var ingestionSession = _services.GetRequiredService<IIngestionSession>();
            ingestionSession.StartAsync().GetAwaiter().GetResult();

            desktop.MainWindow = new MainWindow
            {
                DataContext = _services.GetRequiredService<MainWindowViewModel>(),
            };

            desktop.Exit += (_, _) =>
            {
                ingestionSession.StopAsync().GetAwaiter().GetResult();
                ingestionSession.DisposeAsync().GetAwaiter().GetResult();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static AppThemeMode ParseThemeMode(string? themeMode)
    {
        if (Enum.TryParse<AppThemeMode>(themeMode, true, out var parsed))
        {
            return parsed;
        }

        return AppThemeMode.System;
    }
}
