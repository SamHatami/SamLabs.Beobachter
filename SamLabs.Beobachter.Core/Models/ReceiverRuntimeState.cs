namespace SamLabs.Beobachter.Core.Models;

public sealed record class ReceiverRuntimeState
{
    public string ReceiverId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public ReceiverRunState State { get; init; } = ReceiverRunState.Stopped;

    public string LastError { get; init; } = string.Empty;
}
