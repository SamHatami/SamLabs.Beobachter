using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Queries;

namespace SamLabs.Beobachter.Application.Services;

internal sealed class DesignIngestionSession : IIngestionSession
{
    private readonly List<LogEntry> _entries =
    [
        new()
        {
            Timestamp = DateTimeOffset.Now.AddSeconds(-8),
            Level = LogLevel.Info,
            ReceiverId = "design",
            LoggerName = "Design.Bootstrap",
            RootLoggerName = "Design.Bootstrap",
            Message = "Design-time entry: session initialized."
        },
        new()
        {
            Timestamp = DateTimeOffset.Now.AddSeconds(-2),
            Level = LogLevel.Warn,
            ReceiverId = "design",
            LoggerName = "Design.Network",
            RootLoggerName = "Design.Network",
            Message = "Design-time entry: delayed heartbeat."
        }
    ];

    public event EventHandler<LogEntriesAppendedEventArgs>? EntriesAppended;

    public int TotalCount => _entries.Count;

    public long DroppedCount => 0;

    public bool IsPaused { get; private set; }

    public bool TryPublish(LogEntry entry)
    {
        _entries.Add(entry);
        EntriesAppended?.Invoke(this, new LogEntriesAppendedEventArgs([entry]));
        return true;
    }

    public IReadOnlyList<LogEntry> Snapshot(LogQuery? query = null)
    {
        return _entries.ToArray();
    }

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask SetPausedAsync(bool isPaused, CancellationToken cancellationToken = default)
    {
        IsPaused = isPaused;
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
