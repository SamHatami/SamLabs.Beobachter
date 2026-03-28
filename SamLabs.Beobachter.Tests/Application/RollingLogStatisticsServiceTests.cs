using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

public sealed class RollingLogStatisticsServiceTests
{
    [Fact]
    public void Snapshot_ComputesOneMinuteAndFiveMinuteMetrics()
    {
        var service = new RollingLogStatisticsService();
        var now = DateTimeOffset.UtcNow;

        service.RecordRange(
        [
            CreateEntry(now.AddSeconds(-10), LogLevel.Info, "Orders.Api", "udp-1"),
            CreateEntry(now.AddSeconds(-8), LogLevel.Error, "Orders.Api", "udp-1"),
            CreateEntry(now.AddSeconds(-120), LogLevel.Warn, "Billing.Worker", "tcp-2")
        ]);

        var snapshot = service.GetSnapshot(now);

        Assert.Equal(2, snapshot.Total1Minute);
        Assert.Equal(1, snapshot.Errors1Minute);
        Assert.Equal(3, snapshot.Total5Minutes);
        Assert.Equal(1, snapshot.Errors5Minutes);
        Assert.Equal("Orders.Api", snapshot.TopLoggers[0].Name);
        Assert.Equal(2, snapshot.TopLoggers[0].Count);
        Assert.Equal("udp-1", snapshot.TopReceivers[0].Name);
        Assert.Equal(2, snapshot.TopReceivers[0].Count);
    }

    [Fact]
    public void Snapshot_DropsBucketsOlderThanFiveMinutes()
    {
        var service = new RollingLogStatisticsService();
        var now = DateTimeOffset.UtcNow;

        service.RecordRange(
        [
            CreateEntry(now.AddSeconds(-410), LogLevel.Error, "Old.Logger", "udp-old"),
            CreateEntry(now.AddSeconds(-20), LogLevel.Info, "Live.Logger", "udp-live")
        ]);

        var snapshot = service.GetSnapshot(now);

        Assert.Equal(1, snapshot.Total5Minutes);
        Assert.Equal("Live.Logger", snapshot.TopLoggers[0].Name);
    }

    private static LogEntry CreateEntry(DateTimeOffset timestamp, LogLevel level, string logger, string receiver)
    {
        return new LogEntry
        {
            Timestamp = timestamp,
            Level = level,
            ReceiverId = receiver,
            LoggerName = logger,
            RootLoggerName = logger,
            Message = "sample"
        };
    }
}
