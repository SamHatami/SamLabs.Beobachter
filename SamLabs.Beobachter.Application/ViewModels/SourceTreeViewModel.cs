using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.ViewModels;

public partial class SourceTreeViewModel : ViewModelBase
{
    private LoggerNode _loggerRoot = LoggerNode.CreateRoot();

    public ObservableCollection<LoggerTreeItemViewModel> LoggerTreeItems { get; } = [];

    public event EventHandler? StateChanged;

    [RelayCommand]
    private void EnableAllLoggers()
    {
        _loggerRoot.SetEnabled(true, recursive: true);
        foreach (LoggerTreeItemViewModel item in LoggerTreeItems)
        {
            item.SyncFromNodeRecursive();
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RebuildFromSnapshot(IReadOnlyList<LogEntry> snapshot)
    {
        _loggerRoot = LoggerNode.CreateRoot();
        foreach (LogEntry entry in snapshot)
        {
            RegisterLogger(entry.LoggerName, refreshTree: false);
        }

        RebuildLoggerTreeItems();
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

    public bool IsLoggerEnabled(string loggerName)
    {
        return !_loggerRoot.TryGetPath(loggerName, out LoggerNode? node) || node?.IsEnabled != false;
    }

    private void RebuildLoggerTreeItems()
    {
        LoggerTreeItems.Clear();
        foreach (LoggerNode child in _loggerRoot.Children.Values.OrderBy(static x => x.Name, StringComparer.Ordinal))
        {
            LoggerTreeItems.Add(new LoggerTreeItemViewModel(child, OnLoggerTreeStateChanged));
        }
    }

    private void OnLoggerTreeStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
