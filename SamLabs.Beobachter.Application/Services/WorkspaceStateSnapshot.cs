using System;
using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Application.Services;

public sealed class WorkspaceStateSnapshot
{
    public WorkspaceStateSnapshot(WorkspaceSettings workspaceSettings, UiLayoutSettings uiLayoutSettings)
    {
        WorkspaceSettings = workspaceSettings ?? throw new ArgumentNullException(nameof(workspaceSettings));
        UiLayoutSettings = uiLayoutSettings ?? throw new ArgumentNullException(nameof(uiLayoutSettings));
    }

    public WorkspaceSettings WorkspaceSettings { get; }

    public UiLayoutSettings UiLayoutSettings { get; }
}
