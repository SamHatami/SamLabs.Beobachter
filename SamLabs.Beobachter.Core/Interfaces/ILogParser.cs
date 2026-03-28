using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Core.Interfaces;

public interface ILogParser
{
    string Name { get; }

    bool TryParse(ReadOnlyMemory<byte> payload, LogSourceContext sourceContext, out LogEntry? entry);
}
