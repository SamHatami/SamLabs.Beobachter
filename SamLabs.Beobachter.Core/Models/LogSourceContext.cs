namespace SamLabs.Beobachter.Core.Models;

public sealed record class LogSourceContext
{
    public string ReceiverId { get; init; } = "Unknown";

    public string DefaultLoggerName { get; init; } = "Unknown";

    public string? HostName { get; init; }
}
