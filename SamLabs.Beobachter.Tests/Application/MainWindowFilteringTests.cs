using SamLabs.Beobachter.Application.Services;
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

        MainWindowViewModel vm = new(new ThemeService(), session, new FakeClipboardService());
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

        MainWindowViewModel vm = new(new ThemeService(), session, new FakeClipboardService());

        vm.Filters.ReceiverFilter = "udp-1";
        Assert.Single(vm.Stream.VisibleEntries);

        vm.Filters.LoggerFilter = "Checkout";
        Assert.Single(vm.Stream.VisibleEntries);

        vm.Filters.ThreadFilter = "worker-1";
        Assert.Single(vm.Stream.VisibleEntries);

        vm.Filters.TenantFilter = "alp";
        Assert.Single(vm.Stream.VisibleEntries);

        vm.Filters.TraceIdFilter = "trace-1";
        Assert.Single(vm.Stream.VisibleEntries);

        vm.Filters.MinimumLevelOption = "Fatal";
        Assert.Empty(vm.Stream.VisibleEntries);

        vm.Filters.ClearStructuredFiltersCommand.Execute(null);
        Assert.Equal(2, vm.Stream.VisibleEntries.Count);
    }
}
