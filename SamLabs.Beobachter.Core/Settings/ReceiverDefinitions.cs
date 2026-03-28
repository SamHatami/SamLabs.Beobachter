namespace SamLabs.Beobachter.Core.Settings;

public sealed record class ReceiverDefinitions
{
    public IReadOnlyList<UdpReceiverDefinition> UdpReceivers { get; init; } = [];

    public IReadOnlyList<TcpReceiverDefinition> TcpReceivers { get; init; } = [];

    public IReadOnlyList<FileTailReceiverDefinition> FileTailReceivers { get; init; } = [];
}

public sealed record class UdpReceiverDefinition
{
    public string Id { get; init; } = "udp-default";

    public string DisplayName { get; init; } = "UDP Receiver";

    public bool Enabled { get; init; } = true;

    public string BindAddress { get; init; } = "0.0.0.0";

    public int Port { get; init; } = 7071;

    public string DefaultLoggerName { get; init; } = "UdpReceiver";

    public string? HostName { get; init; }

    public IReadOnlyList<string> ParserOrder { get; init; } = ["Log4jXmlParser", "CsvParser", "PlainTextParser"];
}

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

    public IReadOnlyList<string> ParserOrder { get; init; } = ["Log4jXmlParser", "CsvParser", "PlainTextParser"];
}

public sealed record class FileTailReceiverDefinition
{
    public string Id { get; init; } = "file-default";

    public string DisplayName { get; init; } = "File Tail Receiver";

    public bool Enabled { get; init; } = true;

    public string FilePath { get; init; } = string.Empty;

    public int PollIntervalMs { get; init; } = 150;

    public bool StartAtEnd { get; init; }

    public int MaxBufferedCharacters { get; init; } = 1_000_000;

    public string DefaultLoggerName { get; init; } = "FileTailReceiver";

    public string? HostName { get; init; }

    public IReadOnlyList<string> ParserOrder { get; init; } = ["Log4jXmlParser", "CsvParser", "PlainTextParser"];
}
