using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Core.Settings;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

public sealed class MainWindowWorkspaceStateTests
{
    [Fact]
    public async Task WorkspaceState_RestoresSelectedReceiverFromSettings()
    {
        FakeSettingsStore settings =
        new()
        {
            WorkspaceSettings = new WorkspaceSettings { SelectedReceiverId = "tcp-prod" },
            ReceiverDefinitions = new ReceiverDefinitions
            {
                UdpReceivers = [new UdpReceiverDefinition { Id = "udp-prod", DisplayName = "UDP Prod" }],
                TcpReceivers = [new TcpReceiverDefinition { Id = "tcp-prod", DisplayName = "TCP Prod" }]
            }
        };

        MainWindowViewModel vm = MainWindowTestSupport.CreateMainWindowViewModel(new FakeIngestionSession([]), settingsStore: settings);

        await MainWindowTestSupport.WaitForConditionAsync(() => vm.ReceiverSetup.SelectedReceiverDefinition is not null);

        Assert.NotNull(vm.ReceiverSetup.SelectedReceiverDefinition);
        Assert.Equal("tcp-prod", vm.ReceiverSetup.SelectedReceiverDefinition!.Id);
    }

    [Fact]
    public async Task WorkspaceState_PersistsFiltersDensityAndSelectedReceiver()
    {
        FakeSettingsStore settings =
        new()
        {
            ReceiverDefinitions = new ReceiverDefinitions
            {
                UdpReceivers = [new UdpReceiverDefinition { Id = "udp-a", DisplayName = "UDP A" }],
                TcpReceivers = [new TcpReceiverDefinition { Id = "tcp-b", DisplayName = "TCP B" }]
            }
        };

        MainWindowViewModel vm = MainWindowTestSupport.CreateMainWindowViewModel(new FakeIngestionSession([]), settingsStore: settings);
        await MainWindowTestSupport.WaitForConditionAsync(() => vm.ReceiverSetup.ReceiverDefinitions.Count == 2);

        vm.Filters.SearchText = "gateway";
        vm.Filters.ReceiverFilter = "udp-a";
        vm.Filters.LoggerFilter = "Orders.Api";
        vm.Filters.ThreadFilter = "worker-9";
        vm.Filters.SetPropertyFilterValue("tenant", "alpha");
        vm.Filters.SetPropertyFilterValue("traceId", "trace-xyz");
        vm.Filters.MinimumLevelOption = "Warn";
        vm.Stream.IsCompactDensity = true;
        vm.ReceiverSetup.SelectedReceiverDefinition = vm.ReceiverSetup.ReceiverDefinitions.Single(x => x.Id == "tcp-b");

        await MainWindowTestSupport.WaitForConditionAsync(() => settings.LastSavedWorkspaceSettings is not null);

        WorkspaceSettings saved = settings.LastSavedWorkspaceSettings!;
        Assert.Equal("gateway", saved.SearchText);
        Assert.Equal("udp-a", saved.ReceiverFilter);
        Assert.Equal("Orders.Api", saved.LoggerFilter);
        Assert.Equal("worker-9", saved.ThreadFilter);
        Assert.Equal("alpha", saved.PropertyFilters["tenant"]);
        Assert.Equal("trace-xyz", saved.PropertyFilters["traceId"]);
        Assert.Equal("Warn", saved.MinimumLevelOption);
        Assert.True(saved.CompactDensity);
        Assert.Equal("tcp-b", saved.SelectedReceiverId);
    }

    [Fact]
    public async Task UiLayoutState_PersistsColumnWidths()
    {
        FakeSettingsStore settings = new();
        MainWindowViewModel vm = MainWindowTestSupport.CreateMainWindowViewModel(new FakeIngestionSession([]), settingsStore: settings);

        vm.Stream.TimestampColumnWidth = 210;
        vm.Stream.LevelColumnWidth = 130;
        vm.Stream.LoggerColumnWidth = 260;

        await MainWindowTestSupport.WaitForConditionAsync(() => settings.LastSavedUiLayoutSettings is not null);

        Assert.NotNull(settings.LastSavedUiLayoutSettings);
        Assert.Equal(210, settings.LastSavedUiLayoutSettings!.TimestampColumnWidth);
        Assert.Equal(130, settings.LastSavedUiLayoutSettings.LevelColumnWidth);
        Assert.Equal(260, settings.LastSavedUiLayoutSettings.LoggerColumnWidth);
    }
}
