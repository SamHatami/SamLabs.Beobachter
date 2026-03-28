namespace SamLabs.Beobachter.Core.Settings;

public sealed record class WorkspaceSettings
{
    public string SearchText { get; init; } = string.Empty;

    public bool AutoScroll { get; init; } = true;

    public bool PauseIngest { get; init; }

    public IReadOnlyList<string> EnabledLevels { get; init; } =
        ["Trace", "Debug", "Info", "Warn", "Error", "Fatal"];
}
