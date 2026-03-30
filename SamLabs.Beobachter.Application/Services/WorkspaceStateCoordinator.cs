using System;
using System.Threading;
using System.Threading.Tasks;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Application.Services;

public sealed class WorkspaceStateCoordinator : IWorkspaceStateCoordinator
{
    private readonly ISettingsStore _settingsStore;
    private readonly object _gate = new();
    private WorkspaceSettings _workspaceSettings = new();
    private UiLayoutSettings _uiLayoutSettings = new();
    private CancellationTokenSource? _persistStateCts;

    public WorkspaceStateCoordinator(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
    }

    public async ValueTask<WorkspaceStateSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        WorkspaceSettings workspaceSettings = await _settingsStore.LoadWorkspaceSettingsAsync(cancellationToken).ConfigureAwait(false);
        UiLayoutSettings uiLayoutSettings = await _settingsStore.LoadUiLayoutSettingsAsync(cancellationToken).ConfigureAwait(false);

        lock (_gate)
        {
            _workspaceSettings = workspaceSettings;
            _uiLayoutSettings = uiLayoutSettings;
        }

        return new WorkspaceStateSnapshot(workspaceSettings, uiLayoutSettings);
    }

    public void QueuePersist(WorkspaceStateUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);

        CancellationToken cancellationToken;
        WorkspaceSettings workspaceSettings;
        UiLayoutSettings uiLayoutSettings;

        lock (_gate)
        {
            _persistStateCts?.Cancel();
            _persistStateCts?.Dispose();
            _persistStateCts = new CancellationTokenSource();
            cancellationToken = _persistStateCts.Token;

            workspaceSettings = _workspaceSettings with
            {
                SearchText = update.SearchText,
                ReceiverFilter = update.ReceiverFilter,
                LoggerFilter = update.LoggerFilter,
                ThreadFilter = update.ThreadFilter,
                PropertyFilters = update.PropertyFilters,
                MinimumLevelOption = update.MinimumLevelOption,
                CompactDensity = update.CompactDensity,
                SelectedReceiverId = update.SelectedReceiverId,
                EnabledLevels = update.EnabledLevels,
                AutoScroll = update.AutoScroll,
                PauseIngest = update.PauseIngest
            };

            uiLayoutSettings = _uiLayoutSettings with
            {
                TimestampColumnWidth = update.TimestampColumnWidth,
                LevelColumnWidth = update.LevelColumnWidth,
                LoggerColumnWidth = update.LoggerColumnWidth
            };
        }

        _ = PersistAsync(workspaceSettings, uiLayoutSettings, cancellationToken);
    }

    private async Task PersistAsync(
        WorkspaceSettings workspaceSettings,
        UiLayoutSettings uiLayoutSettings,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            await _settingsStore.SaveWorkspaceSettingsAsync(workspaceSettings, cancellationToken).ConfigureAwait(false);
            await _settingsStore.SaveUiLayoutSettingsAsync(uiLayoutSettings, cancellationToken).ConfigureAwait(false);

            lock (_gate)
            {
                _workspaceSettings = workspaceSettings;
                _uiLayoutSettings = uiLayoutSettings;
            }
        }
        catch (OperationCanceledException)
        {
            // Debounce cancellation path.
        }
    }
}
