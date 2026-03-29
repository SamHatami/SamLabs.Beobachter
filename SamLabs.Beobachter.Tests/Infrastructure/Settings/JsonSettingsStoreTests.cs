using SamLabs.Beobachter.Core.Settings;
using SamLabs.Beobachter.Infrastructure.Settings;
using Xunit;

namespace SamLabs.Beobachter.Tests.Infrastructure.Settings;

public sealed class JsonSettingsStoreTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsAllSettingsFiles()
    {
        var root = CreateTempDirectory();
        var store = new JsonSettingsStore(new JsonSettingsStoreOptions { RootDirectory = root });

        var app = new AppSettings { ThemeMode = "Dark", ChannelCapacity = 64_000 };
        var receivers = new ReceiverDefinitions
        {
            UdpReceivers =
            [
                new UdpReceiverDefinition
                {
                    Id = "udp-1",
                    DisplayName = "UDP One",
                    Port = 9010,
                    FramingMode = ReceiverFramingMode.XmlEvent,
                    ParserOrder = ["Log4jXmlParser", "PlainTextParser"]
                }
            ],
            FileTailReceivers =
            [
                new FileTailReceiverDefinition
                {
                    Id = "file-1",
                    DisplayName = "File One",
                    FilePath = "C:/logs/a.log"
                }
            ]
        };
        var workspace = new WorkspaceSettings { SearchText = "error", PauseIngest = true };
        var ui = new UiLayoutSettings { WindowWidth = 1400, WindowHeight = 900, IsMaximized = true };

        await store.SaveAppSettingsAsync(app);
        await store.SaveReceiverDefinitionsAsync(receivers);
        await store.SaveWorkspaceSettingsAsync(workspace);
        await store.SaveUiLayoutSettingsAsync(ui);

        var loadedApp = await store.LoadAppSettingsAsync();
        var loadedReceivers = await store.LoadReceiverDefinitionsAsync();
        var loadedWorkspace = await store.LoadWorkspaceSettingsAsync();
        var loadedUi = await store.LoadUiLayoutSettingsAsync();

        Assert.Equal(app, loadedApp);
        Assert.Equal("error", loadedWorkspace.SearchText);
        Assert.True(loadedWorkspace.PauseIngest);
        Assert.Equal(workspace.EnabledLevels, loadedWorkspace.EnabledLevels);
        Assert.Equal(ui, loadedUi);
        Assert.Single(loadedReceivers.UdpReceivers);
        Assert.Equal("udp-1", loadedReceivers.UdpReceivers[0].Id);
        Assert.Equal(9010, loadedReceivers.UdpReceivers[0].Port);
        Assert.Equal(ReceiverFramingMode.XmlEvent, loadedReceivers.UdpReceivers[0].FramingMode);
        Assert.Equal(2, loadedReceivers.UdpReceivers[0].ParserOrder.Count);
        Assert.Single(loadedReceivers.FileTailReceivers);
        Assert.Equal("C:/logs/a.log", loadedReceivers.FileTailReceivers[0].FilePath);
        Assert.Equal(ReceiverFramingMode.XmlEvent, loadedReceivers.FileTailReceivers[0].FramingMode);
    }

    [Fact]
    public async Task Load_ReturnsDefaultsWhenFilesMissingOrInvalid()
    {
        var root = CreateTempDirectory();
        var store = new JsonSettingsStore(new JsonSettingsStoreOptions { RootDirectory = root });

        await File.WriteAllTextAsync(Path.Combine(root, "receivers.settings.json"), "{ not valid json");

        var app = await store.LoadAppSettingsAsync();
        var receivers = await store.LoadReceiverDefinitionsAsync();
        var workspace = await store.LoadWorkspaceSettingsAsync();
        var ui = await store.LoadUiLayoutSettingsAsync();

        Assert.Equal("System", app.ThemeMode);
        Assert.Empty(receivers.UdpReceivers);
        Assert.True(workspace.AutoScroll);
        Assert.Equal(1200, ui.WindowWidth);
    }

    [Fact]
    public async Task LoadReceiverDefinitions_MissingFramingMode_UsesDefaultModes()
    {
        var root = CreateTempDirectory();
        var store = new JsonSettingsStore(new JsonSettingsStoreOptions { RootDirectory = root });
        string receiverJson = """
            {
              "udpReceivers": [
                {
                  "id": "udp-legacy",
                  "displayName": "UDP Legacy",
                  "enabled": true,
                  "bindAddress": "0.0.0.0",
                  "port": 7071
                }
              ],
              "tcpReceivers": [
                {
                  "id": "tcp-legacy",
                  "displayName": "TCP Legacy",
                  "enabled": true,
                  "bindAddress": "0.0.0.0",
                  "port": 4505
                }
              ],
              "fileTailReceivers": [
                {
                  "id": "file-legacy",
                  "displayName": "File Legacy",
                  "enabled": true,
                  "filePath": "C:/logs/legacy.log"
                }
              ]
            }
            """;
        await File.WriteAllTextAsync(Path.Combine(root, "receivers.settings.json"), receiverJson);

        ReceiverDefinitions loaded = await store.LoadReceiverDefinitionsAsync();

        Assert.Single(loaded.UdpReceivers);
        Assert.Single(loaded.TcpReceivers);
        Assert.Single(loaded.FileTailReceivers);
        Assert.Equal(ReceiverFramingMode.Datagram, loaded.UdpReceivers[0].FramingMode);
        Assert.Equal(ReceiverFramingMode.XmlEvent, loaded.TcpReceivers[0].FramingMode);
        Assert.Equal(ReceiverFramingMode.XmlEvent, loaded.FileTailReceivers[0].FramingMode);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "Beobachter.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
