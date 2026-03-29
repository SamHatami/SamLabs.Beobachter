using System;
using SamLabs.Beobachter.Application.ViewModels.Sources;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed class WorkspaceSidebarViewModel : ViewModelBase
{
    public WorkspaceSidebarViewModel(
        SourceTreeViewModel sources,
        QuickFiltersViewModel quickFilters,
        ReceiverSetupViewModel receiverSetup,
        LogFiltersViewModel filters)
    {
        Sources = sources ?? throw new ArgumentNullException(nameof(sources));
        QuickFilters = quickFilters ?? throw new ArgumentNullException(nameof(quickFilters));
        ReceiverSetup = receiverSetup ?? throw new ArgumentNullException(nameof(receiverSetup));
        Filters = filters ?? throw new ArgumentNullException(nameof(filters));
    }

    public SourceTreeViewModel Sources { get; }

    public QuickFiltersViewModel QuickFilters { get; }

    public ReceiverSetupViewModel ReceiverSetup { get; }

    public LogFiltersViewModel Filters { get; }
}
