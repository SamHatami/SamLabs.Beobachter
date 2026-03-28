using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Infrastructure.Receivers;

public sealed class UdpReceiver : ILogReceiver
{
    private readonly UdpReceiverOptions _options;
    private readonly ILogParser _parser;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private readonly LogSourceContext _sourceContext;

    private UdpClient? _udpClient;
    private ChannelWriter<LogEntry>? _writer;
    private CancellationTokenSource? _runCts;
    private Task? _runTask;

    public UdpReceiver(UdpReceiverOptions options, ILogParser parser)
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
            if (_runTask is { IsCompleted: false })
            {
                return;
            }

            _writer = writer;
            _udpClient = CreateUdpClient(_options);
            _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _runTask = Task.Run(() => ReceiveLoopAsync(_runCts.Token), CancellationToken.None);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        Task? runTaskToAwait = null;
        CancellationTokenSource? runCtsToDispose = null;
        UdpClient? clientToDispose = null;

        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            runTaskToAwait = _runTask;
            runCtsToDispose = _runCts;
            clientToDispose = _udpClient;

            _runTask = null;
            _runCts = null;
            _udpClient = null;
            _writer = null;

            runCtsToDispose?.Cancel();
        }
        finally
        {
            _lifecycleLock.Release();
        }

        clientToDispose?.Dispose();

        if (runTaskToAwait is not null)
        {
            try
            {
                await runTaskToAwait.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
            catch (ObjectDisposedException)
            {
                // Expected if the socket is disposed while awaiting receive.
            }
        }

        runCtsToDispose?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _lifecycleLock.Dispose();
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        if (_udpClient is null || _writer is null)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult received;
            try
            {
                received = await _udpClient.ReceiveAsync(cancellationToken).ConfigureAwait(false);
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

            if (!_parser.TryParse(received.Buffer, _sourceContext, out var entry) || entry is null)
            {
                continue;
            }

            try
            {
                await _writer.WriteAsync(entry, cancellationToken).ConfigureAwait(false);
            }
            catch (ChannelClosedException)
            {
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static UdpClient CreateUdpClient(UdpReceiverOptions options)
    {
        if (!IPAddress.TryParse(options.BindAddress, out var bindAddress))
        {
            throw new ArgumentException($"Invalid UDP bind address: '{options.BindAddress}'.", nameof(options));
        }

        var endPoint = new IPEndPoint(bindAddress, options.Port);
        return new UdpClient(endPoint);
    }
}
