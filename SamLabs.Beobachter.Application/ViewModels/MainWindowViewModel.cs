using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Application.Services;
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

    private readonly IThemeService _themeService;
    private readonly IIngestionSession _ingestionSession;
    private readonly ISettingsStore _settingsStore;
    private readonly ILogQueryEvaluator _queryEvaluator = new LogQueryEvaluator();
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

    public string LogColumnDefinitions => Stream.LogColumnDefinitions;

    public MainWindowViewModel() : this(
        new ThemeService(),
        new DesignIngestionSession(),
        new NullClipboardService(),
        new DesignSettingsStore(),
        new RollingLogStatisticsService())
    {
    }

    public MainWindowViewModel(
        IThemeService themeService,
        IIngestionSession ingestionSession,
        IClipboardService? clipboardService = null,
        ISettingsStore? settingsStore = null,
        ILogStatisticsService? statisticsService = null)
    {
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _ingestionSession = ingestionSession ?? throw new ArgumentNullException(nameof(ingestionSession));
        _settingsStore = settingsStore ?? new DesignSettingsStore();
        _statisticsService = statisticsService ?? new RollingLogStatisticsService();
        IClipboardService resolvedClipboardService = clipboardService ?? new NullClipboardService();
        Filters = new LogFiltersViewModel();
        Filters.PropertyChanged += OnFiltersPropertyChanged;
        Sources = new SourceTreeViewModel();
        Sources.StateChanged += OnSourcesStateChanged;
        ReceiverSetup = new ReceiverSetupViewModel(_settingsStore, _ingestionSession);
        ReceiverSetup.PropertyChanged += OnReceiverSetupPropertyChanged;
        Details = new EntryDetailsViewModel(resolvedClipboardService);
        Details.PropertyChanged += OnDetailsPropertyChanged;
        Stream = new LogStreamViewModel();
        Stream.PropertyChanged += OnStreamPropertyChanged;

        _ingestionSession.EntriesAppended += OnEntriesAppended;
        IsPaused = _ingestionSession.IsPaused;
        IsAutoScrollEnabled = _ingestionSession.IsAutoScrollEnabled;
        _statisticsService.RecordRange(_ingestionSession.Snapshot());
        Sources.RebuildFromSnapshot(_ingestionSession.Snapshot());
        RebuildVisibleEntries();
        UpdateThemeSummary();
        UpdateStatusSummary();
        UpdateStatisticsSummary();
        _ = LoadReceiverSetupAsync();
        _ = LoadWorkspaceStateAsync();
    }

    public SourceTreeViewModel Sources { get; }

    public ObservableCollection<LoggerTreeItemViewModel> LoggerTreeItems => Sources.LoggerTreeItems;

    public IRelayCommand EnableAllLoggersCommand => Sources.EnableAllLoggersCommand;

    public ReceiverSetupViewModel ReceiverSetup { get; }

    public ObservableCollection<ReceiverDefinitionViewModel> ReceiverDefinitions => ReceiverSetup.ReceiverDefinitions;

    public ReceiverDefinitionViewModel? SelectedReceiverDefinition
    {
        get => ReceiverSetup.SelectedReceiverDefinition;
        set
        {
            if (ReferenceEquals(ReceiverSetup.SelectedReceiverDefinition, value))
            {
                return;
            }

            ReceiverSetup.SelectedReceiverDefinition = value;
            OnPropertyChanged();
        }
    }

    public string ReceiverSetupStatus => ReceiverSetup.ReceiverSetupStatus;

    public IRelayCommand AddUdpReceiverCommand => ReceiverSetup.AddUdpReceiverCommand;

    public IRelayCommand AddTcpReceiverCommand => ReceiverSetup.AddTcpReceiverCommand;

    public IRelayCommand AddFileReceiverCommand => ReceiverSetup.AddFileReceiverCommand;

    public IRelayCommand RemoveSelectedReceiverCommand => ReceiverSetup.RemoveSelectedReceiverCommand;

    public IAsyncRelayCommand SaveReceiverSetupCommand => ReceiverSetup.SaveReceiverSetupCommand;

    public IAsyncRelayCommand ReloadReceiverSetupCommand => ReceiverSetup.ReloadReceiverSetupCommand;

    public LogFiltersViewModel Filters { get; }

    public string SearchText
    {
        get => Filters.SearchText;
        set
        {
            if (string.Equals(Filters.SearchText, value, StringComparison.Ordinal))
            {
                return;
            }

            Filters.SearchText = value;
            OnPropertyChanged();
        }
    }

    public string ReceiverFilter
    {
        get => Filters.ReceiverFilter;
        set
        {
            if (string.Equals(Filters.ReceiverFilter, value, StringComparison.Ordinal))
            {
                return;
            }

            Filters.ReceiverFilter = value;
            OnPropertyChanged();
        }
    }

    public string LoggerFilter
    {
        get => Filters.LoggerFilter;
        set
        {
            if (string.Equals(Filters.LoggerFilter, value, StringComparison.Ordinal))
            {
                return;
            }

            Filters.LoggerFilter = value;
            OnPropertyChanged();
        }
    }

    public string ThreadFilter
    {
        get => Filters.ThreadFilter;
        set
        {
            if (string.Equals(Filters.ThreadFilter, value, StringComparison.Ordinal))
            {
                return;
            }

            Filters.ThreadFilter = value;
            OnPropertyChanged();
        }
    }

    public string TenantFilter
    {
        get => Filters.TenantFilter;
        set
        {
            if (string.Equals(Filters.TenantFilter, value, StringComparison.Ordinal))
            {
                return;
            }

            Filters.TenantFilter = value;
            OnPropertyChanged();
        }
    }

    public string TraceIdFilter
    {
        get => Filters.TraceIdFilter;
        set
        {
            if (string.Equals(Filters.TraceIdFilter, value, StringComparison.Ordinal))
            {
                return;
            }

            Filters.TraceIdFilter = value;
            OnPropertyChanged();
        }
    }

    public string MinimumLevelOption
    {
        get => Filters.MinimumLevelOption;
        set
        {
            if (string.Equals(Filters.MinimumLevelOption, value, StringComparison.Ordinal))
            {
                return;
            }

            Filters.MinimumLevelOption = value;
            OnPropertyChanged();
        }
    }

    public bool ShowTrace
    {
        get => Filters.ShowTrace;
        set
        {
            if (Filters.ShowTrace == value)
            {
                return;
            }

            Filters.ShowTrace = value;
            OnPropertyChanged();
        }
    }

    public bool ShowDebug
    {
        get => Filters.ShowDebug;
        set
        {
            if (Filters.ShowDebug == value)
            {
                return;
            }

            Filters.ShowDebug = value;
            OnPropertyChanged();
        }
    }

    public bool ShowInfo
    {
        get => Filters.ShowInfo;
        set
        {
            if (Filters.ShowInfo == value)
            {
                return;
            }

            Filters.ShowInfo = value;
            OnPropertyChanged();
        }
    }

    public bool ShowWarn
    {
        get => Filters.ShowWarn;
        set
        {
            if (Filters.ShowWarn == value)
            {
                return;
            }

            Filters.ShowWarn = value;
            OnPropertyChanged();
        }
    }

    public bool ShowError
    {
        get => Filters.ShowError;
        set
        {
            if (Filters.ShowError == value)
            {
                return;
            }

            Filters.ShowError = value;
            OnPropertyChanged();
        }
    }

    public bool ShowFatal
    {
        get => Filters.ShowFatal;
        set
        {
            if (Filters.ShowFatal == value)
            {
                return;
            }

            Filters.ShowFatal = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<string> MinimumLevelOptions => Filters.MinimumLevelOptions;

    public IRelayCommand ClearSearchCommand => Filters.ClearSearchCommand;

    public IRelayCommand ClearStructuredFiltersCommand => Filters.ClearStructuredFiltersCommand;

    public IRelayCommand ResetLevelsCommand => Filters.ResetLevelsCommand;

    public LogStreamViewModel Stream { get; }

    public ObservableCollection<LogEntry> VisibleEntries => Stream.VisibleEntries;

    public bool IsCompactDensity
    {
        get => Stream.IsCompactDensity;
        set
        {
            if (Stream.IsCompactDensity == value)
            {
                return;
            }

            Stream.IsCompactDensity = value;
            OnPropertyChanged();
        }
    }

    public string DensityButtonText => Stream.DensityButtonText;

    public double LogRowFontSize => Stream.LogRowFontSize;

    public Thickness LogRowMargin => Stream.LogRowMargin;

    public double TimestampColumnWidth
    {
        get => Stream.TimestampColumnWidth;
        set
        {
            if (Math.Abs(Stream.TimestampColumnWidth - value) < 0.001)
            {
                return;
            }

            Stream.TimestampColumnWidth = value;
            OnPropertyChanged();
        }
    }

    public double LevelColumnWidth
    {
        get => Stream.LevelColumnWidth;
        set
        {
            if (Math.Abs(Stream.LevelColumnWidth - value) < 0.001)
            {
                return;
            }

            Stream.LevelColumnWidth = value;
            OnPropertyChanged();
        }
    }

    public double LoggerColumnWidth
    {
        get => Stream.LoggerColumnWidth;
        set
        {
            if (Math.Abs(Stream.LoggerColumnWidth - value) < 0.001)
            {
                return;
            }

            Stream.LoggerColumnWidth = value;
            OnPropertyChanged();
        }
    }

    public IRelayCommand ToggleDensityCommand => Stream.ToggleDensityCommand;

    public IRelayCommand DecreaseColumnWidthsCommand => Stream.DecreaseColumnWidthsCommand;

    public IRelayCommand IncreaseColumnWidthsCommand => Stream.IncreaseColumnWidthsCommand;

    public IRelayCommand ResetColumnWidthsCommand => Stream.ResetColumnWidthsCommand;

    public EntryDetailsViewModel Details { get; }

    public LogEntry? SelectedEntry
    {
        get => Stream.SelectedEntry;
        set
        {
            if (ReferenceEquals(Stream.SelectedEntry, value))
            {
                return;
            }

            Stream.SelectedEntry = value;
            OnPropertyChanged();
        }
    }

    public string SelectedDetailsText => Details.SelectedDetailsText;

    public string CopyStatus => Details.CopyStatus;

    public IAsyncRelayCommand CopySelectedMessageCommand => Details.CopySelectedMessageCommand;

    public IAsyncRelayCommand CopySelectedDetailsCommand => Details.CopySelectedDetailsCommand;

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
        if (e.PropertyName is null)
        {
            OnPropertyChanged(nameof(SearchText));
            OnPropertyChanged(nameof(ReceiverFilter));
            OnPropertyChanged(nameof(LoggerFilter));
            OnPropertyChanged(nameof(ThreadFilter));
            OnPropertyChanged(nameof(TenantFilter));
            OnPropertyChanged(nameof(TraceIdFilter));
            OnPropertyChanged(nameof(MinimumLevelOption));
            OnPropertyChanged(nameof(ShowTrace));
            OnPropertyChanged(nameof(ShowDebug));
            OnPropertyChanged(nameof(ShowInfo));
            OnPropertyChanged(nameof(ShowWarn));
            OnPropertyChanged(nameof(ShowError));
            OnPropertyChanged(nameof(ShowFatal));
            OnFiltersChanged();
            return;
        }

        if (FilterCriteriaPropertyNames.Contains(e.PropertyName))
        {
            OnPropertyChanged(e.PropertyName);
            OnFiltersChanged();
        }
    }

    private void OnReceiverSetupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null ||
            string.Equals(e.PropertyName, nameof(ReceiverSetupViewModel.SelectedReceiverDefinition), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(SelectedReceiverDefinition));
            QueuePersistWorkspaceState();
        }

        if (e.PropertyName is null ||
            string.Equals(e.PropertyName, nameof(ReceiverSetupViewModel.ReceiverSetupStatus), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(ReceiverSetupStatus));
        }
    }

    private void OnDetailsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null)
        {
            OnPropertyChanged(nameof(SelectedDetailsText));
            OnPropertyChanged(nameof(CopyStatus));
            return;
        }

        if (string.Equals(e.PropertyName, nameof(EntryDetailsViewModel.SelectedDetailsText), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(SelectedDetailsText));
            return;
        }

        if (string.Equals(e.PropertyName, nameof(EntryDetailsViewModel.CopyStatus), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(CopyStatus));
        }
    }

    private void OnStreamPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null)
        {
            Details.SelectedEntry = Stream.SelectedEntry;
            OnPropertyChanged(nameof(SelectedEntry));
            OnPropertyChanged(nameof(IsCompactDensity));
            OnPropertyChanged(nameof(DensityButtonText));
            OnPropertyChanged(nameof(LogRowFontSize));
            OnPropertyChanged(nameof(LogRowMargin));
            OnPropertyChanged(nameof(TimestampColumnWidth));
            OnPropertyChanged(nameof(LevelColumnWidth));
            OnPropertyChanged(nameof(LoggerColumnWidth));
            OnPropertyChanged(nameof(LogColumnDefinitions));
            QueuePersistWorkspaceState();
            return;
        }

        if (string.Equals(e.PropertyName, nameof(LogStreamViewModel.SelectedEntry), StringComparison.Ordinal))
        {
            Details.SelectedEntry = Stream.SelectedEntry;
            OnPropertyChanged(nameof(SelectedEntry));
            return;
        }

        if (string.Equals(e.PropertyName, nameof(LogStreamViewModel.IsCompactDensity), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(IsCompactDensity));
            QueuePersistWorkspaceState();
            return;
        }

        if (string.Equals(e.PropertyName, nameof(LogStreamViewModel.DensityButtonText), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(DensityButtonText));
            return;
        }

        if (string.Equals(e.PropertyName, nameof(LogStreamViewModel.LogRowFontSize), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(LogRowFontSize));
            return;
        }

        if (string.Equals(e.PropertyName, nameof(LogStreamViewModel.LogRowMargin), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(LogRowMargin));
            return;
        }

        if (string.Equals(e.PropertyName, nameof(LogStreamViewModel.TimestampColumnWidth), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(TimestampColumnWidth));
            OnPropertyChanged(nameof(LogColumnDefinitions));
            QueuePersistWorkspaceState();
            return;
        }

        if (string.Equals(e.PropertyName, nameof(LogStreamViewModel.LevelColumnWidth), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(LevelColumnWidth));
            OnPropertyChanged(nameof(LogColumnDefinitions));
            QueuePersistWorkspaceState();
            return;
        }

        if (string.Equals(e.PropertyName, nameof(LogStreamViewModel.LoggerColumnWidth), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(LoggerColumnWidth));
            OnPropertyChanged(nameof(LogColumnDefinitions));
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
        StatusSummary = $"State: {state}  Pin: {pin}  Total: {_ingestionSession.TotalCount}  Visible: {VisibleEntries.Count}  Dropped: {dropped}";
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
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(ApplyPendingReceiverSelection);
    }

    private void ApplyPendingReceiverSelection()
    {
        if (string.IsNullOrWhiteSpace(_pendingSelectedReceiverId) || ReceiverDefinitions.Count == 0)
        {
            return;
        }

        ReceiverSetup.TrySelectReceiverById(_pendingSelectedReceiverId);
        SelectedReceiverDefinition ??= ReceiverDefinitions.FirstOrDefault();
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
            SearchText = workspace.SearchText;
            ReceiverFilter = workspace.ReceiverFilter;
            LoggerFilter = workspace.LoggerFilter;
            ThreadFilter = workspace.ThreadFilter;
            TenantFilter = workspace.TenantFilter;
            TraceIdFilter = workspace.TraceIdFilter;
            MinimumLevelOption = string.IsNullOrWhiteSpace(workspace.MinimumLevelOption) ? "Any" : workspace.MinimumLevelOption;
            IsCompactDensity = workspace.CompactDensity;
            TimestampColumnWidth = ClampColumnWidth(layout.TimestampColumnWidth, 100, 420);
            LevelColumnWidth = ClampColumnWidth(layout.LevelColumnWidth, 70, 200);
            LoggerColumnWidth = ClampColumnWidth(layout.LoggerColumnWidth, 120, 520);

            var enabled = new HashSet<string>(workspace.EnabledLevels, StringComparer.OrdinalIgnoreCase);
            ShowTrace = enabled.Contains(nameof(LogLevel.Trace));
            ShowDebug = enabled.Contains(nameof(LogLevel.Debug));
            ShowInfo = enabled.Contains(nameof(LogLevel.Info));
            ShowWarn = enabled.Contains(nameof(LogLevel.Warn));
            ShowError = enabled.Contains(nameof(LogLevel.Error));
            ShowFatal = enabled.Contains(nameof(LogLevel.Fatal));
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
            SearchText = SearchText,
            ReceiverFilter = ReceiverFilter,
            LoggerFilter = LoggerFilter,
            ThreadFilter = ThreadFilter,
            TenantFilter = TenantFilter,
            TraceIdFilter = TraceIdFilter,
            MinimumLevelOption = MinimumLevelOption,
            CompactDensity = IsCompactDensity,
            SelectedReceiverId = SelectedReceiverDefinition?.Id ?? string.Empty,
            EnabledLevels = Filters.GetEnabledLevels(),
            AutoScroll = IsAutoScrollEnabled,
            PauseIngest = IsPaused
        };
    }

    private UiLayoutSettings BuildUiLayoutSettingsSnapshot()
    {
        return _uiLayoutSettings with
        {
            TimestampColumnWidth = TimestampColumnWidth,
            LevelColumnWidth = LevelColumnWidth,
            LoggerColumnWidth = LoggerColumnWidth
        };
    }
}
