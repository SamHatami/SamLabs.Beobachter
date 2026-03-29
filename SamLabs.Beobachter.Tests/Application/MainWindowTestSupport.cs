using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Application.ViewModels.Sources;
using SamLabs.Beobachter.Application.ViewModels.Status;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Queries;
using SamLabs.Beobachter.Core.Services;
using SamLabs.Beobachter.Core.Settings;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

internal static class MainWindowTestSupport
{
    public static MainWindowViewModel CreateMainWindowViewModel(
        IIngestionSession session,
        IClipboardService? clipboardService = null,
        ISettingsStore? settingsStore = null,
        IThemeService? themeService = null,
        ILogStatisticsService? statisticsService = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        IThemeService resolvedThemeService = themeService ?? new ThemeService();
        IClipboardService resolvedClipboardService = clipboardService ?? new FakeClipboardService();
        ISettingsStore resolvedSettingsStore = settingsStore ?? new FakeSettingsStore();
        IWorkspaceStateCoordinator workspaceStateCoordinator = new WorkspaceStateCoordinator(resolvedSettingsStore);
        IShellStatusFormatter shellStatusFormatter = new ShellStatusFormatter();
        ISampleLogEntryGenerator sampleLogEntryGenerator = new SampleLogEntryGenerator();
        ILogStreamProjectionService logStreamProjectionService = new LogStreamProjectionService(new LogQueryEvaluator());
        ILogStatisticsService resolvedStatisticsService = statisticsService ?? new RollingLogStatisticsService();
        SourceTreeViewModel sources = new();
        QuickFiltersViewModel quickFilters = new();
        ReceiverSetupViewModel receiverSetup = new(resolvedSettingsStore, session);
        WorkspaceSidebarViewModel workspaceSidebar = new(sources, quickFilters, receiverSetup);

        return new MainWindowViewModel(
            shellStatusFormatter,
            sampleLogEntryGenerator,
            resolvedThemeService,
            session,
            workspaceStateCoordinator,
            logStreamProjectionService,
            resolvedStatisticsService,
            sources,
            quickFilters,
            receiverSetup,
            workspaceSidebar,
            new LogFiltersViewModel(),
            new LogStreamViewModel(),
            new EntryDetailsViewModel(resolvedClipboardService),
            new SessionHealthViewModel());
    }

    public static LogEntry CreateEntry(string logger, LogLevel level, string message)
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

    public static IEnumerable<LoggerTreeItemViewModel> Flatten(IEnumerable<LoggerTreeItemViewModel> source)
    {
        foreach (LoggerTreeItemViewModel item in source)
        {
            yield return item;
            foreach (LoggerTreeItemViewModel child in Flatten(item.Children))
            {
                yield return child;
            }
        }
    }

    public static async Task WaitForReceiverLoadAsync(MainWindowViewModel vm)
    {
        var attempt = 0;
        while (attempt < 25 && vm.ReceiverSetup.ReceiverSetupStatus.Length == 0 && vm.ReceiverSetup.ReceiverDefinitions.Count == 0)
        {
            await Task.Delay(10);
            attempt++;
        }
    }

    public static async Task WaitForConditionAsync(Func<bool> condition)
    {
        var attempt = 0;
        while (attempt < 60 && !condition())
        {
            await Task.Delay(20);
            attempt++;
        }

        Assert.True(condition());
    }
}

internal sealed class FakeIngestionSession : IIngestionSession
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

    public ReceiverReloadResult ReloadResult { get; set; } = new();

    public IReadOnlyList<ReceiverRuntimeState> ReceiverRuntimeStates { get; set; } = [];

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

    public IReadOnlyList<ReceiverRuntimeState> GetReceiverRuntimeStates()
    {
        return ReceiverRuntimeStates;
    }

    public ValueTask<IReadOnlyList<ReceiverStartupResult>> StartAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IReadOnlyList<ReceiverStartupResult>>([]);
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

    public ValueTask<ReceiverReloadResult> ReloadReceiversAsync(CancellationToken cancellationToken = default)
    {
        ReloadReceiversCalls++;
        return ValueTask.FromResult(ReloadResult);
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

internal sealed class FakeClipboardService : IClipboardService
{
    public string LastText { get; private set; } = string.Empty;

    public ValueTask SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        LastText = text;
        return ValueTask.CompletedTask;
    }
}

internal sealed class FakeSettingsStore : ISettingsStore
{
    public AppSettings AppSettings { get; set; } = new();

    public ReceiverDefinitions ReceiverDefinitions { get; set; } = new();

    public WorkspaceSettings WorkspaceSettings { get; set; } = new();

    public UiLayoutSettings UiLayoutSettings { get; set; } = new();

    public AppSettings? LastSavedAppSettings { get; private set; }

    public ReceiverDefinitions? LastSavedReceiverDefinitions { get; private set; }

    public WorkspaceSettings? LastSavedWorkspaceSettings { get; private set; }

    public UiLayoutSettings? LastSavedUiLayoutSettings { get; private set; }

    public ValueTask<AppSettings> LoadAppSettingsAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(AppSettings);
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
        AppSettings = settings;
        LastSavedAppSettings = settings;
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
