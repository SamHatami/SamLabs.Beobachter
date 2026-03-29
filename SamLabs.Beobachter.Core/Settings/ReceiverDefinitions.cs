namespace SamLabs.Beobachter.Core.Settings;

public sealed record class ReceiverDefinitions
{
    public IReadOnlyList<UdpReceiverDefinition> UdpReceivers { get; init; } = [];

    public IReadOnlyList<TcpReceiverDefinition> TcpReceivers { get; init; } = [];

    public IReadOnlyList<FileTailReceiverDefinition> FileTailReceivers { get; init; } = [];
}
