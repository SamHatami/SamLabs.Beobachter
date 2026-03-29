using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Services;
using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed partial class AppSettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private int _channelCapacity;

    public AppSettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        LevelColors =
        [
            new("Trace"),
            new("Debug"),
            new("Info"),
            new("Warn"),
            new("Error"),
            new("Fatal")
        ];

        LoadFromCurrent();
    }

    public ObservableCollection<LogLevelColorItemViewModel> LevelColors { get; }

    public bool Saved { get; private set; }

    [RelayCommand]
    private async Task SaveAsync()
    {
        AppSettings updated = BuildSettings();
        await _settingsService.UpdateAppSettingsAsync(updated).ConfigureAwait(false);
        Saved = true;
    }

    private void LoadFromCurrent()
    {
        AppSettings current = _settingsService.CurrentAppSettings;

        SelectedThemeIndex = current.ThemeMode switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };

        ChannelCapacity = current.ChannelCapacity;

        LogLevelColorOverrides c = current.LogLevelColors;
        LevelColors[0].LoadFrom(c.Trace);
        LevelColors[1].LoadFrom(c.Debug);
        LevelColors[2].LoadFrom(c.Info);
        LevelColors[3].LoadFrom(c.Warn);
        LevelColors[4].LoadFrom(c.Error);
        LevelColors[5].LoadFrom(c.Fatal);
    }

    private AppSettings BuildSettings()
    {
        string themeMode = SelectedThemeIndex switch
        {
            1 => nameof(AppThemeMode.Light),
            2 => nameof(AppThemeMode.Dark),
            _ => nameof(AppThemeMode.System)
        };

        return new AppSettings
        {
            ThemeMode = themeMode,
            ChannelCapacity = Math.Clamp(ChannelCapacity, 1_000, 1_000_000),
            LogLevelColors = new LogLevelColorOverrides
            {
                Trace = LevelColors[0].ToOverride(),
                Debug = LevelColors[1].ToOverride(),
                Info = LevelColors[2].ToOverride(),
                Warn = LevelColors[3].ToOverride(),
                Error = LevelColors[4].ToOverride(),
                Fatal = LevelColors[5].ToOverride()
            }
        };
    }
}
