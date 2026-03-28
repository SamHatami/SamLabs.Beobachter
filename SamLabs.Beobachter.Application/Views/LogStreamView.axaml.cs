using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.Views;

public partial class LogStreamView : UserControl
{
    private const double NearBottomThreshold = 72.0;

    private LogStreamViewModel? _boundViewModel;
    private ScrollViewer? _logScrollViewer;

    public LogStreamView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_boundViewModel is not null)
        {
            _boundViewModel.VisibleEntries.CollectionChanged -= OnVisibleEntriesChanged;
            _boundViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _boundViewModel = DataContext as LogStreamViewModel;

        if (_boundViewModel is not null)
        {
            _boundViewModel.VisibleEntries.CollectionChanged += OnVisibleEntriesChanged;
            _boundViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        TryResolveLogScrollViewer();
    }

    private void OnVisibleEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_boundViewModel?.IsAutoScrollEnabled != true)
        {
            return;
        }

        if (e.Action != NotifyCollectionChangedAction.Add &&
            e.Action != NotifyCollectionChangedAction.Reset)
        {
            return;
        }

        if (!IsNearBottom())
        {
            return;
        }

        ScrollToLatest();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_boundViewModel is null)
        {
            return;
        }

        if (e.PropertyName is null)
        {
            ScrollSelectedIntoView();
            return;
        }

        if (string.Equals(e.PropertyName, nameof(LogStreamViewModel.IsAutoScrollEnabled), StringComparison.Ordinal))
        {
            if (_boundViewModel.IsAutoScrollEnabled)
            {
                ScrollToLatest();
            }

            return;
        }

        if (string.Equals(e.PropertyName, nameof(LogStreamViewModel.SelectedEntry), StringComparison.Ordinal))
        {
            ScrollSelectedIntoView();
        }
    }

    private void ScrollSelectedIntoView()
    {
        LogEntry? selected = _boundViewModel?.SelectedEntry;
        if (selected is null)
        {
            return;
        }

        Dispatcher.UIThread.Post(() => LogEntriesList.ScrollIntoView(selected));
    }

    private void ScrollToLatest()
    {
        if (LogEntriesList.ItemsSource is not System.Collections.Generic.IEnumerable<LogEntry> entries)
        {
            return;
        }

        LogEntry? last = entries.LastOrDefault();
        if (last is null)
        {
            return;
        }

        Dispatcher.UIThread.Post(() => LogEntriesList.ScrollIntoView(last));
    }

    private void TryResolveLogScrollViewer()
    {
        _logScrollViewer ??= VisualExtensions.FindDescendantOfType<ScrollViewer>(LogEntriesList, includeSelf: true);
    }

    private bool IsNearBottom()
    {
        TryResolveLogScrollViewer();
        if (_logScrollViewer is null)
        {
            return true;
        }

        var distance = _logScrollViewer.Extent.Height - (_logScrollViewer.Offset.Y + _logScrollViewer.Viewport.Height);
        return distance <= NearBottomThreshold;
    }
}
