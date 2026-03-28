namespace SamLabs.Beobachter.Core.Settings;

public sealed record class WorkspaceSettings
{
    public string SearchText { get; init; } = string.Empty;

    public string ReceiverFilter { get; init; } = string.Empty;

    public string LoggerFilter { get; init; } = string.Empty;

    public string ThreadFilter { get; init; } = string.Empty;

    public string TenantFilter { get; init; } = string.Empty;

    public string TraceIdFilter { get; init; } = string.Empty;

    public string MinimumLevelOption { get; init; } = "Any";

    public bool CompactDensity { get; init; }

    public string SelectedReceiverId { get; init; } = string.Empty;

    public bool AutoScroll { get; init; } = true;

    public bool PauseIngest { get; init; }

    public IReadOnlyList<string> EnabledLevels { get; init; } =
        ["Trace", "Debug", "Info", "Warn", "Error", "Fatal"];
}
