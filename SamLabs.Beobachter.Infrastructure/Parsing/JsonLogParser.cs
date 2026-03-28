using System.Globalization;
using System.Text.Json;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Services;

namespace SamLabs.Beobachter.Infrastructure.Parsing;

public sealed class JsonLogParser : ILogParser
{
    private static readonly string[] TimestampKeys = ["@t", "timestamp", "time", "ts"];
    private static readonly string[] LevelKeys = ["@l", "level", "logLevel", "severity"];
    private static readonly string[] LoggerKeys = ["logger", "loggerName", "SourceContext", "category", "name"];
    private static readonly string[] MessageKeys = ["message", "@m", "@mt", "renderedMessage"];
    private static readonly string[] ThreadKeys = ["thread", "threadName", "threadId"];
    private static readonly string[] ExceptionKeys = ["exception", "@x"];
    private static readonly string[] SequenceKeys = ["sequence", "eventSequenceNumber", "seq"];
    private static readonly string[] HostKeys = ["hostName", "host", "machineName"];
    private static readonly string[] PropertyBagKeys = ["properties", "context", "@p"];

    private static readonly HashSet<string> ReservedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "@t", "timestamp", "time", "ts",
        "@l", "level", "logLevel", "severity",
        "logger", "loggerName", "SourceContext", "category", "name",
        "message", "@m", "@mt", "renderedMessage",
        "thread", "threadName", "threadId",
        "exception", "@x",
        "sequence", "eventSequenceNumber", "seq",
        "hostName", "host", "machineName",
        "properties", "context", "@p"
    };

    public string Name => "JsonLogParser";

    public bool TryParse(ReadOnlyMemory<byte> payload, LogSourceContext sourceContext, out LogEntry? entry)
    {
        entry = null;
        if (payload.IsEmpty)
        {
            return false;
        }

        var span = payload.Span;
        var first = FirstNonWhitespaceByte(span);
        if (first is null || first.Value != (byte)'{')
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var root = document.RootElement;
            var timestamp = ParseTimestamp(root) ?? DateTimeOffset.UtcNow;
            var level = ParseLevel(root, out var rawLevelName, out var rawLevelValue);
            var loggerName = ReadString(root, LoggerKeys) ?? sourceContext.DefaultLoggerName;
            var message = ReadString(root, MessageKeys) ?? root.GetRawText();
            var threadName = ReadString(root, ThreadKeys) ?? string.Empty;
            var exception = ReadString(root, ExceptionKeys);
            var sequence = ParseLong(root, SequenceKeys);
            var hostName = ReadString(root, HostKeys) ?? sourceContext.HostName;

            var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ParsePropertyBag(root, properties);
            ParseAdditionalRootProperties(root, properties);

            entry = new LogEntry
            {
                Timestamp = timestamp,
                SequenceNumber = sequence,
                Level = level,
                ReceiverId = sourceContext.ReceiverId,
                LoggerName = loggerName,
                RootLoggerName = loggerName,
                Message = message,
                ThreadName = threadName,
                HostName = hostName,
                Exception = exception,
                RawLevelName = rawLevelName,
                RawLevelValue = rawLevelValue,
                Properties = properties
            };

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static byte? FirstNonWhitespaceByte(ReadOnlySpan<byte> span)
    {
        foreach (var value in span)
        {
            if (!char.IsWhiteSpace((char)value))
            {
                return value;
            }
        }

        return null;
    }

    private static DateTimeOffset? ParseTimestamp(JsonElement root)
    {
        if (!TryGetFirstProperty(root, TimestampKeys, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();
            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        if (value.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        if (value.TryGetInt64(out var longValue))
        {
            if (longValue > 10_000_000_000)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(longValue);
            }

            return DateTimeOffset.FromUnixTimeSeconds(longValue);
        }

        if (!value.TryGetDouble(out var doubleValue))
        {
            return null;
        }

        var millis = (long)doubleValue;
        return millis > 10_000_000_000
            ? DateTimeOffset.FromUnixTimeMilliseconds(millis)
            : DateTimeOffset.FromUnixTimeSeconds(millis);
    }

    private static LogLevel ParseLevel(JsonElement root, out string? rawLevelName, out int? rawLevelValue)
    {
        rawLevelName = null;
        rawLevelValue = null;

        if (!TryGetFirstProperty(root, LevelKeys, out var value))
        {
            return LogLevel.Info;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            rawLevelName = value.GetString();
            rawLevelValue = TryParseInt(rawLevelName);
            return LogLevelTable.Normalize(rawLevelName, rawLevelValue, LogLevel.Info).Level;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var numericLevel))
        {
            rawLevelName = numericLevel.ToString(CultureInfo.InvariantCulture);
            rawLevelValue = numericLevel;
            return LogLevelTable.Normalize(rawLevelName, rawLevelValue, LogLevel.Info).Level;
        }

        rawLevelName = value.GetRawText();
        return LogLevel.Info;
    }

    private static void ParsePropertyBag(JsonElement root, IDictionary<string, string> properties)
    {
        foreach (var bagKey in PropertyBagKeys)
        {
            if (!TryGetProperty(root, bagKey, out var bag) || bag.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var property in bag.EnumerateObject())
            {
                properties[property.Name] = SerializeValue(property.Value);
            }
        }
    }

    private static void ParseAdditionalRootProperties(JsonElement root, IDictionary<string, string> properties)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (ReservedKeys.Contains(property.Name))
            {
                continue;
            }

            properties[property.Name] = SerializeValue(property.Value);
        }
    }

    private static string? ReadString(JsonElement root, IReadOnlyList<string> keys)
    {
        if (!TryGetFirstProperty(root, keys, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        return value.GetRawText();
    }

    private static long ParseLong(JsonElement root, IReadOnlyList<string> keys)
    {
        if (!TryGetFirstProperty(root, keys, out var value))
        {
            return 0;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt64(out var parsed) => parsed,
            JsonValueKind.String when long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0
        };
    }

    private static int? TryParseInt(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static string SerializeValue(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : value.GetRawText();
    }

    private static bool TryGetFirstProperty(JsonElement element, IReadOnlyList<string> propertyNames, out JsonElement value)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryGetProperty(element, propertyName, out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
