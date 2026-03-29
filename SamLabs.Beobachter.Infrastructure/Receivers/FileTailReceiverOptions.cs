using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Infrastructure.Receivers;

public sealed record class FileTailReceiverOptions
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string FilePath { get; init; }

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(150);

    public bool StartAtEnd { get; init; }

    public string DefaultLoggerName { get; init; } = "FileTailReceiver";

    public string? HostName { get; init; }

    public int MaxBufferedCharacters { get; init; } = 1_000_000;

    public ReceiverFramingMode FramingMode { get; init; } = ReceiverFramingMode.XmlEvent;
}
