using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Core.Interfaces;

public interface ILogStatisticsService
{
    void RecordRange(IReadOnlyList<LogEntry> entries);

    LogStatisticsSnapshot GetSnapshot(DateTimeOffset? nowUtc = null);
}
