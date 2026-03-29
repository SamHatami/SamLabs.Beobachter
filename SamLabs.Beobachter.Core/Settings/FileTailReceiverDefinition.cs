namespace SamLabs.Beobachter.Core.Settings;

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

    public ReceiverFramingMode FramingMode { get; init; } = ReceiverFramingMode.XmlEvent;

    public IReadOnlyList<string> ParserOrder { get; init; } = ["Log4jXmlParser", "JsonLogParser", "CsvParser", "PlainTextParser"];
}
