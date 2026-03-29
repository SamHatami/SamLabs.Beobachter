namespace SamLabs.Beobachter.Core.Settings;

public sealed record class UdpReceiverDefinition
{
    public string Id { get; init; } = "udp-default";

    public string DisplayName { get; init; } = "UDP Receiver";

    public bool Enabled { get; init; } = true;

    public string BindAddress { get; init; } = "0.0.0.0";

    public int Port { get; init; } = 7071;

    public string DefaultLoggerName { get; init; } = "UdpReceiver";

    public string? HostName { get; init; }

    public ReceiverFramingMode FramingMode { get; init; } = ReceiverFramingMode.Datagram;

    public IReadOnlyList<string> ParserOrder { get; init; } = ["Log4jXmlParser", "JsonLogParser", "CsvParser", "PlainTextParser"];
}
