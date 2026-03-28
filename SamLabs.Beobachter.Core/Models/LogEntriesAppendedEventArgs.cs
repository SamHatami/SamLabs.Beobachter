namespace SamLabs.Beobachter.Core.Models;

public sealed class LogEntriesAppendedEventArgs : EventArgs
{
    public LogEntriesAppendedEventArgs(IReadOnlyList<LogEntry> appendedEntries)
    {
        AppendedEntries = appendedEntries;
    }

    public IReadOnlyList<LogEntry> AppendedEntries { get; }
}
