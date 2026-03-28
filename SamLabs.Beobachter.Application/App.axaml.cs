using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SamLabs.Beobachter.Application.Composition;
using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.ViewModels;
using SamLabs.Beobachter.Views;

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
            var themeService = _services.GetRequiredService<IThemeService>();
            themeService.SetTheme(AppThemeMode.System);

            desktop.MainWindow = new MainWindow
            {
                DataContext = _services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
