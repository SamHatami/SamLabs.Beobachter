using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Queries;

namespace SamLabs.Beobachter.Application.Services;

public interface IIngestionSession : IAsyncDisposable
{
    event EventHandler<LogEntriesAppendedEventArgs>? EntriesAppended;

    int TotalCount { get; }

    long DroppedCount { get; }

    bool IsPaused { get; }

    bool IsAutoScrollEnabled { get; }

    bool TryPublish(LogEntry entry);

    IReadOnlyList<LogEntry> Snapshot(LogQuery? query = null);

    ValueTask StartAsync(CancellationToken cancellationToken = default);

    ValueTask SetPausedAsync(bool isPaused, CancellationToken cancellationToken = default);

    ValueTask SetAutoScrollAsync(bool isEnabled, CancellationToken cancellationToken = default);

    ValueTask StopAsync(CancellationToken cancellationToken = default);
}
