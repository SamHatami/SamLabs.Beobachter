using System.Collections.Generic;
using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Application.ViewModels.Sources;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.Services;

public interface ILogStreamProjectionService
{
    void RebuildEntries(
        IReadOnlyList<LogEntry> snapshot,
        SourceTreeViewModel sources,
        QuickFiltersViewModel quickFilters,
        LogFiltersViewModel filters,
        LogStreamViewModel stream);

    void AppendEntries(
        IReadOnlyList<LogEntry> appendedEntries,
        SourceTreeViewModel sources,
        QuickFiltersViewModel quickFilters,
        LogFiltersViewModel filters,
        LogStreamViewModel stream);

    QuickFilterSnapshot ComputeQuickFilterSnapshot(IReadOnlyList<LogEntry> snapshot);
}
