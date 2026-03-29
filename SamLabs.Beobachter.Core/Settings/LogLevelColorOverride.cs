namespace SamLabs.Beobachter.Core.Settings;

public sealed record class LogLevelColorOverride
{
    public string? Row { get; init; }

    public string? Badge { get; init; }

    public string? Message { get; init; }
}
