using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed partial class TopBarViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    public event EventHandler? SearchTextChanged;

    public event EventHandler? SettingsRequested;

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void OpenSettings()
    {
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    partial void OnSearchTextChanged(string value)
    {
        SearchTextChanged?.Invoke(this, EventArgs.Empty);
    }
}
