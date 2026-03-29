namespace SamLabs.Beobachter.Application.Services;

public sealed class QuickFilterSnapshot
{
    public QuickFilterSnapshot(int errorsAndAboveCount, int structuredOnlyCount)
    {
        ErrorsAndAboveCount = errorsAndAboveCount;
        StructuredOnlyCount = structuredOnlyCount;
    }

    public int ErrorsAndAboveCount { get; }

    public int StructuredOnlyCount { get; }
}
