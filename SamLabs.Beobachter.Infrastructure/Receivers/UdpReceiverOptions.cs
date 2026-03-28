namespace SamLabs.Beobachter.Infrastructure.Receivers;

public sealed record class UdpReceiverOptions
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public string BindAddress { get; init; } = "0.0.0.0";

    public int Port { get; init; } = 7071;

    public string DefaultLoggerName { get; init; } = "UdpReceiver";

    public string? HostName { get; init; }
}
