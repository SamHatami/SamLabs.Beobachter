using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Queries;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

public sealed class LogFiltersViewModelTests
{
    [Fact]
    public void BuildQuery_MapsSearchAndStructuredFilters()
    {
        LogFiltersViewModel vm =
        new()
        {
            SearchText = "gateway",
            ReceiverFilter = "udp-1",
            LoggerFilter = "Orders.Api",
            ThreadFilter = "worker-9",
            MinimumLevelOption = "Warn"
        };
        vm.SetPropertyFilterValue("tenant", "alpha");
        vm.SetPropertyFilterValue("traceId", "trace-123");

        LogQuery query = vm.BuildQuery();

        Assert.Equal(LogLevel.Warn, query.MinimumLevel);
        Assert.Equal("gateway", query.TextContains);
        Assert.Equal("udp-1", query.ReceiverId);
        Assert.Equal("Orders.Api", query.LoggerContains);
        Assert.Equal("worker-9", query.ThreadContains);
        Assert.Equal("alpha", query.PropertyContains["tenant"]);
        Assert.Equal("trace-123", query.PropertyContains["traceId"]);
    }

    [Fact]
    public void ClearCommands_ResetExpectedFields()
    {
        LogFiltersViewModel vm =
        new()
        {
            SearchText = "foo",
            ReceiverFilter = "udp-1",
            LoggerFilter = "Orders.Api",
            ThreadFilter = "worker-9",
            MinimumLevelOption = "Error"
        };
        vm.SetPropertyFilterValue("tenant", "alpha");
        vm.SetPropertyFilterValue("traceId", "trace-123");

        vm.ClearSearchCommand.Execute(null);
        vm.ClearStructuredFiltersCommand.Execute(null);

        Assert.Equal(string.Empty, vm.SearchText);
        Assert.Equal(string.Empty, vm.ReceiverFilter);
        Assert.Equal(string.Empty, vm.LoggerFilter);
        Assert.Equal(string.Empty, vm.ThreadFilter);
        Assert.Equal("Any", vm.MinimumLevelOption);
        Assert.All(vm.PropertyFilters, x => Assert.Equal(string.Empty, x.Value));
    }

    [Fact]
    public void ResetLevels_RestoresAllEnabled()
    {
        LogFiltersViewModel vm =
        new()
        {
            ShowTrace = false,
            ShowDebug = false,
            ShowInfo = false,
            ShowWarn = false,
            ShowError = false,
            ShowFatal = false
        };

        vm.ResetLevelsCommand.Execute(null);

        Assert.True(vm.ShowTrace);
        Assert.True(vm.ShowDebug);
        Assert.True(vm.ShowInfo);
        Assert.True(vm.ShowWarn);
        Assert.True(vm.ShowError);
        Assert.True(vm.ShowFatal);
    }
}
