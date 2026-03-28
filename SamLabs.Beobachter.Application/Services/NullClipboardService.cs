using System.Threading;
using System.Threading.Tasks;

namespace SamLabs.Beobachter.Application.Services;

internal sealed class NullClipboardService : IClipboardService
{
    public ValueTask SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
