using System.Text;
using SamLabs.Beobachter.Infrastructure.Framing;
using Xunit;

namespace SamLabs.Beobachter.Tests.Infrastructure.Framing;

public sealed class DatagramPassthroughFramerTests
{
    [Fact]
    public void Push_EnqueuesDatagramAsSingleFrame()
    {
        DatagramPassthroughFramer framer = new();
        byte[] payload = Encoding.UTF8.GetBytes("hello");

        framer.Push(payload);

        Assert.True(framer.TryReadFrame(out ReadOnlyMemory<byte> frame));
        Assert.Equal("hello", Encoding.UTF8.GetString(frame.Span));
        Assert.False(framer.TryReadFrame(out _));
    }

    [Fact]
    public void Reset_ClearsQueuedFrames()
    {
        DatagramPassthroughFramer framer = new();
        framer.Push(Encoding.UTF8.GetBytes("first"));
        framer.Push(Encoding.UTF8.GetBytes("second"));

        framer.Reset();

        Assert.False(framer.TryReadFrame(out _));
    }
}
