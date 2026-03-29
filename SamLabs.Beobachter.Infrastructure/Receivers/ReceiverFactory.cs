using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Settings;
using SamLabs.Beobachter.Infrastructure.Parsing;

namespace SamLabs.Beobachter.Infrastructure.Receivers;

public sealed class ReceiverFactory
{
    private readonly ParserPipelineFactory _parserPipelineFactory;

    public ReceiverFactory(ParserPipelineFactory parserPipelineFactory)
    {
        _parserPipelineFactory = parserPipelineFactory ?? throw new ArgumentNullException(nameof(parserPipelineFactory));
    }

    public IReadOnlyList<ILogReceiver> CreateReceivers(ReceiverDefinitions definitions)
    {
        var receivers = new List<ILogReceiver>();

        foreach (var receiver in definitions.UdpReceivers.Where(static x => x.Enabled))
        {
            var parser = _parserPipelineFactory.Create(receiver.ParserOrder);
            receivers.Add(new UdpReceiver(
                new UdpReceiverOptions
                {
                    Id = receiver.Id,
                    DisplayName = receiver.DisplayName,
                    BindAddress = receiver.BindAddress,
                    Port = receiver.Port,
                    DefaultLoggerName = receiver.DefaultLoggerName,
                    HostName = receiver.HostName,
                    FramingMode = receiver.FramingMode
                },
                parser));
        }

        foreach (var receiver in definitions.TcpReceivers.Where(static x => x.Enabled))
        {
            var parser = _parserPipelineFactory.Create(receiver.ParserOrder);
            receivers.Add(new TcpReceiver(
                new TcpReceiverOptions
                {
                    Id = receiver.Id,
                    DisplayName = receiver.DisplayName,
                    BindAddress = receiver.BindAddress,
                    Port = receiver.Port,
                    Backlog = receiver.Backlog,
                    ReceiveBufferSize = receiver.ReceiveBufferSize,
                    DefaultLoggerName = receiver.DefaultLoggerName,
                    HostName = receiver.HostName,
                    FramingMode = receiver.FramingMode
                },
                parser));
        }

        foreach (var receiver in definitions.FileTailReceivers.Where(static x => x.Enabled))
        {
            if (string.IsNullOrWhiteSpace(receiver.FilePath))
            {
                continue;
            }

            var parser = _parserPipelineFactory.Create(receiver.ParserOrder);
            receivers.Add(new FileTailReceiver(
                new FileTailReceiverOptions
                {
                    Id = receiver.Id,
                    DisplayName = receiver.DisplayName,
                    FilePath = receiver.FilePath,
                    PollInterval = TimeSpan.FromMilliseconds(Math.Max(1, receiver.PollIntervalMs)),
                    StartAtEnd = receiver.StartAtEnd,
                    DefaultLoggerName = receiver.DefaultLoggerName,
                    HostName = receiver.HostName,
                    MaxBufferedCharacters = receiver.MaxBufferedCharacters,
                    FramingMode = receiver.FramingMode
                },
                parser));
        }

        return receivers;
    }
}
