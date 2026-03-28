using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

public sealed class SourceTreeViewModelTests
{
    [Fact]
    public void RebuildFromSnapshot_BuildsLoggerTree()
    {
        SourceTreeViewModel vm = new();
        IReadOnlyList<LogEntry> snapshot =
        [
            MainWindowTestSupport.CreateEntry("Orders.Api.Payments", LogLevel.Warn, "warn"),
            MainWindowTestSupport.CreateEntry("Orders.Api.Checkout", LogLevel.Info, "info")
        ];

        vm.RebuildFromSnapshot(snapshot);

        Assert.NotEmpty(vm.LoggerTreeItems);
        Assert.True(vm.IsLoggerEnabled("Orders.Api.Payments"));
        Assert.True(vm.IsLoggerEnabled("Orders.Api.Checkout"));
    }

    [Fact]
    public void TogglingNode_DisablesLoggerAndRaisesStateChanged()
    {
        SourceTreeViewModel vm = new();
        vm.RebuildFromSnapshot(
        [
            MainWindowTestSupport.CreateEntry("Orders.Api.Payments", LogLevel.Warn, "warn")
        ]);

        var events = 0;
        vm.StateChanged += (_, _) => events++;

        LoggerTreeItemViewModel node = MainWindowTestSupport.Flatten(vm.LoggerTreeItems)
            .First(x => x.FullPath == "Orders.Api.Payments");
        node.IsEnabled = false;

        Assert.False(vm.IsLoggerEnabled("Orders.Api.Payments"));
        Assert.Equal(1, events);
    }

    [Fact]
    public void EnableAllLoggers_ReenablesDisabledNodes()
    {
        SourceTreeViewModel vm = new();
        vm.RebuildFromSnapshot(
        [
            MainWindowTestSupport.CreateEntry("Orders.Api.Payments", LogLevel.Warn, "warn"),
            MainWindowTestSupport.CreateEntry("Orders.Api.Checkout", LogLevel.Info, "info")
        ]);

        LoggerTreeItemViewModel node = MainWindowTestSupport.Flatten(vm.LoggerTreeItems)
            .First(x => x.FullPath == "Orders.Api.Payments");
        node.IsEnabled = false;
        Assert.False(vm.IsLoggerEnabled("Orders.Api.Payments"));

        vm.EnableAllLoggersCommand.Execute(null);

        Assert.True(vm.IsLoggerEnabled("Orders.Api.Payments"));
    }
}
