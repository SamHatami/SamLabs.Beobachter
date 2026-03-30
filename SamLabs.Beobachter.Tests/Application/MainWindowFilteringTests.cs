using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

public sealed class MainWindowFilteringTests
{
    [Fact]
    public void Filters_BySearchLevelAndLoggerState()
    {
        FakeIngestionSession session =
        new(
        [
            MainWindowTestSupport.CreateEntry("Orders.Api.Payments", LogLevel.Warn, "Gateway timeout"),
            MainWindowTestSupport.CreateEntry("Orders.Api.Checkout", LogLevel.Info, "Checkout accepted"),
            MainWindowTestSupport.CreateEntry("Inventory.Api.Sync", LogLevel.Error, "Stock mismatch")
        ]);

        MainWindowViewModel vm = MainWindowTestSupport.CreateMainWindowViewModel(session);
        Assert.Equal(3, vm.Stream.VisibleEntries.Count);

        vm.Filters.SearchText = "gateway";
        Assert.Single(vm.Stream.VisibleEntries);

        vm.Filters.ShowWarn = false;
        Assert.Empty(vm.Stream.VisibleEntries);

        vm.Filters.ShowWarn = true;
        Assert.Single(vm.Stream.VisibleEntries);

        LoggerTreeItemViewModel paymentsNode = MainWindowTestSupport.Flatten(vm.Sources.LoggerTreeItems).First(x => x.FullPath == "Orders.Api.Payments");
        paymentsNode.IsEnabled = false;
        Assert.Empty(vm.Stream.VisibleEntries);
    }

    [Fact]
    public void Filters_ByStructuredFields()
    {
        FakeIngestionSession session =
        new(
        [
            new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = LogLevel.Error,
                ReceiverId = "udp-1",
                LoggerName = "Orders.Api.Checkout",
                RootLoggerName = "Orders.Api.Checkout",
                ThreadName = "worker-1",
                Message = "Payment failed",
                Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["tenant"] = "alpha",
                    ["traceId"] = "trace-1"
                }
            },
            new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = LogLevel.Info,
                ReceiverId = "tcp-2",
                LoggerName = "Inventory.Api.Sync",
                RootLoggerName = "Inventory.Api.Sync",
                ThreadName = "worker-2",
                Message = "Sync complete",
                Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["tenant"] = "beta",
                    ["traceId"] = "trace-2"
                }
            }
        ]);

        MainWindowViewModel vm = MainWindowTestSupport.CreateMainWindowViewModel(session);

        vm.Filters.ReceiverFilter = "udp-1";
        Assert.Single(vm.Stream.VisibleEntries);

        vm.Filters.LoggerFilter = "Checkout";
        Assert.Single(vm.Stream.VisibleEntries);

        vm.Filters.ThreadFilter = "worker-1";
        Assert.Single(vm.Stream.VisibleEntries);

        vm.Filters.SetPropertyFilterValue("tenant", "alp");
        Assert.Single(vm.Stream.VisibleEntries);

        vm.Filters.SetPropertyFilterValue("traceId", "trace-1");
        Assert.Single(vm.Stream.VisibleEntries);

        vm.Filters.MinimumLevelOption = "Fatal";
        Assert.Empty(vm.Stream.VisibleEntries);

        vm.Filters.ClearStructuredFiltersCommand.Execute(null);
        Assert.Equal(2, vm.Stream.VisibleEntries.Count);
    }

    [Fact]
    public void QuickFilters_ErrorsAndAbove_FiltersVisibleEntries()
    {
        FakeIngestionSession session =
        new(
        [
            MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Info, "Accepted"),
            MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Warn, "Slow call"),
            MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Error, "Failed"),
            MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Fatal, "Crashed")
        ]);

        MainWindowViewModel vm = MainWindowTestSupport.CreateMainWindowViewModel(session);
        Assert.Equal(4, vm.Stream.VisibleEntries.Count);

        vm.QuickFilters.ApplyErrorsAndAboveCommand.Execute(null);
        Assert.Equal(2, vm.Stream.VisibleEntries.Count);
        Assert.All(vm.Stream.VisibleEntries, x => Assert.True(x.Level is LogLevel.Error or LogLevel.Fatal));
    }

    [Fact]
    public void QuickFilters_StructuredOnly_FiltersVisibleEntries()
    {
        FakeIngestionSession session =
        new(
        [
            MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Info, "Simple line"),
            new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = LogLevel.Info,
                ReceiverId = "udp-1",
                LoggerName = "Orders.Api",
                RootLoggerName = "Orders.Api",
                Message = "Structured line",
                MessageTemplate = "Structured line {OrderId}",
                Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["OrderId"] = "991"
                }
            }
        ]);

        MainWindowViewModel vm = MainWindowTestSupport.CreateMainWindowViewModel(session);
        Assert.Equal(2, vm.Stream.VisibleEntries.Count);

        vm.QuickFilters.ApplyStructuredOnlyCommand.Execute(null);
        Assert.Single(vm.Stream.VisibleEntries);
        Assert.Equal("Structured line", vm.Stream.VisibleEntries[0].Message);
    }

    [Fact]
    public void ClearEntries_ClearsMessagesAndKeepsSourceTree()
    {
        FakeIngestionSession session =
        new(
        [
            MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Info, "Accepted"),
            MainWindowTestSupport.CreateEntry("Inventory.Api", LogLevel.Warn, "Slow sync")
        ]);

        MainWindowViewModel vm = MainWindowTestSupport.CreateMainWindowViewModel(session);
        Assert.NotEmpty(vm.Sources.LoggerTreeItems);
        Assert.Equal(2, vm.Stream.VisibleEntries.Count);

        vm.Stream.ClearEntriesCommand.Execute(null);

        Assert.Equal(1, session.ClearEntriesCalls);
        Assert.Equal(0, session.TotalCount);
        Assert.Empty(vm.Stream.VisibleEntries);
        Assert.NotEmpty(vm.Sources.LoggerTreeItems);
        Assert.All(vm.Sources.VisibleSourceItems, source => Assert.Equal(0, source.Count));
        Assert.Equal(0, vm.QuickFilters.ErrorsAndAboveCount);
        Assert.Equal(0, vm.QuickFilters.StructuredOnlyCount);
    }
}
