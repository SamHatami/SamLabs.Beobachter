using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

public sealed class EntryDetailsViewModelTests
{
    [Fact]
    public async Task CopySelectedMessage_UsesClipboard()
    {
        FakeClipboardService clipboard = new();
        EntryDetailsViewModel vm = new(clipboard)
        {
            SelectedEntry = MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Error, "Oops")
        };

        await ((IAsyncRelayCommand)vm.CopySelectedMessageCommand).ExecuteAsync(null);

        Assert.Equal("Oops", clipboard.LastText);
        Assert.Equal("Message copied.", vm.CopyStatus);
    }

    [Fact]
    public async Task CopySelectedDetails_UsesClipboard()
    {
        FakeClipboardService clipboard = new();
        EntryDetailsViewModel vm = new(clipboard)
        {
            SelectedEntry = MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Error, "Oops")
        };

        await ((IAsyncRelayCommand)vm.CopySelectedDetailsCommand).ExecuteAsync(null);

        Assert.Contains("Logger: Orders.Api", clipboard.LastText);
        Assert.Equal("Details copied.", vm.CopyStatus);
    }

    [Fact]
    public void SelectedEntry_UpdatesDetailsProjection()
    {
        EntryDetailsViewModel vm = new(new FakeClipboardService())
        {
            CopyStatus = "stale"
        };

        vm.SelectedEntry = MainWindowTestSupport.CreateEntry("Orders.Api", LogLevel.Warn, "Gateway timeout");

        Assert.Contains("Level: Warn", vm.SelectedDetailsText);
        Assert.True(vm.HasSelectedEntry);
        Assert.Equal("WARN", vm.HeaderLevelText);
        Assert.Equal("Gateway timeout", vm.HeaderMessage);
        Assert.Contains(vm.MetadataFields, f => f.Key == "Logger" && f.Value == "Orders.Api");
        Assert.Equal(string.Empty, vm.CopyStatus);
    }

    [Fact]
    public void SelectedEntry_WithPropertiesPayloadAndException_UpdatesStructuredSections()
    {
        EntryDetailsViewModel vm = new(new FakeClipboardService());
        vm.SelectedEntry = new LogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = LogLevel.Error,
            ReceiverId = "udp-prod",
            LoggerName = "Orders.Api",
            RootLoggerName = "Orders.Api",
            ThreadName = "worker-7",
            HostName = "node-1",
            Message = "Payment failed",
            Exception = "Boom",
            StructuredPayloadJson = "{\"orderId\":123,\"tenant\":\"alpha\"}",
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["tenant"] = "alpha",
                ["orderId"] = "123"
            }
        };

        Assert.True(vm.HasAttributes);
        Assert.Equal(2, vm.Attributes.Count);
        Assert.True(vm.HasPayload);
        Assert.Contains(Environment.NewLine, vm.PayloadText);
        Assert.True(vm.HasException);
        Assert.Equal("Boom", vm.ExceptionText);
        Assert.True(vm.HasMetadata);
        Assert.Contains(vm.MetadataFields, f => f.Key == "Receiver" && f.Value == "udp-prod");
        Assert.Contains(vm.MetadataFields, f => f.Key == "Host" && f.Value == "node-1");
    }

    [Fact]
    public void ShowRawPayload_TogglesBetweenPrettyAndRawViews()
    {
        EntryDetailsViewModel vm = new(new FakeClipboardService())
        {
            SelectedEntry = new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = LogLevel.Info,
                ReceiverId = "json",
                LoggerName = "Orders.Api",
                RootLoggerName = "Orders.Api",
                Message = "Structured",
                StructuredPayloadJson = "{\"orderId\":123,\"tenant\":\"alpha\"}"
            }
        };

        string prettyPayload = vm.PayloadText;

        vm.ShowRawPayloadCommand.Execute(null);

        Assert.True(vm.IsRawPayloadView);
        Assert.Equal("{\"orderId\":123,\"tenant\":\"alpha\"}", vm.PayloadText);

        vm.ShowJsonPayloadCommand.Execute(null);

        Assert.True(vm.IsJsonPayloadView);
        Assert.Equal(prettyPayload, vm.PayloadText);
    }
}
