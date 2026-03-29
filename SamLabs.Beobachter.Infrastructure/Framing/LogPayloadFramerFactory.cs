using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Infrastructure.Framing;

public static class LogPayloadFramerFactory
{
    public static ILogPayloadFramer Create(
        ReceiverFramingMode framingMode,
        ReceiverFramingMode fallbackMode)
    {
        ReceiverFramingMode effectiveMode = framingMode == ReceiverFramingMode.Unknown
            ? fallbackMode
            : framingMode;

        return effectiveMode switch
        {
            ReceiverFramingMode.Datagram => new DatagramPassthroughFramer(),
            ReceiverFramingMode.XmlEvent => new XmlEventFramer(),
            _ => throw new ArgumentOutOfRangeException(nameof(framingMode), framingMode, "Unsupported framing mode.")
        };
    }
}
