using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Application.ViewModels.Sources;
using SamLabs.Beobachter.Application.ViewModels.Status;
using SamLabs.Beobachter.Core.Services;

namespace SamLabs.Beobachter.Application.ViewModels.Design;

public sealed class MainWindowDesignViewModel : MainWindowViewModel
{
    public MainWindowDesignViewModel() : this(new DesignIngestionSession(), new DesignSettingsStore(), new DesignSettingsService())
    {
    }

    private MainWindowDesignViewModel(
        DesignIngestionSession ingestionSession,
        DesignSettingsStore settingsStore,
        DesignSettingsService settingsService) : base(
        new ShellStatusFormatter(),
        new SampleLogEntryGenerator(),
        ingestionSession,
        new WorkspaceStateCoordinator(settingsStore),
        new WorkspaceStartupOrchestrator(new WorkspaceStateCoordinator(settingsStore)),
        new LogStreamProjectionService(new LogQueryEvaluator()),
        new RollingLogStatisticsService(),
        new TopBarViewModel(ingestionSession),
        new SourceTreeViewModel(),
        new QuickFiltersViewModel(),
        new ReceiverSetupViewModel(settingsStore, ingestionSession),
        CreateDesignSidebar(),
        new LogFiltersViewModel(),
        new LogStreamViewModel(ingestionSession),
        new EntryDetailsViewModel(new NullClipboardService()),
        new SessionHealthViewModel(settingsService))
    {
    }

    private static WorkspaceSidebarViewModel CreateDesignSidebar()
    {
        return new WorkspaceSidebarViewModel(
            new SourceTreeViewModel(),
            new QuickFiltersViewModel(),
            new LogFiltersViewModel());
    }
}
