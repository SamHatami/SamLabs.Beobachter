using System.Globalization;
using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Core.Models;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

public sealed class ShellStatusFormatterTests
{
    [Fact]
    public void Build_FormatsStatusAndSessionHealth()
    {
        IShellStatusFormatter formatter = new ShellStatusFormatter();
        LogStatisticsSnapshot statisticsSnapshot =
            new()
            {
                LogsPerSecond1Minute = 12.4,
                ErrorsPerSecond1Minute = 1.1,
                LogsPerSecond5Minutes = 8.8,
                ErrorsPerSecond5Minutes = 0.6,
                TopLoggers = [new NamedCount { Name = "Orders.Api", Count = 17 }],
                TopReceivers = [new NamedCount { Name = "udp-prod", Count = 23 }]
            };

        ShellStatusPresentation presentation = formatter.Build(
            isPaused: true,
            isAutoScrollEnabled: false,
            totalCount: 144,
            visibleCount: 77,
            droppedCount: 3,
            activeReceivers: 2,
            structuredOnlyCount: 19,
            statisticsSnapshot: statisticsSnapshot);

        string expectedStatsSummary1Minute = string.Format(CultureInfo.CurrentCulture, "1m: {0:F1} logs/s | {1:F1} err/s", 12.4, 1.1);
        string expectedStatsSummary5Minutes = string.Format(CultureInfo.CurrentCulture, "5m: {0:F1} logs/s | {1:F1} err/s", 8.8, 0.6);

        Assert.Equal("State: Processing Paused  Pin: Off  Total: 144  Visible: 77  Dropped: 3", presentation.StatusSummary);
        Assert.Equal(expectedStatsSummary1Minute, presentation.StatsSummary1Minute);
        Assert.Equal(expectedStatsSummary5Minutes, presentation.StatsSummary5Minutes);
        Assert.Equal("Top loggers (5m): Orders.Api (17)", presentation.TopLoggersSummary);
        Assert.Equal("Top receivers (5m): udp-prod (23)", presentation.TopReceiversSummary);
        Assert.Equal("Active receivers: 2", presentation.ActiveReceiversText);
        Assert.Equal("Buffered entries: 144", presentation.BufferedEntriesText);
        Assert.Equal("Structured events: 19", presentation.StructuredEventsText);
        Assert.Equal("Dropped packets: 3", presentation.DroppedPacketsText);
    }

    [Fact]
    public void Build_UsesDashWhenTopListsAreEmpty()
    {
        IShellStatusFormatter formatter = new ShellStatusFormatter();
        LogStatisticsSnapshot statisticsSnapshot = new();

        ShellStatusPresentation presentation = formatter.Build(
            isPaused: false,
            isAutoScrollEnabled: true,
            totalCount: 0,
            visibleCount: 0,
            droppedCount: 0,
            activeReceivers: 0,
            structuredOnlyCount: 0,
            statisticsSnapshot: statisticsSnapshot);

        Assert.Equal("Top loggers (5m): -", presentation.TopLoggersSummary);
        Assert.Equal("Top receivers (5m): -", presentation.TopReceiversSummary);
    }
}
