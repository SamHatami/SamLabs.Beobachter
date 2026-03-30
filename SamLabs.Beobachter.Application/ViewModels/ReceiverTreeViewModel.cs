using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed partial class ReceiverTreeViewModel : ViewModelBase
{
    private readonly ReceiverTreeGroupViewModel _udpGroup;
    private readonly ReceiverTreeGroupViewModel _tcpGroup;
    private readonly ReceiverTreeGroupViewModel _fileGroup;

    public ReceiverTreeViewModel(ReceiverSetupViewModel receiverSetup)
    {
        ReceiverSetup = receiverSetup ?? throw new ArgumentNullException(nameof(receiverSetup));
        ReceiverSetup.ReceiverDefinitions.CollectionChanged += OnReceiverDefinitionsChanged;
        ReceiverSetup.PropertyChanged += OnReceiverSetupPropertyChanged;

        _udpGroup = new ReceiverTreeGroupViewModel(
            "UDP",
            "fa-solid fa-tower-broadcast",
            ReceiverSetup.AddUdpReceiverCommand);
        _tcpGroup = new ReceiverTreeGroupViewModel(
            "TCP",
            "fa-solid fa-ethernet",
            ReceiverSetup.AddTcpReceiverCommand);
        _fileGroup = new ReceiverTreeGroupViewModel(
            "File",
            "fa-regular fa-file-lines",
            ReceiverSetup.AddFileReceiverCommand);

        Groups.Add(_udpGroup);
        Groups.Add(_tcpGroup);
        Groups.Add(_fileGroup);

        SyncFilteredCollections();
        SelectedNode = ReceiverSetup.SelectedReceiverDefinition;
    }

    public ReceiverSetupViewModel ReceiverSetup { get; }

    public ObservableCollection<ReceiverTreeGroupViewModel> Groups { get; } = [];

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private object? _selectedNode;

    public event EventHandler? OpenEditorRequested;

    public void RequestOpenEditor()
    {
        OpenEditorRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void OpenEditor()
    {
        RequestOpenEditor();
    }

    [RelayCommand]
    private void OpenReceiverEditor(ReceiverDefinitionViewModel? receiver)
    {
        if (receiver is null)
        {
            return;
        }

        ReceiverSetup.SelectedReceiverDefinition = receiver;
        SelectedNode = receiver;
        RequestOpenEditor();
    }

    [RelayCommand]
    private void RemoveReceiver(ReceiverDefinitionViewModel? receiver)
    {
        if (receiver is null)
        {
            return;
        }

        ReceiverSetup.SelectedReceiverDefinition = receiver;
        SelectedNode = receiver;
        ReceiverSetup.RemoveSelectedReceiverCommand.Execute(null);
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

    partial void OnSelectedNodeChanged(object? value)
    {
        if (value is ReceiverDefinitionViewModel receiver)
        {
            ReceiverSetup.SelectedReceiverDefinition = receiver;
        }
    }

    private void OnReceiverSetupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.Equals(
                e.PropertyName,
                nameof(ReceiverSetupViewModel.SelectedReceiverDefinition),
                StringComparison.Ordinal))
        {
            return;
        }

        SelectedNode = ReceiverSetup.SelectedReceiverDefinition;
    }

    private void SyncFilteredCollections()
    {
        SyncCollection(_udpGroup.Children, static x => x.IsUdp);
        SyncCollection(_tcpGroup.Children, static x => x.IsTcp);
        SyncCollection(_fileGroup.Children, static x => x.IsFile);
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
