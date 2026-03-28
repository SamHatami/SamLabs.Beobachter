using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Queries;
using SamLabs.Beobachter.ViewModels;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void Filters_BySearchLevelAndLoggerState()
    {
        var session = new FakeIngestionSession(
        [
            CreateEntry("Orders.Api.Payments", LogLevel.Warn, "Gateway timeout"),
            CreateEntry("Orders.Api.Checkout", LogLevel.Info, "Checkout accepted"),
            CreateEntry("Inventory.Api.Sync", LogLevel.Error, "Stock mismatch")
        ]);

        var vm = new MainWindowViewModel(new ThemeService(), session, new FakeClipboardService());
        Assert.Equal(3, vm.VisibleEntries.Count);

        vm.SearchText = "gateway";
        Assert.Single(vm.VisibleEntries);

        vm.ShowWarn = false;
        Assert.Empty(vm.VisibleEntries);

        vm.ShowWarn = true;
        Assert.Single(vm.VisibleEntries);

        var paymentsNode = Flatten(vm.LoggerTreeItems).First(x => x.FullPath == "Orders.Api.Payments");
        paymentsNode.IsEnabled = false;
        Assert.Empty(vm.VisibleEntries);
    }

    [Fact]
    public async Task TogglePause_UpdatesSessionState()
    {
        var session = new FakeIngestionSession([]);
        var vm = new MainWindowViewModel(new ThemeService(), session, new FakeClipboardService());

        await ((IAsyncRelayCommand)vm.TogglePauseCommand).ExecuteAsync(null);
        Assert.True(vm.IsPaused);
        Assert.True(session.IsPaused);

        await ((IAsyncRelayCommand)vm.TogglePauseCommand).ExecuteAsync(null);
        Assert.False(vm.IsPaused);
        Assert.False(session.IsPaused);
    }

    [Fact]
    public async Task CopyCommands_UseClipboardService()
    {
        var clipboard = new FakeClipboardService();
        var session = new FakeIngestionSession([CreateEntry("Orders.Api", LogLevel.Error, "Oops")]);
        var vm = new MainWindowViewModel(new ThemeService(), session, clipboard)
        {
            SelectedEntry = session.Snapshot().First()
        };

        await ((IAsyncRelayCommand)vm.CopySelectedMessageCommand).ExecuteAsync(null);
        Assert.Equal("Oops", clipboard.LastText);

        await ((IAsyncRelayCommand)vm.CopySelectedDetailsCommand).ExecuteAsync(null);
        Assert.Contains("Logger: Orders.Api", clipboard.LastText);
    }

    private static LogEntry CreateEntry(string logger, LogLevel level, string message)
    {
        return new LogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = level,
            ReceiverId = "test",
            LoggerName = logger,
            RootLoggerName = logger,
            Message = message
        };
    }

    private static IEnumerable<LoggerTreeItemViewModel> Flatten(IEnumerable<LoggerTreeItemViewModel> source)
    {
        foreach (var item in source)
        {
            yield return item;
            foreach (var child in Flatten(item.Children))
            {
                yield return child;
            }
        }
    }

    private sealed class FakeIngestionSession : IIngestionSession
    {
        private readonly List<LogEntry> _entries;

        public FakeIngestionSession(IEnumerable<LogEntry> entries)
        {
            _entries = entries.ToList();
        }

        public event EventHandler<LogEntriesAppendedEventArgs>? EntriesAppended;

        public int TotalCount => _entries.Count;

        public long DroppedCount => 0;

        public bool IsPaused { get; private set; }

        public bool TryPublish(LogEntry entry)
        {
            _entries.Add(entry);
            EntriesAppended?.Invoke(this, new LogEntriesAppendedEventArgs([entry]));
            return true;
        }

        public IReadOnlyList<LogEntry> Snapshot(LogQuery? query = null)
        {
            return _entries.ToArray();
        }

        public ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask SetPausedAsync(bool isPaused, CancellationToken cancellationToken = default)
        {
            IsPaused = isPaused;
            return ValueTask.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeClipboardService : IClipboardService
    {
        public string LastText { get; private set; } = string.Empty;

        public ValueTask SetTextAsync(string text, CancellationToken cancellationToken = default)
        {
            LastText = text;
            return ValueTask.CompletedTask;
        }
    }
}
