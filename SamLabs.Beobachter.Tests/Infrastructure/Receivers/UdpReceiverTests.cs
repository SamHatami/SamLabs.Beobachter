using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Infrastructure.Parsing;
using SamLabs.Beobachter.Infrastructure.Receivers;
using Xunit;

namespace SamLabs.Beobachter.Tests.Infrastructure.Receivers;

public sealed class UdpReceiverTests
{
    [Fact]
    public async Task StartAsync_ParsesIncomingDatagramAndWritesToChannel()
    {
        var port = GetFreeUdpPort();
        var options = new UdpReceiverOptions
        {
            Id = "udp-receiver-1",
            DisplayName = "UDP Receiver 1",
            BindAddress = IPAddress.Loopback.ToString(),
            Port = port,
            DefaultLoggerName = "UdpDefault"
        };

        await using var receiver = new UdpReceiver(options, new Log4jXmlParser());
        var channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(8));

        await receiver.StartAsync(channel.Writer, CancellationToken.None);

        const string xml = """
            <log4j:event logger="Payments.Api" timestamp="1184286222308" level="INFO" thread="7"
                         xmlns:log4j="http://jakarta.apache.org/log4j/">
              <log4j:message>Datagram parsed</log4j:message>
            </log4j:event>
            """;

        using var sender = new UdpClient();
        var bytes = Encoding.UTF8.GetBytes(xml);
        await sender.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, port));

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var entry = await channel.Reader.ReadAsync(timeout.Token);

        Assert.Equal("udp-receiver-1", entry.ReceiverId);
        Assert.Equal("Payments.Api", entry.LoggerName);
        Assert.Equal("Datagram parsed", entry.Message);
        Assert.Equal(LogLevel.Info, entry.Level);

        await receiver.StopAsync();
    }

    [Fact]
    public async Task StopAsync_IsSafeWhenReceiverWasNotStarted()
    {
        var options = new UdpReceiverOptions
        {
            Id = "udp-receiver-2",
            DisplayName = "UDP Receiver 2",
            BindAddress = IPAddress.Loopback.ToString(),
            Port = GetFreeUdpPort()
        };

        await using var receiver = new UdpReceiver(options, new Log4jXmlParser());
        await receiver.StopAsync();
    }

    private static int GetFreeUdpPort()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }
}
