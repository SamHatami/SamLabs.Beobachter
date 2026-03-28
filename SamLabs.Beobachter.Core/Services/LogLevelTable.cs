using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Core.Services;

public static class LogLevelTable
{
    private sealed record RangeBucket(int MinInclusive, int MaxInclusive, LogLevel Level);

    private static readonly RangeBucket[] NumericRanges =
    [
        new(0, 10000, LogLevel.Trace),
        new(10001, 30000, LogLevel.Debug),
        new(30001, 40000, LogLevel.Info),
        new(40001, 60000, LogLevel.Warn),
        new(60001, 70000, LogLevel.Error),
        new(70001, 110000, LogLevel.Fatal)
    ];

    private static readonly IReadOnlyDictionary<string, LogLevel> StringToLevel =
        new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase)
        {
            ["TRACE"] = LogLevel.Trace,
            ["DEBUG"] = LogLevel.Debug,
            ["INFO"] = LogLevel.Info,
            ["INFORMATION"] = LogLevel.Info,
            ["WARN"] = LogLevel.Warn,
            ["WARNING"] = LogLevel.Warn,
            ["ERROR"] = LogLevel.Error,
            ["FATAL"] = LogLevel.Fatal
        };

    public static LogLevel FromNumeric(int levelValue, LogLevel fallback = LogLevel.None)
    {
        foreach (var bucket in NumericRanges)
        {
            if (levelValue >= bucket.MinInclusive && levelValue <= bucket.MaxInclusive)
            {
                return bucket.Level;
            }
        }

        return fallback;
    }

    public static LogLevel FromName(string? levelName, LogLevel fallback = LogLevel.None)
    {
        if (string.IsNullOrWhiteSpace(levelName))
        {
            return fallback;
        }

        return StringToLevel.TryGetValue(levelName.Trim(), out var normalizedLevel)
            ? normalizedLevel
            : fallback;
    }

    // Numeric values are treated as authoritative when available because many
    // log4j/NLog payloads encode level semantics through integer ranges.
    public static NormalizedLogLevel Normalize(
        string? rawLevelName,
        int? rawLevelValue,
        LogLevel fallback = LogLevel.None)
    {
        if (rawLevelValue.HasValue)
        {
            var fromValue = FromNumeric(rawLevelValue.Value, fallback);
            return new NormalizedLogLevel(
                fromValue,
                rawLevelName,
                rawLevelValue,
                fromValue == fallback);
        }

        var fromName = FromName(rawLevelName, fallback);
        return new NormalizedLogLevel(
            fromName,
            rawLevelName,
            rawLevelValue,
            fromName == fallback);
    }
}
