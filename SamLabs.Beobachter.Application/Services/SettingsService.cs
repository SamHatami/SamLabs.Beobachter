using System;
using System.Threading.Tasks;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Services;
using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Application.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly ISettingsStore _settingsStore;
    private readonly IThemeService _themeService;
    private readonly LogLevelColorResourceService _colorService;

    public SettingsService(
        ISettingsStore settingsStore,
        IThemeService themeService,
        LogLevelColorResourceService colorService)
    {
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _colorService = colorService ?? throw new ArgumentNullException(nameof(colorService));
    }

    public AppSettings CurrentAppSettings { get; private set; } = new();

    public event EventHandler<AppSettingsSavedEventArgs>? AppSettingsSaved;

    public async Task InitializeAsync()
    {
        CurrentAppSettings = await _settingsStore.LoadAppSettingsAsync().ConfigureAwait(false);
        ApplySettings(CurrentAppSettings);
    }

    public async Task UpdateAppSettingsAsync(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _settingsStore.SaveAppSettingsAsync(settings).ConfigureAwait(false);
        CurrentAppSettings = settings;
        ApplySettings(settings);
        AppSettingsSaved?.Invoke(this, new AppSettingsSavedEventArgs(settings));
    }

    private void ApplySettings(AppSettings settings)
    {
        if (Enum.TryParse<AppThemeMode>(settings.ThemeMode, true, out var themeMode))
        {
            _themeService.SetTheme(themeMode);
        }

        _colorService.ApplyOverrides(settings.LogLevelColors);
    }
}
