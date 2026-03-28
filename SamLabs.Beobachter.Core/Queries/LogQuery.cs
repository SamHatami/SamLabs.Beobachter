using SamLabs.Beobachter.Core.Enums;

namespace SamLabs.Beobachter.Core.Queries;

public sealed record class LogQuery
{
    public LogLevel? MinimumLevel { get; init; }

    public string? TextContains { get; init; }

    public string? LoggerPrefix { get; init; }

    public string? LoggerContains { get; init; }

    public string? ReceiverId { get; init; }

    public string? ThreadContains { get; init; }

    public IReadOnlyDictionary<string, string> PropertyContains { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public DateTimeOffset? FromUtc { get; init; }

    public DateTimeOffset? ToUtc { get; init; }
}
