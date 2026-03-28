using System.Threading;
using System.Threading.Tasks;

namespace SamLabs.Beobachter.Application.Services;

public interface IClipboardService
{
    ValueTask SetTextAsync(string text, CancellationToken cancellationToken = default);
}
