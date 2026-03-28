using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.Views;

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
            _boundViewModel.Stream.VisibleEntries.CollectionChanged -= OnVisibleEntriesChanged;
            _boundViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _boundViewModel = DataContext as MainWindowViewModel;

        if (_boundViewModel is not null)
        {
            _boundViewModel.Stream.VisibleEntries.CollectionChanged += OnVisibleEntriesChanged;
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

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (_boundViewModel is null)
        {
            return;
        }

        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.F)
        {
            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
            e.Handled = true;
            return;
        }

        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.C)
        {
            _boundViewModel.Details.CopySelectedDetailsCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.KeyModifiers != KeyModifiers.None)
        {
            return;
        }

        if (e.Source is TextBox)
        {
            return;
        }

        if (e.Key == Key.Down)
        {
            MoveSelection(1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Up)
        {
            MoveSelection(-1);
            e.Handled = true;
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

    private void MoveSelection(int delta)
    {
        if (_boundViewModel is null || _boundViewModel.Stream.VisibleEntries.Count == 0)
        {
            return;
        }

        var current = _boundViewModel.Stream.SelectedEntry;
        var currentIndex = current is null ? -1 : _boundViewModel.Stream.VisibleEntries.IndexOf(current);
        var nextIndex = currentIndex < 0
            ? (delta > 0 ? 0 : _boundViewModel.Stream.VisibleEntries.Count - 1)
            : Math.Clamp(currentIndex + delta, 0, _boundViewModel.Stream.VisibleEntries.Count - 1);

        var next = _boundViewModel.Stream.VisibleEntries[nextIndex];
        _boundViewModel.Stream.SelectedEntry = next;
        LogEntriesList.ScrollIntoView(next);
    }
}
