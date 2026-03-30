using System.Collections.Generic;

namespace SamLabs.Beobachter.Core.Settings;

public sealed record class WorkspaceSettings
{
    public string SearchText { get; init; } = string.Empty;

    public string ReceiverFilter { get; init; } = string.Empty;

    public string LoggerFilter { get; init; } = string.Empty;

    public string ThreadFilter { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> PropertyFilters { get; init; } =
        new Dictionary<string, string>();

    public string MinimumLevelOption { get; init; } = "Any";

    public bool CompactDensity { get; init; }

    public string SelectedReceiverId { get; init; } = string.Empty;

    public bool AutoScroll { get; init; } = true;

    // Persists consumer pause state (processing freeze); receiver transports continue running.
    public bool PauseIngest { get; init; }

    public IReadOnlyList<string> EnabledLevels { get; init; } =
        ["Trace", "Debug", "Info", "Warn", "Error", "Fatal"];
}
