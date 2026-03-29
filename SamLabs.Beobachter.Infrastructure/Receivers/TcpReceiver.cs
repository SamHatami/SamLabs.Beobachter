using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Settings;
using SamLabs.Beobachter.Infrastructure.Framing;

namespace SamLabs.Beobachter.Infrastructure.Receivers;

public sealed class TcpReceiver : ILogReceiver
{
    private readonly TcpReceiverOptions _options;
    private readonly ILogParser _parser;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private readonly ConcurrentDictionary<long, Task> _clientTasks = new();
    private readonly LogSourceContext _sourceContext;

    private TcpListener? _listener;
    private ChannelWriter<LogEntry>? _writer;
    private CancellationTokenSource? _runCts;
    private Task? _acceptLoopTask;
    private long _nextClientId;

    public TcpReceiver(TcpReceiverOptions options, ILogParser parser)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));

        _sourceContext = new LogSourceContext
        {
            ReceiverId = _options.Id,
            DefaultLoggerName = _options.DefaultLoggerName,
            HostName = _options.HostName
        };
    }

    public string Id => _options.Id;

    public string DisplayName => _options.DisplayName;

    public async ValueTask StartAsync(ChannelWriter<LogEntry> writer, CancellationToken cancellationToken)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_acceptLoopTask is { IsCompleted: false })
            {
                return;
            }

            _writer = writer;
            _listener = CreateListener(_options);
            _listener.Start(_options.Backlog);

            _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _acceptLoopTask = Task.Run(() => AcceptLoopAsync(_runCts.Token), CancellationToken.None);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        Task? acceptLoopTaskToAwait = null;
        CancellationTokenSource? runCtsToDispose = null;
        TcpListener? listenerToStop = null;

        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            acceptLoopTaskToAwait = _acceptLoopTask;
            runCtsToDispose = _runCts;
            listenerToStop = _listener;

            _acceptLoopTask = null;
            _runCts = null;
            _listener = null;
            _writer = null;

            runCtsToDispose?.Cancel();
        }
        finally
        {
            _lifecycleLock.Release();
        }

        listenerToStop?.Stop();

        if (acceptLoopTaskToAwait is not null)
        {
            try
            {
                await acceptLoopTaskToAwait.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
            catch (ObjectDisposedException)
            {
                // Expected when listener is closed.
            }
        }

        var activeClientTasks = _clientTasks.Values.ToArray();
        if (activeClientTasks.Length > 0)
        {
            try
            {
                await Task.WhenAll(activeClientTasks).WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
        }

        _clientTasks.Clear();
        runCtsToDispose?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _lifecycleLock.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        if (_listener is null)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (SocketException)
            {
                continue;
            }

            client.ReceiveBufferSize = _options.ReceiveBufferSize;

            var clientId = Interlocked.Increment(ref _nextClientId);
            var clientTask = Task.Run(() => HandleClientAsync(client, cancellationToken), CancellationToken.None);
            _clientTasks[clientId] = clientTask;
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        ChannelWriter<LogEntry>? writer = _writer;
        if (writer is null)
        {
            return;
        }

        try
        {
            using var stream = client.GetStream();
            var receiveBufferSize = Math.Max(256, _options.ReceiveBufferSize);
            var receiveBuffer = new byte[receiveBufferSize];
            ILogPayloadFramer framer = LogPayloadFramerFactory.Create(_options.FramingMode, ReceiverFramingMode.XmlEvent);

            while (!cancellationToken.IsCancellationRequested)
            {
                var readBytes = await stream.ReadAsync(receiveBuffer.AsMemory(0, receiveBuffer.Length), cancellationToken)
                    .ConfigureAwait(false);
                if (readBytes <= 0)
                {
                    break;
                }

                framer.Push(receiveBuffer.AsMemory(0, readBytes));
                await DrainParsedFramesAsync(framer, writer, cancellationToken).ConfigureAwait(false);
            }

            await DrainParsedFramesAsync(framer, writer, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
        catch (IOException)
        {
            // Connection dropped.
        }
        catch (ObjectDisposedException)
        {
            // Connection closed during shutdown.
        }
        catch (ChannelClosedException)
        {
            // Ingest channel closed.
        }
        finally
        {
            client.Dispose();
        }
    }

    private async Task DrainParsedFramesAsync(
        ILogPayloadFramer framer,
        ChannelWriter<LogEntry> writer,
        CancellationToken cancellationToken)
    {
        while (framer.TryReadFrame(out ReadOnlyMemory<byte> payload))
        {
            if (!_parser.TryParse(payload, _sourceContext, out var entry) || entry is null)
            {
                continue;
            }

            await writer.WriteAsync(entry, cancellationToken).ConfigureAwait(false);
        }
    }

    private static TcpListener CreateListener(TcpReceiverOptions options)
    {
        if (!IPAddress.TryParse(options.BindAddress, out var bindAddress))
        {
            throw new ArgumentException($"Invalid TCP bind address: '{options.BindAddress}'.", nameof(options));
        }

        return new TcpListener(bindAddress, options.Port);
    }
}
