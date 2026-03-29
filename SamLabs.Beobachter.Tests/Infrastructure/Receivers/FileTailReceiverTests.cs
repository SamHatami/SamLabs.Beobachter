using System.Text;
using System.Threading.Channels;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Settings;
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

    [Fact]
    public async Task StartAsync_HandlesXmlEventSplitAcrossFileAppends()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "Beobachter.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        string logFile = Path.Combine(tempDirectory, "split.xml");
        await File.WriteAllTextAsync(logFile, string.Empty, Encoding.UTF8);

        const string xml = """
            <log4j:event logger="File.Source" timestamp="1184286222408" level="ERROR" thread="1"
                         xmlns:log4j="http://jakarta.apache.org/log4j/">
              <log4j:message>split-file</log4j:message>
            </log4j:event>
            """;
        int splitIndex = xml.Length / 2;

        FileTailReceiverOptions options = new()
        {
            Id = "file-split",
            DisplayName = "File Split",
            FilePath = logFile,
            PollInterval = TimeSpan.FromMilliseconds(40),
            StartAtEnd = false,
            DefaultLoggerName = "FileDefault"
        };

        await using FileTailReceiver receiver = new(options, new Log4jXmlParser());
        Channel<LogEntry> channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(8));
        await receiver.StartAsync(channel.Writer, CancellationToken.None);

        await File.AppendAllTextAsync(logFile, xml[..splitIndex], Encoding.UTF8);
        await AssertNoEntryWithinAsync(channel.Reader, TimeSpan.FromMilliseconds(200));

        await File.AppendAllTextAsync(logFile, xml[splitIndex..], Encoding.UTF8);
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(4));
        LogEntry entry = await channel.Reader.ReadAsync(timeout.Token);

        Assert.Equal("file-split", entry.ReceiverId);
        Assert.Equal("split-file", entry.Message);
        Assert.Equal(LogLevel.Error, entry.Level);

        await receiver.StopAsync();
        TryDeleteDirectory(tempDirectory);
    }

    [Fact]
    public async Task StartAsync_WithDatagramFraming_ParsesPlainTextChunk()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "Beobachter.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        string logFile = Path.Combine(tempDirectory, "plain.log");
        const string payload = "[2026-03-29T10:45:00Z] [WARN] [File.Source] datagram-path";
        await File.WriteAllTextAsync(logFile, payload, Encoding.UTF8);

        FileTailReceiverOptions options = new()
        {
            Id = "file-datagram",
            DisplayName = "File Datagram",
            FilePath = logFile,
            PollInterval = TimeSpan.FromMilliseconds(40),
            StartAtEnd = false,
            DefaultLoggerName = "FileDefault",
            FramingMode = ReceiverFramingMode.Datagram
        };

        await using FileTailReceiver receiver = new(options, new PlainTextParser());
        Channel<LogEntry> channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(8));
        await receiver.StartAsync(channel.Writer, CancellationToken.None);

        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(4));
        LogEntry entry = await channel.Reader.ReadAsync(timeout.Token);
        Assert.Equal("file-datagram", entry.ReceiverId);
        Assert.Equal("datagram-path", entry.Message);
        Assert.Equal(LogLevel.Warn, entry.Level);

        await receiver.StopAsync();
        TryDeleteDirectory(tempDirectory);
    }

    private static async Task AssertNoEntryWithinAsync(ChannelReader<LogEntry> reader, TimeSpan timeout)
    {
        using CancellationTokenSource cts = new(timeout);
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await reader.ReadAsync(cts.Token);
        });
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best effort cleanup in test environment.
        }
    }
}
