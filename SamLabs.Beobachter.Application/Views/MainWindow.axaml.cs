using System;
using Avalonia.Controls;
using Avalonia.Input;
using SamLabs.Beobachter.Application.ViewModels;

namespace SamLabs.Beobachter.Application.Views;

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
        _boundViewModel = DataContext as MainWindowViewModel;
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (_boundViewModel is null)
        {
            return;
        }

        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.F)
        {
            LogQueryBar.FocusSearchBox();
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
            _boundViewModel.Stream.SelectNextEntryCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Up)
        {
            _boundViewModel.Stream.SelectPreviousEntryCommand.Execute(null);
            e.Handled = true;
        }
    }
}
