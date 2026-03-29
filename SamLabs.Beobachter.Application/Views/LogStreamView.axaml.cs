using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using SamLabs.Beobachter.Application.ViewModels;

namespace SamLabs.Beobachter.Application.Views;

public partial class LogStreamView : UserControl
{
    private LogStreamViewModel? _viewModel;
    private bool _scrollPending;

    public LogStreamView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        DetachFromViewModel();
        AttachToViewModel(DataContext as LogStreamViewModel);
    }

    private void AttachToViewModel(LogStreamViewModel? viewModel)
    {
        if (viewModel is null)
        {
            return;
        }

        _viewModel = viewModel;
        _viewModel.VisibleEntries.CollectionChanged += OnVisibleEntriesCollectionChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void DetachFromViewModel()
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.VisibleEntries.CollectionChanged -= OnVisibleEntriesCollectionChanged;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = null;
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        DetachFromViewModel();
    }

    private void OnVisibleEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_viewModel is null || !_viewModel.IsAutoScrollEnabled)
        {
            return;
        }

        if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Reset)
        {
            RequestScrollToBottom();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel is null ||
            !string.Equals(e.PropertyName, nameof(LogStreamViewModel.IsAutoScrollEnabled), StringComparison.Ordinal) ||
            !_viewModel.IsAutoScrollEnabled)
        {
            return;
        }

        RequestScrollToBottom();
    }

    private void RequestScrollToBottom()
    {
        if (_scrollPending)
        {
            return;
        }

        _scrollPending = true;
        Dispatcher.UIThread.Post(() =>
        {
            _scrollPending = false;
            if (_viewModel is null ||
                !_viewModel.IsAutoScrollEnabled ||
                _viewModel.VisibleEntries.Count == 0)
            {
                return;
            }

            object lastEntry = _viewModel.VisibleEntries[_viewModel.VisibleEntries.Count - 1];
            LogEntriesList.ScrollIntoView(lastEntry);
        }, DispatcherPriority.Background);
    }
}
