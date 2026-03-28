namespace SamLabs.Beobachter.Infrastructure.Receivers;

public sealed record class TcpReceiverOptions
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public string BindAddress { get; init; } = "0.0.0.0";

    public int Port { get; init; } = 4505;

    public int Backlog { get; init; } = 100;

    public int ReceiveBufferSize { get; init; } = 10_000;

    public string DefaultLoggerName { get; init; } = "TcpReceiver";

    public string? HostName { get; init; }
}
