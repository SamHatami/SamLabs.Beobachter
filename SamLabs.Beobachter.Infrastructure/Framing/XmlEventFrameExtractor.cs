using System.Text;

namespace SamLabs.Beobachter.Infrastructure.Framing;

internal static class XmlEventFrameExtractor
{
    private const string LegacyEventCloseTag = "</log4j:event>";
    private const string Log4j2EventCloseTag = "</Event>";

    public static bool TryExtractNext(StringBuilder buffer, out string xmlEvent)
    {
        xmlEvent = string.Empty;
        if (buffer.Length == 0)
        {
            return false;
        }

        var text = buffer.ToString();
        var start = text.IndexOf('<');
        if (start < 0)
        {
            buffer.Clear();
            return false;
        }

        var legacyCloseIndex = text.IndexOf(LegacyEventCloseTag, start, StringComparison.OrdinalIgnoreCase);
        var log4j2CloseIndex = text.IndexOf(Log4j2EventCloseTag, start, StringComparison.OrdinalIgnoreCase);

        var endIndex = -1;
        var closeTagLength = 0;

        if (legacyCloseIndex >= 0 && (log4j2CloseIndex < 0 || legacyCloseIndex < log4j2CloseIndex))
        {
            endIndex = legacyCloseIndex;
            closeTagLength = LegacyEventCloseTag.Length;
        }
        else if (log4j2CloseIndex >= 0)
        {
            endIndex = log4j2CloseIndex;
            closeTagLength = Log4j2EventCloseTag.Length;
        }

        if (endIndex < 0)
        {
            return false;
        }

        var consumedLength = endIndex + closeTagLength;
        xmlEvent = text.Substring(start, consumedLength - start);
        buffer.Remove(0, consumedLength);
        return true;
    }
}
