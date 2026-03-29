namespace SamLabs.Beobachter.Core.Settings;

public sealed record class AppSettings
{
    public string ThemeMode { get; init; } = "System";

    public int ChannelCapacity { get; init; } = 50_000;

    public LogLevelColorOverrides LogLevelColors { get; init; } = new();
}
