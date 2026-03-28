namespace SamLabs.Beobachter.Core.Interfaces;

public interface IClipboardService
{
    ValueTask SetTextAsync(string text, CancellationToken cancellationToken = default);
}
