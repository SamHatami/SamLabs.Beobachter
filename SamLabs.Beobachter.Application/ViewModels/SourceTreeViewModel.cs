using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.ViewModels;

public partial class SourceTreeViewModel : ViewModelBase
{
    private LoggerNode _loggerRoot = LoggerNode.CreateRoot();
    private readonly Dictionary<string, SourceFilterItemViewModel> _sourceIndex = new(StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<LoggerTreeItemViewModel> LoggerTreeItems { get; } = [];

    public ObservableCollection<SourceFilterItemViewModel> VisibleSourceItems { get; } = [];

    [ObservableProperty]
    private string _sourceSearchText = string.Empty;

    public event EventHandler? StateChanged;

    [RelayCommand]
    private void EnableAllLoggers()
    {
        _loggerRoot.SetEnabled(true, recursive: true);
        foreach (LoggerTreeItemViewModel item in LoggerTreeItems)
        {
            item.SyncFromNodeRecursive();
        }

        SyncSourceItemsFromTree();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RebuildFromSnapshot(IReadOnlyList<LogEntry> snapshot)
    {
        _loggerRoot = LoggerNode.CreateRoot();
        foreach (SourceFilterItemViewModel sourceItem in _sourceIndex.Values)
        {
            sourceItem.PropertyChanged -= OnSourceItemPropertyChanged;
        }

        _sourceIndex.Clear();
        VisibleSourceItems.Clear();
        foreach (LogEntry entry in snapshot)
        {
            RegisterLogger(entry.LoggerName, refreshTree: false);
            RegisterSource(entry.LoggerName, refreshVisibleItems: false);
        }

        RebuildLoggerTreeItems();
        SyncSourceItemsFromTree();
        RefreshVisibleSourceItems();
    }

    public void RegisterLogger(string loggerName, bool refreshTree = true)
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

    public void RegisterEntry(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        RegisterLogger(entry.LoggerName);
        RegisterSource(entry.LoggerName, refreshVisibleItems: true);
        SyncSourceItemsFromTree();
    }

    public void ResetSourceCounts()
    {
        foreach (SourceFilterItemViewModel sourceItem in _sourceIndex.Values)
        {
            sourceItem.Count = 0;
        }

        RefreshVisibleSourceItems();
    }

    public bool IsLoggerEnabled(string loggerName)
    {
        return !_loggerRoot.TryGetPath(loggerName, out LoggerNode? node) || node?.IsEnabled != false;
    }

    public bool IsEntryEnabled(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return !_sourceIndex.TryGetValue(entry.LoggerName, out SourceFilterItemViewModel? sourceItem) ||
               sourceItem.IsEnabled;
    }

    partial void OnSourceSearchTextChanged(string value)
    {
        RefreshVisibleSourceItems();
    }

    private void RebuildLoggerTreeItems()
    {
        LoggerTreeItems.Clear();
        foreach (LoggerNode child in _loggerRoot.Children.Values.OrderBy(static x => x.Name, StringComparer.Ordinal))
        {
            LoggerTreeItems.Add(new LoggerTreeItemViewModel(child, OnLoggerTreeStateChanged));
        }
    }

    private void RegisterSource(string loggerName, bool refreshVisibleItems)
    {
        if (string.IsNullOrWhiteSpace(loggerName))
        {
            return;
        }

        if (!_sourceIndex.TryGetValue(loggerName, out SourceFilterItemViewModel? sourceItem))
        {
            sourceItem = new SourceFilterItemViewModel(loggerName, OnSourceItemStateChanged)
            {
                Count = 0
            };
            sourceItem.PropertyChanged += OnSourceItemPropertyChanged;
            _sourceIndex[loggerName] = sourceItem;

            if (refreshVisibleItems)
            {
                RefreshVisibleSourceItems();
            }
        }

        sourceItem.Count++;
    }

    private void OnLoggerTreeStateChanged()
    {
        SyncSourceItemsFromTree();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnSourceItemStateChanged(SourceFilterItemViewModel sourceItem)
    {
        if (!_loggerRoot.TryGetPath(sourceItem.Name, out LoggerNode? node) || node is null)
        {
            return;
        }

        node.SetEnabled(sourceItem.IsEnabled, recursive: true);
        foreach (LoggerTreeItemViewModel item in LoggerTreeItems)
        {
            item.SyncFromNodeRecursive();
        }

        SyncSourceItemsFromTree();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnSourceItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not SourceFilterItemViewModel ||
            !string.Equals(e.PropertyName, nameof(SourceFilterItemViewModel.IsEnabled), StringComparison.Ordinal))
        {
            return;
        }

        RefreshVisibleSourceItems();
    }

    private void SyncSourceItemsFromTree()
    {
        foreach (KeyValuePair<string, SourceFilterItemViewModel> entry in _sourceIndex)
        {
            entry.Value.SetIsEnabledSilently(IsLoggerEnabled(entry.Key));
        }
    }

    private void RefreshVisibleSourceItems()
    {
        string filter = SourceSearchText.Trim();
        List<SourceFilterItemViewModel> matching = _sourceIndex.Values
            .Where(item =>
                filter.Length == 0 ||
                item.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        VisibleSourceItems.Clear();
        foreach (SourceFilterItemViewModel item in matching)
        {
            VisibleSourceItems.Add(item);
        }
    }
}
