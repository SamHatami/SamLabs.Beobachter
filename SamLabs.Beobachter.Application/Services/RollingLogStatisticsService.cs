using System;
using System.Collections.Generic;
using System.Linq;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.Services;

public sealed class RollingLogStatisticsService : ILogStatisticsService
{
    private const int OneMinuteWindowSeconds = 60;
    private const int FiveMinuteWindowSeconds = 300;

    private readonly object _gate = new();
    private readonly Dictionary<long, SecondBucket> _buckets = new();

    public void RecordRange(IReadOnlyList<LogEntry> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        lock (_gate)
        {
            foreach (var entry in entries)
            {
                var second = entry.Timestamp.ToUniversalTime().ToUnixTimeSeconds();
                if (!_buckets.TryGetValue(second, out var bucket))
                {
                    bucket = new SecondBucket();
                    _buckets[second] = bucket;
                }

                bucket.Total++;
                if (entry.Level >= LogLevel.Error)
                {
                    bucket.Errors++;
                }

                Increment(bucket.LoggerCounts, entry.LoggerName);
                Increment(bucket.ReceiverCounts, entry.ReceiverId);
            }

            TrimBuckets(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }
    }

    public void Reset()
    {
        lock (_gate)
        {
            _buckets.Clear();
        }
    }

    public LogStatisticsSnapshot GetSnapshot(DateTimeOffset? nowUtc = null)
    {
        var now = (nowUtc ?? DateTimeOffset.UtcNow).ToUniversalTime().ToUnixTimeSeconds();
        var min1Start = now - (OneMinuteWindowSeconds - 1);
        var min5Start = now - (FiveMinuteWindowSeconds - 1);

        lock (_gate)
        {
            TrimBuckets(now);

            var total1 = 0;
            var errors1 = 0;
            var total5 = 0;
            var errors5 = 0;
            var loggerTotals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var receiverTotals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in _buckets)
            {
                var second = pair.Key;
                var bucket = pair.Value;
                if (second >= min1Start)
                {
                    total1 += bucket.Total;
                    errors1 += bucket.Errors;
                }

                if (second < min5Start)
                {
                    continue;
                }

                total5 += bucket.Total;
                errors5 += bucket.Errors;
                AddCounts(loggerTotals, bucket.LoggerCounts);
                AddCounts(receiverTotals, bucket.ReceiverCounts);
            }

            return new LogStatisticsSnapshot
            {
                Total1Minute = total1,
                Errors1Minute = errors1,
                LogsPerSecond1Minute = total1 / (double)OneMinuteWindowSeconds,
                ErrorsPerSecond1Minute = errors1 / (double)OneMinuteWindowSeconds,
                Total5Minutes = total5,
                Errors5Minutes = errors5,
                LogsPerSecond5Minutes = total5 / (double)FiveMinuteWindowSeconds,
                ErrorsPerSecond5Minutes = errors5 / (double)FiveMinuteWindowSeconds,
                TopLoggers = TopN(loggerTotals, 3),
                TopReceivers = TopN(receiverTotals, 3)
            };
        }
    }

    private void TrimBuckets(long nowSecond)
    {
        var cutoff = nowSecond - (FiveMinuteWindowSeconds - 1);
        if (_buckets.Count == 0)
        {
            return;
        }

        var stale = _buckets.Keys.Where(second => second < cutoff).ToArray();
        foreach (var second in stale)
        {
            _buckets.Remove(second);
        }
    }

    private static void Increment(IDictionary<string, int> counts, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        counts[name] = counts.TryGetValue(name, out var current) ? current + 1 : 1;
    }

    private static void AddCounts(IDictionary<string, int> target, IReadOnlyDictionary<string, int> source)
    {
        foreach (var pair in source)
        {
            target[pair.Key] = target.TryGetValue(pair.Key, out var current)
                ? current + pair.Value
                : pair.Value;
        }
    }

    private static IReadOnlyList<NamedCount> TopN(IReadOnlyDictionary<string, int> counts, int size)
    {
        return counts
            .OrderByDescending(static x => x.Value)
            .ThenBy(static x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Take(size)
            .Select(static x => new NamedCount { Name = x.Key, Count = x.Value })
            .ToArray();
    }

    private sealed class SecondBucket
    {
        public int Total { get; set; }

        public int Errors { get; set; }

        public Dictionary<string, int> LoggerCounts { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, int> ReceiverCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
