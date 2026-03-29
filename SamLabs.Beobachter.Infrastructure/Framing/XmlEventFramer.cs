using System.Text;
using SamLabs.Beobachter.Core.Interfaces;

namespace SamLabs.Beobachter.Infrastructure.Framing;

public sealed class XmlEventFramer : ILogPayloadFramer
{
    private readonly Queue<byte[]> _frames = new();
    private readonly StringBuilder _payloadBuffer = new();
    private readonly Encoding _encoding;

    public XmlEventFramer()
        : this(Encoding.UTF8)
    {
    }

    public XmlEventFramer(Encoding encoding)
    {
        _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
    }

    public void Push(ReadOnlyMemory<byte> transportPayload)
    {
        if (transportPayload.Length == 0)
        {
            return;
        }

        _payloadBuffer.Append(_encoding.GetString(transportPayload.Span));
        while (XmlEventFrameExtractor.TryExtractNext(_payloadBuffer, out var xmlEvent))
        {
            _frames.Enqueue(_encoding.GetBytes(xmlEvent));
        }
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
        _payloadBuffer.Clear();
    }
}
