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
        Assert.Equal(3, vm.VisibleEntries.Count);

        vm.SearchText = "gateway";
        Assert.Single(vm.VisibleEntries);

        vm.ShowWarn = false;
        Assert.Empty(vm.VisibleEntries);

        vm.ShowWarn = true;
        Assert.Single(vm.VisibleEntries);

        LoggerTreeItemViewModel paymentsNode = MainWindowTestSupport.Flatten(vm.LoggerTreeItems).First(x => x.FullPath == "Orders.Api.Payments");
        paymentsNode.IsEnabled = false;
        Assert.Empty(vm.VisibleEntries);
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

        vm.ReceiverFilter = "udp-1";
        Assert.Single(vm.VisibleEntries);

        vm.LoggerFilter = "Checkout";
        Assert.Single(vm.VisibleEntries);

        vm.ThreadFilter = "worker-1";
        Assert.Single(vm.VisibleEntries);

        vm.TenantFilter = "alp";
        Assert.Single(vm.VisibleEntries);

        vm.TraceIdFilter = "trace-1";
        Assert.Single(vm.VisibleEntries);

        vm.MinimumLevelOption = "Fatal";
        Assert.Empty(vm.VisibleEntries);

        vm.ClearStructuredFiltersCommand.Execute(null);
        Assert.Equal(2, vm.VisibleEntries.Count);
    }
}
