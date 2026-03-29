namespace SamLabs.Beobachter.Core.Interfaces;

public interface ILogPayloadFramer
{
    void Push(ReadOnlyMemory<byte> transportPayload);

    bool TryReadFrame(out ReadOnlyMemory<byte> frame);

    void Reset();
}
