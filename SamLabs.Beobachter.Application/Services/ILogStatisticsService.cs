using System;
using System.Collections.Generic;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.Services;

public interface ILogStatisticsService
{
    void RecordRange(IReadOnlyList<LogEntry> entries);

    LogStatisticsSnapshot GetSnapshot(DateTimeOffset? nowUtc = null);
}
