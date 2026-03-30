using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SamLabs.Beobachter.Application.Composition;
using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Core.Interfaces;
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
            var settingsService = _services.GetRequiredService<ISettingsService>();
            var releaseNotesProvider = _services.GetRequiredService<IReleaseNotesProvider>();
            settingsService.InitializeAsync().GetAwaiter().GetResult();

            var ingestionSession = _services.GetRequiredService<IIngestionSession>();
            ingestionSession.StartAsync().GetAwaiter().GetResult();

            desktop.MainWindow = new MainWindow
            {
                DataContext = _services.GetRequiredService<MainWindowViewModel>(),
                SettingsService = settingsService,
                ReleaseNotesProvider = releaseNotesProvider
            };

            desktop.Exit += (_, _) =>
            {
                ingestionSession.StopAsync().GetAwaiter().GetResult();
                ingestionSession.DisposeAsync().GetAwaiter().GetResult();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
