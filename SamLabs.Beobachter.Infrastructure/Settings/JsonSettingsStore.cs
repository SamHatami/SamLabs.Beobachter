using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Linq;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Infrastructure.Settings;

public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string _rootDirectory;

    public JsonSettingsStore(JsonSettingsStoreOptions? options = null)
    {
        var resolved = options ?? new JsonSettingsStoreOptions();
        _rootDirectory = resolved.RootDirectory;
    }

    public ValueTask<AppSettings> LoadAppSettingsAsync(CancellationToken cancellationToken = default)
    {
        return LoadAsync("app.settings.json", new AppSettings(), SettingsJsonContext.Default.AppSettings, cancellationToken);
    }

    public ValueTask<ReceiverDefinitions> LoadReceiverDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return LoadReceiverDefinitionsCoreAsync(cancellationToken);
    }

    private async ValueTask<ReceiverDefinitions> LoadReceiverDefinitionsCoreAsync(CancellationToken cancellationToken)
    {
        ReceiverDefinitions loaded = await LoadAsync(
            "receivers.settings.json",
            new ReceiverDefinitions(),
            SettingsJsonContext.Default.ReceiverDefinitions,
            cancellationToken).ConfigureAwait(false);

        return NormalizeFramingModes(loaded);
    }

    public ValueTask<WorkspaceSettings> LoadWorkspaceSettingsAsync(CancellationToken cancellationToken = default)
    {
        return LoadAsync(
            "workspace.settings.json",
            new WorkspaceSettings(),
            SettingsJsonContext.Default.WorkspaceSettings,
            cancellationToken);
    }

    public ValueTask<UiLayoutSettings> LoadUiLayoutSettingsAsync(CancellationToken cancellationToken = default)
    {
        return LoadAsync(
            "uilayout.settings.json",
            new UiLayoutSettings(),
            SettingsJsonContext.Default.UiLayoutSettings,
            cancellationToken);
    }

    public ValueTask SaveAppSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        return SaveAsync("app.settings.json", settings, SettingsJsonContext.Default.AppSettings, cancellationToken);
    }

    public ValueTask SaveReceiverDefinitionsAsync(ReceiverDefinitions settings, CancellationToken cancellationToken = default)
    {
        return SaveAsync(
            "receivers.settings.json",
            settings,
            SettingsJsonContext.Default.ReceiverDefinitions,
            cancellationToken);
    }

    public ValueTask SaveWorkspaceSettingsAsync(WorkspaceSettings settings, CancellationToken cancellationToken = default)
    {
        return SaveAsync(
            "workspace.settings.json",
            settings,
            SettingsJsonContext.Default.WorkspaceSettings,
            cancellationToken);
    }

    public ValueTask SaveUiLayoutSettingsAsync(UiLayoutSettings settings, CancellationToken cancellationToken = default)
    {
        return SaveAsync(
            "uilayout.settings.json",
            settings,
            SettingsJsonContext.Default.UiLayoutSettings,
            cancellationToken);
    }

    private async ValueTask<TSettings> LoadAsync<TSettings>(
        string fileName,
        TSettings fallback,
        JsonTypeInfo<TSettings> jsonTypeInfo,
        CancellationToken cancellationToken)
        where TSettings : class
    {
        var path = GetPath(fileName);
        if (!File.Exists(path))
        {
            return fallback;
        }

        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var loaded = await JsonSerializer.DeserializeAsync(stream, jsonTypeInfo, cancellationToken).ConfigureAwait(false);
            return loaded ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
        catch (IOException)
        {
            return fallback;
        }
        catch (UnauthorizedAccessException)
        {
            return fallback;
        }
    }

    private async ValueTask SaveAsync<TSettings>(
        string fileName,
        TSettings settings,
        JsonTypeInfo<TSettings> jsonTypeInfo,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_rootDirectory);
        var path = GetPath(fileName);

        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, settings, jsonTypeInfo, cancellationToken).ConfigureAwait(false);
    }

    private string GetPath(string fileName)
    {
        return Path.Combine(_rootDirectory, fileName);
    }

    private static ReceiverDefinitions NormalizeFramingModes(ReceiverDefinitions definitions)
    {
        UdpReceiverDefinition[] udp = definitions.UdpReceivers
            .Select(static x => x with
            {
                FramingMode = x.FramingMode == ReceiverFramingMode.Unknown
                    ? ReceiverFramingMode.Datagram
                    : x.FramingMode
            })
            .ToArray();

        TcpReceiverDefinition[] tcp = definitions.TcpReceivers
            .Select(static x => x with
            {
                FramingMode = x.FramingMode == ReceiverFramingMode.Unknown
                    ? ReceiverFramingMode.XmlEvent
                    : x.FramingMode
            })
            .ToArray();

        FileTailReceiverDefinition[] file = definitions.FileTailReceivers
            .Select(static x => x with
            {
                FramingMode = x.FramingMode == ReceiverFramingMode.Unknown
                    ? ReceiverFramingMode.XmlEvent
                    : x.FramingMode
            })
            .ToArray();

        return definitions with
        {
            UdpReceivers = udp,
            TcpReceivers = tcp,
            FileTailReceivers = file
        };
    }
}
