using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.Services;

public interface IShellStatusFormatter
{
    ShellStatusPresentation Build(
        bool isPaused,
        bool isAutoScrollEnabled,
        int totalCount,
        int visibleCount,
        long droppedCount,
        int activeReceivers,
        int structuredOnlyCount,
        LogStatisticsSnapshot statisticsSnapshot);
}
