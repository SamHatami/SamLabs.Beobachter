using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
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
        if (_boundViewModel is not null)
        {
            _boundViewModel.TopBar.SettingsRequested -= OnTopBarSettingsRequested;
        }

        _boundViewModel = DataContext as MainWindowViewModel;
        if (_boundViewModel is not null)
        {
            _boundViewModel.TopBar.SettingsRequested += OnTopBarSettingsRequested;
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
            TopBarView? topBarView = TopBarHost.GetVisualDescendants()
                .OfType<TopBarView>()
                .FirstOrDefault();
            topBarView?.FocusSearchBox();
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

    private async void OnTopBarSettingsRequested(object? sender, EventArgs e)
    {
        if (_boundViewModel is null)
        {
            return;
        }

        ReceiverSetupWindow window = new()
        {
            DataContext = _boundViewModel.ReceiverSetup
        };

        await window.ShowDialog(this);
    }
}
