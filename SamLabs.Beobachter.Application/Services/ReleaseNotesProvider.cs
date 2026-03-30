using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia.Platform;

namespace SamLabs.Beobachter.Application.Services;

public sealed class ReleaseNotesProvider : IReleaseNotesProvider
{
    private static readonly Uri ReleaseNotesUri = new("avares://SamLabs.Beobachter.Application/Assets/release-notes.json");

    public ReleaseNotesSnapshot GetCurrentReleaseNotes()
    {
        string versionFallback = GetApplicationVersion();
        string publishedOnFallback = DateTime.UtcNow.ToString("yyyy-MM-dd");

        if (!AssetLoader.Exists(ReleaseNotesUri))
        {
            return new ReleaseNotesSnapshot(
                versionFallback,
                publishedOnFallback,
                "No release notes were bundled with this build.",
                []);
        }

        try
        {
            using System.IO.Stream stream = AssetLoader.Open(ReleaseNotesUri);
            ReleaseNotesDocument? loaded = JsonSerializer.Deserialize<ReleaseNotesDocument>(stream);
            if (loaded is null)
            {
                return new ReleaseNotesSnapshot(
                    versionFallback,
                    publishedOnFallback,
                    "Release notes could not be loaded.",
                    []);
            }

            List<string> highlights = loaded.Highlights?
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Select(static x => x.Trim())
                .ToList() ?? [];

            return new ReleaseNotesSnapshot(
                string.IsNullOrWhiteSpace(loaded.Version) ? versionFallback : loaded.Version,
                string.IsNullOrWhiteSpace(loaded.PublishedOn) ? publishedOnFallback : loaded.PublishedOn,
                loaded.Summary ?? string.Empty,
                highlights);
        }
        catch (JsonException)
        {
            return new ReleaseNotesSnapshot(
                versionFallback,
                publishedOnFallback,
                "Release notes are present but not in a valid JSON format.",
                []);
        }
        catch (System.IO.IOException)
        {
            return new ReleaseNotesSnapshot(
                versionFallback,
                publishedOnFallback,
                "Release notes could not be read.",
                []);
        }
        catch (UnauthorizedAccessException)
        {
            return new ReleaseNotesSnapshot(
                versionFallback,
                publishedOnFallback,
                "Release notes could not be accessed.",
                []);
        }
    }

    private static string GetApplicationVersion()
    {
        Assembly assembly = typeof(ReleaseNotesProvider).Assembly;
        string? informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            int plusIndex = informational.IndexOf('+');
            return plusIndex > 0 ? informational[..plusIndex] : informational;
        }

        Version? assemblyVersion = assembly.GetName().Version;
        return assemblyVersion is null ? "0.0.0" : assemblyVersion.ToString();
    }

    private sealed class ReleaseNotesDocument
    {
        public string Version { get; init; } = string.Empty;

        public string PublishedOn { get; init; } = string.Empty;

        public string Summary { get; init; } = string.Empty;

        public string[] Highlights { get; init; } = [];
    }
}
