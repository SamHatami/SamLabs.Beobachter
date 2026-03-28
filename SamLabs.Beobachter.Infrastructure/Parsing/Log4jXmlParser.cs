using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Services;

namespace SamLabs.Beobachter.Infrastructure.Parsing;

public sealed class Log4jXmlParser : ILogParser
{
    private const string LegacyLog4jNamespace = "http://jakarta.apache.org/log4j/";
    private const string Log4j2EventsNamespace = "http://logging.apache.org/log4j/2.0/events";
    private static readonly DateTimeOffset UnixEpoch = DateTimeOffset.UnixEpoch;

    // Shared settings/context mirror the legacy optimization pattern:
    // avoid repeatedly allocating parser config per message.
    private static readonly XmlReaderSettings XmlSettings = CreateXmlSettings();
    private static readonly XmlParserContext XmlContext = CreateXmlContext();

    public string Name => "Log4jXmlParser";

    public bool TryParse(ReadOnlyMemory<byte> payload, LogSourceContext sourceContext, out LogEntry? entry)
    {
        entry = null;

        if (payload.IsEmpty)
        {
            return false;
        }

        var xml = Encoding.UTF8.GetString(payload.Span);
        if (string.IsNullOrWhiteSpace(xml))
        {
            return false;
        }

        try
        {
            using var textReader = new StringReader(xml);
            using var reader = XmlReader.Create(textReader, XmlSettings, XmlContext);
            var eventElement = XElement.Load(reader);

            if (IsLog4j2Event(eventElement))
            {
                entry = ParseLog4j2Event(eventElement, sourceContext);
                return true;
            }

            if (IsLegacyLog4jEvent(eventElement))
            {
                entry = ParseLegacyEvent(eventElement, sourceContext);
                return true;
            }

            return false;
        }
        catch (XmlException)
        {
            return false;
        }
    }

    private static LogEntry ParseLegacyEvent(XElement eventElement, LogSourceContext sourceContext)
    {
        var loggerName = GetAttributeValue(eventElement, "logger") ?? sourceContext.DefaultLoggerName;
        var rawLevelText = GetAttributeValue(eventElement, "level");
        var threadName = GetAttributeValue(eventElement, "thread") ?? string.Empty;
        var timestampValue = ParseTimestamp(GetAttributeValue(eventElement, "timestamp"));
        var rawLevelNumeric = TryParseInt(rawLevelText);
        var normalizedLevel = LogLevelTable.Normalize(rawLevelText, rawLevelNumeric, fallback: LogLevel.Info);

        var message = FindFirstChild(eventElement, "message")?.Value ?? string.Empty;
        var throwableParts = eventElement
            .Elements()
            .Where(static x => x.Name.LocalName.Equals("throwable", StringComparison.OrdinalIgnoreCase))
            .Select(static x => x.Value)
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
        string? exception = throwableParts.Length == 0 ? null : string.Join(Environment.NewLine, throwableParts);

        var location = ParseLocation(eventElement, "locationInfo");
        var sequenceNumber = ParseLong(FindFirstChild(eventElement, "eventSequenceNumber")?.Value);

        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var hostName = sourceContext.HostName;
        ParseLegacyProperties(eventElement, properties, ref exception, ref hostName);

        return CreateEntry(
            sourceContext,
            loggerName,
            threadName,
            message,
            timestampValue,
            sequenceNumber,
            normalizedLevel.Level,
            rawLevelText,
            rawLevelNumeric,
            hostName,
            exception,
            location.CallSiteClass,
            location.CallSiteMethod,
            location.SourceFileName,
            location.SourceFileLineNumber,
            properties);
    }

    private static LogEntry ParseLog4j2Event(XElement eventElement, LogSourceContext sourceContext)
    {
        var loggerName = GetAttributeValue(eventElement, "loggerName", "logger") ?? sourceContext.DefaultLoggerName;
        var rawLevelText = GetAttributeValue(eventElement, "level");
        var rawLevelNumeric = TryParseInt(rawLevelText);
        var normalizedLevel = LogLevelTable.Normalize(rawLevelText, rawLevelNumeric, fallback: LogLevel.Info);
        var threadName = GetAttributeValue(eventElement, "threadName", "thread") ?? string.Empty;

        var timeMillis = GetAttributeValue(eventElement, "timeMillis", "timestamp");
        var timestampValue = ParseTimestamp(timeMillis);
        if (timeMillis is null)
        {
            timestampValue = ParseLog4j2Instant(eventElement) ?? timestampValue;
        }

        var message = FindFirstChild(eventElement, "Message", "message")?.Value ?? string.Empty;

        string? exception = null;
        var thrownElement = FindFirstChild(eventElement, "Thrown", "throwable");
        if (thrownElement is not null)
        {
            exception = string.IsNullOrWhiteSpace(thrownElement.Value)
                ? GetAttributeValue(thrownElement, "message", "localizedMessage")
                : thrownElement.Value;
        }

        var location = ParseLocation(eventElement, "Source", "locationInfo");
        var sequenceNumber = ParseLong(FindFirstChild(eventElement, "eventSequenceNumber")?.Value);

        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var hostName = sourceContext.HostName;
        ParseLog4j2ContextMap(eventElement, properties, ref hostName);

        return CreateEntry(
            sourceContext,
            loggerName,
            threadName,
            message,
            timestampValue,
            sequenceNumber,
            normalizedLevel.Level,
            rawLevelText,
            rawLevelNumeric,
            hostName,
            exception,
            location.CallSiteClass,
            location.CallSiteMethod,
            location.SourceFileName,
            location.SourceFileLineNumber,
            properties);
    }

    private static XmlReaderSettings CreateXmlSettings()
    {
        return new XmlReaderSettings
        {
            CloseInput = false,
            ValidationType = ValidationType.None,
            IgnoreComments = true,
            IgnoreWhitespace = true
        };
    }

    private static XmlParserContext CreateXmlContext()
    {
        var nt = new NameTable();
        var nsManager = new XmlNamespaceManager(nt);
        nsManager.AddNamespace("log4j", LegacyLog4jNamespace);
        nsManager.AddNamespace("nlog", "http://nlog-project.org");
        nsManager.AddNamespace("log4j2", Log4j2EventsNamespace);
        return new XmlParserContext(nt, nsManager, "event", XmlSpace.None);
    }

    private static bool IsLegacyLog4jEvent(XElement eventElement)
    {
        if (!eventElement.Name.LocalName.Equals("event", StringComparison.Ordinal))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(eventElement.Name.NamespaceName))
        {
            return true;
        }

        return eventElement.Name.NamespaceName.Equals(LegacyLog4jNamespace, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLog4j2Event(XElement eventElement)
    {
        if (eventElement.Name.NamespaceName.Equals(Log4j2EventsNamespace, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return eventElement.Name.LocalName.Equals("Event", StringComparison.Ordinal);
    }

    private static DateTimeOffset ParseTimestamp(string? value)
    {
        if (long.TryParse(value, out var milliseconds))
        {
            return UnixEpoch.AddMilliseconds(milliseconds).ToLocalTime();
        }

        return DateTimeOffset.UtcNow;
    }

    private static DateTimeOffset? ParseLog4j2Instant(XElement eventElement)
    {
        var instantElement = FindFirstChild(eventElement, "Instant");
        if (instantElement is null)
        {
            return null;
        }

        var epochSecond = ParseLong(GetAttributeValue(instantElement, "epochSecond"));
        var nanoOfSecond = ParseLong(GetAttributeValue(instantElement, "nanoOfSecond"));
        if (epochSecond <= 0)
        {
            return null;
        }

        var timestamp = DateTimeOffset.FromUnixTimeSeconds(epochSecond);
        if (nanoOfSecond > 0)
        {
            timestamp = timestamp.AddTicks(nanoOfSecond / 100);
        }

        return timestamp.ToLocalTime();
    }

    private static int? TryParseInt(string? value)
    {
        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private static uint? ParseUint(string? value)
    {
        return uint.TryParse(value, out var parsed) ? parsed : null;
    }

    private static long ParseLong(string? value)
    {
        return long.TryParse(value, out var parsed) ? parsed : 0;
    }

    private static XElement? FindFirstChild(XElement parent, params string[] names)
    {
        return parent.Elements().FirstOrDefault(x =>
            names.Any(name => x.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase)));
    }

    private static string? GetAttributeValue(XElement element, params string[] names)
    {
        foreach (var name in names)
        {
            var exact = element.Attribute(name);
            if (exact is not null)
            {
                return exact.Value;
            }
        }

        foreach (var attr in element.Attributes())
        {
            if (names.Any(name => attr.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                return attr.Value;
            }
        }

        return null;
    }

    private static (string? CallSiteClass, string? CallSiteMethod, string? SourceFileName, uint? SourceFileLineNumber)
        ParseLocation(XElement eventElement, params string[] candidateElementNames)
    {
        var locationElement = FindFirstChild(eventElement, candidateElementNames);
        if (locationElement is null)
        {
            return (null, null, null, null);
        }

        var callSiteClass = GetAttributeValue(locationElement, "class");
        var callSiteMethod = GetAttributeValue(locationElement, "method");
        var sourceFile = GetAttributeValue(locationElement, "file");
        var sourceLine = ParseUint(GetAttributeValue(locationElement, "line"));
        return (callSiteClass, callSiteMethod, sourceFile, sourceLine);
    }

    private static void ParseLegacyProperties(
        XElement eventElement,
        IDictionary<string, string> properties,
        ref string? exception,
        ref string? hostName)
    {
        var propertiesElement = FindFirstChild(eventElement, "properties");
        if (propertiesElement is null)
        {
            return;
        }

        foreach (var dataElement in propertiesElement.Elements().Where(static x =>
                     x.Name.LocalName.Equals("data", StringComparison.OrdinalIgnoreCase)))
        {
            var name = GetAttributeValue(dataElement, "name");
            var value = GetAttributeValue(dataElement, "value");
            if (string.IsNullOrWhiteSpace(name) || value is null)
            {
                continue;
            }

            if (name.Equals("exceptions", StringComparison.OrdinalIgnoreCase))
            {
                exception = value;
                continue;
            }

            properties[name] = value;
            if (hostName is null &&
                (name.Equals("log4net:HostName", StringComparison.OrdinalIgnoreCase) ||
                 name.Equals("log4jmachinename", StringComparison.OrdinalIgnoreCase)))
            {
                hostName = value;
            }
        }
    }

    private static void ParseLog4j2ContextMap(
        XElement eventElement,
        IDictionary<string, string> properties,
        ref string? hostName)
    {
        var contextMap = FindFirstChild(eventElement, "ContextMap");
        if (contextMap is null)
        {
            return;
        }

        foreach (var item in contextMap.Elements().Where(static x =>
                     x.Name.LocalName.Equals("item", StringComparison.OrdinalIgnoreCase)))
        {
            var key = GetAttributeValue(item, "key", "name");
            var value = GetAttributeValue(item, "value");
            if (string.IsNullOrWhiteSpace(key) || value is null)
            {
                continue;
            }

            properties[key] = value;
            if (hostName is null &&
                (key.Equals("hostName", StringComparison.OrdinalIgnoreCase) ||
                 key.Equals("machineName", StringComparison.OrdinalIgnoreCase) ||
                 key.Equals("log4net:HostName", StringComparison.OrdinalIgnoreCase)))
            {
                hostName = value;
            }
        }
    }

    private static LogEntry CreateEntry(
        LogSourceContext sourceContext,
        string loggerName,
        string threadName,
        string message,
        DateTimeOffset timestampValue,
        long sequenceNumber,
        LogLevel normalizedLevel,
        string? rawLevelText,
        int? rawLevelNumeric,
        string? hostName,
        string? exception,
        string? callSiteClass,
        string? callSiteMethod,
        string? sourceFile,
        uint? sourceLine,
        IReadOnlyDictionary<string, string> properties)
    {
        var resolvedLoggerName = string.IsNullOrWhiteSpace(loggerName) ? sourceContext.DefaultLoggerName : loggerName;

        return new LogEntry
        {
            Timestamp = timestampValue,
            SequenceNumber = sequenceNumber,
            Level = normalizedLevel,
            ReceiverId = sourceContext.ReceiverId,
            LoggerName = resolvedLoggerName,
            RootLoggerName = resolvedLoggerName,
            Message = message,
            ThreadName = threadName,
            HostName = hostName,
            Exception = exception,
            CallSiteClass = callSiteClass,
            CallSiteMethod = callSiteMethod,
            SourceFileName = sourceFile,
            SourceFileLineNumber = sourceLine,
            RawLevelName = rawLevelText,
            RawLevelValue = rawLevelNumeric,
            Properties = properties
        };
    }
}
