using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

public sealed class MainWindowSessionAndDetailsTests
{
    [Fact]
    public async Task TogglePause_UpdatesSessionState()
    {
        FakeIngestionSession session = new([]);
        MainWindowViewModel vm = MainWindowTestSupport.CreateMainWindowViewModel(session);

        await ((IAsyncRelayCommand)vm.TopBar.TogglePauseCommand).ExecuteAsync(null);
        Assert.True(vm.TopBar.IsPaused);
        Assert.True(session.IsPaused);

        await ((IAsyncRelayCommand)vm.TopBar.TogglePauseCommand).ExecuteAsync(null);
        Assert.False(vm.TopBar.IsPaused);
        Assert.False(session.IsPaused);
    }

    [Fact]
    public async Task ToggleAutoScroll_UpdatesSessionState()
    {
        FakeIngestionSession session = new([]);
        MainWindowViewModel vm = MainWindowTestSupport.CreateMainWindowViewModel(session);

        await ((IAsyncRelayCommand)vm.Stream.ToggleAutoScrollCommand).ExecuteAsync(null);
        Assert.False(vm.Stream.IsAutoScrollEnabled);
        Assert.False(session.IsAutoScrollEnabled);

        await ((IAsyncRelayCommand)vm.Stream.ToggleAutoScrollCommand).ExecuteAsync(null);
        Assert.True(vm.Stream.IsAutoScrollEnabled);
        Assert.True(session.IsAutoScrollEnabled);
    }

    [Fact]
    public async Task CopyCommands_UseClipboardService()
    {
        FakeClipboardService clipboard = new();
        FakeIngestionSession session = new([MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Error, "Oops")]);
        MainWindowViewModel vm = MainWindowTestSupport.CreateMainWindowViewModel(session, clipboard);
        vm.Stream.SelectedEntry = session.Snapshot().First();

        await ((IAsyncRelayCommand)vm.Details.CopySelectedMessageCommand).ExecuteAsync(null);
        Assert.Equal("Oops", clipboard.LastText);

        await ((IAsyncRelayCommand)vm.Details.CopySelectedDetailsCommand).ExecuteAsync(null);
        Assert.Contains("Logger: Orders.Api", clipboard.LastText);
    }

    [Fact]
    public void SelectedEntry_WithStructuredPayload_ShowsTemplateAndPayloadInDetails()
    {
        FakeIngestionSession session =
        new(
        [
            new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = LogLevel.Warn,
                ReceiverId = "json",
                LoggerName = "Orders.Api",
                RootLoggerName = "Orders.Api",
                Message = "Payment warning",
                MessageTemplate = "Payment warning for {OrderId}",
                StructuredPayloadJson = "{\"OrderId\":123,\"Tenant\":\"alpha\"}",
                Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["OrderId"] = "123"
                }
            }
        ]);

        MainWindowViewModel vm = MainWindowTestSupport.CreateMainWindowViewModel(session);
        vm.Stream.SelectedEntry = session.Snapshot().First();

        Assert.Equal("WARN", vm.Details.HeaderLevelText);
        Assert.Equal("Payment warning", vm.Details.HeaderMessage);
        Assert.True(vm.Details.HasPayload);
        Assert.True(vm.Details.HasAttributes);
        Assert.Single(vm.Details.Attributes);
        Assert.Contains(Environment.NewLine, vm.Details.PayloadText);
        Assert.Contains("MessageTemplate: Payment warning for {OrderId}", vm.Details.SelectedDetailsText);
        Assert.Contains("StructuredPayload:", vm.Details.SelectedDetailsText);
        Assert.Contains("\"OrderId\":123", vm.Details.SelectedDetailsText);
    }

    [Fact]
    public void ToggleDensity_UpdatesRowMetrics()
    {
        MainWindowViewModel vm = MainWindowTestSupport.CreateMainWindowViewModel(new FakeIngestionSession([]));

        Assert.False(vm.Stream.IsCompactDensity);
        Assert.Equal("Density: Comfortable", vm.Stream.DensityButtonText);
        Assert.Equal(12, vm.Stream.LogRowFontSize);

        vm.Stream.ToggleDensityCommand.Execute(null);
        Assert.True(vm.Stream.IsCompactDensity);
        Assert.Equal("Density: Compact", vm.Stream.DensityButtonText);
        Assert.Equal(11, vm.Stream.LogRowFontSize);

        vm.Stream.ToggleDensityCommand.Execute(null);
        Assert.False(vm.Stream.IsCompactDensity);
        Assert.Equal("Density: Comfortable", vm.Stream.DensityButtonText);
        Assert.Equal(12, vm.Stream.LogRowFontSize);
    }

    [Fact]
    public void ShellStatus_UsesRuntimeRunningReceiverCount_WhenAvailable()
    {
        FakeIngestionSession session = new([]);
        session.ReceiverRuntimeStates =
        [
            new ReceiverRuntimeState { ReceiverId = "udp-a", DisplayName = "UDP A", State = ReceiverRunState.Running },
            new ReceiverRuntimeState { ReceiverId = "tcp-b", DisplayName = "TCP B", State = ReceiverRunState.Faulted, LastError = "bind failed" }
        ];

        MainWindowViewModel vm = MainWindowTestSupport.CreateMainWindowViewModel(session);

        Assert.Equal("Active receivers: 1", vm.SessionHealth.ActiveReceiversText);
    }
}
