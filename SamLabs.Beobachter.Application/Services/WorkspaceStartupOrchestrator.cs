using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Application.Services;

public sealed class WorkspaceStartupOrchestrator : IWorkspaceStartupOrchestrator
{
    private readonly IWorkspaceStateCoordinator _workspaceStateCoordinator;

    public WorkspaceStartupOrchestrator(IWorkspaceStateCoordinator workspaceStateCoordinator)
    {
        _workspaceStateCoordinator = workspaceStateCoordinator ?? throw new ArgumentNullException(nameof(workspaceStateCoordinator));
    }

    public async ValueTask InitializeAsync(
        ReceiverSetupViewModel receiverSetup,
        Action<WorkspaceSettings, UiLayoutSettings> applyWorkspaceState,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receiverSetup);
        ArgumentNullException.ThrowIfNull(applyWorkspaceState);

        await receiverSetup.LoadAsync().ConfigureAwait(false);
        WorkspaceStateSnapshot snapshot = await _workspaceStateCoordinator.LoadAsync(cancellationToken).ConfigureAwait(false);

        await InvokeOnUiThreadAsync(() =>
        {
            applyWorkspaceState(snapshot.WorkspaceSettings, snapshot.UiLayoutSettings);
            ApplyInitialReceiverSelection(receiverSetup, snapshot.WorkspaceSettings.SelectedReceiverId);
        }).ConfigureAwait(false);
    }

    private static void ApplyInitialReceiverSelection(ReceiverSetupViewModel receiverSetup, string? selectedReceiverId)
    {
        if (receiverSetup.ReceiverDefinitions.Count == 0)
        {
            return;
        }

        receiverSetup.TrySelectReceiverById(selectedReceiverId);
        receiverSetup.SelectedReceiverDefinition ??= receiverSetup.ReceiverDefinitions.FirstOrDefault();
    }

    private static async ValueTask InvokeOnUiThreadAsync(Action callback)
    {
        if (Avalonia.Application.Current is null || Dispatcher.UIThread.CheckAccess())
        {
            callback();
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(callback);
    }
}
