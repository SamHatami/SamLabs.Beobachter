using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Queries;

namespace SamLabs.Beobachter.Core.Services;

public sealed class InMemoryLogStore : ILogStore
{
    private readonly object _gate = new();
    private readonly List<LogEntry> _entries = [];
    private readonly ILogQueryEvaluator _queryEvaluator;

    public InMemoryLogStore(ILogQueryEvaluator? queryEvaluator = null)
    {
        _queryEvaluator = queryEvaluator ?? new LogQueryEvaluator();
    }

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _entries.Count;
            }
        }
    }

    public event EventHandler<LogEntriesAppendedEventArgs>? EntriesAppended;

    public void Append(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        AppendRange([entry]);
    }

    public void AppendRange(IEnumerable<LogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var appended = entries as IReadOnlyList<LogEntry> ?? entries.ToList();
        if (appended.Count == 0)
        {
            return;
        }

        lock (_gate)
        {
            _entries.AddRange(appended);
        }

        EntriesAppended?.Invoke(this, new LogEntriesAppendedEventArgs(appended));
    }

    public IReadOnlyList<LogEntry> Snapshot(LogQuery? query = null)
    {
        lock (_gate)
        {
            if (query is null)
            {
                return _entries.ToArray();
            }

            return _entries.Where(entry => _queryEvaluator.Matches(entry, query)).ToArray();
        }
    }
}
