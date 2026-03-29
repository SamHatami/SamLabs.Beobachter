using System;
using System.Collections.Generic;
using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Application.ViewModels.Sources;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

public sealed class WorkspaceSidebarViewModelTests
{
    [Fact]
    public void UpdateSnapshot_ComputesCountsAndFacets()
    {
        SourceTreeViewModel sources = new();
        QuickFiltersViewModel quickFilters = new();
        LogFiltersViewModel filters = new();
        WorkspaceSidebarViewModel vm = new(sources, quickFilters, filters);

        IReadOnlyList<LogEntry> snapshot =
        [
            new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = LogLevel.Error,
                ReceiverId = "udp-1",
                LoggerName = "Orders.Api",
                RootLoggerName = "Orders.Api",
                HostName = "node-a",
                Message = "failed",
                Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["env"] = "prod"
                }
            },
            new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = LogLevel.Info,
                ReceiverId = "udp-1",
                LoggerName = "Orders.Api",
                RootLoggerName = "Orders.Api",
                HostName = "node-a",
                Message = "processed",
                MessageTemplate = "processed {OrderId}",
                Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["env"] = "prod"
                }
            }
        ];

        vm.UpdateSnapshot(snapshot);

        Assert.Equal(1, vm.ErrorCount);
        Assert.Equal(1, vm.InfoCount);
        Assert.Equal(1, quickFilters.ErrorsAndAboveCount);
        Assert.Equal(2, quickFilters.StructuredOnlyCount);
        Assert.Contains(vm.HostOptions, x => x.Label == "node-a" && x.Count == 2);
        Assert.Contains(vm.TagOptions, x => x.Label == "env:prod" && x.Count == 2);
    }

    [Fact]
    public void ApplyTagFacet_TogglesSearchTextSelection()
    {
        SourceTreeViewModel sources = new();
        QuickFiltersViewModel quickFilters = new();
        LogFiltersViewModel filters = new();
        WorkspaceSidebarViewModel vm = new(sources, quickFilters, filters);
        SidebarFacetOptionViewModel tag = new("env:prod", "env:prod", 3);

        vm.ApplyTagFacetCommand.Execute(tag);

        Assert.Equal("env:prod", filters.SearchText);

        vm.ApplyTagFacetCommand.Execute(tag);

        Assert.Equal(string.Empty, filters.SearchText);
    }
}
