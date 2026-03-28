using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.ViewModels;

namespace SamLabs.Beobachter.Views;

public partial class MainWindow : Window
{
    private const double NearBottomThreshold = 72.0;

    private MainWindowViewModel? _boundViewModel;
    private ScrollViewer? _logScrollViewer;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Opened += OnOpened;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_boundViewModel is not null)
        {
            _boundViewModel.VisibleEntries.CollectionChanged -= OnVisibleEntriesChanged;
            _boundViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _boundViewModel = DataContext as MainWindowViewModel;

        if (_boundViewModel is not null)
        {
            _boundViewModel.VisibleEntries.CollectionChanged += OnVisibleEntriesChanged;
            _boundViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnOpened(object? sender, EventArgs e)
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
        if (_boundViewModel is null ||
            !string.Equals(e.PropertyName, nameof(MainWindowViewModel.IsAutoScrollEnabled), StringComparison.Ordinal))
        {
            return;
        }

        if (_boundViewModel.IsAutoScrollEnabled)
        {
            ScrollToLatest();
        }
    }

    private void ScrollToLatest()
    {
        TryResolveLogScrollViewer();

        if (LogEntriesList.ItemsSource is not System.Collections.Generic.IEnumerable<LogEntry> entries)
        {
            return;
        }

        var last = entries.LastOrDefault();
        if (last is null)
        {
            return;
        }

        Dispatcher.UIThread.Post(() => LogEntriesList.ScrollIntoView(last));
    }

    private void TryResolveLogScrollViewer()
    {
        _logScrollViewer ??= LogEntriesList.FindDescendantOfType<ScrollViewer>(includeSelf: true);
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
