using System.Text;
using System.Threading.Channels;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Infrastructure.Parsing;
using SamLabs.Beobachter.Infrastructure.Receivers;
using Xunit;

namespace SamLabs.Beobachter.Tests.Infrastructure.Receivers;

public sealed class FileTailReceiverTests
{
    [Fact]
    public async Task StartAsync_ReadsExistingAndAppendedEvents()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "Beobachter.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var logFile = Path.Combine(tempDirectory, "log.xml");

        const string event1 = """
            <log4j:event logger="File.Source" timestamp="1184286222308" level="INFO" thread="1"
                         xmlns:log4j="http://jakarta.apache.org/log4j/">
              <log4j:message>first</log4j:message>
            </log4j:event>
            """;

        const string event2 = """
            <log4j:event logger="File.Source" timestamp="1184286222408" level="ERROR" thread="1"
                         xmlns:log4j="http://jakarta.apache.org/log4j/">
              <log4j:message>second</log4j:message>
            </log4j:event>
            """;

        await File.WriteAllTextAsync(logFile, event1, Encoding.UTF8);

        var options = new FileTailReceiverOptions
        {
            Id = "file-1",
            DisplayName = "File Receiver",
            FilePath = logFile,
            PollInterval = TimeSpan.FromMilliseconds(40),
            StartAtEnd = false,
            DefaultLoggerName = "FileDefault"
        };

        await using var receiver = new FileTailReceiver(options, new Log4jXmlParser());
        var channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(16));
        await receiver.StartAsync(channel.Writer, CancellationToken.None);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
        var first = await channel.Reader.ReadAsync(timeout.Token);
        Assert.Equal("file-1", first.ReceiverId);
        Assert.Equal("first", first.Message);
        Assert.Equal(LogLevel.Info, first.Level);

        await File.AppendAllTextAsync(logFile, event2, Encoding.UTF8, timeout.Token);
        var second = await channel.Reader.ReadAsync(timeout.Token);
        Assert.Equal("file-1", second.ReceiverId);
        Assert.Equal("second", second.Message);
        Assert.Equal(LogLevel.Error, second.Level);

        await receiver.StopAsync();

        try
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
        catch
        {
            // Best effort cleanup in test environment.
        }
    }

    [Fact]
    public async Task StopAsync_IsSafeWhenReceiverWasNotStarted()
    {
        var options = new FileTailReceiverOptions
        {
            Id = "file-2",
            DisplayName = "File Receiver 2",
            FilePath = Path.Combine(Path.GetTempPath(), $"beobachter-{Guid.NewGuid():N}.log")
        };

        await using var receiver = new FileTailReceiver(options, new Log4jXmlParser());
        await receiver.StopAsync();
    }
}
