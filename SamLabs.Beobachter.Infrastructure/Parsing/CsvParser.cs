using System.Globalization;
using System.Text;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Services;

namespace SamLabs.Beobachter.Infrastructure.Parsing;

public sealed class CsvParser : ILogParser
{
    private readonly CsvParserOptions _options;
    private readonly Dictionary<string, int> _columnIndex;

    public CsvParser(CsvParserOptions? options = null)
    {
        _options = options ?? new CsvParserOptions();
        _columnIndex = BuildColumnIndex(_options.ColumnNames);
    }

    public string Name => "CsvParser";

    public bool TryParse(ReadOnlyMemory<byte> payload, LogSourceContext sourceContext, out LogEntry? entry)
    {
        entry = null;
        if (payload.IsEmpty)
        {
            return false;
        }

        var text = Encoding.UTF8.GetString(payload.Span).Trim();
        if (string.IsNullOrWhiteSpace(text) || LooksLikeXml(text))
        {
            return false;
        }

        var fields = ParseFields(text, _options.Delimiter, _options.Quote);
        if (fields.Count == 0)
        {
            return false;
        }

        // Header line from CSV files should not be treated as a log entry.
        if (LooksLikeHeader(fields))
        {
            return false;
        }

        var rawLevelName = GetField(fields, "level");
        var rawLevelValue = TryParseInt(rawLevelName);
        var normalizedLevel = LogLevelTable.Normalize(rawLevelName, rawLevelValue, LogLevel.Info);

        var callSiteClass = GetField(fields, "class");
        var loggerName = callSiteClass ?? sourceContext.DefaultLoggerName;
        var sourceFileValue = GetField(fields, "file");
        ParseSourceFile(sourceFileValue, out var sourceFileName, out var sourceFileLineNumber);

        entry = new LogEntry
        {
            Timestamp = ParseTimestamp(GetField(fields, "time")),
            SequenceNumber = ParseSequence(GetField(fields, "sequence")),
            Level = normalizedLevel.Level,
            ReceiverId = sourceContext.ReceiverId,
            LoggerName = loggerName,
            RootLoggerName = loggerName,
            Message = GetField(fields, "message") ?? text,
            ThreadName = GetField(fields, "thread") ?? string.Empty,
            HostName = sourceContext.HostName,
            Exception = GetField(fields, "exception"),
            CallSiteClass = callSiteClass,
            CallSiteMethod = GetField(fields, "method"),
            SourceFileName = sourceFileName,
            SourceFileLineNumber = sourceFileLineNumber,
            RawLevelName = rawLevelName,
            RawLevelValue = rawLevelValue,
            Properties = BuildProperties(fields)
        };

        return true;
    }

    private static Dictionary<string, int> BuildColumnIndex(IReadOnlyList<string> columnNames)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < columnNames.Count; i++)
        {
            var name = columnNames[i].Trim();
            if (name.Length == 0 || index.ContainsKey(name))
            {
                continue;
            }

            index[name] = i;
        }

        return index;
    }

    private IReadOnlyDictionary<string, string> BuildProperties(IReadOnlyList<string> fields)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < fields.Count; i++)
        {
            if (i >= _options.ColumnNames.Length)
            {
                result[$"column_{i}"] = fields[i];
                continue;
            }

            var name = _options.ColumnNames[i];
            if (name.Equals("sequence", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("time", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("level", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("thread", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("class", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("method", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("message", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("exception", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("file", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result[name] = fields[i];
        }

        return result;
    }

    private static bool LooksLikeXml(string text)
    {
        return text.StartsWith('<');
    }

    private bool LooksLikeHeader(IReadOnlyList<string> fields)
    {
        var sameCount = Math.Min(fields.Count, _options.ColumnNames.Length);
        if (sameCount == 0)
        {
            return false;
        }

        for (var i = 0; i < sameCount; i++)
        {
            if (!fields[i].Equals(_options.ColumnNames[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private string? GetField(IReadOnlyList<string> fields, string columnName)
    {
        if (!_columnIndex.TryGetValue(columnName, out var column) || column >= fields.Count)
        {
            return null;
        }

        var value = fields[column].Trim();
        return value.Length == 0 ? null : value;
    }

    private static IReadOnlyList<string> ParseFields(string line, char delimiter = ',', char quote = '"')
    {
        var fields = new List<string>();
        var buffer = new StringBuilder(line.Length);
        var inQuote = false;

        foreach (var ch in line)
        {
            if (ch == quote)
            {
                inQuote = !inQuote;
                continue;
            }

            if (ch == delimiter && !inQuote)
            {
                fields.Add(buffer.ToString());
                buffer.Clear();
                continue;
            }

            buffer.Append(ch);
        }

        fields.Add(buffer.ToString());
        return fields;
    }

    private static int? TryParseInt(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static long ParseSequence(string? value)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    private DateTimeOffset ParseTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DateTimeOffset.UtcNow;
        }

        foreach (var format in _options.TimestampFormats)
        {
            if (DateTimeOffset.TryParseExact(
                    value,
                    format,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var parsedOffset))
            {
                return parsedOffset;
            }
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
        {
            return parsed;
        }

        return DateTimeOffset.UtcNow;
    }

    private static void ParseSourceFile(string? value, out string? fileName, out uint? lineNumber)
    {
        fileName = value;
        lineNumber = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var trimmed = value.Trim().Trim('(', ')');
        var lineSeparator = trimmed.LastIndexOf(':');
        if (lineSeparator <= 0 || lineSeparator >= trimmed.Length - 1)
        {
            fileName = trimmed;
            return;
        }

        var lineText = trimmed[(lineSeparator + 1)..];
        if (!uint.TryParse(lineText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLine))
        {
            fileName = trimmed;
            return;
        }

        fileName = trimmed[..lineSeparator];
        lineNumber = parsedLine;
    }
}
