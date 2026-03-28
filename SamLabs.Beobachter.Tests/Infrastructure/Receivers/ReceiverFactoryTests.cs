using SamLabs.Beobachter.Core.Settings;
using SamLabs.Beobachter.Infrastructure.Parsing;
using SamLabs.Beobachter.Infrastructure.Receivers;
using Xunit;

namespace SamLabs.Beobachter.Tests.Infrastructure.Receivers;

public sealed class ReceiverFactoryTests
{
    [Fact]
    public void CreateReceivers_CreatesEnabledReceiversOnly()
    {
        var parserFactory = new ParserPipelineFactory([new Log4jXmlParser(), new CsvParser(), new PlainTextParser()]);
        var factory = new ReceiverFactory(parserFactory);
        var definitions = new ReceiverDefinitions
        {
            UdpReceivers =
            [
                new UdpReceiverDefinition { Id = "udp-1", DisplayName = "UDP 1", Enabled = true },
                new UdpReceiverDefinition { Id = "udp-2", DisplayName = "UDP 2", Enabled = false }
            ],
            TcpReceivers =
            [
                new TcpReceiverDefinition { Id = "tcp-1", DisplayName = "TCP 1", Enabled = true }
            ],
            FileTailReceivers =
            [
                new FileTailReceiverDefinition { Id = "file-1", DisplayName = "File 1", Enabled = true, FilePath = string.Empty },
                new FileTailReceiverDefinition { Id = "file-2", DisplayName = "File 2", Enabled = true, FilePath = "C:/logs/app.log" }
            ]
        };

        var receivers = factory.CreateReceivers(definitions);

        Assert.Equal(3, receivers.Count);
        Assert.Contains(receivers, static x => x is UdpReceiver && x.Id == "udp-1");
        Assert.Contains(receivers, static x => x is TcpReceiver && x.Id == "tcp-1");
        Assert.Contains(receivers, static x => x is FileTailReceiver && x.Id == "file-2");
    }
}
