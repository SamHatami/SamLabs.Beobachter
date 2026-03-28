using System;
using Microsoft.Extensions.DependencyInjection;
using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Services;
using SamLabs.Beobachter.Infrastructure.Parsing;
using SamLabs.Beobachter.Infrastructure.Receivers;
using SamLabs.Beobachter.Infrastructure.Settings;
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
        services.AddSingleton<IClipboardService, AvaloniaClipboardService>();
        services.AddSingleton<ILogStore, InMemoryLogStore>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();

        services.AddSingleton<ILogParser, Log4jXmlParser>();
        services.AddSingleton<ILogParser, CsvParser>();
        services.AddSingleton<ILogParser, PlainTextParser>();

        services.AddSingleton<ParserPipelineFactory>();
        services.AddSingleton<ReceiverFactory>();

        services.AddSingleton<IIngestionSession, IngestionSession>();
        services.AddSingleton<MainWindowViewModel>();
    }
}
