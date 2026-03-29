using System.Threading;
using System.Threading.Tasks;
using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Application.Services;

public interface IWorkspaceStateCoordinator
{
    ValueTask<WorkspaceStateSnapshot> LoadAsync(CancellationToken cancellationToken = default);

    void QueuePersist(WorkspaceStateUpdate update);
}
