using System;
using SamLabs.Beobachter.Application.ViewModels.Sources;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed class WorkspaceSidebarViewModel : ViewModelBase
{
    public WorkspaceSidebarViewModel(
        SourceTreeViewModel sources,
        QuickFiltersViewModel quickFilters,
        ReceiverSetupViewModel receiverSetup)
    {
        Sources = sources ?? throw new ArgumentNullException(nameof(sources));
        QuickFilters = quickFilters ?? throw new ArgumentNullException(nameof(quickFilters));
        ReceiverSetup = receiverSetup ?? throw new ArgumentNullException(nameof(receiverSetup));
    }

    public SourceTreeViewModel Sources { get; }

    public QuickFiltersViewModel QuickFilters { get; }

    public ReceiverSetupViewModel ReceiverSetup { get; }
}
