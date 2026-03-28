namespace SamLabs.Beobachter.Core.Models;

public sealed record class LogStatisticsSnapshot
{
    public int Total1Minute { get; init; }

    public int Errors1Minute { get; init; }

    public double LogsPerSecond1Minute { get; init; }

    public double ErrorsPerSecond1Minute { get; init; }

    public int Total5Minutes { get; init; }

    public int Errors5Minutes { get; init; }

    public double LogsPerSecond5Minutes { get; init; }

    public double ErrorsPerSecond5Minutes { get; init; }

    public IReadOnlyList<NamedCount> TopLoggers { get; init; } = [];

    public IReadOnlyList<NamedCount> TopReceivers { get; init; } = [];
}

public sealed record class NamedCount
{
    public string Name { get; init; } = string.Empty;

    public int Count { get; init; }
}
