using System;
using System.Collections.Generic;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.Services;

public sealed class SampleLogEntryGenerator : ISampleLogEntryGenerator
{
    public IReadOnlyList<LogEntry> CreateBatch(int startSequenceNumber, int count)
    {
        if (count <= 0)
        {
            return Array.Empty<LogEntry>();
        }

        DateTimeOffset now = DateTimeOffset.Now;
        List<LogEntry> entries = new(count);
        for (int i = 0; i < count; i++)
        {
            int index = i + 1;
            LogLevel level = PickRandomLevel();
            entries.Add(new LogEntry
            {
                Timestamp = now.AddMilliseconds(index * 10),
                SequenceNumber = startSequenceNumber + i,
                Level = level,
                ReceiverId = "sample",
                LoggerName = $"Sample.Component.{Random.Shared.Next(1, 5)}",
                RootLoggerName = $"Sample.Component.{Random.Shared.Next(1, 5)}",
                ThreadName = $"worker-{Random.Shared.Next(1, 4)}",
                Message = $"Sample {level} event #{Random.Shared.Next(100, 999)}"
            });
        }

        return entries;
    }

    private static LogLevel PickRandomLevel()
    {
        LogLevel[] levels =
        [
            LogLevel.Trace,
            LogLevel.Debug,
            LogLevel.Info,
            LogLevel.Warn,
            LogLevel.Error,
            LogLevel.Fatal
        ];

        return levels[Random.Shared.Next(0, levels.Length)];
    }
}
