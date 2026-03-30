using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Queries;

namespace SamLabs.Beobachter.Core.Interfaces;

public interface ILogStore
{
    int Count { get; }

    event EventHandler<LogEntriesAppendedEventArgs>? EntriesAppended;

    void Append(LogEntry entry);

    void AppendRange(IEnumerable<LogEntry> entries);

    void Clear();

    IReadOnlyList<LogEntry> Snapshot(LogQuery? query = null);
}
