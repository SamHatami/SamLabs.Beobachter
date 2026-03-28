using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Queries;

namespace SamLabs.Beobachter.Core.Interfaces;

public interface ILogQueryEvaluator
{
    bool Matches(LogEntry entry, LogQuery query);
}
