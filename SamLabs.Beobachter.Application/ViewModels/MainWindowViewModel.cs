using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Application.ViewModels.Sources;
using SamLabs.Beobachter.Application.ViewModels.Status;
using SamLabs.Beobachter.Application.ViewModels.Toolbar;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Queries;
using SamLabs.Beobachter.Core.Services;
using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Application.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly HashSet<string> FilterCriteriaPropertyNames = new(StringComparer.Ordinal)
    {
        nameof(LogFiltersViewModel.SearchText),
        nameof(LogFiltersViewModel.ReceiverFilter),
        nameof(LogFiltersViewModel.LoggerFilter),
        nameof(LogFiltersViewModel.ThreadFilter),
        nameof(LogFiltersViewModel.TenantFilter),
        nameof(LogFiltersViewModel.TraceIdFilter),
        nameof(LogFiltersViewModel.MinimumLevelOption),
        nameof(LogFiltersViewModel.ShowTrace),
        nameof(LogFiltersViewModel.ShowDebug),
        nameof(LogFiltersViewModel.ShowInfo),
        nameof(LogFiltersViewModel.ShowWarn),
        nameof(LogFiltersViewModel.ShowError),
        nameof(LogFiltersViewModel.ShowFatal)
    };

    private static readonly HashSet<string> QuickFilterCriteriaPropertyNames = new(StringComparer.Ordinal)
    {
        nameof(QuickFiltersViewModel.IsErrorsAndAboveEnabled),
        nameof(QuickFiltersViewModel.IsStructuredOnlyEnabled)
    };

    private readonly IThemeService _themeService;
    private readonly IIngestionSession _ingestionSession;
    private readonly ISettingsStore _settingsStore;
    private readonly ILogQueryEvaluator _queryEvaluator;
    private readonly ILogStatisticsService _statisticsService;
    private readonly Random _random = new();
    private WorkspaceSettings _workspaceSettings = new();
    private UiLayoutSettings _uiLayoutSettings = new();
    private CancellationTokenSource? _persistStateCts;
    private string? _pendingSelectedReceiverId;
    private bool _isApplyingWorkspaceState;

    [ObservableProperty]
    private string _themeSummary = string.Empty;

    [ObservableProperty]
    private string _statusSummary = string.Empty;

    [ObservableProperty]
    private string _statsSummary1Minute = "1m: 0 logs/s | 0 err/s";

    [ObservableProperty]
    private string _statsSummary5Minutes = "5m: 0 logs/s | 0 err/s";

    [ObservableProperty]
    private string _topLoggersSummary = "Top loggers (5m): -";

    [ObservableProperty]
    private string _topReceiversSummary = "Top receivers (5m): -";

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private string _pauseButtonText = "Pause";

    [ObservableProperty]
    private bool _isAutoScrollEnabled = true;

    [ObservableProperty]
    private string _autoScrollButtonText = "Pin: On";

    [Obsolete("Design-time constructor only. Use the DI constructor for runtime composition.")]
    public MainWindowViewModel() : this(
        new ThemeService(),
        new DesignIngestionSession(),
        new DesignSettingsStore(),
        new RollingLogStatisticsService(),
        new LogQueryEvaluator(),
        new SourceTreeViewModel(),
        new QuickFiltersViewModel(),
        new ReceiverSetupViewModel(new DesignSettingsStore(), new DesignIngestionSession()),
        new LogFiltersViewModel(),
        new LogStreamViewModel(),
        new EntryDetailsViewModel(new NullClipboardService()),
        new SessionHealthViewModel())
    {
    }

    public MainWindowViewModel(
        IThemeService themeService,
        IIngestionSession ingestionSession,
        ISettingsStore settingsStore,
        ILogStatisticsService statisticsService,
        ILogQueryEvaluator queryEvaluator,
        SourceTreeViewModel sources,
        QuickFiltersViewModel quickFilters,
        ReceiverSetupViewModel receiverSetup,
        LogFiltersViewModel filters,
        LogStreamViewModel stream,
        EntryDetailsViewModel details,
        SessionHealthViewModel sessionHealth)
    {
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _ingestionSession = ingestionSession ?? throw new ArgumentNullException(nameof(ingestionSession));
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _queryEvaluator = queryEvaluator ?? throw new ArgumentNullException(nameof(queryEvaluator));

        Sources = sources ?? throw new ArgumentNullException(nameof(sources));
        QuickFilters = quickFilters ?? throw new ArgumentNullException(nameof(quickFilters));
        ReceiverSetup = receiverSetup ?? throw new ArgumentNullException(nameof(receiverSetup));
        Filters = filters ?? throw new ArgumentNullException(nameof(filters));
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        Details = details ?? throw new ArgumentNullException(nameof(details));
        SessionHealth = sessionHealth ?? throw new ArgumentNullException(nameof(sessionHealth));

        Filters.PropertyChanged += OnFiltersPropertyChanged;
        Sources.StateChanged += OnSourcesStateChanged;
        QuickFilters.PropertyChanged += OnQuickFiltersPropertyChanged;
        ReceiverSetup.PropertyChanged += OnReceiverSetupPropertyChanged;
        Stream.PropertyChanged += OnStreamPropertyChanged;
        Toolbar = new MainToolbarViewModel(this);

        _ingestionSession.EntriesAppended += OnEntriesAppended;
        IsPaused = _ingestionSession.IsPaused;
        IsAutoScrollEnabled = _ingestionSession.IsAutoScrollEnabled;
        _statisticsService.RecordRange(_ingestionSession.Snapshot());
        Sources.RebuildFromSnapshot(_ingestionSession.Snapshot());
        RebuildVisibleEntries();
        UpdateThemeSummary();
        UpdateStatusSummary();
        UpdateStatisticsSummary();
        UpdateQuickFiltersSnapshot();
        UpdateSessionHealthSummary();
        _ = LoadReceiverSetupAsync();
        _ = LoadWorkspaceStateAsync();
    }

    public MainToolbarViewModel Toolbar { get; }

    public SourceTreeViewModel Sources { get; }

    public QuickFiltersViewModel QuickFilters { get; }

    public ReceiverSetupViewModel ReceiverSetup { get; }

    public LogFiltersViewModel Filters { get; }

    public LogStreamViewModel Stream { get; }

    public EntryDetailsViewModel Details { get; }

    public SessionHealthViewModel SessionHealth { get; }

    [RelayCommand]
    private void UseSystemTheme()
    {
        _themeService.SetTheme(AppThemeMode.System);
        UpdateThemeSummary();
    }

    [RelayCommand]
    private void UseLightTheme()
    {
        _themeService.SetTheme(AppThemeMode.Light);
        UpdateThemeSummary();
    }

    [RelayCommand]
    private void UseDarkTheme()
    {
        _themeService.SetTheme(AppThemeMode.Dark);
        UpdateThemeSummary();
    }

    [RelayCommand]
    private void GenerateSampleEntries()
    {
        var now = DateTimeOffset.Now;
        for (var i = 0; i < 12; i++)
        {
            var level = PickRandomLevel();
            var index = i + 1;

            var entry = new LogEntry
            {
                Timestamp = now.AddMilliseconds(index * 10),
                SequenceNumber = _ingestionSession.TotalCount + index,
                Level = level,
                ReceiverId = "sample",
                LoggerName = $"Sample.Component.{_random.Next(1, 5)}",
                RootLoggerName = $"Sample.Component.{_random.Next(1, 5)}",
                ThreadName = $"worker-{_random.Next(1, 4)}",
                Message = $"Sample {level} event #{_random.Next(100, 999)}"
            };

            _ingestionSession.TryPublish(entry);
        }

        UpdateStatusSummary();
    }

    [RelayCommand]
    private async Task TogglePauseAsync()
    {
        var nextState = !IsPaused;
        await _ingestionSession.SetPausedAsync(nextState).ConfigureAwait(false);
        IsPaused = nextState;
        Dispatcher.UIThread.Post(UpdateStatusSummary);
    }

    [RelayCommand]
    private async Task ToggleAutoScrollAsync()
    {
        var nextState = !IsAutoScrollEnabled;
        await _ingestionSession.SetAutoScrollAsync(nextState).ConfigureAwait(false);
        IsAutoScrollEnabled = nextState;
        Dispatcher.UIThread.Post(UpdateStatusSummary);
    }

    partial void OnIsPausedChanged(bool value)
    {
        PauseButtonText = value ? "Resume" : "Pause";
        UpdateStatusSummary();
    }

    partial void OnIsAutoScrollEnabledChanged(bool value)
    {
        AutoScrollButtonText = value ? "Pin: On" : "Pin: Off";
        UpdateStatusSummary();
    }

    private void OnFiltersPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null || FilterCriteriaPropertyNames.Contains(e.PropertyName))
        {
            OnFiltersChanged();
        }
    }

    private void OnReceiverSetupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null ||
            string.Equals(e.PropertyName, nameof(ReceiverSetupViewModel.SelectedReceiverDefinition), StringComparison.Ordinal))
        {
            QueuePersistWorkspaceState();
        }

        UpdateSessionHealthSummary();
    }

    private void OnStreamPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null)
        {
            Details.SelectedEntry = Stream.SelectedEntry;
            QueuePersistWorkspaceState();
            return;
        }

        if (string.Equals(e.PropertyName, nameof(LogStreamViewModel.SelectedEntry), StringComparison.Ordinal))
        {
            Details.SelectedEntry = Stream.SelectedEntry;
            return;
        }

        if (string.Equals(e.PropertyName, nameof(LogStreamViewModel.IsCompactDensity), StringComparison.Ordinal))
        {
            QueuePersistWorkspaceState();
            return;
        }

        if (string.Equals(e.PropertyName, nameof(LogStreamViewModel.TimestampColumnWidth), StringComparison.Ordinal))
        {
            QueuePersistWorkspaceState();
            return;
        }

        if (string.Equals(e.PropertyName, nameof(LogStreamViewModel.LevelColumnWidth), StringComparison.Ordinal))
        {
            QueuePersistWorkspaceState();
            return;
        }

        if (string.Equals(e.PropertyName, nameof(LogStreamViewModel.LoggerColumnWidth), StringComparison.Ordinal))
        {
            QueuePersistWorkspaceState();
        }
    }

    private void OnEntriesAppended(object? sender, LogEntriesAppendedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _statisticsService.RecordRange(e.AppendedEntries);
            LogQuery query = BuildCurrentQuery();
            foreach (LogEntry entry in e.AppendedEntries)
            {
                Sources.RegisterLogger(entry.LoggerName);
            }

            Stream.AppendEntries(e.AppendedEntries, entry => MatchesFilter(entry, query));

            UpdateStatusSummary();
            UpdateStatisticsSummary();
            UpdateQuickFiltersSnapshot();
            UpdateSessionHealthSummary();
        });
    }

    private void RebuildVisibleEntries()
    {
        IReadOnlyList<LogEntry> snapshot = _ingestionSession.Snapshot();
        LogQuery query = BuildCurrentQuery();
        Stream.RebuildEntries(snapshot, entry => MatchesFilter(entry, query));
    }

    private bool MatchesFilter(LogEntry entry, LogQuery query)
    {
        if (!IsLevelEnabled(entry.Level))
        {
            return false;
        }

        if (!IsLoggerEnabled(entry.LoggerName))
        {
            return false;
        }

        if (QuickFilters.IsErrorsAndAboveEnabled &&
            entry.Level is not LogLevel.Error and not LogLevel.Fatal)
        {
            return false;
        }

        if (QuickFilters.IsStructuredOnlyEnabled && !HasStructuredData(entry))
        {
            return false;
        }

        return _queryEvaluator.Matches(entry, query);
    }

    private bool IsLoggerEnabled(string loggerName)
    {
        return Sources.IsLoggerEnabled(loggerName);
    }

    private bool IsLevelEnabled(LogLevel level)
    {
        return Filters.IsLevelEnabled(level);
    }

    private void OnSourcesStateChanged(object? sender, EventArgs e)
    {
        RebuildVisibleEntries();
        UpdateStatusSummary();
    }

    private void OnQuickFiltersPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null || QuickFilterCriteriaPropertyNames.Contains(e.PropertyName))
        {
            RebuildVisibleEntries();
            UpdateStatusSummary();
        }
    }

    private void OnFiltersChanged()
    {
        RebuildVisibleEntries();
        UpdateStatusSummary();
        QueuePersistWorkspaceState();
    }

    private LogQuery BuildCurrentQuery()
    {
        return Filters.BuildQuery();
    }

    private static double ClampColumnWidth(double value, double min, double max)
    {
        return Math.Clamp(value, min, max);
    }

    private void UpdateThemeSummary()
    {
        ThemeSummary = $"Theme: {_themeService.CurrentMode}";
    }

    private void UpdateStatusSummary()
    {
        var dropped = _ingestionSession.DroppedCount;
        var state = IsPaused ? "Paused" : "Running";
        var pin = IsAutoScrollEnabled ? "On" : "Off";
        StatusSummary = $"State: {state}  Pin: {pin}  Total: {_ingestionSession.TotalCount}  Visible: {Stream.VisibleEntries.Count}  Dropped: {dropped}";
        UpdateSessionHealthSummary();
    }

    private void UpdateStatisticsSummary()
    {
        var snapshot = _statisticsService.GetSnapshot();
        StatsSummary1Minute = $"1m: {snapshot.LogsPerSecond1Minute:F1} logs/s | {snapshot.ErrorsPerSecond1Minute:F1} err/s";
        StatsSummary5Minutes = $"5m: {snapshot.LogsPerSecond5Minutes:F1} logs/s | {snapshot.ErrorsPerSecond5Minutes:F1} err/s";
        TopLoggersSummary = FormatTop("Top loggers (5m)", snapshot.TopLoggers);
        TopReceiversSummary = FormatTop("Top receivers (5m)", snapshot.TopReceivers);
    }

    private static string FormatTop(string label, IReadOnlyList<NamedCount> entries)
    {
        if (entries.Count == 0)
        {
            return $"{label}: -";
        }

        var summary = string.Join(", ", entries.Select(static x => $"{x.Name} ({x.Count})"));
        return $"{label}: {summary}";
    }

    private static bool HasStructuredData(LogEntry entry)
    {
        return entry.Properties.Count > 0 ||
               !string.IsNullOrWhiteSpace(entry.StructuredPayloadJson) ||
               !string.IsNullOrWhiteSpace(entry.MessageTemplate);
    }

    private void UpdateQuickFiltersSnapshot()
    {
        IReadOnlyList<LogEntry> snapshot = _ingestionSession.Snapshot();
        QuickFilters.ErrorsAndAboveCount = snapshot.Count(static entry => entry.Level is LogLevel.Error or LogLevel.Fatal);
        QuickFilters.StructuredOnlyCount = snapshot.Count(HasStructuredData);
    }

    private void UpdateSessionHealthSummary()
    {
        var activeReceivers = ReceiverSetup.ReceiverDefinitions.Count(static x => x.Enabled);
        SessionHealth.ActiveReceiversText = $"Active receivers: {activeReceivers:N0}";
        SessionHealth.BufferedEntriesText = $"Buffered entries: {_ingestionSession.TotalCount:N0}";
        SessionHealth.StructuredEventsText = $"Structured events: {QuickFilters.StructuredOnlyCount:N0}";
        SessionHealth.DroppedPacketsText = $"Dropped packets: {_ingestionSession.DroppedCount:N0}";
    }

    private LogLevel PickRandomLevel()
    {
        LogLevel[] levels =
        [
            LogLevel.Trace,
            LogLevel.Debug,
            LogLevel.Info,
            LogLevel.Warn,
            LogLevel.Error,
            LogLevel.Fatal
        ];
        return levels[_random.Next(0, levels.Length)];
    }

    private async Task LoadReceiverSetupAsync()
    {
        await ReceiverSetup.LoadAsync().ConfigureAwait(false);

        if (Avalonia.Application.Current is null || Dispatcher.UIThread.CheckAccess())
        {
            ApplyPendingReceiverSelection();
            UpdateSessionHealthSummary();
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ApplyPendingReceiverSelection();
            UpdateSessionHealthSummary();
        });
    }

    private void ApplyPendingReceiverSelection()
    {
        if (string.IsNullOrWhiteSpace(_pendingSelectedReceiverId) || ReceiverSetup.ReceiverDefinitions.Count == 0)
        {
            return;
        }

        ReceiverSetup.TrySelectReceiverById(_pendingSelectedReceiverId);
        ReceiverSetup.SelectedReceiverDefinition ??= ReceiverSetup.ReceiverDefinitions.FirstOrDefault();
        _pendingSelectedReceiverId = null;
    }

    private async Task LoadWorkspaceStateAsync()
    {
        var workspace = await _settingsStore.LoadWorkspaceSettingsAsync().ConfigureAwait(false);
        var layout = await _settingsStore.LoadUiLayoutSettingsAsync().ConfigureAwait(false);

        if (Avalonia.Application.Current is null || Dispatcher.UIThread.CheckAccess())
        {
            ApplyWorkspaceState(workspace, layout);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => ApplyWorkspaceState(workspace, layout));
    }

    private void ApplyWorkspaceState(WorkspaceSettings workspace, UiLayoutSettings layout)
    {
        _workspaceSettings = workspace;
        _uiLayoutSettings = layout;
        _pendingSelectedReceiverId = workspace.SelectedReceiverId;
        _isApplyingWorkspaceState = true;

        try
        {
            Filters.SearchText = workspace.SearchText;
            Filters.ReceiverFilter = workspace.ReceiverFilter;
            Filters.LoggerFilter = workspace.LoggerFilter;
            Filters.ThreadFilter = workspace.ThreadFilter;
            Filters.TenantFilter = workspace.TenantFilter;
            Filters.TraceIdFilter = workspace.TraceIdFilter;
            Filters.MinimumLevelOption = string.IsNullOrWhiteSpace(workspace.MinimumLevelOption) ? "Any" : workspace.MinimumLevelOption;
            Stream.IsCompactDensity = workspace.CompactDensity;
            Stream.TimestampColumnWidth = ClampColumnWidth(layout.TimestampColumnWidth, 100, 420);
            Stream.LevelColumnWidth = ClampColumnWidth(layout.LevelColumnWidth, 70, 200);
            Stream.LoggerColumnWidth = ClampColumnWidth(layout.LoggerColumnWidth, 120, 520);

            var enabled = new HashSet<string>(workspace.EnabledLevels, StringComparer.OrdinalIgnoreCase);
            Filters.ShowTrace = enabled.Contains(nameof(LogLevel.Trace));
            Filters.ShowDebug = enabled.Contains(nameof(LogLevel.Debug));
            Filters.ShowInfo = enabled.Contains(nameof(LogLevel.Info));
            Filters.ShowWarn = enabled.Contains(nameof(LogLevel.Warn));
            Filters.ShowError = enabled.Contains(nameof(LogLevel.Error));
            Filters.ShowFatal = enabled.Contains(nameof(LogLevel.Fatal));
        }
        finally
        {
            _isApplyingWorkspaceState = false;
        }

        ApplyPendingReceiverSelection();

        RebuildVisibleEntries();
        UpdateStatusSummary();
    }

    private void QueuePersistWorkspaceState()
    {
        if (_isApplyingWorkspaceState)
        {
            return;
        }

        _persistStateCts?.Cancel();
        _persistStateCts = new CancellationTokenSource();
        _ = PersistWorkspaceStateAsync(_persistStateCts.Token);
    }

    private async Task PersistWorkspaceStateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            var workspace = BuildWorkspaceSettingsSnapshot();
            var layout = BuildUiLayoutSettingsSnapshot();
            await _settingsStore.SaveWorkspaceSettingsAsync(workspace, cancellationToken).ConfigureAwait(false);
            await _settingsStore.SaveUiLayoutSettingsAsync(layout, cancellationToken).ConfigureAwait(false);
            _workspaceSettings = workspace;
            _uiLayoutSettings = layout;
        }
        catch (OperationCanceledException)
        {
            // Debounce cancellation path.
        }
    }

    private WorkspaceSettings BuildWorkspaceSettingsSnapshot()
    {
        return _workspaceSettings with
        {
            SearchText = Filters.SearchText,
            ReceiverFilter = Filters.ReceiverFilter,
            LoggerFilter = Filters.LoggerFilter,
            ThreadFilter = Filters.ThreadFilter,
            TenantFilter = Filters.TenantFilter,
            TraceIdFilter = Filters.TraceIdFilter,
            MinimumLevelOption = Filters.MinimumLevelOption,
            CompactDensity = Stream.IsCompactDensity,
            SelectedReceiverId = ReceiverSetup.SelectedReceiverDefinition?.Id ?? string.Empty,
            EnabledLevels = Filters.GetEnabledLevels(),
            AutoScroll = IsAutoScrollEnabled,
            PauseIngest = IsPaused
        };
    }

    private UiLayoutSettings BuildUiLayoutSettingsSnapshot()
    {
        return _uiLayoutSettings with
        {
            TimestampColumnWidth = Stream.TimestampColumnWidth,
            LevelColumnWidth = Stream.LevelColumnWidth,
            LoggerColumnWidth = Stream.LoggerColumnWidth
        };
    }
}
