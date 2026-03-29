using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
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

    private readonly IShellStatusFormatter _shellStatusFormatter;
    private readonly ISampleLogEntryGenerator _sampleLogEntryGenerator;
    private readonly IThemeService _themeService;
    private readonly IIngestionSession _ingestionSession;
    private readonly IWorkspaceStateCoordinator _workspaceStateCoordinator;
    private readonly ILogStreamProjectionService _logStreamProjectionService;
    private readonly ILogStatisticsService _statisticsService;
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

    public MainWindowViewModel(
        IShellStatusFormatter shellStatusFormatter,
        ISampleLogEntryGenerator sampleLogEntryGenerator,
        IThemeService themeService,
        IIngestionSession ingestionSession,
        IWorkspaceStateCoordinator workspaceStateCoordinator,
        ILogStreamProjectionService logStreamProjectionService,
        ILogStatisticsService statisticsService,
        SourceTreeViewModel sources,
        QuickFiltersViewModel quickFilters,
        ReceiverSetupViewModel receiverSetup,
        WorkspaceSidebarViewModel workspaceSidebar,
        LogFiltersViewModel filters,
        LogStreamViewModel stream,
        EntryDetailsViewModel details,
        SessionHealthViewModel sessionHealth)
    {
        _shellStatusFormatter = shellStatusFormatter ?? throw new ArgumentNullException(nameof(shellStatusFormatter));
        _sampleLogEntryGenerator = sampleLogEntryGenerator ?? throw new ArgumentNullException(nameof(sampleLogEntryGenerator));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _ingestionSession = ingestionSession ?? throw new ArgumentNullException(nameof(ingestionSession));
        _workspaceStateCoordinator = workspaceStateCoordinator ?? throw new ArgumentNullException(nameof(workspaceStateCoordinator));
        _logStreamProjectionService = logStreamProjectionService ?? throw new ArgumentNullException(nameof(logStreamProjectionService));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));

        Sources = sources ?? throw new ArgumentNullException(nameof(sources));
        QuickFilters = quickFilters ?? throw new ArgumentNullException(nameof(quickFilters));
        ReceiverSetup = receiverSetup ?? throw new ArgumentNullException(nameof(receiverSetup));
        WorkspaceSidebar = workspaceSidebar ?? throw new ArgumentNullException(nameof(workspaceSidebar));
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
        Stream.IsAutoScrollEnabled = IsAutoScrollEnabled;
        _statisticsService.RecordRange(_ingestionSession.Snapshot());
        Sources.RebuildFromSnapshot(_ingestionSession.Snapshot());
        RebuildVisibleEntries();
        UpdateQuickFiltersSnapshot();
        UpdateThemeSummary();
        UpdateShellStatusPresentation();
        _ = LoadReceiverSetupAsync();
        _ = LoadWorkspaceStateAsync();
    }

    public MainToolbarViewModel Toolbar { get; }

    public WorkspaceSidebarViewModel WorkspaceSidebar { get; }

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
        IReadOnlyList<LogEntry> sampleEntries = _sampleLogEntryGenerator.CreateBatch(_ingestionSession.TotalCount + 1, 12);
        foreach (LogEntry entry in sampleEntries)
        {
            _ingestionSession.TryPublish(entry);
        }

        UpdateShellStatusPresentation();
    }

    [RelayCommand]
    private async Task TogglePauseAsync()
    {
        var nextState = !IsPaused;
        await _ingestionSession.SetPausedAsync(nextState).ConfigureAwait(false);
        IsPaused = nextState;
        Dispatcher.UIThread.Post(UpdateShellStatusPresentation);
    }

    [RelayCommand]
    private async Task ToggleAutoScrollAsync()
    {
        var nextState = !IsAutoScrollEnabled;
        await _ingestionSession.SetAutoScrollAsync(nextState).ConfigureAwait(false);
        IsAutoScrollEnabled = nextState;
        Dispatcher.UIThread.Post(UpdateShellStatusPresentation);
    }

    partial void OnIsPausedChanged(bool value)
    {
        PauseButtonText = value ? "Resume" : "Pause";
        UpdateShellStatusPresentation();
    }

    partial void OnIsAutoScrollEnabledChanged(bool value)
    {
        AutoScrollButtonText = value ? "Pin: On" : "Pin: Off";
        Stream.IsAutoScrollEnabled = value;
        UpdateShellStatusPresentation();
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

        UpdateShellStatusPresentation();
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
            _logStreamProjectionService.AppendEntries(e.AppendedEntries, Sources, QuickFilters, Filters, Stream);

            UpdateQuickFiltersSnapshot();
            UpdateShellStatusPresentation();
        });
    }

    private void RebuildVisibleEntries()
    {
        IReadOnlyList<LogEntry> snapshot = _ingestionSession.Snapshot();
        _logStreamProjectionService.RebuildEntries(snapshot, Sources, QuickFilters, Filters, Stream);
    }

    private void OnSourcesStateChanged(object? sender, EventArgs e)
    {
        RebuildVisibleEntries();
        UpdateShellStatusPresentation();
    }

    private void OnQuickFiltersPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null || QuickFilterCriteriaPropertyNames.Contains(e.PropertyName))
        {
            RebuildVisibleEntries();
            UpdateShellStatusPresentation();
        }
    }

    private void OnFiltersChanged()
    {
        RebuildVisibleEntries();
        UpdateShellStatusPresentation();
        QueuePersistWorkspaceState();
    }

    private static double ClampColumnWidth(double value, double min, double max)
    {
        return Math.Clamp(value, min, max);
    }

    private void UpdateThemeSummary()
    {
        ThemeSummary = $"Theme: {_themeService.CurrentMode}";
    }

    private void UpdateQuickFiltersSnapshot()
    {
        IReadOnlyList<LogEntry> snapshot = _ingestionSession.Snapshot();
        QuickFilterSnapshot quickFilterSnapshot = _logStreamProjectionService.ComputeQuickFilterSnapshot(snapshot);
        QuickFilters.ErrorsAndAboveCount = quickFilterSnapshot.ErrorsAndAboveCount;
        QuickFilters.StructuredOnlyCount = quickFilterSnapshot.StructuredOnlyCount;
    }

    private void UpdateShellStatusPresentation()
    {
        int activeReceivers = ReceiverSetup.ReceiverDefinitions.Count(static x => x.Enabled);
        ShellStatusPresentation presentation = _shellStatusFormatter.Build(
            IsPaused,
            IsAutoScrollEnabled,
            _ingestionSession.TotalCount,
            Stream.VisibleEntries.Count,
            _ingestionSession.DroppedCount,
            activeReceivers,
            QuickFilters.StructuredOnlyCount,
            _statisticsService.GetSnapshot());

        StatusSummary = presentation.StatusSummary;
        StatsSummary1Minute = presentation.StatsSummary1Minute;
        StatsSummary5Minutes = presentation.StatsSummary5Minutes;
        TopLoggersSummary = presentation.TopLoggersSummary;
        TopReceiversSummary = presentation.TopReceiversSummary;
        SessionHealth.ActiveReceiversText = presentation.ActiveReceiversText;
        SessionHealth.BufferedEntriesText = presentation.BufferedEntriesText;
        SessionHealth.StructuredEventsText = presentation.StructuredEventsText;
        SessionHealth.DroppedPacketsText = presentation.DroppedPacketsText;
    }

    private async Task LoadReceiverSetupAsync()
    {
        await ReceiverSetup.LoadAsync().ConfigureAwait(false);

        if (Avalonia.Application.Current is null || Dispatcher.UIThread.CheckAccess())
        {
            ApplyPendingReceiverSelection();
            UpdateShellStatusPresentation();
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ApplyPendingReceiverSelection();
            UpdateShellStatusPresentation();
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
        WorkspaceStateSnapshot snapshot = await _workspaceStateCoordinator.LoadAsync().ConfigureAwait(false);
        WorkspaceSettings workspace = snapshot.WorkspaceSettings;
        UiLayoutSettings layout = snapshot.UiLayoutSettings;

        if (Avalonia.Application.Current is null || Dispatcher.UIThread.CheckAccess())
        {
            ApplyWorkspaceState(workspace, layout);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => ApplyWorkspaceState(workspace, layout));
    }

    private void ApplyWorkspaceState(WorkspaceSettings workspace, UiLayoutSettings layout)
    {
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
        UpdateQuickFiltersSnapshot();
        UpdateShellStatusPresentation();
    }

    private void QueuePersistWorkspaceState()
    {
        if (_isApplyingWorkspaceState)
        {
            return;
        }

        WorkspaceStateUpdate update = new(
            Filters.SearchText,
            Filters.ReceiverFilter,
            Filters.LoggerFilter,
            Filters.ThreadFilter,
            Filters.TenantFilter,
            Filters.TraceIdFilter,
            Filters.MinimumLevelOption,
            Stream.IsCompactDensity,
            ReceiverSetup.SelectedReceiverDefinition?.Id ?? string.Empty,
            Filters.GetEnabledLevels(),
            IsAutoScrollEnabled,
            IsPaused,
            Stream.TimestampColumnWidth,
            Stream.LevelColumnWidth,
            Stream.LoggerColumnWidth);

        _workspaceStateCoordinator.QueuePersist(update);
    }
}
