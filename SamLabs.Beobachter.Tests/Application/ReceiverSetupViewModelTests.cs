using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Core.Settings;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

public sealed class ReceiverSetupViewModelTests
{
    [Fact]
    public async Task SaveReceiverSetup_PersistsDefinitionsAndReloadsSession()
    {
        FakeSettingsStore settings = new();
        FakeIngestionSession session = new([]);
        ReceiverSetupViewModel vm = new(settings, session);
        await vm.LoadAsync();

        vm.AddUdpReceiverCommand.Execute(null);
        vm.AddTcpReceiverCommand.Execute(null);
        vm.AddFileReceiverCommand.Execute(null);

        ReceiverDefinitionViewModel fileReceiver = vm.ReceiverDefinitions.Single(x => x.IsFile);
        fileReceiver.FilePath = "C:/logs/app.log";
        ReceiverDefinitionViewModel udpReceiver = vm.ReceiverDefinitions.Single(x => x.IsUdp);
        udpReceiver.ParserOrderText = "JsonLogParser, PlainTextParser";

        await ((IAsyncRelayCommand)vm.SaveReceiverSetupCommand).ExecuteAsync(null);

        ReceiverDefinitions? saved = settings.LastSavedReceiverDefinitions;
        Assert.NotNull(saved);
        Assert.Single(saved!.UdpReceivers);
        Assert.Single(saved.TcpReceivers);
        Assert.Single(saved.FileTailReceivers);
        Assert.Equal("C:/logs/app.log", saved.FileTailReceivers[0].FilePath);
        Assert.Equal(["JsonLogParser", "PlainTextParser"], saved.UdpReceivers[0].ParserOrder);
        Assert.Equal(1, session.ReloadReceiversCalls);
    }

    [Fact]
    public async Task SaveReceiverSetup_FileReceiverWithoutPort_IsValid()
    {
        FakeSettingsStore settings = new();
        FakeIngestionSession session = new([]);
        ReceiverSetupViewModel vm = new(settings, session);
        await vm.LoadAsync();

        vm.AddFileReceiverCommand.Execute(null);
        ReceiverDefinitionViewModel fileReceiver = vm.ReceiverDefinitions.Single(x => x.IsFile);
        fileReceiver.FilePath = "C:/logs/app.log";
        fileReceiver.Port = 0;

        await ((IAsyncRelayCommand)vm.SaveReceiverSetupCommand).ExecuteAsync(null);

        Assert.NotNull(settings.LastSavedReceiverDefinitions);
        Assert.Single(settings.LastSavedReceiverDefinitions!.FileTailReceivers);
        Assert.Equal("C:/logs/app.log", settings.LastSavedReceiverDefinitions.FileTailReceivers[0].FilePath);
        Assert.Equal(1, session.ReloadReceiversCalls);
    }

    [Fact]
    public async Task SaveReceiverSetup_InvalidConfiguration_DoesNotPersistOrReload()
    {
        FakeSettingsStore settings = new();
        FakeIngestionSession session = new([]);
        ReceiverSetupViewModel vm = new(settings, session);
        await vm.LoadAsync();

        vm.AddUdpReceiverCommand.Execute(null);
        ReceiverDefinitionViewModel udp = vm.ReceiverDefinitions.Single(x => x.IsUdp);
        udp.Port = 70_000;

        await ((IAsyncRelayCommand)vm.SaveReceiverSetupCommand).ExecuteAsync(null);

        Assert.Null(settings.LastSavedReceiverDefinitions);
        Assert.Equal(0, session.ReloadReceiversCalls);
        Assert.Contains("Validation failed", vm.ReceiverSetupStatus);
    }

    [Fact]
    public async Task SaveReceiverSetup_LocalhostBindAddress_IsAccepted()
    {
        FakeSettingsStore settings = new();
        FakeIngestionSession session = new([]);
        ReceiverSetupViewModel vm = new(settings, session);
        await vm.LoadAsync();

        vm.AddUdpReceiverCommand.Execute(null);
        ReceiverDefinitionViewModel udp = vm.ReceiverDefinitions.Single(x => x.IsUdp);
        udp.BindAddress = "localhost";

        await ((IAsyncRelayCommand)vm.SaveReceiverSetupCommand).ExecuteAsync(null);

        Assert.NotNull(settings.LastSavedReceiverDefinitions);
        Assert.Equal("localhost", settings.LastSavedReceiverDefinitions!.UdpReceivers[0].BindAddress);
        Assert.Equal(1, session.ReloadReceiversCalls);
    }

    [Fact]
    public async Task SaveReceiverSetup_NonLocalhostHostnameBindAddress_IsRejectedBeforeReload()
    {
        FakeSettingsStore settings = new();
        FakeIngestionSession session = new([]);
        ReceiverSetupViewModel vm = new(settings, session);
        await vm.LoadAsync();

        vm.AddUdpReceiverCommand.Execute(null);
        ReceiverDefinitionViewModel udp = vm.ReceiverDefinitions.Single(x => x.IsUdp);
        udp.BindAddress = "loghost";

        await ((IAsyncRelayCommand)vm.SaveReceiverSetupCommand).ExecuteAsync(null);

        Assert.Null(settings.LastSavedReceiverDefinitions);
        Assert.Equal(0, session.ReloadReceiversCalls);
        Assert.Contains("literal IPv4 or IPv6", udp.BindAddressValidationError);
    }

    [Fact]
    public async Task SaveReceiverSetup_LiteralIpv6BindAddress_IsAccepted()
    {
        FakeSettingsStore settings = new();
        FakeIngestionSession session = new([]);
        ReceiverSetupViewModel vm = new(settings, session);
        await vm.LoadAsync();

        vm.AddUdpReceiverCommand.Execute(null);
        ReceiverDefinitionViewModel udp = vm.ReceiverDefinitions.Single(x => x.IsUdp);
        udp.BindAddress = "::1";

        await ((IAsyncRelayCommand)vm.SaveReceiverSetupCommand).ExecuteAsync(null);

        Assert.NotNull(settings.LastSavedReceiverDefinitions);
        Assert.Equal(1, session.ReloadReceiversCalls);
    }

    [Fact]
    public async Task ReloadReceiverSetup_LoadsDefinitionsFromSettings()
    {
        FakeSettingsStore settings =
        new()
        {
            ReceiverDefinitions = new ReceiverDefinitions
            {
                UdpReceivers =
                [
                    new UdpReceiverDefinition
                    {
                        Id = "udp-prod",
                        DisplayName = "UDP Prod",
                        BindAddress = "127.0.0.1",
                        Port = 17071,
                        ParserOrder = ["JsonLogParser", "PlainTextParser"]
                    }
                ],
                FileTailReceivers =
                [
                    new FileTailReceiverDefinition
                    {
                        Id = "file-prod",
                        DisplayName = "File Prod",
                        FilePath = "C:/logs/prod.log",
                        PollIntervalMs = 250
                    }
                ]
            }
        };

        FakeIngestionSession session = new([]);
        ReceiverSetupViewModel vm = new(settings, session);
        await ((IAsyncRelayCommand)vm.ReloadReceiverSetupCommand).ExecuteAsync(null);

        Assert.Equal(2, vm.ReceiverDefinitions.Count);
        Assert.Contains(vm.ReceiverDefinitions, x => x.Id == "udp-prod" && x.IsUdp && x.Port == 17071);
        Assert.Contains(vm.ReceiverDefinitions, x => x.Id == "file-prod" && x.IsFile && x.PollIntervalMs == 250);
        Assert.Contains(vm.ReceiverDefinitions, x => x.Id == "udp-prod" && x.ParserOrderText.Contains("JsonLogParser"));
        Assert.Equal(1, session.ReloadReceiversCalls);
    }

    [Fact]
    public async Task ToggleEnabled_AutoSavesAndReloadsSession()
    {
        FakeSettingsStore settings =
        new()
        {
            ReceiverDefinitions = new ReceiverDefinitions
            {
                UdpReceivers =
                [
                    new UdpReceiverDefinition
                    {
                        Id = "udp-a",
                        DisplayName = "UDP A",
                        Enabled = true,
                        BindAddress = "127.0.0.1",
                        Port = 17071
                    }
                ]
            }
        };

        FakeIngestionSession session = new([]);
        ReceiverSetupViewModel vm = new(settings, session);
        await vm.LoadAsync();

        Assert.Equal(0, session.ReloadReceiversCalls);

        ReceiverDefinitionViewModel udp = vm.ReceiverDefinitions.Single(x => x.Id == "udp-a");
        udp.Enabled = false;

        await MainWindowTestSupport.WaitForConditionAsync(() => session.ReloadReceiversCalls == 1);

        Assert.NotNull(settings.LastSavedReceiverDefinitions);
        Assert.False(settings.LastSavedReceiverDefinitions!.UdpReceivers[0].Enabled);
        Assert.Equal(string.Empty, vm.ReceiverSetupStatus);
    }

    [Fact]
    public async Task TrySelectReceiverById_SelectsMatchingReceiver()
    {
        FakeSettingsStore settings =
        new()
        {
            ReceiverDefinitions = new ReceiverDefinitions
            {
                UdpReceivers = [new UdpReceiverDefinition { Id = "udp-prod", DisplayName = "UDP Prod" }],
                TcpReceivers = [new TcpReceiverDefinition { Id = "tcp-prod", DisplayName = "TCP Prod" }]
            }
        };
        ReceiverSetupViewModel vm = new(settings, new FakeIngestionSession([]));
        await vm.LoadAsync();

        vm.TrySelectReceiverById("tcp-prod");

        Assert.NotNull(vm.SelectedReceiverDefinition);
        Assert.Equal("tcp-prod", vm.SelectedReceiverDefinition!.Id);
    }
}
