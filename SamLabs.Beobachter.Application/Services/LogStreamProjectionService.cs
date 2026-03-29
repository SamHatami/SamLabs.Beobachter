using System;
using System.Collections.Generic;
using System.Linq;
using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Application.ViewModels.Sources;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Queries;

namespace SamLabs.Beobachter.Application.Services;

public sealed class LogStreamProjectionService : ILogStreamProjectionService
{
    private readonly ILogQueryEvaluator _queryEvaluator;

    public LogStreamProjectionService(ILogQueryEvaluator queryEvaluator)
    {
        _queryEvaluator = queryEvaluator ?? throw new ArgumentNullException(nameof(queryEvaluator));
    }

    public void RebuildEntries(
        IReadOnlyList<LogEntry> snapshot,
        SourceTreeViewModel sources,
        QuickFiltersViewModel quickFilters,
        LogFiltersViewModel filters,
        LogStreamViewModel stream)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(quickFilters);
        ArgumentNullException.ThrowIfNull(filters);
        ArgumentNullException.ThrowIfNull(stream);

        LogQuery query = filters.BuildQuery();
        stream.RebuildEntries(snapshot, entry => MatchesFilter(entry, query, sources, quickFilters, filters));
    }

    public void AppendEntries(
        IReadOnlyList<LogEntry> appendedEntries,
        SourceTreeViewModel sources,
        QuickFiltersViewModel quickFilters,
        LogFiltersViewModel filters,
        LogStreamViewModel stream)
    {
        ArgumentNullException.ThrowIfNull(appendedEntries);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(quickFilters);
        ArgumentNullException.ThrowIfNull(filters);
        ArgumentNullException.ThrowIfNull(stream);

        foreach (LogEntry entry in appendedEntries)
        {
            sources.RegisterEntry(entry);
        }

        LogQuery query = filters.BuildQuery();
        stream.AppendEntries(appendedEntries, entry => MatchesFilter(entry, query, sources, quickFilters, filters));
    }

    public QuickFilterSnapshot ComputeQuickFilterSnapshot(IReadOnlyList<LogEntry> snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        int errorsAndAboveCount = snapshot.Count(static entry => entry.Level is LogLevel.Error or LogLevel.Fatal);
        int structuredOnlyCount = snapshot.Count(HasStructuredData);
        return new QuickFilterSnapshot(errorsAndAboveCount, structuredOnlyCount);
    }

    private bool MatchesFilter(
        LogEntry entry,
        LogQuery query,
        SourceTreeViewModel sources,
        QuickFiltersViewModel quickFilters,
        LogFiltersViewModel filters)
    {
        if (!filters.IsLevelEnabled(entry.Level))
        {
            return false;
        }

        if (!sources.IsEntryEnabled(entry))
        {
            return false;
        }

        if (quickFilters.IsErrorsAndAboveEnabled &&
            entry.Level is not LogLevel.Error and not LogLevel.Fatal)
        {
            return false;
        }

        if (quickFilters.IsStructuredOnlyEnabled && !HasStructuredData(entry))
        {
            return false;
        }

        return _queryEvaluator.Matches(entry, query);
    }

    private static bool HasStructuredData(LogEntry entry)
    {
        return entry.Properties.Count > 0 ||
               !string.IsNullOrWhiteSpace(entry.StructuredPayloadJson) ||
               !string.IsNullOrWhiteSpace(entry.MessageTemplate);
    }
}
