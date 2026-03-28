using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaApp = Avalonia.Application;

namespace SamLabs.Beobachter.Application.Services;

public sealed class AvaloniaClipboardService : IClipboardService
{
    public async ValueTask SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        if (AvaloniaApp.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        if (desktop.MainWindow?.Clipboard is not { } clipboard)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        await clipboard.SetTextAsync(text ?? string.Empty).ConfigureAwait(false);
    }
}
