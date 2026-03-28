using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private const int MaxVisibleEntries = 2_000;

    private readonly IThemeService _themeService;
    private readonly IIngestionSession _ingestionSession;
    private readonly Random _random = new();

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

    public MainWindowViewModel() : this(new ThemeService(), new DesignIngestionSession())
    {
    }

    public MainWindowViewModel(IThemeService themeService, IIngestionSession ingestionSession)
    {
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _ingestionSession = ingestionSession ?? throw new ArgumentNullException(nameof(ingestionSession));

        _ingestionSession.EntriesAppended += OnEntriesAppended;
        IsPaused = _ingestionSession.IsPaused;
        RebuildVisibleEntries();
        UpdateThemeSummary();
        UpdateStatusSummary();
    }

    public ObservableCollection<LogEntry> VisibleEntries { get; } = [];

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

    private void OnEntriesAppended(object? sender, LogEntriesAppendedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var entry in e.AppendedEntries)
            {
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

    private void UpdateThemeSummary()
    {
        ThemeSummary = $"Theme: {_themeService.CurrentMode}";
    }

    private void UpdateStatusSummary()
    {
        var dropped = _ingestionSession.DroppedCount;
        var state = IsPaused ? "Paused" : "Running";
        StatusSummary = $"State: {state}  Total: {_ingestionSession.TotalCount}  Visible: {VisibleEntries.Count}  Dropped: {dropped}";
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
}
