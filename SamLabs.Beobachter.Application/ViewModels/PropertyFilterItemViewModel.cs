using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed partial class PropertyFilterItemViewModel : ViewModelBase
{
    public PropertyFilterItemViewModel(string key)
    {
        Key = key;
    }

    public string Key { get; }

    [ObservableProperty]
    private string _value = string.Empty;

    [RelayCommand]
    private void ClearValue()
    {
        Value = string.Empty;
    }
}
