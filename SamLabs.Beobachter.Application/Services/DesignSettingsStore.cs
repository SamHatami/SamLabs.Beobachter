using System.Threading;
using System.Threading.Tasks;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Application.Services;

internal sealed class DesignSettingsStore : ISettingsStore
{
    private AppSettings _app = new();
    private ReceiverDefinitions _receivers = new();
    private WorkspaceSettings _workspace = new();
    private UiLayoutSettings _layout = new();

    public ValueTask<AppSettings> LoadAppSettingsAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_app);
    }

    public ValueTask<ReceiverDefinitions> LoadReceiverDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_receivers);
    }

    public ValueTask<WorkspaceSettings> LoadWorkspaceSettingsAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_workspace);
    }

    public ValueTask<UiLayoutSettings> LoadUiLayoutSettingsAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_layout);
    }

    public ValueTask SaveAppSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        _app = settings;
        return ValueTask.CompletedTask;
    }

    public ValueTask SaveReceiverDefinitionsAsync(ReceiverDefinitions settings, CancellationToken cancellationToken = default)
    {
        _receivers = settings;
        return ValueTask.CompletedTask;
    }

    public ValueTask SaveWorkspaceSettingsAsync(WorkspaceSettings settings, CancellationToken cancellationToken = default)
    {
        _workspace = settings;
        return ValueTask.CompletedTask;
    }

    public ValueTask SaveUiLayoutSettingsAsync(UiLayoutSettings settings, CancellationToken cancellationToken = default)
    {
        _layout = settings;
        return ValueTask.CompletedTask;
    }
}
