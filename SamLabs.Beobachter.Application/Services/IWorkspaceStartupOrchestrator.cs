using System;
using System.Threading;
using System.Threading.Tasks;
using SamLabs.Beobachter.Application.ViewModels;
using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Application.Services;

public interface IWorkspaceStartupOrchestrator
{
    ValueTask InitializeAsync(
        ReceiverSetupViewModel receiverSetup,
        Action<WorkspaceSettings, UiLayoutSettings> applyWorkspaceState,
        CancellationToken cancellationToken = default);
}
