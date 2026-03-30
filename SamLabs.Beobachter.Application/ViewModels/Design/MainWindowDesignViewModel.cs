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
        DesignSettingsService settingsService) : this(
        ingestionSession,
        settingsStore,
        settingsService,
        new ReceiverSetupViewModel(settingsStore, ingestionSession))
    {
    }

    private MainWindowDesignViewModel(
        DesignIngestionSession ingestionSession,
        DesignSettingsStore settingsStore,
        DesignSettingsService settingsService,
        ReceiverSetupViewModel receiverSetup) : base(
        new ShellStatusFormatter(),
        new SampleLogEntryGenerator(),
        ingestionSession,
        new WorkspaceStateCoordinator(settingsStore),
        new WorkspaceStartupOrchestrator(new WorkspaceStateCoordinator(settingsStore)),
        new LogStreamProjectionService(new LogQueryEvaluator()),
        new RollingLogStatisticsService(),
        new TopBarViewModel(),
        new SourceTreeViewModel(),
        new QuickFiltersViewModel(),
        receiverSetup,
        new ReceiverTreeViewModel(receiverSetup),
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
