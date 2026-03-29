using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Core.Settings;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

public sealed class WorkspaceStartupOrchestratorTests
{
    [Fact]
    public async Task InitializeAsync_LoadsWorkspaceStateAndSelectsConfiguredReceiver()
    {
        FakeSettingsStore settings =
        new()
        {
            WorkspaceSettings = new WorkspaceSettings { SelectedReceiverId = "tcp-prod" },
            UiLayoutSettings = new UiLayoutSettings { TimestampColumnWidth = 210, LevelColumnWidth = 130, LoggerColumnWidth = 260 },
            ReceiverDefinitions = new ReceiverDefinitions
            {
                UdpReceivers = [new UdpReceiverDefinition { Id = "udp-prod", DisplayName = "UDP Prod" }],
                TcpReceivers = [new TcpReceiverDefinition { Id = "tcp-prod", DisplayName = "TCP Prod" }]
            }
        };

        IWorkspaceStateCoordinator workspaceStateCoordinator = new WorkspaceStateCoordinator(settings);
        IWorkspaceStartupOrchestrator orchestrator = new WorkspaceStartupOrchestrator(workspaceStateCoordinator);
        ReceiverSetupViewModel receiverSetup = new(settings, new FakeIngestionSession([]));

        WorkspaceSettings? appliedWorkspace = null;
        UiLayoutSettings? appliedLayout = null;
        await orchestrator.InitializeAsync(receiverSetup, (workspace, layout) =>
        {
            appliedWorkspace = workspace;
            appliedLayout = layout;
        });

        Assert.NotNull(appliedWorkspace);
        Assert.NotNull(appliedLayout);
        Assert.Equal("tcp-prod", receiverSetup.SelectedReceiverDefinition?.Id);
        Assert.Equal(210, appliedLayout!.TimestampColumnWidth);
        Assert.Equal(130, appliedLayout.LevelColumnWidth);
        Assert.Equal(260, appliedLayout.LoggerColumnWidth);
    }

    [Fact]
    public async Task InitializeAsync_WhenSelectedReceiverMissing_KeepsFirstReceiverSelection()
    {
        FakeSettingsStore settings =
        new()
        {
            WorkspaceSettings = new WorkspaceSettings { SelectedReceiverId = "missing-id" },
            ReceiverDefinitions = new ReceiverDefinitions
            {
                UdpReceivers = [new UdpReceiverDefinition { Id = "udp-a", DisplayName = "UDP A" }],
                TcpReceivers = [new TcpReceiverDefinition { Id = "tcp-b", DisplayName = "TCP B" }]
            }
        };

        IWorkspaceStateCoordinator workspaceStateCoordinator = new WorkspaceStateCoordinator(settings);
        IWorkspaceStartupOrchestrator orchestrator = new WorkspaceStartupOrchestrator(workspaceStateCoordinator);
        ReceiverSetupViewModel receiverSetup = new(settings, new FakeIngestionSession([]));

        await orchestrator.InitializeAsync(receiverSetup, static (_, _) => { });

        Assert.NotNull(receiverSetup.SelectedReceiverDefinition);
        Assert.Equal("udp-a", receiverSetup.SelectedReceiverDefinition!.Id);
    }
}
