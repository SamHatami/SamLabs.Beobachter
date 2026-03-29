using System.Linq;

namespace SamLabs.Beobachter.Core.Models;

public sealed record class ReceiverReloadResult
{
    public IReadOnlyList<ReceiverStartupResult> ReceiverStartupResults { get; init; } = [];

    public int AttemptedCount => ReceiverStartupResults.Count;

    public int SuccessfulCount => ReceiverStartupResults.Count(static x => x.Succeeded);

    public int FailedCount => AttemptedCount - SuccessfulCount;
}
