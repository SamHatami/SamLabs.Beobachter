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

public sealed class TcpReceiverTests
{
    [Fact]
    public async Task StartAsync_ParsesMultipleEventsFromSingleConnection()
    {
        var port = GetFreeTcpPort();
        var options = new TcpReceiverOptions
        {
            Id = "tcp-receiver-1",
            DisplayName = "TCP Receiver 1",
            BindAddress = IPAddress.Loopback.ToString(),
            Port = port,
            DefaultLoggerName = "TcpDefault"
        };

        await using var receiver = new TcpReceiver(options, new Log4jXmlParser());
        var channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(16));
        await receiver.StartAsync(channel.Writer, CancellationToken.None);

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        using var stream = client.GetStream();

        const string event1 = """
            <log4j:event logger="Orders.Api" timestamp="1184286222308" level="INFO" thread="12"
                         xmlns:log4j="http://jakarta.apache.org/log4j/">
              <log4j:message>Order accepted</log4j:message>
            </log4j:event>
            """;

        const string event2 = """
            <log4j:event logger="Orders.Api" timestamp="1184286222350" level="ERROR" thread="12"
                         xmlns:log4j="http://jakarta.apache.org/log4j/">
              <log4j:message>Order failed</log4j:message>
            </log4j:event>
            """;

        var payload = Encoding.UTF8.GetBytes(event1 + event2);
        await stream.WriteAsync(payload);
        await stream.FlushAsync();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var first = await channel.Reader.ReadAsync(timeout.Token);
        var second = await channel.Reader.ReadAsync(timeout.Token);

        Assert.Equal("tcp-receiver-1", first.ReceiverId);
        Assert.Equal("Orders.Api", first.LoggerName);
        Assert.Equal("Order accepted", first.Message);
        Assert.Equal(LogLevel.Info, first.Level);

        Assert.Equal("tcp-receiver-1", second.ReceiverId);
        Assert.Equal("Orders.Api", second.LoggerName);
        Assert.Equal("Order failed", second.Message);
        Assert.Equal(LogLevel.Error, second.Level);

        await receiver.StopAsync();
    }

    [Fact]
    public async Task StopAsync_IsSafeWhenReceiverWasNotStarted()
    {
        var options = new TcpReceiverOptions
        {
            Id = "tcp-receiver-2",
            DisplayName = "TCP Receiver 2",
            BindAddress = IPAddress.Loopback.ToString(),
            Port = GetFreeTcpPort()
        };

        await using var receiver = new TcpReceiver(options, new Log4jXmlParser());
        await receiver.StopAsync();
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
