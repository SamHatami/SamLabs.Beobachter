using CommunityToolkit.Mvvm.ComponentModel;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed partial class SidebarFacetOptionViewModel : ObservableObject
{
    [ObservableProperty]
    private int _count;

    [ObservableProperty]
    private bool _isSelected;

    public SidebarFacetOptionViewModel(string label, string searchText, int count)
    {
        Label = label;
        SearchText = searchText;
        _count = count;
    }

    public string Label { get; }

    public string SearchText { get; }
}
