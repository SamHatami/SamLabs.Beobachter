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
            entry.Message.IndexOf(query.TextContains, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.LoggerPrefix) &&
            !entry.LoggerName.StartsWith(query.LoggerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.ReceiverId) &&
            !string.Equals(entry.ReceiverId, query.ReceiverId, StringComparison.OrdinalIgnoreCase))
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
}
