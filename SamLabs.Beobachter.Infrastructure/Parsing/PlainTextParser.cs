using System.Text;
using System.Text.RegularExpressions;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Services;

namespace SamLabs.Beobachter.Infrastructure.Parsing;

public sealed partial class PlainTextParser : ILogParser
{
    public string Name => "PlainTextParser";

    public bool TryParse(ReadOnlyMemory<byte> payload, LogSourceContext sourceContext, out LogEntry? entry)
    {
        entry = null;
        if (payload.IsEmpty)
        {
            return false;
        }

        var text = Encoding.UTF8.GetString(payload.Span).Trim();
        if (string.IsNullOrWhiteSpace(text) || text.StartsWith('<'))
        {
            return false;
        }

        var timestamp = DateTimeOffset.UtcNow;
        var level = LogLevel.Info;
        var rawLevelName = "INFO";
        var loggerName = sourceContext.DefaultLoggerName;
        var message = text;
        string? threadName = null;

        var bracketMatch = BracketPattern().Match(text);
        if (bracketMatch.Success)
        {
            timestamp = ParseTimestamp(bracketMatch.Groups["ts"].Value) ?? timestamp;
            rawLevelName = bracketMatch.Groups["level"].Value;
            level = LogLevelTable.FromName(rawLevelName, LogLevel.Info);
            loggerName = bracketMatch.Groups["logger"].Value;
            message = bracketMatch.Groups["msg"].Value;
            threadName = bracketMatch.Groups["thread"].Success ? bracketMatch.Groups["thread"].Value : null;
        }
        else
        {
            var dashMatch = DashPattern().Match(text);
            if (dashMatch.Success)
            {
                timestamp = ParseTimestamp(dashMatch.Groups["ts"].Value) ?? timestamp;
                rawLevelName = dashMatch.Groups["level"].Value;
                level = LogLevelTable.FromName(rawLevelName, LogLevel.Info);
                loggerName = dashMatch.Groups["logger"].Value;
                message = dashMatch.Groups["msg"].Value;
            }
            else
            {
                var pipeMatch = PipePattern().Match(text);
                if (pipeMatch.Success)
                {
                    timestamp = ParseTimestamp(pipeMatch.Groups["ts"].Value) ?? timestamp;
                    rawLevelName = pipeMatch.Groups["level"].Value;
                    level = LogLevelTable.FromName(rawLevelName, LogLevel.Info);
                    loggerName = pipeMatch.Groups["logger"].Value.Trim();
                    message = pipeMatch.Groups["msg"].Value.Trim();
                }
            }
        }

        entry = new LogEntry
        {
            Timestamp = timestamp,
            SequenceNumber = 0,
            Level = level,
            ReceiverId = sourceContext.ReceiverId,
            LoggerName = loggerName,
            RootLoggerName = loggerName,
            Message = message,
            ThreadName = threadName ?? string.Empty,
            HostName = sourceContext.HostName,
            Exception = null,
            CallSiteClass = null,
            CallSiteMethod = null,
            SourceFileName = null,
            SourceFileLineNumber = null,
            RawLevelName = rawLevelName,
            RawLevelValue = null,
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };

        return true;
    }

    private static DateTimeOffset? ParseTimestamp(string value)
    {
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }

    [GeneratedRegex(
        @"^\[(?<ts>[^\]]+)\]\s+\[(?<level>[A-Za-z0-9]+)\]\s+\[(?<logger>[^\]]+)\](?:\s+\[(?<thread>[^\]]+)\])?\s+(?<msg>.+)$",
        RegexOptions.Compiled)]
    private static partial Regex BracketPattern();

    [GeneratedRegex(
        @"^(?<ts>\d{4}-\d{2}-\d{2}[T\s][^ ]+)\s+(?<level>[A-Za-z0-9]+)\s+(?<logger>\S+)\s+-\s+(?<msg>.+)$",
        RegexOptions.Compiled)]
    private static partial Regex DashPattern();

    [GeneratedRegex(
        @"^(?<ts>[^|]+?)\s*\|\s*(?<level>[A-Za-z0-9]+)\s*\|\s*(?<logger>[^|]+?)\s*\|\s*(?<msg>.+)$",
        RegexOptions.Compiled)]
    private static partial Regex PipePattern();
}
