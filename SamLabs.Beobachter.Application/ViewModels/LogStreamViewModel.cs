using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.ViewModels;

public partial class LogStreamViewModel : ViewModelBase
{
    private const int MaxVisibleEntries = 2_000;

    [ObservableProperty]
    private bool _isCompactDensity;

    [ObservableProperty]
    private bool _isAutoScrollEnabled = true;

    [ObservableProperty]
    private string _densityButtonText = "Density: Comfortable";

    [ObservableProperty]
    private double _logRowFontSize = 12;

    [ObservableProperty]
    private Thickness _logRowMargin = new(4, 2, 4, 2);

    [NotifyPropertyChangedFor(nameof(LogColumnDefinitions))]
    [ObservableProperty]
    private double _timestampColumnWidth = 180;

    [NotifyPropertyChangedFor(nameof(LogColumnDefinitions))]
    [ObservableProperty]
    private double _levelColumnWidth = 90;

    [NotifyPropertyChangedFor(nameof(LogColumnDefinitions))]
    [ObservableProperty]
    private double _loggerColumnWidth = 220;

    [ObservableProperty]
    private LogEntry? _selectedEntry;

    public ObservableCollection<LogEntry> VisibleEntries { get; } = [];

    public string LogColumnDefinitions =>
        $"{TimestampColumnWidth:0},{LevelColumnWidth:0},{LoggerColumnWidth:0},*";

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
    private void SelectNextEntry()
    {
        MoveSelection(1);
    }

    [RelayCommand]
    private void SelectPreviousEntry()
    {
        MoveSelection(-1);
    }

    public void AppendEntries(
        IReadOnlyList<LogEntry> entries,
        Func<LogEntry, bool> filter)
    {
        foreach (LogEntry entry in entries)
        {
            if (!filter(entry))
            {
                continue;
            }

            VisibleEntries.Add(entry);
            if (VisibleEntries.Count > MaxVisibleEntries)
            {
                VisibleEntries.RemoveAt(0);
            }
        }
    }

    public void RebuildEntries(
        IReadOnlyList<LogEntry> snapshot,
        Func<LogEntry, bool> filter)
    {
        LogEntry[] filtered = snapshot
            .Where(filter)
            .TakeLast(MaxVisibleEntries)
            .ToArray();

        VisibleEntries.Clear();
        foreach (LogEntry entry in filtered)
        {
            VisibleEntries.Add(entry);
        }
    }

    partial void OnIsCompactDensityChanged(bool value)
    {
        UpdateDensityVisuals();
    }

    private void AdjustColumnWidths(double delta)
    {
        TimestampColumnWidth = ClampColumnWidth(TimestampColumnWidth + delta, 100, 420);
        LevelColumnWidth = ClampColumnWidth(LevelColumnWidth + delta, 70, 200);
        LoggerColumnWidth = ClampColumnWidth(LoggerColumnWidth + delta, 120, 520);
    }

    private void MoveSelection(int delta)
    {
        if (VisibleEntries.Count == 0)
        {
            return;
        }

        var current = SelectedEntry;
        var currentIndex = current is null ? -1 : VisibleEntries.IndexOf(current);
        var nextIndex = currentIndex < 0
            ? (delta > 0 ? 0 : VisibleEntries.Count - 1)
            : Math.Clamp(currentIndex + delta, 0, VisibleEntries.Count - 1);

        SelectedEntry = VisibleEntries[nextIndex];
    }

    private static double ClampColumnWidth(double value, double min, double max)
    {
        return Math.Clamp(value, min, max);
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
}
