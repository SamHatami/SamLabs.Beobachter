using System;
using System.Collections.Generic;
using System.Linq;

namespace SamLabs.Beobachter.Application.Services;

public sealed class WorkspaceStateUpdate
{
    public WorkspaceStateUpdate(
        string searchText,
        string receiverFilter,
        string loggerFilter,
        string threadFilter,
        IReadOnlyDictionary<string, string> propertyFilters,
        string minimumLevelOption,
        bool compactDensity,
        string selectedReceiverId,
        IReadOnlyList<string> enabledLevels,
        bool autoScroll,
        bool pauseIngest,
        double timestampColumnWidth,
        double levelColumnWidth,
        double loggerColumnWidth)
    {
        ArgumentNullException.ThrowIfNull(searchText);
        ArgumentNullException.ThrowIfNull(receiverFilter);
        ArgumentNullException.ThrowIfNull(loggerFilter);
        ArgumentNullException.ThrowIfNull(threadFilter);
        ArgumentNullException.ThrowIfNull(propertyFilters);
        ArgumentNullException.ThrowIfNull(minimumLevelOption);
        ArgumentNullException.ThrowIfNull(selectedReceiverId);
        ArgumentNullException.ThrowIfNull(enabledLevels);

        SearchText = searchText;
        ReceiverFilter = receiverFilter;
        LoggerFilter = loggerFilter;
        ThreadFilter = threadFilter;
        PropertyFilters = propertyFilters;
        MinimumLevelOption = minimumLevelOption;
        CompactDensity = compactDensity;
        SelectedReceiverId = selectedReceiverId;
        EnabledLevels = enabledLevels.ToArray();
        AutoScroll = autoScroll;
        PauseIngest = pauseIngest;
        TimestampColumnWidth = timestampColumnWidth;
        LevelColumnWidth = levelColumnWidth;
        LoggerColumnWidth = loggerColumnWidth;
    }

    public string SearchText { get; }

    public string ReceiverFilter { get; }

    public string LoggerFilter { get; }

    public string ThreadFilter { get; }

    public IReadOnlyDictionary<string, string> PropertyFilters { get; }

    public string MinimumLevelOption { get; }

    public bool CompactDensity { get; }

    public string SelectedReceiverId { get; }

    public IReadOnlyList<string> EnabledLevels { get; }

    public bool AutoScroll { get; }

    public bool PauseIngest { get; }

    public double TimestampColumnWidth { get; }

    public double LevelColumnWidth { get; }

    public double LoggerColumnWidth { get; }
}
