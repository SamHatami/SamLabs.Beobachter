namespace SamLabs.Beobachter.Core.Models;

public sealed record class ReceiverStartupResult
{
    public string ReceiverId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool Succeeded { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;
}
