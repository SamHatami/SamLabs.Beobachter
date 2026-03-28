using SamLabs.Beobachter.Core.Enums;

namespace SamLabs.Beobachter.Core.Models;

public sealed record class LogEntry
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public long SequenceNumber { get; init; }

    public LogLevel Level { get; init; } = LogLevel.Info;

    public string ReceiverId { get; init; } = "Unknown";

    public string LoggerName { get; init; } = "Unknown";

    public string RootLoggerName { get; init; } = "Unknown";

    public string Message { get; init; } = string.Empty;

    public string ThreadName { get; init; } = string.Empty;

    public string? HostName { get; init; }

    public string? Exception { get; init; }

    public string? CallSiteClass { get; init; }

    public string? CallSiteMethod { get; init; }

    public string? SourceFileName { get; init; }

    public uint? SourceFileLineNumber { get; init; }

    public string? RawLevelName { get; init; }

    public int? RawLevelValue { get; init; }

    public IReadOnlyDictionary<string, string> Properties { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
