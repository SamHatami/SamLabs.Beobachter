using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Application.ViewModels.Sources;
using SamLabs.Beobachter.Application.ViewModels.Status;
using SamLabs.Beobachter.Core.Services;

namespace SamLabs.Beobachter.Application.ViewModels.Design;

public sealed class MainWindowDesignViewModel : MainWindowViewModel
{
    public MainWindowDesignViewModel() : base(
        new ShellStatusFormatter(),
        new SampleLogEntryGenerator(),
        new ThemeService(),
        new DesignIngestionSession(),
        new WorkspaceStateCoordinator(new DesignSettingsStore()),
        new LogStreamProjectionService(new LogQueryEvaluator()),
        new RollingLogStatisticsService(),
        new SourceTreeViewModel(),
        new QuickFiltersViewModel(),
        new ReceiverSetupViewModel(new DesignSettingsStore(), new DesignIngestionSession()),
        new WorkspaceSidebarViewModel(
            new SourceTreeViewModel(),
            new QuickFiltersViewModel(),
            new ReceiverSetupViewModel(new DesignSettingsStore(), new DesignIngestionSession())),
        new LogFiltersViewModel(),
        new LogStreamViewModel(),
        new EntryDetailsViewModel(new NullClipboardService()),
        new SessionHealthViewModel())
    {
    }
}
