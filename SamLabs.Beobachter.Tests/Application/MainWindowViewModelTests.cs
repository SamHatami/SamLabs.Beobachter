using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Queries;
using SamLabs.Beobachter.Core.Settings;
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
    public void Filters_ByStructuredFields()
    {
        var session = new FakeIngestionSession(
        [
            new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = LogLevel.Error,
                ReceiverId = "udp-1",
                LoggerName = "Orders.Api.Checkout",
                RootLoggerName = "Orders.Api.Checkout",
                ThreadName = "worker-1",
                Message = "Payment failed",
                Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["tenant"] = "alpha",
                    ["traceId"] = "trace-1"
                }
            },
            new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = LogLevel.Info,
                ReceiverId = "tcp-2",
                LoggerName = "Inventory.Api.Sync",
                RootLoggerName = "Inventory.Api.Sync",
                ThreadName = "worker-2",
                Message = "Sync complete",
                Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["tenant"] = "beta",
                    ["traceId"] = "trace-2"
                }
            }
        ]);

        var vm = new MainWindowViewModel(new ThemeService(), session, new FakeClipboardService());

        vm.ReceiverFilter = "udp-1";
        Assert.Single(vm.VisibleEntries);

        vm.LoggerFilter = "Checkout";
        Assert.Single(vm.VisibleEntries);

        vm.ThreadFilter = "worker-1";
        Assert.Single(vm.VisibleEntries);

        vm.TenantFilter = "alp";
        Assert.Single(vm.VisibleEntries);

        vm.TraceIdFilter = "trace-1";
        Assert.Single(vm.VisibleEntries);

        vm.MinimumLevelOption = "Fatal";
        Assert.Empty(vm.VisibleEntries);

        vm.ClearStructuredFiltersCommand.Execute(null);
        Assert.Equal(2, vm.VisibleEntries.Count);
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
    public async Task ToggleAutoScroll_UpdatesSessionState()
    {
        var session = new FakeIngestionSession([]);
        var vm = new MainWindowViewModel(new ThemeService(), session, new FakeClipboardService());

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

    [Fact]
    public void SelectedEntry_WithStructuredPayload_ShowsTemplateAndPayloadInDetails()
    {
        var session = new FakeIngestionSession(
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

        var vm = new MainWindowViewModel(new ThemeService(), session, new FakeClipboardService())
        {
            SelectedEntry = session.Snapshot().First()
        };

        Assert.Contains("MessageTemplate: Payment warning for {OrderId}", vm.SelectedDetailsText);
        Assert.Contains("StructuredPayload:", vm.SelectedDetailsText);
        Assert.Contains("\"OrderId\":123", vm.SelectedDetailsText);
    }

    [Fact]
    public void ToggleDensity_UpdatesRowMetrics()
    {
        var vm = new MainWindowViewModel(new ThemeService(), new FakeIngestionSession([]), new FakeClipboardService());

        Assert.False(vm.IsCompactDensity);
        Assert.Equal("Density: Comfortable", vm.DensityButtonText);
        Assert.Equal(12, vm.LogRowFontSize);

        vm.ToggleDensityCommand.Execute(null);
        Assert.True(vm.IsCompactDensity);
        Assert.Equal("Density: Compact", vm.DensityButtonText);
        Assert.Equal(11, vm.LogRowFontSize);

        vm.ToggleDensityCommand.Execute(null);
        Assert.False(vm.IsCompactDensity);
        Assert.Equal("Density: Comfortable", vm.DensityButtonText);
        Assert.Equal(12, vm.LogRowFontSize);
    }

    [Fact]
    public async Task SaveReceiverSetup_PersistsDefinitionsAndReloadsSession()
    {
        var settings = new FakeSettingsStore();
        var session = new FakeIngestionSession([]);
        var vm = new MainWindowViewModel(new ThemeService(), session, new FakeClipboardService(), settings);

        await WaitForReceiverLoadAsync(vm);

        vm.AddUdpReceiverCommand.Execute(null);
        vm.AddTcpReceiverCommand.Execute(null);
        vm.AddFileReceiverCommand.Execute(null);

        var fileReceiver = vm.ReceiverDefinitions.Single(x => x.IsFile);
        fileReceiver.FilePath = "C:/logs/app.log";
        var udpReceiver = vm.ReceiverDefinitions.Single(x => x.IsUdp);
        udpReceiver.ParserOrderText = "JsonLogParser, PlainTextParser";

        await ((IAsyncRelayCommand)vm.SaveReceiverSetupCommand).ExecuteAsync(null);

        var saved = settings.LastSavedReceiverDefinitions;
        Assert.NotNull(saved);
        Assert.Single(saved!.UdpReceivers);
        Assert.Single(saved.TcpReceivers);
        Assert.Single(saved.FileTailReceivers);
        Assert.Equal("C:/logs/app.log", saved.FileTailReceivers[0].FilePath);
        Assert.Equal(["JsonLogParser", "PlainTextParser"], saved.UdpReceivers[0].ParserOrder);
        Assert.Equal(1, session.ReloadReceiversCalls);
    }

    [Fact]
    public async Task SaveReceiverSetup_InvalidConfiguration_DoesNotPersistOrReload()
    {
        var settings = new FakeSettingsStore();
        var session = new FakeIngestionSession([]);
        var vm = new MainWindowViewModel(new ThemeService(), session, new FakeClipboardService(), settings);

        await WaitForReceiverLoadAsync(vm);

        vm.AddUdpReceiverCommand.Execute(null);
        var udp = vm.ReceiverDefinitions.Single(x => x.IsUdp);
        udp.Port = 70_000;

        await ((IAsyncRelayCommand)vm.SaveReceiverSetupCommand).ExecuteAsync(null);

        Assert.Null(settings.LastSavedReceiverDefinitions);
        Assert.Equal(0, session.ReloadReceiversCalls);
        Assert.Contains("Validation failed", vm.ReceiverSetupStatus);
    }

    [Fact]
    public async Task ReloadReceiverSetup_LoadsDefinitionsFromSettings()
    {
        var settings = new FakeSettingsStore
        {
            ReceiverDefinitions = new ReceiverDefinitions
            {
                UdpReceivers =
                [
                    new UdpReceiverDefinition
                    {
                        Id = "udp-prod",
                        DisplayName = "UDP Prod",
                        BindAddress = "127.0.0.1",
                        Port = 17071,
                        ParserOrder = ["JsonLogParser", "PlainTextParser"]
                    }
                ],
                FileTailReceivers =
                [
                    new FileTailReceiverDefinition
                    {
                        Id = "file-prod",
                        DisplayName = "File Prod",
                        FilePath = "C:/logs/prod.log",
                        PollIntervalMs = 250
                    }
                ]
            }
        };

        var session = new FakeIngestionSession([]);
        var vm = new MainWindowViewModel(new ThemeService(), session, new FakeClipboardService(), settings);

        await ((IAsyncRelayCommand)vm.ReloadReceiverSetupCommand).ExecuteAsync(null);

        Assert.Equal(2, vm.ReceiverDefinitions.Count);
        Assert.Contains(vm.ReceiverDefinitions, x => x.Id == "udp-prod" && x.IsUdp && x.Port == 17071);
        Assert.Contains(vm.ReceiverDefinitions, x => x.Id == "file-prod" && x.IsFile && x.PollIntervalMs == 250);
        Assert.Contains(vm.ReceiverDefinitions, x => x.Id == "udp-prod" && x.ParserOrderText.Contains("JsonLogParser"));
        Assert.Equal(1, session.ReloadReceiversCalls);
    }

    [Fact]
    public async Task WorkspaceState_RestoresSelectedReceiverFromSettings()
    {
        var settings = new FakeSettingsStore
        {
            WorkspaceSettings = new WorkspaceSettings { SelectedReceiverId = "tcp-prod" },
            ReceiverDefinitions = new ReceiverDefinitions
            {
                UdpReceivers = [new UdpReceiverDefinition { Id = "udp-prod", DisplayName = "UDP Prod" }],
                TcpReceivers = [new TcpReceiverDefinition { Id = "tcp-prod", DisplayName = "TCP Prod" }]
            }
        };

        var vm = new MainWindowViewModel(new ThemeService(), new FakeIngestionSession([]), new FakeClipboardService(), settings);

        await WaitForConditionAsync(() => vm.SelectedReceiverDefinition is not null);

        Assert.NotNull(vm.SelectedReceiverDefinition);
        Assert.Equal("tcp-prod", vm.SelectedReceiverDefinition!.Id);
    }

    [Fact]
    public async Task WorkspaceState_PersistsFiltersDensityAndSelectedReceiver()
    {
        var settings = new FakeSettingsStore
        {
            ReceiverDefinitions = new ReceiverDefinitions
            {
                UdpReceivers = [new UdpReceiverDefinition { Id = "udp-a", DisplayName = "UDP A" }],
                TcpReceivers = [new TcpReceiverDefinition { Id = "tcp-b", DisplayName = "TCP B" }]
            }
        };

        var vm = new MainWindowViewModel(new ThemeService(), new FakeIngestionSession([]), new FakeClipboardService(), settings);
        await WaitForConditionAsync(() => vm.ReceiverDefinitions.Count == 2);

        vm.SearchText = "gateway";
        vm.ReceiverFilter = "udp-a";
        vm.LoggerFilter = "Orders.Api";
        vm.ThreadFilter = "worker-9";
        vm.TenantFilter = "alpha";
        vm.TraceIdFilter = "trace-xyz";
        vm.MinimumLevelOption = "Warn";
        vm.IsCompactDensity = true;
        vm.SelectedReceiverDefinition = vm.ReceiverDefinitions.Single(x => x.Id == "tcp-b");

        await WaitForConditionAsync(() => settings.LastSavedWorkspaceSettings is not null);

        var saved = settings.LastSavedWorkspaceSettings!;
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
        var settings = new FakeSettingsStore();
        var vm = new MainWindowViewModel(new ThemeService(), new FakeIngestionSession([]), new FakeClipboardService(), settings);

        vm.TimestampColumnWidth = 210;
        vm.LevelColumnWidth = 130;
        vm.LoggerColumnWidth = 260;

        await WaitForConditionAsync(() => settings.LastSavedUiLayoutSettings is not null);

        Assert.NotNull(settings.LastSavedUiLayoutSettings);
        Assert.Equal(210, settings.LastSavedUiLayoutSettings!.TimestampColumnWidth);
        Assert.Equal(130, settings.LastSavedUiLayoutSettings.LevelColumnWidth);
        Assert.Equal(260, settings.LastSavedUiLayoutSettings.LoggerColumnWidth);
    }

    private static async Task WaitForReceiverLoadAsync(MainWindowViewModel vm)
    {
        var attempt = 0;
        while (attempt < 25 && vm.ReceiverSetupStatus.Length == 0 && vm.ReceiverDefinitions.Count == 0)
        {
            await Task.Delay(10);
            attempt++;
        }
    }

    private static async Task WaitForConditionAsync(Func<bool> condition)
    {
        var attempt = 0;
        while (attempt < 60 && !condition())
        {
            await Task.Delay(20);
            attempt++;
        }

        Assert.True(condition());
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

        public bool IsAutoScrollEnabled { get; private set; } = true;

        public int ReloadReceiversCalls { get; private set; }

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

        public ValueTask SetAutoScrollAsync(bool isEnabled, CancellationToken cancellationToken = default)
        {
            IsAutoScrollEnabled = isEnabled;
            return ValueTask.CompletedTask;
        }

        public ValueTask ReloadReceiversAsync(CancellationToken cancellationToken = default)
        {
            ReloadReceiversCalls++;
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

    private sealed class FakeSettingsStore : ISettingsStore
    {
        public ReceiverDefinitions ReceiverDefinitions { get; set; } = new();

        public WorkspaceSettings WorkspaceSettings { get; set; } = new();

        public UiLayoutSettings UiLayoutSettings { get; set; } = new();

        public ReceiverDefinitions? LastSavedReceiverDefinitions { get; private set; }

        public WorkspaceSettings? LastSavedWorkspaceSettings { get; private set; }

        public UiLayoutSettings? LastSavedUiLayoutSettings { get; private set; }

        public ValueTask<AppSettings> LoadAppSettingsAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new AppSettings());
        }

        public ValueTask<ReceiverDefinitions> LoadReceiverDefinitionsAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(ReceiverDefinitions);
        }

        public ValueTask<WorkspaceSettings> LoadWorkspaceSettingsAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(WorkspaceSettings);
        }

        public ValueTask<UiLayoutSettings> LoadUiLayoutSettingsAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(UiLayoutSettings);
        }

        public ValueTask SaveAppSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask SaveReceiverDefinitionsAsync(ReceiverDefinitions settings, CancellationToken cancellationToken = default)
        {
            ReceiverDefinitions = settings;
            LastSavedReceiverDefinitions = settings;
            return ValueTask.CompletedTask;
        }

        public ValueTask SaveWorkspaceSettingsAsync(WorkspaceSettings settings, CancellationToken cancellationToken = default)
        {
            WorkspaceSettings = settings;
            LastSavedWorkspaceSettings = settings;
            return ValueTask.CompletedTask;
        }

        public ValueTask SaveUiLayoutSettingsAsync(UiLayoutSettings settings, CancellationToken cancellationToken = default)
        {
            UiLayoutSettings = settings;
            LastSavedUiLayoutSettings = settings;
            return ValueTask.CompletedTask;
        }
    }
}
