using System;
using System.Threading.Tasks;
using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Core.Interfaces;

public interface ISettingsService
{
    AppSettings CurrentAppSettings { get; }

    event EventHandler<AppSettingsSavedEventArgs>? AppSettingsSaved;

    Task InitializeAsync();

    Task UpdateAppSettingsAsync(AppSettings settings);
}

public sealed class AppSettingsSavedEventArgs : EventArgs
{
    public AppSettingsSavedEventArgs(AppSettings settings)
    {
        Settings = settings;
    }

    public AppSettings Settings { get; }
}
