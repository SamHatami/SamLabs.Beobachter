using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.ViewModels;

public partial class LoggerTreeItemViewModel : ObservableObject
{
    private readonly Action _stateChanged;
    private bool _isSyncing;

    [ObservableProperty]
    private bool _isEnabled;

    public LoggerTreeItemViewModel(LoggerNode node, Action stateChanged)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
        _stateChanged = stateChanged ?? throw new ArgumentNullException(nameof(stateChanged));
        _isEnabled = Node.IsEnabled;
        Children = new ObservableCollection<LoggerTreeItemViewModel>();

        foreach (var child in Node.Children.Values.OrderBy(static x => x.Name, StringComparer.Ordinal))
        {
            Children.Add(new LoggerTreeItemViewModel(child, stateChanged));
        }
    }

    public LoggerNode Node { get; }

    public string Name => Node.Name;

    public string FullPath => Node.FullPath;

    public ObservableCollection<LoggerTreeItemViewModel> Children { get; }

    partial void OnIsEnabledChanged(bool value)
    {
        if (_isSyncing)
        {
            return;
        }

        Node.SetEnabled(value, recursive: true);
        SyncFromNodeRecursive();
        _stateChanged();
    }

    public void SyncFromNodeRecursive()
    {
        _isSyncing = true;
        try
        {
            IsEnabled = Node.IsEnabled;
        }
        finally
        {
            _isSyncing = false;
        }

        foreach (var child in Children)
        {
            child.SyncFromNodeRecursive();
        }
    }
}
