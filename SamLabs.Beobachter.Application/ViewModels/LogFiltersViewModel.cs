using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Queries;

namespace SamLabs.Beobachter.Application.ViewModels;

public partial class LogFiltersViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _receiverFilter = string.Empty;

    [ObservableProperty]
    private string _loggerFilter = string.Empty;

    [ObservableProperty]
    private string _threadFilter = string.Empty;

    [ObservableProperty]
    private string _tenantFilter = string.Empty;

    [ObservableProperty]
    private string _traceIdFilter = string.Empty;

    [ObservableProperty]
    private string _minimumLevelOption = "Any";

    [ObservableProperty]
    private bool _showTrace = true;

    [ObservableProperty]
    private bool _showDebug = true;

    [ObservableProperty]
    private bool _showInfo = true;

    [ObservableProperty]
    private bool _showWarn = true;

    [ObservableProperty]
    private bool _showError = true;

    [ObservableProperty]
    private bool _showFatal = true;

    public IReadOnlyList<string> MinimumLevelOptions { get; } =
    [
        "Any",
        nameof(LogLevel.Trace),
        nameof(LogLevel.Debug),
        nameof(LogLevel.Info),
        nameof(LogLevel.Warn),
        nameof(LogLevel.Error),
        nameof(LogLevel.Fatal)
    ];

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void ClearStructuredFilters()
    {
        ReceiverFilter = string.Empty;
        LoggerFilter = string.Empty;
        ThreadFilter = string.Empty;
        TenantFilter = string.Empty;
        TraceIdFilter = string.Empty;
        MinimumLevelOption = "Any";
    }

    [RelayCommand]
    private void ResetLevels()
    {
        ShowTrace = true;
        ShowDebug = true;
        ShowInfo = true;
        ShowWarn = true;
        ShowError = true;
        ShowFatal = true;
    }

    public LogQuery BuildQuery()
    {
        Dictionary<string, string> propertyFilters = new(StringComparer.OrdinalIgnoreCase);
        string? tenant = NormalizeFilter(TenantFilter);
        if (tenant is not null)
        {
            propertyFilters["tenant"] = tenant;
        }

        string? traceId = NormalizeFilter(TraceIdFilter);
        if (traceId is not null)
        {
            propertyFilters["traceId"] = traceId;
        }

        return new LogQuery
        {
            MinimumLevel = ParseMinimumLevel(MinimumLevelOption),
            TextContains = NormalizeFilter(SearchText),
            ReceiverId = NormalizeFilter(ReceiverFilter),
            LoggerContains = NormalizeFilter(LoggerFilter),
            ThreadContains = NormalizeFilter(ThreadFilter),
            PropertyContains = propertyFilters
        };
    }

    public bool IsLevelEnabled(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => ShowTrace,
            LogLevel.Debug => ShowDebug,
            LogLevel.Info => ShowInfo,
            LogLevel.Warn => ShowWarn,
            LogLevel.Error => ShowError,
            LogLevel.Fatal => ShowFatal,
            _ => true
        };
    }

    public IReadOnlyList<string> GetEnabledLevels()
    {
        List<string> enabledLevels = new(6);
        if (ShowTrace)
        {
            enabledLevels.Add(nameof(LogLevel.Trace));
        }

        if (ShowDebug)
        {
            enabledLevels.Add(nameof(LogLevel.Debug));
        }

        if (ShowInfo)
        {
            enabledLevels.Add(nameof(LogLevel.Info));
        }

        if (ShowWarn)
        {
            enabledLevels.Add(nameof(LogLevel.Warn));
        }

        if (ShowError)
        {
            enabledLevels.Add(nameof(LogLevel.Error));
        }

        if (ShowFatal)
        {
            enabledLevels.Add(nameof(LogLevel.Fatal));
        }

        return enabledLevels;
    }

    private static LogLevel? ParseMinimumLevel(string? option)
    {
        if (string.IsNullOrWhiteSpace(option) ||
            option.Equals("Any", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Enum.TryParse<LogLevel>(option, true, out LogLevel parsed) ? parsed : null;
    }

    private static string? NormalizeFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
