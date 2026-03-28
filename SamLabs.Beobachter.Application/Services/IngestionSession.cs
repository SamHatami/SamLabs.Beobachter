using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Queries;
using SamLabs.Beobachter.Core.Settings;
using SamLabs.Beobachter.Core.Services;
using SamLabs.Beobachter.Infrastructure.Receivers;

namespace SamLabs.Beobachter.Application.Services;

public sealed class IngestionSession : IIngestionSession
{
    private const int DefaultChannelCapacity = 50_000;
    private static readonly TimeSpan BatchFlushInterval = TimeSpan.FromMilliseconds(120);
    private const int MaxBatchSize = 512;

    private readonly ILogStore _store;
    private readonly ISettingsStore _settingsStore;
    private readonly ReceiverFactory _receiverFactory;
    private readonly object _gate = new();

    private Channel<LogEntry>? _channel;
    private ChannelReader<LogEntry>? _reader;
    private ChannelWriter<LogEntry>? _writer;
    private CancellationTokenSource? _runCts;
    private Task? _consumeTask;
    private IReadOnlyList<ILogReceiver> _receivers = [];
    private bool _started;
    private bool _isPaused;
    private int _channelCapacity = DefaultChannelCapacity;
    private long _droppedCount;
    private WorkspaceSettings _workspaceSettings = new();

    public IngestionSession(
        ISettingsStore settingsStore,
        ReceiverFactory receiverFactory,
        ILogStore? store = null)
    {
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _receiverFactory = receiverFactory ?? throw new ArgumentNullException(nameof(receiverFactory));
        _store = store ?? new InMemoryLogStore();
        _store.EntriesAppended += OnStoreEntriesAppended;
    }

    public event EventHandler<LogEntriesAppendedEventArgs>? EntriesAppended;

    public int TotalCount => _store.Count;

    public long DroppedCount => Interlocked.Read(ref _droppedCount);

    public bool IsPaused
    {
        get
        {
            lock (_gate)
            {
                return _isPaused;
            }
        }
    }

    public bool IsAutoScrollEnabled
    {
        get
        {
            lock (_gate)
            {
                return _workspaceSettings.AutoScroll;
            }
        }
    }

    public bool TryPublish(LogEntry entry)
    {
        var writer = _writer;
        return writer is not null && writer.TryWrite(entry);
    }

    public IReadOnlyList<LogEntry> Snapshot(LogQuery? query = null)
    {
        return _store.Snapshot(query);
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_started)
            {
                return;
            }

            _started = true;
        }

        var appSettings = await _settingsStore.LoadAppSettingsAsync(cancellationToken).ConfigureAwait(false);
        _channelCapacity = Math.Max(1, appSettings.ChannelCapacity);
        _workspaceSettings = await _settingsStore.LoadWorkspaceSettingsAsync(cancellationToken).ConfigureAwait(false);
        InitializeChannel(_channelCapacity);
        _isPaused = _workspaceSettings.PauseIngest;

        _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _consumeTask = Task.Run(() => ConsumeLoopAsync(_runCts.Token), CancellationToken.None);

        var receiverDefinitions = await _settingsStore.LoadReceiverDefinitionsAsync(cancellationToken).ConfigureAwait(false);
        _receivers = _receiverFactory.CreateReceivers(receiverDefinitions);

        foreach (var receiver in _receivers)
        {
            await receiver.StartAsync(_writer!, _runCts.Token).ConfigureAwait(false);
        }
    }

    public async ValueTask SetPausedAsync(bool isPaused, CancellationToken cancellationToken = default)
    {
        bool changed;
        lock (_gate)
        {
            changed = _isPaused != isPaused;
            _isPaused = isPaused;
            _workspaceSettings = _workspaceSettings with { PauseIngest = isPaused };
        }

        if (!changed)
        {
            return;
        }

        await _settingsStore.SaveWorkspaceSettingsAsync(_workspaceSettings, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask SetAutoScrollAsync(bool isEnabled, CancellationToken cancellationToken = default)
    {
        bool changed;
        lock (_gate)
        {
            changed = _workspaceSettings.AutoScroll != isEnabled;
            _workspaceSettings = _workspaceSettings with { AutoScroll = isEnabled };
        }

        if (!changed)
        {
            return;
        }

        await _settingsStore.SaveWorkspaceSettingsAsync(_workspaceSettings, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask ReloadReceiversAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ILogReceiver> currentReceivers;
        ChannelWriter<LogEntry>? writer;
        CancellationToken receiverToken;
        bool shouldReload;

        lock (_gate)
        {
            shouldReload = _started && _runCts is not null && _writer is not null;
            currentReceivers = _receivers;
            _receivers = [];
            writer = _writer;
            receiverToken = _runCts?.Token ?? CancellationToken.None;
        }

        if (!shouldReload || writer is null)
        {
            return;
        }

        await StopReceiversAsync(currentReceivers, cancellationToken).ConfigureAwait(false);

        var receiverDefinitions = await _settingsStore.LoadReceiverDefinitionsAsync(cancellationToken).ConfigureAwait(false);
        var reloaded = _receiverFactory.CreateReceivers(receiverDefinitions);
        foreach (var receiver in reloaded)
        {
            await receiver.StartAsync(writer, receiverToken).ConfigureAwait(false);
        }

        lock (_gate)
        {
            if (_started)
            {
                _receivers = reloaded;
            }
        }
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        bool shouldStop;
        lock (_gate)
        {
            shouldStop = _started;
            _started = false;
        }

        if (!shouldStop)
        {
            return;
        }

        var runCts = _runCts;
        var consumeTask = _consumeTask;
        var writer = _writer;
        var receivers = _receivers;

        _runCts = null;
        _consumeTask = null;
        _reader = null;
        _writer = null;
        _channel = null;
        _receivers = [];

        writer?.TryComplete();
        runCts?.Cancel();

        await StopReceiversAsync(receivers, cancellationToken).ConfigureAwait(false);

        if (consumeTask is not null)
        {
            try
            {
                await consumeTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }

        runCts?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        _store.EntriesAppended -= OnStoreEntriesAppended;
        await StopAsync().ConfigureAwait(false);
    }

    private async Task ConsumeLoopAsync(CancellationToken cancellationToken)
    {
        if (_reader is null)
        {
            return;
        }

        var batch = new List<LogEntry>(MaxBatchSize);
        using var timer = new PeriodicTimer(BatchFlushInterval);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsPaused)
                {
                    if (!await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                    {
                        break;
                    }

                    continue;
                }

                while (batch.Count < MaxBatchSize && _reader.TryRead(out var next))
                {
                    batch.Add(next);
                }

                if (batch.Count > 0)
                {
                    _store.AppendRange(batch.ToArray());
                    batch.Clear();
                    continue;
                }

                if (!await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown path.
        }
        finally
        {
            while (_reader.TryRead(out var trailing))
            {
                batch.Add(trailing);
                if (batch.Count >= MaxBatchSize)
                {
                    _store.AppendRange(batch.ToArray());
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                _store.AppendRange(batch.ToArray());
            }
        }
    }

    private void InitializeChannel(int capacity)
    {
        var channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        _channel = channel;
        _reader = channel.Reader;
        _writer = new DropCountingWriter(channel.Writer, channel.Reader, capacity, IncrementDroppedCounter);
    }

    private void IncrementDroppedCounter()
    {
        Interlocked.Increment(ref _droppedCount);
    }

    private void OnStoreEntriesAppended(object? sender, LogEntriesAppendedEventArgs e)
    {
        EntriesAppended?.Invoke(this, e);
    }

    private static async ValueTask StopReceiversAsync(
        IEnumerable<ILogReceiver> receivers,
        CancellationToken cancellationToken)
    {
        foreach (var receiver in receivers)
        {
            try
            {
                await receiver.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await receiver.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private sealed class DropCountingWriter : ChannelWriter<LogEntry>
    {
        private readonly ChannelWriter<LogEntry> _inner;
        private readonly ChannelReader<LogEntry> _reader;
        private readonly int _capacity;
        private readonly Action _onPotentialDrop;

        public DropCountingWriter(
            ChannelWriter<LogEntry> inner,
            ChannelReader<LogEntry> reader,
            int capacity,
            Action onPotentialDrop)
        {
            _inner = inner;
            _reader = reader;
            _capacity = capacity;
            _onPotentialDrop = onPotentialDrop;
        }

        public override bool TryComplete(Exception? error = null)
        {
            return _inner.TryComplete(error);
        }

        public override bool TryWrite(LogEntry item)
        {
            CapturePotentialDrop();
            return _inner.TryWrite(item);
        }

        public override ValueTask WriteAsync(LogEntry item, CancellationToken cancellationToken = default)
        {
            CapturePotentialDrop();
            return _inner.WriteAsync(item, cancellationToken);
        }

        public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
        {
            return _inner.WaitToWriteAsync(cancellationToken);
        }

        private void CapturePotentialDrop()
        {
            if (_reader.CanCount && _reader.Count >= _capacity)
            {
                _onPotentialDrop();
            }
        }
    }
}
