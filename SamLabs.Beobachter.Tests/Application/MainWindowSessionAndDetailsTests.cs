using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Application.Services;
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
        MainWindowViewModel vm = new(new ThemeService(), session, new FakeClipboardService());

        await ((IAsyncRelayCommand)vm.TogglePauseCommand).ExecuteAsync(null);
        Assert.True(vm.IsPaused);
        Assert.True(session.IsPaused);

        await ((IAsyncRelayCommand)vm.TogglePauseCommand).ExecuteAsync(null);
        Assert.False(vm.IsPaused);
        Assert.False(session.IsPaused);
    }

    [Fact]
    public async Task ToggleAutoScroll_UpdatesSessionState()
    {
        FakeIngestionSession session = new([]);
        MainWindowViewModel vm = new(new ThemeService(), session, new FakeClipboardService());

        await ((IAsyncRelayCommand)vm.ToggleAutoScrollCommand).ExecuteAsync(null);
        Assert.False(vm.IsAutoScrollEnabled);
        Assert.False(session.IsAutoScrollEnabled);

        await ((IAsyncRelayCommand)vm.ToggleAutoScrollCommand).ExecuteAsync(null);
        Assert.True(vm.IsAutoScrollEnabled);
        Assert.True(session.IsAutoScrollEnabled);
    }

    [Fact]
    public async Task CopyCommands_UseClipboardService()
    {
        FakeClipboardService clipboard = new();
        FakeIngestionSession session = new([MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Error, "Oops")]);
        MainWindowViewModel vm = new(new ThemeService(), session, clipboard);
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

        MainWindowViewModel vm = new(new ThemeService(), session, new FakeClipboardService());
        vm.Stream.SelectedEntry = session.Snapshot().First();

        Assert.Contains("MessageTemplate: Payment warning for {OrderId}", vm.Details.SelectedDetailsText);
        Assert.Contains("StructuredPayload:", vm.Details.SelectedDetailsText);
        Assert.Contains("\"OrderId\":123", vm.Details.SelectedDetailsText);
    }

    [Fact]
    public void ToggleDensity_UpdatesRowMetrics()
    {
        MainWindowViewModel vm = new(new ThemeService(), new FakeIngestionSession([]), new FakeClipboardService());

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
}
