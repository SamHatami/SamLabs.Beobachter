using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Services;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed partial class TopBarViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly IIngestionSession _ingestionSession;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private string _pauseButtonText = "Pause";

    [ObservableProperty]
    private string _pauseButtonIcon = "fa-solid fa-pause";

    [ObservableProperty]
    private string _themeSummary = string.Empty;

    public TopBarViewModel(
        IThemeService themeService,
        IIngestionSession ingestionSession)
    {
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _ingestionSession = ingestionSession ?? throw new ArgumentNullException(nameof(ingestionSession));

        IsPaused = _ingestionSession.IsPaused;
        UpdateThemeSummary();
    }

    public event EventHandler? SearchTextChanged;

    public event EventHandler? PauseToggled;

    public event EventHandler? SettingsRequested;

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    [RelayCommand]
    private async Task TogglePauseAsync()
    {
        bool nextState = !IsPaused;
        await _ingestionSession.SetPausedAsync(nextState).ConfigureAwait(false);
        IsPaused = nextState;
        PauseToggled?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void UseSystemTheme()
    {
        _themeService.SetTheme(AppThemeMode.System);
        UpdateThemeSummary();
    }

    [RelayCommand]
    private void UseLightTheme()
    {
        _themeService.SetTheme(AppThemeMode.Light);
        UpdateThemeSummary();
    }

    [RelayCommand]
    private void UseDarkTheme()
    {
        _themeService.SetTheme(AppThemeMode.Dark);
        UpdateThemeSummary();
    }

    partial void OnIsPausedChanged(bool value)
    {
        PauseButtonText = value ? "Resume" : "Pause";
        PauseButtonIcon = value ? "fa-solid fa-play" : "fa-solid fa-pause";
    }

    partial void OnSearchTextChanged(string value)
    {
        SearchTextChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateThemeSummary()
    {
        ThemeSummary = _themeService.CurrentMode.ToString();
    }
}
