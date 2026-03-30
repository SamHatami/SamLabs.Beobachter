using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Queries;

namespace SamLabs.Beobachter.Core.Interfaces;

public interface IIngestionSession : IAsyncDisposable
{
    event EventHandler<LogEntriesAppendedEventArgs>? EntriesAppended;

    int TotalCount { get; }

    long DroppedCount { get; }

    // Pauses consumer-side processing only; receivers continue acquiring and writing to the ingest channel.
    bool IsPaused { get; }

    bool IsAutoScrollEnabled { get; }

    bool TryPublish(LogEntry entry);

    void ClearEntries();

    IReadOnlyList<LogEntry> Snapshot(LogQuery? query = null);

    IReadOnlyList<ReceiverRuntimeState> GetReceiverRuntimeStates();

    ValueTask<IReadOnlyList<ReceiverStartupResult>> StartAsync(CancellationToken cancellationToken = default);

    // Controls consumer-side processing pause state. This does not stop receiver transports.
    ValueTask SetPausedAsync(bool isPaused, CancellationToken cancellationToken = default);

    ValueTask SetAutoScrollAsync(bool isEnabled, CancellationToken cancellationToken = default);

    ValueTask<ReceiverReloadResult> ReloadReceiversAsync(CancellationToken cancellationToken = default);

    ValueTask StopAsync(CancellationToken cancellationToken = default);
}
