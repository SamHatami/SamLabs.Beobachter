using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Core.Interfaces;

public interface ISettingsStore
{
    ValueTask<AppSettings> LoadAppSettingsAsync(CancellationToken cancellationToken = default);

    ValueTask<ReceiverDefinitions> LoadReceiverDefinitionsAsync(CancellationToken cancellationToken = default);

    ValueTask<WorkspaceSettings> LoadWorkspaceSettingsAsync(CancellationToken cancellationToken = default);

    ValueTask<UiLayoutSettings> LoadUiLayoutSettingsAsync(CancellationToken cancellationToken = default);

    ValueTask SaveAppSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default);

    ValueTask SaveReceiverDefinitionsAsync(ReceiverDefinitions settings, CancellationToken cancellationToken = default);

    ValueTask SaveWorkspaceSettingsAsync(WorkspaceSettings settings, CancellationToken cancellationToken = default);

    ValueTask SaveUiLayoutSettingsAsync(UiLayoutSettings settings, CancellationToken cancellationToken = default);
}
