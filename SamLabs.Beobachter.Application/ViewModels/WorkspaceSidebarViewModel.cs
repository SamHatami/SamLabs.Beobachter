using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Application.ViewModels.Sources;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed partial class WorkspaceSidebarViewModel : ViewModelBase
{
    public WorkspaceSidebarViewModel(
        SourceTreeViewModel sources,
        QuickFiltersViewModel quickFilters,
        LogFiltersViewModel filters)
    {
        Sources = sources ?? throw new ArgumentNullException(nameof(sources));
        QuickFilters = quickFilters ?? throw new ArgumentNullException(nameof(quickFilters));
        Filters = filters ?? throw new ArgumentNullException(nameof(filters));
    }

    [ObservableProperty]
    private int _traceCount;

    [ObservableProperty]
    private int _debugCount;

    [ObservableProperty]
    private int _infoCount;

    [ObservableProperty]
    private int _warnCount;

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private int _fatalCount;

    public SourceTreeViewModel Sources { get; }

    public QuickFiltersViewModel QuickFilters { get; }

    public LogFiltersViewModel Filters { get; }

    public ObservableCollection<SidebarFacetOptionViewModel> HostOptions { get; } = [];

    public ObservableCollection<SidebarFacetOptionViewModel> TagOptions { get; } = [];

    [RelayCommand]
    private void ClearAllFilters()
    {
        Filters.ClearSearchCommand.Execute(null);
        Filters.ClearStructuredFiltersCommand.Execute(null);
        Filters.ResetLevelsCommand.Execute(null);
        QuickFilters.ClearQuickFiltersCommand.Execute(null);
        Sources.EnableAllLoggersCommand.Execute(null);
        Sources.SourceSearchText = string.Empty;
        SyncFacetSelectionState();
    }

    [RelayCommand]
    private void ApplyHostFacet(SidebarFacetOptionViewModel? facet)
    {
        ApplySearchFacet(facet);
    }

    [RelayCommand]
    private void ApplyTagFacet(SidebarFacetOptionViewModel? facet)
    {
        ApplySearchFacet(facet);
    }

    public void UpdateSnapshot(IReadOnlyList<LogEntry> snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        TraceCount = snapshot.Count(static entry => entry.Level == LogLevel.Trace);
        DebugCount = snapshot.Count(static entry => entry.Level == LogLevel.Debug);
        InfoCount = snapshot.Count(static entry => entry.Level == LogLevel.Info);
        WarnCount = snapshot.Count(static entry => entry.Level == LogLevel.Warn);
        ErrorCount = snapshot.Count(static entry => entry.Level == LogLevel.Error);
        FatalCount = snapshot.Count(static entry => entry.Level == LogLevel.Fatal);

        QuickFilters.ErrorsAndAboveCount = snapshot.Count(static entry => entry.Level is LogLevel.Error or LogLevel.Fatal);
        QuickFilters.StructuredOnlyCount = snapshot.Count(static entry =>
            entry.Properties.Count > 0 ||
            !string.IsNullOrWhiteSpace(entry.StructuredPayloadJson) ||
            !string.IsNullOrWhiteSpace(entry.MessageTemplate));

        RebuildFacetOptions(
            HostOptions,
            snapshot
                .Where(static entry => !string.IsNullOrWhiteSpace(entry.HostName))
                .GroupBy(static entry => entry.HostName!, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(static group => group.Count())
                .ThenBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .Select(static group => new SidebarFacetOptionViewModel(group.Key, group.Key, group.Count()))
                .ToArray());

        RebuildFacetOptions(
            TagOptions,
            snapshot
                .SelectMany(static entry => entry.Properties)
                .GroupBy(static pair => $"{pair.Key}:{pair.Value}", StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(static group => group.Count())
                .ThenBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .Select(static group => new SidebarFacetOptionViewModel(group.Key, group.Key, group.Count()))
                .ToArray());

        SyncFacetSelectionState();
    }

    public void SyncFacetSelectionState()
    {
        string searchText = Filters.SearchText.Trim();
        foreach (SidebarFacetOptionViewModel hostOption in HostOptions)
        {
            hostOption.IsSelected = searchText.Length > 0 &&
                                    string.Equals(searchText, hostOption.SearchText, StringComparison.OrdinalIgnoreCase);
        }

        foreach (SidebarFacetOptionViewModel tagOption in TagOptions)
        {
            tagOption.IsSelected = searchText.Length > 0 &&
                                   string.Equals(searchText, tagOption.SearchText, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void ApplySearchFacet(SidebarFacetOptionViewModel? facet)
    {
        if (facet is null)
        {
            return;
        }

        Filters.SearchText = string.Equals(Filters.SearchText, facet.SearchText, StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : facet.SearchText;
        SyncFacetSelectionState();
    }

    private static void RebuildFacetOptions(
        ObservableCollection<SidebarFacetOptionViewModel> target,
        IReadOnlyList<SidebarFacetOptionViewModel> items)
    {
        target.Clear();
        foreach (SidebarFacetOptionViewModel item in items)
        {
            target.Add(item);
        }
    }
}
