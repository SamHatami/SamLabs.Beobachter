using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed partial class ReceiverTreeViewModel : ViewModelBase
{
    public ReceiverTreeViewModel(ReceiverSetupViewModel receiverSetup)
    {
        ReceiverSetup = receiverSetup ?? throw new ArgumentNullException(nameof(receiverSetup));
        ReceiverSetup.ReceiverDefinitions.CollectionChanged += OnReceiverDefinitionsChanged;
        SyncFilteredCollections();
    }

    public ReceiverSetupViewModel ReceiverSetup { get; }

    [ObservableProperty]
    private bool _isExpanded = true;

    public ObservableCollection<ReceiverDefinitionViewModel> UdpReceivers { get; } = [];
    public ObservableCollection<ReceiverDefinitionViewModel> TcpReceivers { get; } = [];
    public ObservableCollection<ReceiverDefinitionViewModel> FileReceivers { get; } = [];

    public event EventHandler? OpenEditorRequested;

    public void RequestOpenEditor()
    {
        OpenEditorRequested?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateRuntimeStates(IReadOnlyList<ReceiverRuntimeState> runtimeStates)
    {
        foreach (ReceiverDefinitionViewModel receiver in ReceiverSetup.ReceiverDefinitions)
        {
            ReceiverRuntimeState? state = runtimeStates.FirstOrDefault(
                x => string.Equals(x.ReceiverId, receiver.Id, StringComparison.OrdinalIgnoreCase));
            receiver.RunState = state?.State ?? ReceiverRunState.Stopped;
        }
    }

    private void OnReceiverDefinitionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncFilteredCollections();
    }

    private void SyncFilteredCollections()
    {
        SyncCollection(UdpReceivers, static x => x.IsUdp);
        SyncCollection(TcpReceivers, static x => x.IsTcp);
        SyncCollection(FileReceivers, static x => x.IsFile);
    }

    private void SyncCollection(
        ObservableCollection<ReceiverDefinitionViewModel> target,
        Func<ReceiverDefinitionViewModel, bool> filter)
    {
        var fresh = ReceiverSetup.ReceiverDefinitions.Where(filter).ToList();

        for (var i = target.Count - 1; i >= 0; i--)
        {
            if (!fresh.Contains(target[i]))
            {
                target.RemoveAt(i);
            }
        }

        for (var i = 0; i < fresh.Count; i++)
        {
            if (i >= target.Count || !ReferenceEquals(target[i], fresh[i]))
            {
                target.Insert(i, fresh[i]);
            }
        }
    }
}
