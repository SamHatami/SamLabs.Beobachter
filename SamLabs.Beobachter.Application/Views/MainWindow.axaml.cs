using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Core.Interfaces;

namespace SamLabs.Beobachter.Application.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _boundViewModel;
    private ISettingsService? _settingsService;
    private IReleaseNotesProvider? _releaseNotesProvider;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        PropertyChanged += OnWindowPropertyChanged;
        Closed += OnClosed;
        UpdateWindowStateIcons();
    }

    public ISettingsService? SettingsService
    {
        get => _settingsService;
        set => _settingsService = value;
    }

    public IReleaseNotesProvider? ReleaseNotesProvider
    {
        get => _releaseNotesProvider;
        set => _releaseNotesProvider = value;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_boundViewModel is not null)
        {
            _boundViewModel.TopBar.SettingsRequested -= OnTopBarSettingsRequested;
            _boundViewModel.ReceiverTree.OpenEditorRequested -= OnReceiverTreeOpenEditorRequested;
        }

        _boundViewModel = DataContext as MainWindowViewModel;
        if (_boundViewModel is not null)
        {
            _boundViewModel.TopBar.SettingsRequested += OnTopBarSettingsRequested;
            _boundViewModel.ReceiverTree.OpenEditorRequested += OnReceiverTreeOpenEditorRequested;
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
            TopBarView? topBarView = StreamSearchHost.GetVisualDescendants()
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

        if (e.Key == Key.Escape)
        {
            _boundViewModel.Stream.SelectedEntry = null;
            e.Handled = true;
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
        await OpenReceiverEditorAsync();
    }

    private async void OnReceiverTreeOpenEditorRequested(object? sender, EventArgs e)
    {
        await OpenReceiverEditorAsync();
    }

    private async System.Threading.Tasks.Task OpenReceiverEditorAsync()
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

    private async void OnAppSettingsClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_settingsService is null || _releaseNotesProvider is null)
        {
            return;
        }

        AppSettingsWindow window = new()
        {
            DataContext = new AppSettingsViewModel(_settingsService, _releaseNotesProvider)
        };

        await window.ShowDialog(this);
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.Source is Visual sourceVisual &&
            sourceVisual.GetSelfAndVisualAncestors().OfType<Button>().Any())
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            e.Handled = true;
            return;
        }

        if (WindowState == WindowState.Normal)
        {
            BeginMoveDrag(e);
            e.Handled = true;
        }
    }

    private void OnMinimizeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WindowStateProperty)
        {
            UpdateWindowStateIcons();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_boundViewModel is not null)
        {
            _boundViewModel.TopBar.SettingsRequested -= OnTopBarSettingsRequested;
            _boundViewModel.ReceiverTree.OpenEditorRequested -= OnReceiverTreeOpenEditorRequested;
            _boundViewModel = null;
        }

        DataContextChanged -= OnDataContextChanged;
        PropertyChanged -= OnWindowPropertyChanged;
        Closed -= OnClosed;
    }

    private void UpdateWindowStateIcons()
    {
        if (this.FindControl<Control>("WindowMaximizeIcon") is not { } maximizeIcon ||
            this.FindControl<Control>("WindowRestoreIcon") is not { } restoreIcon)
        {
            return;
        }

        bool isMaximized = WindowState == WindowState.Maximized;
        maximizeIcon.IsVisible = !isMaximized;
        restoreIcon.IsVisible = isMaximized;
    }
}
