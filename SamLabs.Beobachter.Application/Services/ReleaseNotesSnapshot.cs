using System;
using System.Collections.Generic;

namespace SamLabs.Beobachter.Application.Services;

public sealed class ReleaseNotesSnapshot
{
    public string Version { get; }

    public string PublishedOn { get; }

    public string Summary { get; }

    public IReadOnlyList<string> Highlights { get; }

    public ReleaseNotesSnapshot(
        string version,
        string publishedOn,
        string summary,
        IReadOnlyList<string> highlights)
    {
        Version = string.IsNullOrWhiteSpace(version) ? "0.0.0" : version.Trim();
        PublishedOn = string.IsNullOrWhiteSpace(publishedOn) ? "Unknown" : publishedOn.Trim();
        Summary = string.IsNullOrWhiteSpace(summary)
            ? "No release notes were provided for this build."
            : summary.Trim();
        Highlights = highlights ?? throw new ArgumentNullException(nameof(highlights));
    }
}
