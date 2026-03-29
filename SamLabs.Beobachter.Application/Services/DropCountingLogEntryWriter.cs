using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.Services;

internal sealed class DropCountingLogEntryWriter : ChannelWriter<LogEntry>
{
    private readonly ChannelWriter<LogEntry> _inner;
    private readonly ChannelReader<LogEntry> _reader;
    private readonly int _capacity;
    private readonly Action _onPotentialDrop;

    public DropCountingLogEntryWriter(
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
