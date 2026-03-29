using System.Text;
using System.Threading.Channels;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Settings;
using SamLabs.Beobachter.Infrastructure.Framing;

namespace SamLabs.Beobachter.Infrastructure.Receivers;

public sealed class FileTailReceiver : ILogReceiver
{
    private readonly FileTailReceiverOptions _options;
    private readonly ILogParser _parser;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private readonly LogSourceContext _sourceContext;

    private ChannelWriter<LogEntry>? _writer;
    private CancellationTokenSource? _runCts;
    private Task? _runTask;

    public FileTailReceiver(FileTailReceiverOptions options, ILogParser parser)
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
            _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _runTask = Task.Run(() => TailLoopAsync(_runCts.Token), CancellationToken.None);
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

        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            runTaskToAwait = _runTask;
            runCtsToDispose = _runCts;

            _runTask = null;
            _runCts = null;
            _writer = null;

            runCtsToDispose?.Cancel();
        }
        finally
        {
            _lifecycleLock.Release();
        }

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
        }

        runCtsToDispose?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _lifecycleLock.Dispose();
    }

    private async Task TailLoopAsync(CancellationToken cancellationToken)
    {
        ILogPayloadFramer framer = LogPayloadFramerFactory.Create(_options.FramingMode, ReceiverFramingMode.XmlEvent);
        var pendingCharacterCount = 0;
        var filePosition = ResolveInitialPosition();

        while (!cancellationToken.IsCancellationRequested)
        {
            var writer = _writer;
            if (writer is null)
            {
                break;
            }

            if (!File.Exists(_options.FilePath))
            {
                await DelayAsync(cancellationToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                using var fileStream = new FileStream(
                    _options.FilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);

                if (filePosition > fileStream.Length)
                {
                    filePosition = 0;
                    pendingCharacterCount = 0;
                    framer.Reset();
                }

                fileStream.Position = filePosition;
                using var streamReader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var chunk = await streamReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

                filePosition = fileStream.Position;
                if (chunk.Length == 0)
                {
                    await DelayAsync(cancellationToken).ConfigureAwait(false);
                    continue;
                }

                pendingCharacterCount += chunk.Length;
                framer.Push(Encoding.UTF8.GetBytes(chunk));
                await DrainParsedFramesAsync(framer, writer, cancellationToken).ConfigureAwait(false);

                if (pendingCharacterCount > _options.MaxBufferedCharacters)
                {
                    pendingCharacterCount = 0;
                    framer.Reset();
                }
            }
            catch (FileNotFoundException)
            {
                await DelayAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DirectoryNotFoundException)
            {
                await DelayAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                await DelayAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                await DelayAsync(cancellationToken).ConfigureAwait(false);
            }
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

    private long ResolveInitialPosition()
    {
        if (!_options.StartAtEnd || !File.Exists(_options.FilePath))
        {
            return 0;
        }

        try
        {
            return new FileInfo(_options.FilePath).Length;
        }
        catch (IOException)
        {
            return 0;
        }
    }

    private Task DelayAsync(CancellationToken cancellationToken)
    {
        if (_options.PollInterval <= TimeSpan.Zero)
        {
            return Task.CompletedTask;
        }

        return Task.Delay(_options.PollInterval, cancellationToken);
    }
}
