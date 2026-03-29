using System;
using System.Collections.Generic;
using System.Linq;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.Services;

public sealed class ShellStatusFormatter : IShellStatusFormatter
{
    public ShellStatusPresentation Build(
        bool isPaused,
        bool isAutoScrollEnabled,
        int totalCount,
        int visibleCount,
        long droppedCount,
        int activeReceivers,
        int structuredOnlyCount,
        LogStatisticsSnapshot statisticsSnapshot)
    {
        ArgumentNullException.ThrowIfNull(statisticsSnapshot);

        string state = isPaused ? "Paused" : "Running";
        string pin = isAutoScrollEnabled ? "On" : "Off";

        string statusSummary = $"State: {state}  Pin: {pin}  Total: {totalCount}  Visible: {visibleCount}  Dropped: {droppedCount}";
        string statsSummary1Minute = $"1m: {statisticsSnapshot.LogsPerSecond1Minute:F1} logs/s | {statisticsSnapshot.ErrorsPerSecond1Minute:F1} err/s";
        string statsSummary5Minutes = $"5m: {statisticsSnapshot.LogsPerSecond5Minutes:F1} logs/s | {statisticsSnapshot.ErrorsPerSecond5Minutes:F1} err/s";

        return new ShellStatusPresentation(
            statusSummary,
            statsSummary1Minute,
            statsSummary5Minutes,
            FormatTop("Top loggers (5m)", statisticsSnapshot.TopLoggers),
            FormatTop("Top receivers (5m)", statisticsSnapshot.TopReceivers),
            $"Active receivers: {activeReceivers:N0}",
            $"Buffered entries: {totalCount:N0}",
            $"Structured events: {structuredOnlyCount:N0}",
            $"Dropped packets: {droppedCount:N0}");
    }

    private static string FormatTop(string label, IReadOnlyList<NamedCount> entries)
    {
        if (entries.Count == 0)
        {
            return $"{label}: -";
        }

        string summary = string.Join(", ", entries.Select(static x => $"{x.Name} ({x.Count})"));
        return $"{label}: {summary}";
    }
}
