using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace SamLabs.Beobachter.Application.ViewModels.Toolbar;

public sealed partial class MainToolbarViewModel : ViewModelBase
{
    private static readonly HashSet<string> ObservedShellPropertyNames = new(StringComparer.Ordinal)
    {
        nameof(MainWindowViewModel.ThemeSummary),
        nameof(MainWindowViewModel.StatusSummary),
        nameof(MainWindowViewModel.StatsSummary1Minute),
        nameof(MainWindowViewModel.StatsSummary5Minutes),
        nameof(MainWindowViewModel.TopLoggersSummary),
        nameof(MainWindowViewModel.TopReceiversSummary),
        nameof(MainWindowViewModel.PauseButtonText),
        nameof(MainWindowViewModel.AutoScrollButtonText)
    };

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

    [RelayCommand]
    private void UseSystemTheme()
    {
        _shell.UseSystemThemeCommand.Execute(null);
    }

    [RelayCommand]
    private void UseLightTheme()
    {
        _shell.UseLightThemeCommand.Execute(null);
    }

    [RelayCommand]
    private void UseDarkTheme()
    {
        _shell.UseDarkThemeCommand.Execute(null);
    }

    [RelayCommand]
    private async Task TogglePause()
    {
        await _shell.TogglePauseCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task ToggleAutoScroll()
    {
        await _shell.ToggleAutoScrollCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void ToggleDensity()
    {
        _shell.Stream.ToggleDensityCommand.Execute(null);
    }

    private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null || ObservedShellPropertyNames.Contains(e.PropertyName))
        {
            RaiseToolbarStateChanged();
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

    private void RaiseToolbarStateChanged()
    {
        OnPropertyChanged(nameof(ThemeSummary));
        OnPropertyChanged(nameof(StatusSummary));
        OnPropertyChanged(nameof(StatsSummary1Minute));
        OnPropertyChanged(nameof(StatsSummary5Minutes));
        OnPropertyChanged(nameof(TopLoggersSummary));
        OnPropertyChanged(nameof(TopReceiversSummary));
        OnPropertyChanged(nameof(PauseButtonText));
        OnPropertyChanged(nameof(AutoScrollButtonText));
    }
}
