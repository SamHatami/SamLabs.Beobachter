using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed class ReceiverTreeGroupViewModel : ObservableObject
{
    public string Name { get; }

    public string Icon { get; }

    public ICommand AddReceiverCommand { get; }

    public ObservableCollection<ReceiverDefinitionViewModel> Children { get; } = [];

    public int ReceiverCount => Children.Count;

    public ReceiverTreeGroupViewModel(
        string name,
        string icon,
        ICommand addReceiverCommand)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Icon = icon ?? throw new ArgumentNullException(nameof(icon));
        AddReceiverCommand = addReceiverCommand ?? throw new ArgumentNullException(nameof(addReceiverCommand));
        Children.CollectionChanged += OnChildrenCollectionChanged;
    }

    private void OnChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ReceiverCount));
    }
}
