using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
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
    private const int MaxVisibleEntries = 2_000;
    private static readonly StringComparer ParserNameComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly string[] DefaultParserOrder =
    [
        "Log4jXmlParser",
        "JsonLogParser",
        "CsvParser",
        "PlainTextParser"
    ];
    private static readonly HashSet<string> KnownParserNames = new(ParserNameComparer)
    {
        "Log4jXmlParser",
        "JsonLogParser",
        "CsvParser",
        "PlainTextParser"
    };
    private static readonly HashSet<string> ReceiverEditablePropertyNames = new(StringComparer.Ordinal)
    {
        nameof(ReceiverDefinitionViewModel.Id),
        nameof(ReceiverDefinitionViewModel.DisplayName),
        nameof(ReceiverDefinitionViewModel.Enabled),
        nameof(ReceiverDefinitionViewModel.BindAddress),
        nameof(ReceiverDefinitionViewModel.Port),
        nameof(ReceiverDefinitionViewModel.FilePath),
        nameof(ReceiverDefinitionViewModel.PollIntervalMs),
        nameof(ReceiverDefinitionViewModel.ParserOrderText)
    };

    private readonly IThemeService _themeService;
    private readonly IIngestionSession _ingestionSession;
    private readonly IClipboardService _clipboardService;
    private readonly ISettingsStore _settingsStore;
    private readonly ILogQueryEvaluator _queryEvaluator = new LogQueryEvaluator();
    private readonly ILogStatisticsService _statisticsService;
    private readonly Random _random = new();
    private LoggerNode _loggerRoot = LoggerNode.CreateRoot();
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
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _receiverFilter = string.Empty;

    [ObservableProperty]
    private string _loggerFilter = string.Empty;

    [ObservableProperty]
    private string _threadFilter = string.Empty;

    [ObservableProperty]
    private string _tenantFilter = string.Empty;

    [ObservableProperty]
    private string _traceIdFilter = string.Empty;

    [ObservableProperty]
    private string _minimumLevelOption = "Any";

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private string _pauseButtonText = "Pause";

    [ObservableProperty]
    private bool _isAutoScrollEnabled = true;

    [ObservableProperty]
    private string _autoScrollButtonText = "Pin: On";

    [ObservableProperty]
    private bool _isCompactDensity;

    [ObservableProperty]
    private string _densityButtonText = "Density: Comfortable";

    [ObservableProperty]
    private double _logRowFontSize = 12;

    [ObservableProperty]
    private Thickness _logRowMargin = new(4, 2, 4, 2);

    [ObservableProperty]
    private double _timestampColumnWidth = 180;

    [ObservableProperty]
    private double _levelColumnWidth = 90;

    [ObservableProperty]
    private double _loggerColumnWidth = 220;

    [ObservableProperty]
    private bool _showTrace = true;

    [ObservableProperty]
    private bool _showDebug = true;

    [ObservableProperty]
    private bool _showInfo = true;

    [ObservableProperty]
    private bool _showWarn = true;

    [ObservableProperty]
    private bool _showError = true;

    [ObservableProperty]
    private bool _showFatal = true;

    [ObservableProperty]
    private LogEntry? _selectedEntry;

    [ObservableProperty]
    private string _selectedDetailsText = "No entry selected.";

    [ObservableProperty]
    private string _copyStatus = string.Empty;

    [ObservableProperty]
    private ReceiverDefinitionViewModel? _selectedReceiverDefinition;

    [ObservableProperty]
    private string _receiverSetupStatus = string.Empty;

    public IReadOnlyList<string> MinimumLevelOptions { get; } =
    [
        "Any",
        nameof(LogLevel.Trace),
        nameof(LogLevel.Debug),
        nameof(LogLevel.Info),
        nameof(LogLevel.Warn),
        nameof(LogLevel.Error),
        nameof(LogLevel.Fatal)
    ];

    public string LogColumnDefinitions =>
        $"{TimestampColumnWidth:0},{LevelColumnWidth:0},{LoggerColumnWidth:0},*";

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
        _clipboardService = clipboardService ?? new NullClipboardService();
        _settingsStore = settingsStore ?? new DesignSettingsStore();
        _statisticsService = statisticsService ?? new RollingLogStatisticsService();

        _ingestionSession.EntriesAppended += OnEntriesAppended;
        IsPaused = _ingestionSession.IsPaused;
        IsAutoScrollEnabled = _ingestionSession.IsAutoScrollEnabled;
        _statisticsService.RecordRange(_ingestionSession.Snapshot());
        RebuildLoggerTreeFromSnapshot();
        RebuildVisibleEntries();
        UpdateThemeSummary();
        UpdateDensityVisuals();
        UpdateStatusSummary();
        UpdateStatisticsSummary();
        _ = LoadReceiverDefinitionsAsync();
        _ = LoadWorkspaceStateAsync();
    }

    public ObservableCollection<LogEntry> VisibleEntries { get; } = [];

    public ObservableCollection<LoggerTreeItemViewModel> LoggerTreeItems { get; } = [];

    public ObservableCollection<ReceiverDefinitionViewModel> ReceiverDefinitions { get; } = [];

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
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void ClearStructuredFilters()
    {
        ReceiverFilter = string.Empty;
        LoggerFilter = string.Empty;
        ThreadFilter = string.Empty;
        TenantFilter = string.Empty;
        TraceIdFilter = string.Empty;
        MinimumLevelOption = "Any";
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

    [RelayCommand]
    private void ToggleDensity()
    {
        IsCompactDensity = !IsCompactDensity;
    }

    [RelayCommand]
    private void DecreaseColumnWidths()
    {
        AdjustColumnWidths(-16);
    }

    [RelayCommand]
    private void IncreaseColumnWidths()
    {
        AdjustColumnWidths(16);
    }

    [RelayCommand]
    private void ResetColumnWidths()
    {
        TimestampColumnWidth = 180;
        LevelColumnWidth = 90;
        LoggerColumnWidth = 220;
    }

    [RelayCommand]
    private void ResetLevels()
    {
        ShowTrace = true;
        ShowDebug = true;
        ShowInfo = true;
        ShowWarn = true;
        ShowError = true;
        ShowFatal = true;
    }

    [RelayCommand]
    private void EnableAllLoggers()
    {
        _loggerRoot.SetEnabled(true, recursive: true);
        foreach (var item in LoggerTreeItems)
        {
            item.SyncFromNodeRecursive();
        }

        RebuildVisibleEntries();
        UpdateStatusSummary();
    }

    [RelayCommand]
    private async Task CopySelectedMessageAsync()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        await _clipboardService.SetTextAsync(SelectedEntry.Message).ConfigureAwait(false);
        CopyStatus = "Message copied.";
    }

    [RelayCommand]
    private async Task CopySelectedDetailsAsync()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        await _clipboardService.SetTextAsync(SelectedDetailsText).ConfigureAwait(false);
        CopyStatus = "Details copied.";
    }

    [RelayCommand]
    private void AddUdpReceiver()
    {
        var vm = new ReceiverDefinitionViewModel(ReceiverKinds.Udp)
        {
            Id = BuildUniqueReceiverId("udp"),
            DisplayName = $"UDP {ReceiverDefinitions.Count(x => x.IsUdp) + 1}",
            BindAddress = "0.0.0.0",
            Port = 7071
        };
        AttachReceiverDefinition(vm);
        ReceiverDefinitions.Add(vm);
        SelectedReceiverDefinition = vm;
        TryValidateReceiverDefinitions(out _);
    }

    [RelayCommand]
    private void AddTcpReceiver()
    {
        var vm = new ReceiverDefinitionViewModel(ReceiverKinds.Tcp)
        {
            Id = BuildUniqueReceiverId("tcp"),
            DisplayName = $"TCP {ReceiverDefinitions.Count(x => x.IsTcp) + 1}",
            BindAddress = "0.0.0.0",
            Port = 4505
        };
        AttachReceiverDefinition(vm);
        ReceiverDefinitions.Add(vm);
        SelectedReceiverDefinition = vm;
        TryValidateReceiverDefinitions(out _);
    }

    [RelayCommand]
    private void AddFileReceiver()
    {
        var vm = new ReceiverDefinitionViewModel(ReceiverKinds.File)
        {
            Id = BuildUniqueReceiverId("file"),
            DisplayName = $"File {ReceiverDefinitions.Count(x => x.IsFile) + 1}",
            FilePath = string.Empty,
            PollIntervalMs = 150
        };
        AttachReceiverDefinition(vm);
        ReceiverDefinitions.Add(vm);
        SelectedReceiverDefinition = vm;
        TryValidateReceiverDefinitions(out _);
    }

    [RelayCommand]
    private void RemoveSelectedReceiver()
    {
        if (SelectedReceiverDefinition is null)
        {
            return;
        }

        var toRemove = SelectedReceiverDefinition;
        DetachReceiverDefinition(toRemove);
        ReceiverDefinitions.Remove(toRemove);
        if (!string.IsNullOrWhiteSpace(_pendingSelectedReceiverId))
        {
            SelectedReceiverDefinition = ReceiverDefinitions.FirstOrDefault(x =>
                x.Id.Equals(_pendingSelectedReceiverId, StringComparison.OrdinalIgnoreCase));
        }

        SelectedReceiverDefinition ??= ReceiverDefinitions.FirstOrDefault();
        _pendingSelectedReceiverId = null;
        TryValidateReceiverDefinitions(out _);
    }

    [RelayCommand]
    private async Task SaveReceiverSetupAsync()
    {
        if (!TryValidateReceiverDefinitions(out var validationError))
        {
            ReceiverSetupStatus = $"Validation failed: {validationError}";
            return;
        }

        var mapped = MapToReceiverDefinitions();
        await _settingsStore.SaveReceiverDefinitionsAsync(mapped);
        await _ingestionSession.ReloadReceiversAsync();
        ReceiverSetupStatus = $"Saved {ReceiverDefinitions.Count} receiver(s) and reloaded listeners.";
    }

    [RelayCommand]
    private async Task ReloadReceiverSetupAsync()
    {
        await LoadReceiverDefinitionsAsync();
        await _ingestionSession.ReloadReceiversAsync();
        ReceiverSetupStatus = $"Reloaded {ReceiverDefinitions.Count} receiver(s) from settings.";
    }

    partial void OnSearchTextChanged(string value)
    {
        RebuildVisibleEntries();
        UpdateStatusSummary();
        QueuePersistWorkspaceState();
    }

    partial void OnReceiverFilterChanged(string value) => OnFieldFilterChanged();

    partial void OnLoggerFilterChanged(string value) => OnFieldFilterChanged();

    partial void OnThreadFilterChanged(string value) => OnFieldFilterChanged();

    partial void OnTenantFilterChanged(string value) => OnFieldFilterChanged();

    partial void OnTraceIdFilterChanged(string value) => OnFieldFilterChanged();

    partial void OnMinimumLevelOptionChanged(string value) => OnFieldFilterChanged();

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

    partial void OnSelectedEntryChanged(LogEntry? value)
    {
        SelectedDetailsText = BuildDetailsText(value);
        CopyStatus = string.Empty;
    }

    partial void OnSelectedReceiverDefinitionChanged(ReceiverDefinitionViewModel? value)
    {
        QueuePersistWorkspaceState();
    }

    partial void OnIsCompactDensityChanged(bool value)
    {
        UpdateDensityVisuals();
        QueuePersistWorkspaceState();
    }

    partial void OnTimestampColumnWidthChanged(double value)
    {
        OnPropertyChanged(nameof(LogColumnDefinitions));
        QueuePersistWorkspaceState();
    }

    partial void OnLevelColumnWidthChanged(double value)
    {
        OnPropertyChanged(nameof(LogColumnDefinitions));
        QueuePersistWorkspaceState();
    }

    partial void OnLoggerColumnWidthChanged(double value)
    {
        OnPropertyChanged(nameof(LogColumnDefinitions));
        QueuePersistWorkspaceState();
    }

    partial void OnShowTraceChanged(bool value) => OnLevelFilterChanged();

    partial void OnShowDebugChanged(bool value) => OnLevelFilterChanged();

    partial void OnShowInfoChanged(bool value) => OnLevelFilterChanged();

    partial void OnShowWarnChanged(bool value) => OnLevelFilterChanged();

    partial void OnShowErrorChanged(bool value) => OnLevelFilterChanged();

    partial void OnShowFatalChanged(bool value) => OnLevelFilterChanged();

    private void OnReceiverDefinitionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null || ReceiverEditablePropertyNames.Contains(e.PropertyName))
        {
            TryValidateReceiverDefinitions(out _);
        }
    }

    private void OnEntriesAppended(object? sender, LogEntriesAppendedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _statisticsService.RecordRange(e.AppendedEntries);
            var query = BuildCurrentQuery();
            foreach (var entry in e.AppendedEntries)
            {
                RegisterLogger(entry.LoggerName);
                if (!MatchesFilter(entry, query))
                {
                    continue;
                }

                VisibleEntries.Add(entry);
                if (VisibleEntries.Count > MaxVisibleEntries)
                {
                    VisibleEntries.RemoveAt(0);
                }
            }

            UpdateStatusSummary();
            UpdateStatisticsSummary();
        });
    }

    private void RebuildVisibleEntries()
    {
        var snapshot = _ingestionSession.Snapshot();
        var query = BuildCurrentQuery();
        var filtered = snapshot.Where(entry => MatchesFilter(entry, query)).TakeLast(MaxVisibleEntries).ToArray();

        VisibleEntries.Clear();
        foreach (var entry in filtered)
        {
            VisibleEntries.Add(entry);
        }
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
        return !_loggerRoot.TryGetPath(loggerName, out var node) || node?.IsEnabled != false;
    }

    private bool IsLevelEnabled(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => ShowTrace,
            LogLevel.Debug => ShowDebug,
            LogLevel.Info => ShowInfo,
            LogLevel.Warn => ShowWarn,
            LogLevel.Error => ShowError,
            LogLevel.Fatal => ShowFatal,
            _ => true
        };
    }

    private void RebuildLoggerTreeFromSnapshot()
    {
        _loggerRoot = LoggerNode.CreateRoot();
        foreach (var entry in _ingestionSession.Snapshot())
        {
            RegisterLogger(entry.LoggerName, refreshTree: false);
        }

        RebuildLoggerTreeItems();
    }

    private void RegisterLogger(string loggerName, bool refreshTree = true)
    {
        if (string.IsNullOrWhiteSpace(loggerName))
        {
            return;
        }

        if (_loggerRoot.TryGetPath(loggerName, out _))
        {
            return;
        }

        _loggerRoot.GetOrCreatePath(loggerName);
        if (refreshTree)
        {
            RebuildLoggerTreeItems();
        }
    }

    private void RebuildLoggerTreeItems()
    {
        LoggerTreeItems.Clear();
        foreach (var child in _loggerRoot.Children.Values.OrderBy(static x => x.Name, StringComparer.Ordinal))
        {
            LoggerTreeItems.Add(new LoggerTreeItemViewModel(child, OnLoggerTreeStateChanged));
        }
    }

    private void OnLoggerTreeStateChanged()
    {
        RebuildVisibleEntries();
        UpdateStatusSummary();
    }

    private void OnLevelFilterChanged()
    {
        RebuildVisibleEntries();
        UpdateStatusSummary();
        QueuePersistWorkspaceState();
    }

    private void OnFieldFilterChanged()
    {
        RebuildVisibleEntries();
        UpdateStatusSummary();
        QueuePersistWorkspaceState();
    }

    private LogQuery BuildCurrentQuery()
    {
        var propertyFilters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var tenant = NormalizeFilter(TenantFilter);
        if (tenant is not null)
        {
            propertyFilters["tenant"] = tenant;
        }

        var traceId = NormalizeFilter(TraceIdFilter);
        if (traceId is not null)
        {
            propertyFilters["traceId"] = traceId;
        }

        return new LogQuery
        {
            MinimumLevel = ParseMinimumLevel(MinimumLevelOption),
            TextContains = NormalizeFilter(SearchText),
            ReceiverId = NormalizeFilter(ReceiverFilter),
            LoggerContains = NormalizeFilter(LoggerFilter),
            ThreadContains = NormalizeFilter(ThreadFilter),
            PropertyContains = propertyFilters
        };
    }

    private static LogLevel? ParseMinimumLevel(string? option)
    {
        if (string.IsNullOrWhiteSpace(option) ||
            option.Equals("Any", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Enum.TryParse<LogLevel>(option, ignoreCase: true, out var parsed) ? parsed : null;
    }

    private static string? NormalizeFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private void AdjustColumnWidths(double delta)
    {
        TimestampColumnWidth = ClampColumnWidth(TimestampColumnWidth + delta, 100, 420);
        LevelColumnWidth = ClampColumnWidth(LevelColumnWidth + delta, 70, 200);
        LoggerColumnWidth = ClampColumnWidth(LoggerColumnWidth + delta, 120, 520);
    }

    private static double ClampColumnWidth(double value, double min, double max)
    {
        return Math.Clamp(value, min, max);
    }

    private void UpdateThemeSummary()
    {
        ThemeSummary = $"Theme: {_themeService.CurrentMode}";
    }

    private void UpdateDensityVisuals()
    {
        if (IsCompactDensity)
        {
            DensityButtonText = "Density: Compact";
            LogRowFontSize = 11;
            LogRowMargin = new Thickness(4, 0, 4, 0);
            return;
        }

        DensityButtonText = "Density: Comfortable";
        LogRowFontSize = 12;
        LogRowMargin = new Thickness(4, 2, 4, 2);
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

    private static string BuildDetailsText(LogEntry? entry)
    {
        if (entry is null)
        {
            return "No entry selected.";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Timestamp: {entry.Timestamp:O}");
        builder.AppendLine($"Level: {entry.Level}");
        builder.AppendLine($"Receiver: {entry.ReceiverId}");
        builder.AppendLine($"Logger: {entry.LoggerName}");
        builder.AppendLine($"Thread: {entry.ThreadName}");
        builder.AppendLine($"Message: {entry.Message}");
        if (!string.IsNullOrWhiteSpace(entry.MessageTemplate))
        {
            builder.AppendLine($"MessageTemplate: {entry.MessageTemplate}");
        }

        if (!string.IsNullOrWhiteSpace(entry.Exception))
        {
            builder.AppendLine();
            builder.AppendLine("Exception:");
            builder.AppendLine(entry.Exception);
        }

        if (!string.IsNullOrWhiteSpace(entry.SourceFileName) || entry.SourceFileLineNumber.HasValue)
        {
            builder.AppendLine();
            builder.AppendLine($"Source: {entry.SourceFileName}:{entry.SourceFileLineNumber}");
        }

        if (entry.Properties.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Properties:");
            foreach (var pair in entry.Properties.OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine($"- {pair.Key}: {pair.Value}");
            }
        }

        if (!string.IsNullOrWhiteSpace(entry.StructuredPayloadJson))
        {
            builder.AppendLine();
            builder.AppendLine("StructuredPayload:");
            builder.AppendLine(entry.StructuredPayloadJson);
        }

        return builder.ToString();
    }

    private void AttachReceiverDefinition(ReceiverDefinitionViewModel receiver)
    {
        receiver.PropertyChanged += OnReceiverDefinitionPropertyChanged;
    }

    private void DetachReceiverDefinition(ReceiverDefinitionViewModel receiver)
    {
        receiver.PropertyChanged -= OnReceiverDefinitionPropertyChanged;
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

        if (!string.IsNullOrWhiteSpace(_pendingSelectedReceiverId) && ReceiverDefinitions.Count > 0)
        {
            SelectedReceiverDefinition = ReceiverDefinitions.FirstOrDefault(x =>
                x.Id.Equals(_pendingSelectedReceiverId, StringComparison.OrdinalIgnoreCase))
                ?? ReceiverDefinitions.FirstOrDefault();
            _pendingSelectedReceiverId = null;
        }

        RebuildVisibleEntries();
        UpdateStatusSummary();
    }

    private async Task LoadReceiverDefinitionsAsync()
    {
        var definitions = await _settingsStore.LoadReceiverDefinitionsAsync().ConfigureAwait(false);

        if (Avalonia.Application.Current is null || Dispatcher.UIThread.CheckAccess())
        {
            ApplyReceiverDefinitions(definitions);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => ApplyReceiverDefinitions(definitions));
    }

    private void ApplyReceiverDefinitions(ReceiverDefinitions definitions)
    {
        foreach (var existing in ReceiverDefinitions)
        {
            DetachReceiverDefinition(existing);
        }

        ReceiverDefinitions.Clear();
        foreach (var udp in definitions.UdpReceivers)
        {
            var receiver = new ReceiverDefinitionViewModel(ReceiverKinds.Udp)
            {
                Id = udp.Id,
                DisplayName = udp.DisplayName,
                Enabled = udp.Enabled,
                BindAddress = udp.BindAddress,
                Port = udp.Port,
                ParserOrderText = FormatParserOrder(udp.ParserOrder)
            };
            AttachReceiverDefinition(receiver);
            ReceiverDefinitions.Add(receiver);
        }

        foreach (var tcp in definitions.TcpReceivers)
        {
            var receiver = new ReceiverDefinitionViewModel(ReceiverKinds.Tcp)
            {
                Id = tcp.Id,
                DisplayName = tcp.DisplayName,
                Enabled = tcp.Enabled,
                BindAddress = tcp.BindAddress,
                Port = tcp.Port,
                ParserOrderText = FormatParserOrder(tcp.ParserOrder)
            };
            AttachReceiverDefinition(receiver);
            ReceiverDefinitions.Add(receiver);
        }

        foreach (var file in definitions.FileTailReceivers)
        {
            var receiver = new ReceiverDefinitionViewModel(ReceiverKinds.File)
            {
                Id = file.Id,
                DisplayName = file.DisplayName,
                Enabled = file.Enabled,
                FilePath = file.FilePath,
                PollIntervalMs = file.PollIntervalMs,
                ParserOrderText = FormatParserOrder(file.ParserOrder)
            };
            AttachReceiverDefinition(receiver);
            ReceiverDefinitions.Add(receiver);
        }

        SelectedReceiverDefinition = ReceiverDefinitions.FirstOrDefault();
        TryValidateReceiverDefinitions(out _);
    }

    private ReceiverDefinitions MapToReceiverDefinitions()
    {
        var udp = ReceiverDefinitions
            .Where(static x => x.IsUdp)
            .Select(x => new UdpReceiverDefinition
            {
                Id = x.Id,
                DisplayName = x.DisplayName,
                Enabled = x.Enabled,
                BindAddress = string.IsNullOrWhiteSpace(x.BindAddress) ? "0.0.0.0" : x.BindAddress.Trim(),
                Port = x.Port <= 0 ? 7071 : x.Port,
                DefaultLoggerName = x.DisplayName,
                ParserOrder = ParseParserOrder(x.ParserOrderText)
            })
            .ToArray();

        var tcp = ReceiverDefinitions
            .Where(static x => x.IsTcp)
            .Select(x => new TcpReceiverDefinition
            {
                Id = x.Id,
                DisplayName = x.DisplayName,
                Enabled = x.Enabled,
                BindAddress = string.IsNullOrWhiteSpace(x.BindAddress) ? "0.0.0.0" : x.BindAddress.Trim(),
                Port = x.Port <= 0 ? 4505 : x.Port,
                DefaultLoggerName = x.DisplayName,
                ParserOrder = ParseParserOrder(x.ParserOrderText)
            })
            .ToArray();

        var file = ReceiverDefinitions
            .Where(static x => x.IsFile)
            .Select(x => new FileTailReceiverDefinition
            {
                Id = x.Id,
                DisplayName = x.DisplayName,
                Enabled = x.Enabled,
                FilePath = x.FilePath.Trim(),
                PollIntervalMs = x.PollIntervalMs <= 0 ? 150 : x.PollIntervalMs,
                DefaultLoggerName = x.DisplayName,
                ParserOrder = ParseParserOrder(x.ParserOrderText)
            })
            .ToArray();

        return new ReceiverDefinitions
        {
            UdpReceivers = udp,
            TcpReceivers = tcp,
            FileTailReceivers = file
        };
    }

    private bool TryValidateReceiverDefinitions(out string error)
    {
        error = string.Empty;
        var isValid = true;
        var ids = new Dictionary<string, List<ReceiverDefinitionViewModel>>(StringComparer.OrdinalIgnoreCase);
        string? firstError = null;

        foreach (var receiver in ReceiverDefinitions)
        {
            receiver.ClearValidationErrors();
        }

        void RegisterError(string message)
        {
            isValid = false;
            if (firstError == null)
            {
                firstError = message;
            }
        }

        for (var index = 0; index < ReceiverDefinitions.Count; index++)
        {
            var receiver = ReceiverDefinitions[index];
            var label = $"{receiver.Kind} #{index + 1}";

            if (string.IsNullOrWhiteSpace(receiver.Id))
            {
                receiver.IdValidationError = "Id is required.";
                RegisterError($"{label} requires a non-empty Id.");
            }
            else
            {
                var normalizedId = receiver.Id.Trim();
                if (!ids.TryGetValue(normalizedId, out var matching))
                {
                    matching = [];
                    ids[normalizedId] = matching;
                }
                matching.Add(receiver);
            }

            if (string.IsNullOrWhiteSpace(receiver.DisplayName))
            {
                receiver.DisplayNameValidationError = "Display name is required.";
                RegisterError($"{label} requires a display name.");
            }

            if (!IsValidPort(receiver.Port))
            {
                receiver.PortValidationError = "Port must be between 1 and 65535.";
                RegisterError($"{label} port must be between 1 and 65535.");
            }

            // Use correct enum for kind
            if (receiver.Kind == ReceiverKinds.Udp || receiver.Kind == ReceiverKinds.Tcp)
            {
                if (!IsValidBindAddress(receiver.BindAddress))
                {
                    receiver.BindAddressValidationError = $"Bind address '{receiver.BindAddress}' is invalid.";
                    RegisterError($"{label} bind address '{receiver.BindAddress}' is invalid.");
                }
            }

            if (receiver.Kind == ReceiverKinds.File)
            {
                if (string.IsNullOrWhiteSpace(receiver.FilePath))
                {
                    receiver.FilePathValidationError = "File path is required.";
                    RegisterError($"{label} file path is required.");
                }
                if (receiver.PollIntervalMs <= 0)
                {
                    receiver.PollIntervalValidationError = "Poll interval must be greater than zero.";
                    RegisterError($"{label} poll interval must be greater than zero.");
                }
            }

            // Parser order validation
            var parserOrder = ParseParserOrder(receiver.ParserOrderText);
            if (parserOrder.Count == 0)
            {
                receiver.ParserOrderValidationError = "Parser order cannot be empty.";
                RegisterError($"{label} parser order cannot be empty.");
            }
            else
            {
                var unknown = parserOrder.FirstOrDefault(p => !KnownParserNames.Contains(p));
                if (!string.IsNullOrEmpty(unknown))
                {
                    receiver.ParserOrderValidationError = $"Unknown parser '{unknown}'.";
                    RegisterError($"{label} uses unknown parser '{unknown}'");
                }
            }
        }

        foreach (var entry in ids)
        {
            if (entry.Value.Count > 1)
            {
                foreach (var receiver in entry.Value)
                {
                    receiver.IdValidationError = "Id must be unique.";
                }
                RegisterError($"Receiver Id '{entry.Key}' is duplicated.");
            }
        }

        error = firstError ?? string.Empty;
        return isValid;
    }

    private static bool IsValidPort(int value)
    {
        return value is >= 1 and <= 65535;
    }

    private static bool IsValidBindAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed == "*")
        {
            return true;
        }

        if (IPAddress.TryParse(trimmed, out _))
        {
            return true;
        }

        return Uri.CheckHostName(trimmed) != UriHostNameType.Unknown;
    }

    private static string FormatParserOrder(IReadOnlyList<string> parserOrder)
    {
        return parserOrder.Count == 0
            ? string.Join(", ", DefaultParserOrder)
            : string.Join(", ", parserOrder);
    }

    private static IReadOnlyList<string> ParseParserOrder(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DefaultParserOrder;
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static name => name.Length > 0)
            .Distinct(ParserNameComparer)
            .ToArray();
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
        var enabledLevels = new List<string>(6);
        if (ShowTrace) enabledLevels.Add(nameof(LogLevel.Trace));
        if (ShowDebug) enabledLevels.Add(nameof(LogLevel.Debug));
        if (ShowInfo) enabledLevels.Add(nameof(LogLevel.Info));
        if (ShowWarn) enabledLevels.Add(nameof(LogLevel.Warn));
        if (ShowError) enabledLevels.Add(nameof(LogLevel.Error));
        if (ShowFatal) enabledLevels.Add(nameof(LogLevel.Fatal));

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
            EnabledLevels = enabledLevels,
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

    private string BuildUniqueReceiverId(string prefix)
    {
        var next = 1;
        var existing = new HashSet<string>(ReceiverDefinitions.Select<ReceiverDefinitionViewModel, string>(x => x.Id), StringComparer.OrdinalIgnoreCase);
        while (true)
        {
            var candidate = $"{prefix}-{next}";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }

            next++;
        }
    }
}
