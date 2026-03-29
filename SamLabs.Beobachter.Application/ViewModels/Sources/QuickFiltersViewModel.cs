using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SamLabs.Beobachter.Application.ViewModels.Sources;

public sealed partial class QuickFiltersViewModel : ViewModelBase
{
    [NotifyPropertyChangedFor(nameof(ErrorsAndAboveText))]
    [ObservableProperty]
    private bool _isErrorsAndAboveEnabled;

    [NotifyPropertyChangedFor(nameof(StructuredOnlyText))]
    [ObservableProperty]
    private bool _isStructuredOnlyEnabled;

    [NotifyPropertyChangedFor(nameof(ErrorsAndAboveText))]
    [ObservableProperty]
    private int _errorsAndAboveCount;

    [NotifyPropertyChangedFor(nameof(StructuredOnlyText))]
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
}
