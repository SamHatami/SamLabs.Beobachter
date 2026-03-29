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
    private readonly IIngestionSession _ingestionSession;
    private readonly IWorkspaceStateCoordinator _workspaceStateCoordinator;
    private readonly IWorkspaceStartupOrchestrator _workspaceStartupOrchestrator;
    private readonly ILogStreamProjectionService _logStreamProjectionService;
    private readonly ILogStatisticsService _statisticsService;
    private bool _isApplyingWorkspaceState;
    private bool _isSyncingSearchText;

    [ObservableProperty]
    private string _statsSummary1Minute = "1m: 0 logs/s | 0 err/s";

    [ObservableProperty]
    private string _statsSummary5Minutes = "5m: 0 logs/s | 0 err/s";

    [ObservableProperty]
    private string _topLoggersSummary = "Top loggers (5m): -";

    [ObservableProperty]
    private string _topReceiversSummary = "Top receivers (5m): -";

    public MainWindowViewModel(
        IShellStatusFormatter shellStatusFormatter,
        ISampleLogEntryGenerator sampleLogEntryGenerator,
        IIngestionSession ingestionSession,
        IWorkspaceStateCoordinator workspaceStateCoordinator,
        IWorkspaceStartupOrchestrator workspaceStartupOrchestrator,
        ILogStreamProjectionService logStreamProjectionService,
        ILogStatisticsService statisticsService,
        TopBarViewModel topBar,
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
        _ingestionSession = ingestionSession ?? throw new ArgumentNullException(nameof(ingestionSession));
        _workspaceStateCoordinator = workspaceStateCoordinator ?? throw new ArgumentNullException(nameof(workspaceStateCoordinator));
        _workspaceStartupOrchestrator = workspaceStartupOrchestrator ?? throw new ArgumentNullException(nameof(workspaceStartupOrchestrator));
        _logStreamProjectionService = logStreamProjectionService ?? throw new ArgumentNullException(nameof(logStreamProjectionService));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));

        TopBar = topBar ?? throw new ArgumentNullException(nameof(topBar));
        Sources = sources ?? throw new ArgumentNullException(nameof(sources));
        QuickFilters = quickFilters ?? throw new ArgumentNullException(nameof(quickFilters));
        ReceiverSetup = receiverSetup ?? throw new ArgumentNullException(nameof(receiverSetup));
        WorkspaceSidebar = workspaceSidebar ?? throw new ArgumentNullException(nameof(workspaceSidebar));
        Filters = filters ?? throw new ArgumentNullException(nameof(filters));
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        Details = details ?? throw new ArgumentNullException(nameof(details));
        SessionHealth = sessionHealth ?? throw new ArgumentNullException(nameof(sessionHealth));

        TopBar.SearchTextChanged += OnTopBarSearchTextChanged;
        TopBar.PauseToggled += OnTopBarPauseToggled;
        TopBar.AutoScrollToggled += OnTopBarAutoScrollToggled;
        Stream.AutoScrollToggleRequested += OnStreamAutoScrollToggleRequested;
        Filters.PropertyChanged += OnFiltersPropertyChanged;
        Sources.StateChanged += OnSourcesStateChanged;
        QuickFilters.PropertyChanged += OnQuickFiltersPropertyChanged;
        ReceiverSetup.PropertyChanged += OnReceiverSetupPropertyChanged;
        Stream.PropertyChanged += OnStreamPropertyChanged;

        _ingestionSession.EntriesAppended += OnEntriesAppended;
        Stream.IsAutoScrollEnabled = TopBar.IsAutoScrollEnabled;
        IReadOnlyList<LogEntry> initialSnapshot = _ingestionSession.Snapshot();
        _statisticsService.RecordRange(initialSnapshot);
        Sources.RebuildFromSnapshot(initialSnapshot);
        WorkspaceSidebar.UpdateSnapshot(initialSnapshot);
        RebuildVisibleEntries();
        UpdateShellStatusPresentation();
        _ = InitializeWorkspaceAsync();
    }

    public TopBarViewModel TopBar { get; }

    public WorkspaceSidebarViewModel WorkspaceSidebar { get; }

    public SourceTreeViewModel Sources { get; }

    public QuickFiltersViewModel QuickFilters { get; }

    public ReceiverSetupViewModel ReceiverSetup { get; }

    public LogFiltersViewModel Filters { get; }

    public LogStreamViewModel Stream { get; }

    public EntryDetailsViewModel Details { get; }

    public SessionHealthViewModel SessionHealth { get; }

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

    private void OnTopBarSearchTextChanged(object? sender, EventArgs e)
    {
        if (_isSyncingSearchText)
        {
            return;
        }

        _isSyncingSearchText = true;
        try
        {
            Filters.SearchText = TopBar.SearchText;
        }
        finally
        {
            _isSyncingSearchText = false;
        }
    }

    private void OnTopBarPauseToggled(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(UpdateShellStatusPresentation);
    }

    private void OnTopBarAutoScrollToggled(object? sender, EventArgs e)
    {
        Stream.IsAutoScrollEnabled = TopBar.IsAutoScrollEnabled;
        Dispatcher.UIThread.Post(UpdateShellStatusPresentation);
    }

    private void OnStreamAutoScrollToggleRequested(object? sender, EventArgs e)
    {
        TopBar.ToggleAutoScrollCommand.Execute(null);
    }

    private void OnFiltersPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null || FilterCriteriaPropertyNames.Contains(e.PropertyName))
        {
            if (string.Equals(e.PropertyName, nameof(LogFiltersViewModel.SearchText), StringComparison.Ordinal) &&
                !_isSyncingSearchText)
            {
                _isSyncingSearchText = true;
                try
                {
                    TopBar.SearchText = Filters.SearchText;
                }
                finally
                {
                    _isSyncingSearchText = false;
                }
            }

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

        if (string.Equals(e.PropertyName, nameof(LogStreamViewModel.IsCompactDensity), StringComparison.Ordinal) ||
            string.Equals(e.PropertyName, nameof(LogStreamViewModel.TimestampColumnWidth), StringComparison.Ordinal) ||
            string.Equals(e.PropertyName, nameof(LogStreamViewModel.LevelColumnWidth), StringComparison.Ordinal) ||
            string.Equals(e.PropertyName, nameof(LogStreamViewModel.LoggerColumnWidth), StringComparison.Ordinal))
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
            WorkspaceSidebar.UpdateSnapshot(_ingestionSession.Snapshot());
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
            WorkspaceSidebar.SyncFacetSelectionState();
            UpdateShellStatusPresentation();
        }
    }

    private void OnFiltersChanged()
    {
        RebuildVisibleEntries();
        WorkspaceSidebar.SyncFacetSelectionState();
        UpdateShellStatusPresentation();
        QueuePersistWorkspaceState();
    }

    private void UpdateShellStatusPresentation()
    {
        IReadOnlyList<ReceiverRuntimeState> runtimeStates = _ingestionSession.GetReceiverRuntimeStates();
        int activeReceivers = runtimeStates.Count > 0
            ? runtimeStates.Count(static x => x.State == ReceiverRunState.Running)
            : ReceiverSetup.ReceiverDefinitions.Count(static x => x.Enabled);
        ShellStatusPresentation presentation = _shellStatusFormatter.Build(
            TopBar.IsPaused,
            TopBar.IsAutoScrollEnabled,
            _ingestionSession.TotalCount,
            Stream.VisibleEntries.Count,
            _ingestionSession.DroppedCount,
            activeReceivers,
            QuickFilters.StructuredOnlyCount,
            _statisticsService.GetSnapshot());

        TopBar.StatusSummary = presentation.StatusSummary;
        StatsSummary1Minute = presentation.StatsSummary1Minute;
        StatsSummary5Minutes = presentation.StatsSummary5Minutes;
        TopLoggersSummary = presentation.TopLoggersSummary;
        TopReceiversSummary = presentation.TopReceiversSummary;
        SessionHealth.ActiveReceiversText = presentation.ActiveReceiversText;
        SessionHealth.BufferedEntriesText = presentation.BufferedEntriesText;
        SessionHealth.StructuredEventsText = presentation.StructuredEventsText;
        SessionHealth.DroppedPacketsText = presentation.DroppedPacketsText;
    }

    private async Task InitializeWorkspaceAsync()
    {
        await _workspaceStartupOrchestrator
            .InitializeAsync(ReceiverSetup, ApplyWorkspaceState)
            .ConfigureAwait(false);
        UpdateShellStatusPresentation();
    }

    private void ApplyWorkspaceState(WorkspaceSettings workspace, UiLayoutSettings layout)
    {
        _isApplyingWorkspaceState = true;

        try
        {
            Filters.SearchText = workspace.SearchText;
            TopBar.SearchText = workspace.SearchText;
            Filters.ReceiverFilter = workspace.ReceiverFilter;
            Filters.LoggerFilter = workspace.LoggerFilter;
            Filters.ThreadFilter = workspace.ThreadFilter;
            Filters.TenantFilter = workspace.TenantFilter;
            Filters.TraceIdFilter = workspace.TraceIdFilter;
            Filters.MinimumLevelOption = string.IsNullOrWhiteSpace(workspace.MinimumLevelOption) ? "Any" : workspace.MinimumLevelOption;
            Stream.IsCompactDensity = workspace.CompactDensity;
            Stream.TimestampColumnWidth = Math.Clamp(layout.TimestampColumnWidth, 100, 420);
            Stream.LevelColumnWidth = Math.Clamp(layout.LevelColumnWidth, 70, 200);
            Stream.LoggerColumnWidth = Math.Clamp(layout.LoggerColumnWidth, 120, 520);

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

        RebuildVisibleEntries();
        WorkspaceSidebar.UpdateSnapshot(_ingestionSession.Snapshot());
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
            TopBar.IsAutoScrollEnabled,
            TopBar.IsPaused,
            Stream.TimestampColumnWidth,
            Stream.LevelColumnWidth,
            Stream.LoggerColumnWidth);

        _workspaceStateCoordinator.QueuePersist(update);
    }
}
