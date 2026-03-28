using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.ViewModels;

namespace SamLabs.Beobachter.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _boundViewModel;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_boundViewModel is not null)
        {
            _boundViewModel.VisibleEntries.CollectionChanged -= OnVisibleEntriesChanged;
        }

        _boundViewModel = DataContext as MainWindowViewModel;

        if (_boundViewModel is not null)
        {
            _boundViewModel.VisibleEntries.CollectionChanged += OnVisibleEntriesChanged;
        }
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

        ScrollToLatest();
    }

    private void ScrollToLatest()
    {
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
}
