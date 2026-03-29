using System.Net;
using System.Net.Sockets;
using System.Text;
using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Settings;
using SamLabs.Beobachter.Infrastructure.Parsing;
using SamLabs.Beobachter.Infrastructure.Receivers;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

public sealed class IngestionSessionTests
{
    [Fact]
    public async Task StartAsync_ReturnsPerReceiverStartupResults_WhenOneReceiverFailsToStart()
    {
        FakeSettingsStore settings =
        new()
        {
            ReceiverDefinitions = new ReceiverDefinitions
            {
                UdpReceivers =
                [
                    new UdpReceiverDefinition { Id = "udp-good", DisplayName = "UDP Good", BindAddress = "127.0.0.1", Port = 0 },
                    new UdpReceiverDefinition { Id = "udp-bad", DisplayName = "UDP Bad", BindAddress = "localhost", Port = 7071 }
                ]
            }
        };

        await using IngestionSession session = new(settings, CreateReceiverFactory());

        IReadOnlyList<ReceiverStartupResult> startupResults = await session.StartAsync();

        Assert.Equal(2, startupResults.Count);
        Assert.Contains(startupResults, static x => x.ReceiverId == "udp-good" && x.Succeeded);
        Assert.Contains(startupResults, static x => x.ReceiverId == "udp-bad" && !x.Succeeded);

        IReadOnlyList<ReceiverRuntimeState> runtimeStates = session.GetReceiverRuntimeStates();
        Assert.Contains(runtimeStates, static x => x.ReceiverId == "udp-good" && x.State == ReceiverRunState.Running);
        Assert.Contains(runtimeStates, static x => x.ReceiverId == "udp-bad" && x.State == ReceiverRunState.Faulted && x.LastError.Length > 0);
    }

    [Fact]
    public async Task ReloadReceiversAsync_ReturnsPerReceiverResults_WhenOneReceiverFailsToStart()
    {
        FakeSettingsStore settings =
        new()
        {
            ReceiverDefinitions = new ReceiverDefinitions
            {
                UdpReceivers = [new UdpReceiverDefinition { Id = "udp-initial", DisplayName = "UDP Initial", BindAddress = "127.0.0.1", Port = 0 }]
            }
        };

        await using IngestionSession session = new(settings, CreateReceiverFactory());
        await session.StartAsync();

        settings.ReceiverDefinitions = new ReceiverDefinitions
        {
            UdpReceivers =
            [
                new UdpReceiverDefinition { Id = "udp-reload-good", DisplayName = "UDP Reload Good", BindAddress = "127.0.0.1", Port = 0 },
                new UdpReceiverDefinition { Id = "udp-reload-bad", DisplayName = "UDP Reload Bad", BindAddress = "localhost", Port = 7072 }
            ]
        };

        ReceiverReloadResult reloadResult = await session.ReloadReceiversAsync();

        Assert.Equal(2, reloadResult.AttemptedCount);
        Assert.Equal(1, reloadResult.SuccessfulCount);
        Assert.Equal(1, reloadResult.FailedCount);
        Assert.Contains(reloadResult.ReceiverStartupResults, static x => x.ReceiverId == "udp-reload-good" && x.Succeeded);
        Assert.Contains(reloadResult.ReceiverStartupResults, static x => x.ReceiverId == "udp-reload-bad" && !x.Succeeded);

        IReadOnlyList<ReceiverRuntimeState> runtimeStates = session.GetReceiverRuntimeStates();
        Assert.DoesNotContain(runtimeStates, static x => x.ReceiverId == "udp-initial");
        Assert.Contains(runtimeStates, static x => x.ReceiverId == "udp-reload-good" && x.State == ReceiverRunState.Running);
        Assert.Contains(runtimeStates, static x => x.ReceiverId == "udp-reload-bad" && x.State == ReceiverRunState.Faulted);
    }

    [Fact]
    public async Task StopAsync_MarksRunningReceiversAsStopped()
    {
        FakeSettingsStore settings =
        new()
        {
            ReceiverDefinitions = new ReceiverDefinitions
            {
                UdpReceivers = [new UdpReceiverDefinition { Id = "udp-1", DisplayName = "UDP One", BindAddress = "127.0.0.1", Port = 0 }]
            }
        };

        await using IngestionSession session = new(settings, CreateReceiverFactory());
        await session.StartAsync();
        await session.StopAsync();

        IReadOnlyList<ReceiverRuntimeState> runtimeStates = session.GetReceiverRuntimeStates();
        Assert.Single(runtimeStates);
        Assert.Equal("udp-1", runtimeStates[0].ReceiverId);
        Assert.Equal(ReceiverRunState.Stopped, runtimeStates[0].State);
    }

    [Fact]
    public async Task SetPausedAsync_PausesConsumptionAndResumesBufferedEntries()
    {
        FakeSettingsStore settings = new();
        await using IngestionSession session = new(settings, CreateReceiverFactory());
        await session.StartAsync();

        await session.SetPausedAsync(true);
        Assert.True(session.IsPaused);

        session.TryPublish(CreateEntry("paused-1"));
        session.TryPublish(CreateEntry("paused-2"));
        session.TryPublish(CreateEntry("paused-3"));
        await Task.Delay(300);
        Assert.Empty(session.Snapshot());

        await session.SetPausedAsync(false);
        await WaitForConditionAsync(() => session.Snapshot().Count == 3, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task StartAsync_WhenPaused_ReceiversCanStillWriteAndEntriesFlowAfterResume()
    {
        int port = GetFreeUdpPort();
        FakeSettingsStore settings =
        new()
        {
            WorkspaceSettings = new WorkspaceSettings { PauseIngest = true },
            ReceiverDefinitions = new ReceiverDefinitions
            {
                UdpReceivers =
                [
                    new UdpReceiverDefinition
                    {
                        Id = "udp-paused",
                        DisplayName = "UDP Paused",
                        BindAddress = IPAddress.Loopback.ToString(),
                        Port = port,
                        ParserOrder = ["Log4jXmlParser"]
                    }
                ]
            }
        };

        await using IngestionSession session = new(settings, CreateReceiverFactoryWithXmlSupport());
        await session.StartAsync();
        Assert.True(session.IsPaused);

        const string xml = """
            <log4j:event logger="Paused.Receiver" timestamp="1184286222308" level="INFO" thread="7"
                         xmlns:log4j="http://jakarta.apache.org/log4j/">
              <log4j:message>paused-path</log4j:message>
            </log4j:event>
            """;

        using UdpClient sender = new();
        byte[] payload = Encoding.UTF8.GetBytes(xml);
        await sender.SendAsync(payload, payload.Length, new IPEndPoint(IPAddress.Loopback, port));
        await Task.Delay(250);
        Assert.Empty(session.Snapshot());

        await session.SetPausedAsync(false);
        await WaitForConditionAsync(() => session.Snapshot().Count == 1, TimeSpan.FromSeconds(4));
        Assert.Equal("paused-path", session.Snapshot()[0].Message);
    }

    [Fact]
    public async Task SetPausedAsync_WithSmallChannelCapacity_DropsOldestUnderPressure()
    {
        FakeSettingsStore settings =
        new()
        {
            AppSettings = new AppSettings { ChannelCapacity = 2 }
        };

        await using IngestionSession session = new(settings, CreateReceiverFactory());
        await session.StartAsync();
        await session.SetPausedAsync(true);

        session.TryPublish(CreateEntry("m1"));
        session.TryPublish(CreateEntry("m2"));
        session.TryPublish(CreateEntry("m3"));
        session.TryPublish(CreateEntry("m4"));

        await Task.Delay(250);
        Assert.True(session.DroppedCount >= 2);
        Assert.Empty(session.Snapshot());

        await session.SetPausedAsync(false);
        await WaitForConditionAsync(() => session.Snapshot().Count == 2, TimeSpan.FromSeconds(3));

        IReadOnlyList<LogEntry> snapshot = session.Snapshot();
        Assert.Equal("m3", snapshot[0].Message);
        Assert.Equal("m4", snapshot[1].Message);
    }

    private static ReceiverFactory CreateReceiverFactory()
    {
        ParserPipelineFactory parserFactory = new([new PlainTextParser()]);
        return new ReceiverFactory(parserFactory);
    }

    private static ReceiverFactory CreateReceiverFactoryWithXmlSupport()
    {
        ParserPipelineFactory parserFactory = new([new Log4jXmlParser(), new PlainTextParser()]);
        return new ReceiverFactory(parserFactory);
    }

    private static LogEntry CreateEntry(string message)
    {
        return new LogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = LogLevel.Info,
            ReceiverId = "test",
            LoggerName = "Test.Logger",
            RootLoggerName = "Test.Logger",
            Message = message
        };
    }

    private static async Task WaitForConditionAsync(Func<bool> condition, TimeSpan timeout)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(25);
        }

        Assert.True(condition());
    }

    private static int GetFreeUdpPort()
    {
        using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }
}
