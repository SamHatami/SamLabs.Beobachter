using System;
using Microsoft.Extensions.DependencyInjection;
using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.ViewModels;

namespace SamLabs.Beobachter.Application.Composition;

public static class Root
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        return services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<MainWindowViewModel>();
    }
}
