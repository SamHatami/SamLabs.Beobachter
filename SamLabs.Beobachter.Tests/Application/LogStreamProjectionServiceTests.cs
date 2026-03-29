using System;
using System.Collections.Generic;
using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Application.ViewModels.Sources;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Services;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

public sealed class LogStreamProjectionServiceTests
{
    [Fact]
    public void RebuildEntries_AppliesQuickFilters()
    {
        ILogStreamProjectionService service = new LogStreamProjectionService(new LogQueryEvaluator());
        SourceTreeViewModel sources = new();
        QuickFiltersViewModel quickFilters = new();
        LogFiltersViewModel filters = new();
        LogStreamViewModel stream = new();

        IReadOnlyList<LogEntry> snapshot =
        [
            MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Info, "Accepted"),
            MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Error, "Failed"),
            MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Fatal, "Crashed")
        ];

        quickFilters.IsErrorsAndAboveEnabled = true;

        service.RebuildEntries(snapshot, sources, quickFilters, filters, stream);

        Assert.Equal(2, stream.VisibleEntries.Count);
        Assert.All(stream.VisibleEntries, static x => Assert.True(x.Level is LogLevel.Error or LogLevel.Fatal));
    }

    [Fact]
    public void AppendEntries_RegistersNewLoggers()
    {
        ILogStreamProjectionService service = new LogStreamProjectionService(new LogQueryEvaluator());
        SourceTreeViewModel sources = new();
        QuickFiltersViewModel quickFilters = new();
        LogFiltersViewModel filters = new();
        LogStreamViewModel stream = new();

        IReadOnlyList<LogEntry> appendedEntries =
        [
            MainWindowTestSupport.CreateEntry("Orders.Api.Payments", LogLevel.Info, "Payment accepted"),
            MainWindowTestSupport.CreateEntry("Inventory.Api.Sync", LogLevel.Info, "Inventory synced")
        ];

        service.AppendEntries(appendedEntries, sources, quickFilters, filters, stream);

        Assert.Equal(2, stream.VisibleEntries.Count);
        Assert.NotEmpty(sources.LoggerTreeItems);
        Assert.Equal(2, sources.VisibleSourceItems.Count);
        Assert.Contains(MainWindowTestSupport.Flatten(sources.LoggerTreeItems), x => x.FullPath == "Orders.Api.Payments");
        Assert.Contains(MainWindowTestSupport.Flatten(sources.LoggerTreeItems), x => x.FullPath == "Inventory.Api.Sync");
        Assert.Contains(sources.VisibleSourceItems, x => x.Name == "Orders.Api.Payments" && x.Count == 1);
    }

    [Fact]
    public void ComputeQuickFilterSnapshot_ReturnsExpectedCounts()
    {
        ILogStreamProjectionService service = new LogStreamProjectionService(new LogQueryEvaluator());
        IReadOnlyList<LogEntry> snapshot =
        [
            MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Error, "Failed"),
            MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Info, "Accepted"),
            new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = LogLevel.Info,
                ReceiverId = "udp-1",
                LoggerName = "Orders.Api",
                RootLoggerName = "Orders.Api",
                Message = "Structured message",
                MessageTemplate = "Structured message {OrderId}",
                Properties = new Dictionary<string, string>
                {
                    ["OrderId"] = "42"
                }
            }
        ];

        QuickFilterSnapshot quickFilterSnapshot = service.ComputeQuickFilterSnapshot(snapshot);

        Assert.Equal(1, quickFilterSnapshot.ErrorsAndAboveCount);
        Assert.Equal(1, quickFilterSnapshot.StructuredOnlyCount);
    }
}
