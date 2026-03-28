using System.Threading;
using System.Threading.Tasks;
using SamLabs.Beobachter.Core.Interfaces;

namespace SamLabs.Beobachter.Application.Services;

internal sealed class NullClipboardService : IClipboardService
{
    public ValueTask SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
