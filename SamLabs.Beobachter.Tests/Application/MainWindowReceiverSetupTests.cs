using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Core.Settings;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

public sealed class MainWindowReceiverSetupTests
{
    [Fact]
    public async Task SaveReceiverSetup_PersistsDefinitionsAndReloadsSession()
    {
        FakeSettingsStore settings = new();
        FakeIngestionSession session = new([]);
        MainWindowViewModel vm = MainWindowTestSupport.CreateMainWindowViewModel(session, settingsStore: settings);

        await MainWindowTestSupport.WaitForReceiverLoadAsync(vm);

        vm.ReceiverSetup.AddUdpReceiverCommand.Execute(null);
        vm.ReceiverSetup.AddTcpReceiverCommand.Execute(null);
        vm.ReceiverSetup.AddFileReceiverCommand.Execute(null);

        ReceiverDefinitionViewModel fileReceiver = vm.ReceiverSetup.ReceiverDefinitions.Single(x => x.IsFile);
        fileReceiver.FilePath = "C:/logs/app.log";
        fileReceiver.Port = 7071;
        ReceiverDefinitionViewModel udpReceiver = vm.ReceiverSetup.ReceiverDefinitions.Single(x => x.IsUdp);
        udpReceiver.ParserOrderText = "JsonLogParser, PlainTextParser";

        await ((IAsyncRelayCommand)vm.ReceiverSetup.SaveReceiverSetupCommand).ExecuteAsync(null);

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
    public async Task SaveReceiverSetup_InvalidConfiguration_DoesNotPersistOrReload()
    {
        FakeSettingsStore settings = new();
        FakeIngestionSession session = new([]);
        MainWindowViewModel vm = MainWindowTestSupport.CreateMainWindowViewModel(session, settingsStore: settings);

        await MainWindowTestSupport.WaitForReceiverLoadAsync(vm);

        vm.ReceiverSetup.AddUdpReceiverCommand.Execute(null);
        ReceiverDefinitionViewModel udp = vm.ReceiverSetup.ReceiverDefinitions.Single(x => x.IsUdp);
        udp.Port = 70_000;

        await ((IAsyncRelayCommand)vm.ReceiverSetup.SaveReceiverSetupCommand).ExecuteAsync(null);

        Assert.Null(settings.LastSavedReceiverDefinitions);
        Assert.Equal(0, session.ReloadReceiversCalls);
        Assert.Contains("Validation failed", vm.ReceiverSetup.ReceiverSetupStatus);
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
        MainWindowViewModel vm = MainWindowTestSupport.CreateMainWindowViewModel(session, settingsStore: settings);

        await ((IAsyncRelayCommand)vm.ReceiverSetup.ReloadReceiverSetupCommand).ExecuteAsync(null);

        Assert.Equal(2, vm.ReceiverSetup.ReceiverDefinitions.Count);
        Assert.Contains(vm.ReceiverSetup.ReceiverDefinitions, x => x.Id == "udp-prod" && x.IsUdp && x.Port == 17071);
        Assert.Contains(vm.ReceiverSetup.ReceiverDefinitions, x => x.Id == "file-prod" && x.IsFile && x.PollIntervalMs == 250);
        Assert.Contains(vm.ReceiverSetup.ReceiverDefinitions, x => x.Id == "udp-prod" && x.ParserOrderText.Contains("JsonLogParser"));
        Assert.Equal(1, session.ReloadReceiversCalls);
    }
}
