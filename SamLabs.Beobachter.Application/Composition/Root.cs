using System;
using Microsoft.Extensions.DependencyInjection;
using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Application.ViewModels.Sources;
using SamLabs.Beobachter.Application.ViewModels.Status;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Services;
using SamLabs.Beobachter.Infrastructure.Parsing;
using SamLabs.Beobachter.Infrastructure.Receivers;
using SamLabs.Beobachter.Infrastructure.Settings;

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
        services.AddSingleton<ILogStatisticsService, RollingLogStatisticsService>();
        services.AddSingleton<ILogStore, InMemoryLogStore>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton<ILogQueryEvaluator, LogQueryEvaluator>();

        services.AddSingleton<ILogParser, Log4jXmlParser>();
        services.AddSingleton<ILogParser, JsonLogParser>();
        services.AddSingleton<ILogParser, CsvParser>();
        services.AddSingleton<ILogParser, PlainTextParser>();

        services.AddSingleton<ParserPipelineFactory>();
        services.AddSingleton<ReceiverFactory>();

        services.AddSingleton<IIngestionSession, IngestionSession>();

        services.AddSingleton<SourceTreeViewModel>();
        services.AddSingleton<QuickFiltersViewModel>();
        services.AddSingleton<ReceiverSetupViewModel>();
        services.AddSingleton<LogFiltersViewModel>();
        services.AddSingleton<LogStreamViewModel>();
        services.AddSingleton<EntryDetailsViewModel>();
        services.AddSingleton<SessionHealthViewModel>();

        services.AddSingleton<MainWindowViewModel>();
    }
}
