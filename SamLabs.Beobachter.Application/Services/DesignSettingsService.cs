using System;
using System.Threading.Tasks;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Application.Services;

internal sealed class DesignSettingsService : ISettingsService
{
    public AppSettings CurrentAppSettings { get; private set; } = new();

    public event EventHandler<AppSettingsSavedEventArgs>? AppSettingsSaved;

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task UpdateAppSettingsAsync(AppSettings settings)
    {
        CurrentAppSettings = settings;
        AppSettingsSaved?.Invoke(this, new AppSettingsSavedEventArgs(settings));
        return Task.CompletedTask;
    }
}
