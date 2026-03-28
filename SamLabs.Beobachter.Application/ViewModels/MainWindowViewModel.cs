using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private const int MaxVisibleEntries = 2_000;

    private readonly IThemeService _themeService;
    private readonly IIngestionSession _ingestionSession;
    private readonly IClipboardService _clipboardService;
    private readonly ISettingsStore _settingsStore;
    private readonly Random _random = new();
    private LoggerNode _loggerRoot = LoggerNode.CreateRoot();

    [ObservableProperty]
    private string _themeSummary = string.Empty;

    [ObservableProperty]
    private string _statusSummary = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

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

    public MainWindowViewModel() : this(
        new ThemeService(),
        new DesignIngestionSession(),
        new NullClipboardService(),
        new DesignSettingsStore())
    {
    }

    public MainWindowViewModel(
        IThemeService themeService,
        IIngestionSession ingestionSession,
        IClipboardService? clipboardService = null,
        ISettingsStore? settingsStore = null)
    {
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _ingestionSession = ingestionSession ?? throw new ArgumentNullException(nameof(ingestionSession));
        _clipboardService = clipboardService ?? new NullClipboardService();
        _settingsStore = settingsStore ?? new DesignSettingsStore();

        _ingestionSession.EntriesAppended += OnEntriesAppended;
        IsPaused = _ingestionSession.IsPaused;
        IsAutoScrollEnabled = _ingestionSession.IsAutoScrollEnabled;
        RebuildLoggerTreeFromSnapshot();
        RebuildVisibleEntries();
        UpdateThemeSummary();
        UpdateDensityVisuals();
        UpdateStatusSummary();
        _ = LoadReceiverDefinitionsAsync();
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
        ReceiverDefinitions.Add(vm);
        SelectedReceiverDefinition = vm;
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
        ReceiverDefinitions.Add(vm);
        SelectedReceiverDefinition = vm;
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
        ReceiverDefinitions.Add(vm);
        SelectedReceiverDefinition = vm;
    }

    [RelayCommand]
    private void RemoveSelectedReceiver()
    {
        if (SelectedReceiverDefinition is null)
        {
            return;
        }

        var toRemove = SelectedReceiverDefinition;
        ReceiverDefinitions.Remove(toRemove);
        SelectedReceiverDefinition = ReceiverDefinitions.FirstOrDefault();
    }

    [RelayCommand]
    private async Task SaveReceiverSetupAsync()
    {
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

    partial void OnSelectedEntryChanged(LogEntry? value)
    {
        SelectedDetailsText = BuildDetailsText(value);
        CopyStatus = string.Empty;
    }

    partial void OnIsCompactDensityChanged(bool value)
    {
        UpdateDensityVisuals();
    }

    partial void OnShowTraceChanged(bool value) => OnLevelFilterChanged();

    partial void OnShowDebugChanged(bool value) => OnLevelFilterChanged();

    partial void OnShowInfoChanged(bool value) => OnLevelFilterChanged();

    partial void OnShowWarnChanged(bool value) => OnLevelFilterChanged();

    partial void OnShowErrorChanged(bool value) => OnLevelFilterChanged();

    partial void OnShowFatalChanged(bool value) => OnLevelFilterChanged();

    private void OnEntriesAppended(object? sender, LogEntriesAppendedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var entry in e.AppendedEntries)
            {
                RegisterLogger(entry.LoggerName);
                if (!MatchesFilter(entry))
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
        });
    }

    private void RebuildVisibleEntries()
    {
        var snapshot = _ingestionSession.Snapshot();
        var filtered = snapshot.Where(MatchesFilter).TakeLast(MaxVisibleEntries).ToArray();

        VisibleEntries.Clear();
        foreach (var entry in filtered)
        {
            VisibleEntries.Add(entry);
        }
    }

    private bool MatchesFilter(LogEntry entry)
    {
        if (!IsLevelEnabled(entry.Level))
        {
            return false;
        }

        if (!IsLoggerEnabled(entry.LoggerName))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        var term = SearchText.Trim();
        return Contains(entry.LoggerName, term) ||
               Contains(entry.Message, term) ||
               Contains(entry.Exception, term) ||
               Contains(entry.ThreadName, term);
    }

    private static bool Contains(string? text, string term)
    {
        return !string.IsNullOrWhiteSpace(text) &&
               text.Contains(term, StringComparison.OrdinalIgnoreCase);
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

        return builder.ToString();
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
        ReceiverDefinitions.Clear();
        foreach (var udp in definitions.UdpReceivers)
        {
            ReceiverDefinitions.Add(new ReceiverDefinitionViewModel(ReceiverKinds.Udp)
            {
                Id = udp.Id,
                DisplayName = udp.DisplayName,
                Enabled = udp.Enabled,
                BindAddress = udp.BindAddress,
                Port = udp.Port
            });
        }

        foreach (var tcp in definitions.TcpReceivers)
        {
            ReceiverDefinitions.Add(new ReceiverDefinitionViewModel(ReceiverKinds.Tcp)
            {
                Id = tcp.Id,
                DisplayName = tcp.DisplayName,
                Enabled = tcp.Enabled,
                BindAddress = tcp.BindAddress,
                Port = tcp.Port
            });
        }

        foreach (var file in definitions.FileTailReceivers)
        {
            ReceiverDefinitions.Add(new ReceiverDefinitionViewModel(ReceiverKinds.File)
            {
                Id = file.Id,
                DisplayName = file.DisplayName,
                Enabled = file.Enabled,
                FilePath = file.FilePath,
                PollIntervalMs = file.PollIntervalMs
            });
        }

        SelectedReceiverDefinition = ReceiverDefinitions.FirstOrDefault();
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
                DefaultLoggerName = x.DisplayName
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
                DefaultLoggerName = x.DisplayName
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
                DefaultLoggerName = x.DisplayName
            })
            .ToArray();

        return new ReceiverDefinitions
        {
            UdpReceivers = udp,
            TcpReceivers = tcp,
            FileTailReceivers = file
        };
    }

    private string BuildUniqueReceiverId(string prefix)
    {
        var next = 1;
        var existing = new HashSet<string>(ReceiverDefinitions.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
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
