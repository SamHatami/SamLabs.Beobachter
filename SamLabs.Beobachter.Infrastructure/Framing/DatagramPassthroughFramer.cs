using SamLabs.Beobachter.Core.Interfaces;

namespace SamLabs.Beobachter.Infrastructure.Framing;

public sealed class DatagramPassthroughFramer : ILogPayloadFramer
{
    private readonly Queue<byte[]> _frames = new();

    public void Push(ReadOnlyMemory<byte> transportPayload)
    {
        if (transportPayload.Length == 0)
        {
            return;
        }

        _frames.Enqueue(transportPayload.ToArray());
    }

    public bool TryReadFrame(out ReadOnlyMemory<byte> frame)
    {
        if (_frames.Count == 0)
        {
            frame = ReadOnlyMemory<byte>.Empty;
            return false;
        }

        frame = _frames.Dequeue();
        return true;
    }

    public void Reset()
    {
        _frames.Clear();
    }
}
