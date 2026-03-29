using CommunityToolkit.Mvvm.ComponentModel;

namespace SamLabs.Beobachter.Application.ViewModels.Status;

public sealed partial class SessionHealthViewModel : ViewModelBase
{
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

    public bool IsRunning => !IsPaused;

    public string StatusText => IsPaused ? "Status: Paused" : "Status: Running";
}
