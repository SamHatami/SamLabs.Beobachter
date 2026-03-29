namespace SamLabs.Beobachter.Core.Settings;

public sealed record class LogLevelColorOverrides
{
    public LogLevelColorOverride Trace { get; init; } = new();

    public LogLevelColorOverride Debug { get; init; } = new();

    public LogLevelColorOverride Info { get; init; } = new();

    public LogLevelColorOverride Warn { get; init; } = new();

    public LogLevelColorOverride Error { get; init; } = new();

    public LogLevelColorOverride Fatal { get; init; } = new();
}
