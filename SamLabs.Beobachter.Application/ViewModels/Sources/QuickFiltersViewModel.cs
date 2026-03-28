using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SamLabs.Beobachter.Application.ViewModels.Sources;

public sealed partial class QuickFiltersViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isErrorsAndAboveEnabled;

    [ObservableProperty]
    private bool _isStructuredOnlyEnabled;

    [ObservableProperty]
    private int _errorsAndAboveCount;

    [ObservableProperty]
    private int _structuredOnlyCount;

    public string ErrorsAndAboveText => IsErrorsAndAboveEnabled
        ? $"Errors and above ({ErrorsAndAboveCount}) *"
        : $"Errors and above ({ErrorsAndAboveCount})";

    public string StructuredOnlyText => IsStructuredOnlyEnabled
        ? $"Structured only ({StructuredOnlyCount}) *"
        : $"Structured only ({StructuredOnlyCount})";

    [RelayCommand]
    private void ApplyErrorsAndAbove()
    {
        IsErrorsAndAboveEnabled = true;
    }

    [RelayCommand]
    private void ApplyStructuredOnly()
    {
        IsStructuredOnlyEnabled = true;
    }

    [RelayCommand]
    private void ClearQuickFilters()
    {
        IsErrorsAndAboveEnabled = false;
        IsStructuredOnlyEnabled = false;
    }

    partial void OnIsErrorsAndAboveEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(ErrorsAndAboveText));
    }

    partial void OnIsStructuredOnlyEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(StructuredOnlyText));
    }

    partial void OnErrorsAndAboveCountChanged(int value)
    {
        OnPropertyChanged(nameof(ErrorsAndAboveText));
    }

    partial void OnStructuredOnlyCountChanged(int value)
    {
        OnPropertyChanged(nameof(StructuredOnlyText));
    }
}
