using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Core.Interfaces;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed partial class TopBarViewModel : ViewModelBase
{
    private readonly IIngestionSession _ingestionSession;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private string _pauseButtonText = "Pause";

    [ObservableProperty]
    private string _pauseButtonIcon = "fa-solid fa-pause";

    public TopBarViewModel(IIngestionSession ingestionSession)
    {
        _ingestionSession = ingestionSession ?? throw new ArgumentNullException(nameof(ingestionSession));
        IsPaused = _ingestionSession.IsPaused;
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

    partial void OnIsPausedChanged(bool value)
    {
        PauseButtonText = value ? "Resume" : "Pause";
        PauseButtonIcon = value ? "fa-solid fa-play" : "fa-solid fa-pause";
    }

    partial void OnSearchTextChanged(string value)
    {
        SearchTextChanged?.Invoke(this, EventArgs.Empty);
    }
}
