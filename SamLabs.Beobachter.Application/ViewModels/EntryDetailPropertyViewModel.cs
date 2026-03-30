using System;
using CommunityToolkit.Mvvm.Input;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed class EntryDetailPropertyViewModel
{
    private readonly Action<string, string> _requestFilter;

    public EntryDetailPropertyViewModel(string key, string value, Action<string, string> requestFilter)
    {
        Key = key;
        Value = value;
        _requestFilter = requestFilter;
        FilterCommand = new RelayCommand(() => _requestFilter(Key, Value));
    }

    public string Key { get; }

    public string Value { get; }

    public IRelayCommand FilterCommand { get; }
}
