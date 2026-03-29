using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SamLabs.Beobachter.Core.Interfaces;

namespace SamLabs.Beobachter.Application.ViewModels.Status;

public sealed partial class SessionHealthViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private CancellationTokenSource? _notificationCts;

    [NotifyPropertyChangedFor(nameof(IsRunning))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private string _activeReceiversText = "Active receivers: 0";

    [ObservableProperty]
    private string _bufferedEntriesText = "Buffered entries: 0";

    [ObservableProperty]
    private string _structuredEventsText = "Structured events: 0";

    [ObservableProperty]
    private string _droppedPacketsText = "Dropped packets: 0";

    [ObservableProperty]
    private string _settingsNotification = string.Empty;

    [ObservableProperty]
    private bool _hasSettingsNotification;

    public SessionHealthViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _settingsService.AppSettingsSaved += OnAppSettingsSaved;
    }

    public bool IsRunning => !IsPaused;

    public string StatusText => IsPaused ? "Status: Paused" : "Status: Running";

    private void OnAppSettingsSaved(object? sender, AppSettingsSavedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => ShowSettingsNotification("Settings saved"));
    }

    private void ShowSettingsNotification(string message)
    {
        _notificationCts?.Cancel();
        _notificationCts?.Dispose();
        _notificationCts = new CancellationTokenSource();

        SettingsNotification = message;
        HasSettingsNotification = true;

        _ = DismissNotificationAsync(_notificationCts.Token);
    }

    private async Task DismissNotificationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(3000, cancellationToken).ConfigureAwait(false);
            Dispatcher.UIThread.Post(() =>
            {
                SettingsNotification = string.Empty;
                HasSettingsNotification = false;
            });
        }
        catch (TaskCanceledException)
        {
        }
    }
}
