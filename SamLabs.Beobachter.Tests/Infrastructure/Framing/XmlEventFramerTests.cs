using System.Text;
using SamLabs.Beobachter.Infrastructure.Framing;
using Xunit;

namespace SamLabs.Beobachter.Tests.Infrastructure.Framing;

public sealed class XmlEventFramerTests
{
    [Fact]
    public void Push_OneEventInOneChunk_ProducesOneFrame()
    {
        XmlEventFramer framer = new();
        framer.Push(Encoding.UTF8.GetBytes(EventOne));

        Assert.True(framer.TryReadFrame(out ReadOnlyMemory<byte> frame));
        Assert.Equal(Normalize(EventOne), Normalize(Encoding.UTF8.GetString(frame.Span)));
        Assert.False(framer.TryReadFrame(out _));
    }

    [Fact]
    public void Push_OneEventSplitAcrossChunks_ProducesOneFrameAfterCompletion()
    {
        XmlEventFramer framer = new();
        string head = EventOne[..(EventOne.Length / 2)];
        string tail = EventOne[(EventOne.Length / 2)..];

        framer.Push(Encoding.UTF8.GetBytes(head));
        Assert.False(framer.TryReadFrame(out _));

        framer.Push(Encoding.UTF8.GetBytes(tail));

        Assert.True(framer.TryReadFrame(out ReadOnlyMemory<byte> frame));
        Assert.Equal(Normalize(EventOne), Normalize(Encoding.UTF8.GetString(frame.Span)));
        Assert.False(framer.TryReadFrame(out _));
    }

    [Fact]
    public void Push_MultipleEventsInOneChunk_ProducesMultipleFrames()
    {
        XmlEventFramer framer = new();
        framer.Push(Encoding.UTF8.GetBytes(EventOne + EventTwo));

        Assert.True(framer.TryReadFrame(out ReadOnlyMemory<byte> first));
        Assert.True(framer.TryReadFrame(out ReadOnlyMemory<byte> second));
        Assert.Equal(Normalize(EventOne), Normalize(Encoding.UTF8.GetString(first.Span)));
        Assert.Equal(Normalize(EventTwo), Normalize(Encoding.UTF8.GetString(second.Span)));
        Assert.False(framer.TryReadFrame(out _));
    }

    [Fact]
    public void Push_IncompleteTrailingPayload_OnlyProducesCompleteEvents()
    {
        XmlEventFramer framer = new();
        string incomplete = """
            <log4j:event logger="Orders.Api" timestamp="1184286222308" level="INFO" thread="12"
                         xmlns:log4j="http://jakarta.apache.org/log4j/">
              <log4j:message>Trailing fragment
            """;

        framer.Push(Encoding.UTF8.GetBytes(EventOne + incomplete));

        Assert.True(framer.TryReadFrame(out ReadOnlyMemory<byte> frame));
        Assert.Equal(Normalize(EventOne), Normalize(Encoding.UTF8.GetString(frame.Span)));
        Assert.False(framer.TryReadFrame(out _));
    }

    private const string EventOne = """
        <log4j:event logger="Orders.Api" timestamp="1184286222308" level="INFO" thread="12"
                     xmlns:log4j="http://jakarta.apache.org/log4j/">
          <log4j:message>Order accepted</log4j:message>
        </log4j:event>
        """;

    private const string EventTwo = """
        <log4j:event logger="Orders.Api" timestamp="1184286222350" level="ERROR" thread="12"
                     xmlns:log4j="http://jakarta.apache.org/log4j/">
          <log4j:message>Order failed</log4j:message>
        </log4j:event>
        """;

    private static string Normalize(string value)
    {
        return value.Replace("\r", string.Empty).Trim();
    }
}
