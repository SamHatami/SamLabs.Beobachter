using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Core.Interfaces;

public interface ILogStatisticsService
{
    void RecordRange(IReadOnlyList<LogEntry> entries);

    void Reset();

    LogStatisticsSnapshot GetSnapshot(DateTimeOffset? nowUtc = null);
}
