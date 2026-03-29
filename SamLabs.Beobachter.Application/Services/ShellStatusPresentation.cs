using System;

namespace SamLabs.Beobachter.Application.Services;

public sealed class ShellStatusPresentation
{
    public ShellStatusPresentation(
        string statusSummary,
        string statsSummary1Minute,
        string statsSummary5Minutes,
        string topLoggersSummary,
        string topReceiversSummary,
        string activeReceiversText,
        string bufferedEntriesText,
        string structuredEventsText,
        string droppedPacketsText)
    {
        StatusSummary = statusSummary ?? throw new ArgumentNullException(nameof(statusSummary));
        StatsSummary1Minute = statsSummary1Minute ?? throw new ArgumentNullException(nameof(statsSummary1Minute));
        StatsSummary5Minutes = statsSummary5Minutes ?? throw new ArgumentNullException(nameof(statsSummary5Minutes));
        TopLoggersSummary = topLoggersSummary ?? throw new ArgumentNullException(nameof(topLoggersSummary));
        TopReceiversSummary = topReceiversSummary ?? throw new ArgumentNullException(nameof(topReceiversSummary));
        ActiveReceiversText = activeReceiversText ?? throw new ArgumentNullException(nameof(activeReceiversText));
        BufferedEntriesText = bufferedEntriesText ?? throw new ArgumentNullException(nameof(bufferedEntriesText));
        StructuredEventsText = structuredEventsText ?? throw new ArgumentNullException(nameof(structuredEventsText));
        DroppedPacketsText = droppedPacketsText ?? throw new ArgumentNullException(nameof(droppedPacketsText));
    }

    public string StatusSummary { get; }

    public string StatsSummary1Minute { get; }

    public string StatsSummary5Minutes { get; }

    public string TopLoggersSummary { get; }

    public string TopReceiversSummary { get; }

    public string ActiveReceiversText { get; }

    public string BufferedEntriesText { get; }

    public string StructuredEventsText { get; }

    public string DroppedPacketsText { get; }
}
