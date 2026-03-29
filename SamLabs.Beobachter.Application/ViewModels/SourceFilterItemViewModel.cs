using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed partial class SourceFilterItemViewModel : ObservableObject
{
    private readonly Action<SourceFilterItemViewModel> _stateChanged;
    private bool _isSyncing;

    [ObservableProperty]
    private int _count;

    [ObservableProperty]
    private bool _isEnabled = true;

    public SourceFilterItemViewModel(string name, Action<SourceFilterItemViewModel> stateChanged)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
        _stateChanged = stateChanged ?? throw new ArgumentNullException(nameof(stateChanged));
    }

    public string Name { get; }

    partial void OnIsEnabledChanged(bool value)
    {
        if (_isSyncing)
        {
            return;
        }

        _stateChanged(this);
    }

    public void SetIsEnabledSilently(bool value)
    {
        _isSyncing = true;
        try
        {
            IsEnabled = value;
        }
        finally
        {
            _isSyncing = false;
        }
    }
}
