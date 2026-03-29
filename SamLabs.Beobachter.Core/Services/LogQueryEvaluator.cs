using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Queries;

namespace SamLabs.Beobachter.Core.Services;

public sealed class LogQueryEvaluator : ILogQueryEvaluator
{
    public bool Matches(LogEntry entry, LogQuery query)
    {
        if (query.MinimumLevel.HasValue && entry.Level < query.MinimumLevel.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.TextContains) &&
            !MatchesTextFallback(entry, query.TextContains))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.LoggerPrefix) &&
            !entry.LoggerName.StartsWith(query.LoggerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.LoggerContains) &&
            entry.LoggerName.IndexOf(query.LoggerContains, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.ReceiverId) &&
            !string.Equals(entry.ReceiverId, query.ReceiverId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.ThreadContains) &&
            entry.ThreadName.IndexOf(query.ThreadContains, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        if (query.PropertyContains.Count > 0 && !MatchesPropertyFilters(entry, query.PropertyContains))
        {
            return false;
        }

        if (query.FromUtc.HasValue && entry.Timestamp < query.FromUtc.Value)
        {
            return false;
        }

        if (query.ToUtc.HasValue && entry.Timestamp > query.ToUtc.Value)
        {
            return false;
        }

        return true;
    }

    private static bool MatchesTextFallback(LogEntry entry, string term)
    {
        return Contains(entry.Message, term) ||
               Contains(entry.LoggerName, term) ||
               Contains(entry.HostName, term) ||
               Contains(entry.Exception, term) ||
               Contains(entry.ThreadName, term) ||
               entry.Properties.Any(pair =>
                   Contains(pair.Key, term) ||
                   Contains(pair.Value, term) ||
                   Contains($"{pair.Key}:{pair.Value}", term));
    }

    private static bool MatchesPropertyFilters(LogEntry entry, IReadOnlyDictionary<string, string> filters)
    {
        foreach (var filter in filters)
        {
            if (!entry.Properties.TryGetValue(filter.Key, out var value))
            {
                return false;
            }

            if (filter.Value.Length == 0)
            {
                continue;
            }

            if (value.IndexOf(filter.Value, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }
        }

        return true;
    }

    private static bool Contains(string? value, string term)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
