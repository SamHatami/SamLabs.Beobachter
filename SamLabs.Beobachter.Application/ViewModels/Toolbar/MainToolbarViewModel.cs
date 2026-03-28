using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SamLabs.Beobachter.Application.ViewModels.Toolbar;

public sealed class MainToolbarViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _shell;

    public MainToolbarViewModel(MainWindowViewModel shell)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _shell.PropertyChanged += OnShellPropertyChanged;
        _shell.Stream.PropertyChanged += OnStreamPropertyChanged;
    }

    public string Title => "SamLabs.Beobachter";

    public string ThemeSummary => _shell.ThemeSummary;

    public string StatusSummary => _shell.StatusSummary;

    public string StatsSummary1Minute => _shell.StatsSummary1Minute;

    public string StatsSummary5Minutes => _shell.StatsSummary5Minutes;

    public string TopLoggersSummary => _shell.TopLoggersSummary;

    public string TopReceiversSummary => _shell.TopReceiversSummary;

    public string PauseButtonText => _shell.PauseButtonText;

    public string AutoScrollButtonText => _shell.AutoScrollButtonText;

    public string DensityButtonText => _shell.Stream.DensityButtonText;

    public IRelayCommand UseSystemThemeCommand => _shell.UseSystemThemeCommand;

    public IRelayCommand UseLightThemeCommand => _shell.UseLightThemeCommand;

    public IRelayCommand UseDarkThemeCommand => _shell.UseDarkThemeCommand;

    public IAsyncRelayCommand TogglePauseCommand => _shell.TogglePauseCommand;

    public IAsyncRelayCommand ToggleAutoScrollCommand => _shell.ToggleAutoScrollCommand;

    public IRelayCommand ToggleDensityCommand => _shell.Stream.ToggleDensityCommand;

    private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null)
        {
            OnPropertyChanged(nameof(ThemeSummary));
            OnPropertyChanged(nameof(StatusSummary));
            OnPropertyChanged(nameof(StatsSummary1Minute));
            OnPropertyChanged(nameof(StatsSummary5Minutes));
            OnPropertyChanged(nameof(TopLoggersSummary));
            OnPropertyChanged(nameof(TopReceiversSummary));
            OnPropertyChanged(nameof(PauseButtonText));
            OnPropertyChanged(nameof(AutoScrollButtonText));
            return;
        }

        if (string.Equals(e.PropertyName, nameof(MainWindowViewModel.ThemeSummary), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(ThemeSummary));
            return;
        }

        if (string.Equals(e.PropertyName, nameof(MainWindowViewModel.StatusSummary), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(StatusSummary));
            return;
        }

        if (string.Equals(e.PropertyName, nameof(MainWindowViewModel.StatsSummary1Minute), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(StatsSummary1Minute));
            return;
        }

        if (string.Equals(e.PropertyName, nameof(MainWindowViewModel.StatsSummary5Minutes), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(StatsSummary5Minutes));
            return;
        }

        if (string.Equals(e.PropertyName, nameof(MainWindowViewModel.TopLoggersSummary), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(TopLoggersSummary));
            return;
        }

        if (string.Equals(e.PropertyName, nameof(MainWindowViewModel.TopReceiversSummary), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(TopReceiversSummary));
            return;
        }

        if (string.Equals(e.PropertyName, nameof(MainWindowViewModel.PauseButtonText), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(PauseButtonText));
            return;
        }

        if (string.Equals(e.PropertyName, nameof(MainWindowViewModel.AutoScrollButtonText), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(AutoScrollButtonText));
        }
    }

    private void OnStreamPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null ||
            string.Equals(e.PropertyName, nameof(LogStreamViewModel.DensityButtonText), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(DensityButtonText));
        }
    }
}
