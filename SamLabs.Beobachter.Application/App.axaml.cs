using System;
using System.Diagnostics;
using System.Threading;
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
    private int _shutdownStarted;

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

            desktop.Exit += OnDesktopExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        ShutdownServices();
    }

    private void ShutdownServices()
    {
        if (Interlocked.Exchange(ref _shutdownStarted, 1) != 0)
        {
            return;
        }

        IServiceProvider? services = _services;
        _services = null;
        if (services is null)
        {
            return;
        }

        try
        {
            IIngestionSession? ingestionSession = services.GetService<IIngestionSession>();
            ingestionSession?.StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Shutdown stop failed: {ex}");
        }

        try
        {
            switch (services)
            {
                case IAsyncDisposable asyncDisposable:
                    asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Service provider dispose failed: {ex}");
        }
    }
}
