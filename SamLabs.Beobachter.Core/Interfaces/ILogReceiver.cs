using System.Threading.Channels;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Core.Interfaces;

public interface ILogReceiver : IAsyncDisposable
{
    string Id { get; }

    string DisplayName { get; }

    ValueTask StartAsync(ChannelWriter<LogEntry> writer, CancellationToken cancellationToken);

    ValueTask StopAsync(CancellationToken cancellationToken = default);
}
