namespace SamLabs.Beobachter.Core.Settings;

public sealed record class TcpReceiverDefinition
{
    public string Id { get; init; } = "tcp-default";

    public string DisplayName { get; init; } = "TCP Receiver";

    public bool Enabled { get; init; } = true;

    public string BindAddress { get; init; } = "0.0.0.0";

    public int Port { get; init; } = 4505;

    public int Backlog { get; init; } = 100;

    public int ReceiveBufferSize { get; init; } = 10_000;

    public string DefaultLoggerName { get; init; } = "TcpReceiver";

    public string? HostName { get; init; }

    public ReceiverFramingMode FramingMode { get; init; } = ReceiverFramingMode.XmlEvent;

    public IReadOnlyList<string> ParserOrder { get; init; } = ["Log4jXmlParser", "JsonLogParser", "CsvParser", "PlainTextParser"];
}
