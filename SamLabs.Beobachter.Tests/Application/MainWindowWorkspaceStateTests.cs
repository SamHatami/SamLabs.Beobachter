using SamLabs.Beobachter.Application.Services;
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

        MainWindowViewModel vm = new(new ThemeService(), new FakeIngestionSession([]), new FakeClipboardService(), settings);

        await MainWindowTestSupport.WaitForConditionAsync(() => vm.SelectedReceiverDefinition is not null);

        Assert.NotNull(vm.SelectedReceiverDefinition);
        Assert.Equal("tcp-prod", vm.SelectedReceiverDefinition!.Id);
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

        MainWindowViewModel vm = new(new ThemeService(), new FakeIngestionSession([]), new FakeClipboardService(), settings);
        await MainWindowTestSupport.WaitForConditionAsync(() => vm.ReceiverDefinitions.Count == 2);

        vm.SearchText = "gateway";
        vm.ReceiverFilter = "udp-a";
        vm.LoggerFilter = "Orders.Api";
        vm.ThreadFilter = "worker-9";
        vm.TenantFilter = "alpha";
        vm.TraceIdFilter = "trace-xyz";
        vm.MinimumLevelOption = "Warn";
        vm.IsCompactDensity = true;
        vm.SelectedReceiverDefinition = vm.ReceiverDefinitions.Single(x => x.Id == "tcp-b");

        await MainWindowTestSupport.WaitForConditionAsync(() => settings.LastSavedWorkspaceSettings is not null);

        WorkspaceSettings saved = settings.LastSavedWorkspaceSettings!;
        Assert.Equal("gateway", saved.SearchText);
        Assert.Equal("udp-a", saved.ReceiverFilter);
        Assert.Equal("Orders.Api", saved.LoggerFilter);
        Assert.Equal("worker-9", saved.ThreadFilter);
        Assert.Equal("alpha", saved.TenantFilter);
        Assert.Equal("trace-xyz", saved.TraceIdFilter);
        Assert.Equal("Warn", saved.MinimumLevelOption);
        Assert.True(saved.CompactDensity);
        Assert.Equal("tcp-b", saved.SelectedReceiverId);
    }

    [Fact]
    public async Task UiLayoutState_PersistsColumnWidths()
    {
        FakeSettingsStore settings = new();
        MainWindowViewModel vm = new(new ThemeService(), new FakeIngestionSession([]), new FakeClipboardService(), settings);

        vm.TimestampColumnWidth = 210;
        vm.LevelColumnWidth = 130;
        vm.LoggerColumnWidth = 260;

        await MainWindowTestSupport.WaitForConditionAsync(() => settings.LastSavedUiLayoutSettings is not null);

        Assert.NotNull(settings.LastSavedUiLayoutSettings);
        Assert.Equal(210, settings.LastSavedUiLayoutSettings!.TimestampColumnWidth);
        Assert.Equal(130, settings.LastSavedUiLayoutSettings.LevelColumnWidth);
        Assert.Equal(260, settings.LastSavedUiLayoutSettings.LoggerColumnWidth);
    }
}
