namespace SamLabs.Beobachter.Core.Settings;

public sealed record class UiLayoutSettings
{
    public double WindowWidth { get; init; } = 1200;

    public double WindowHeight { get; init; } = 760;

    public bool IsMaximized { get; init; }

    public double DetailsPaneWidthRatio { get; init; } = 0.35;

    public double TimestampColumnWidth { get; init; } = 180;

    public double LevelColumnWidth { get; init; } = 90;

    public double LoggerColumnWidth { get; init; } = 220;
}
