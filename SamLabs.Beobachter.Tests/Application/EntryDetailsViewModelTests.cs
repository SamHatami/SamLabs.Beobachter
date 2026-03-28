using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Core.Enums;
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
        Assert.Equal(string.Empty, vm.CopyStatus);
    }
}
