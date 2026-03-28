namespace SamLabs.Beobachter.Infrastructure.Settings;

public sealed record class JsonSettingsStoreOptions
{
    public string RootDirectory { get; init; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SamLabs.Beobachter");
}
