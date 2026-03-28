using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Infrastructure.Parsing;

public sealed class CompositeLogParser : ILogParser
{
    private readonly IReadOnlyList<ILogParser> _parsers;

    public CompositeLogParser(IEnumerable<ILogParser> parsers)
    {
        _parsers = parsers?.ToArray() ?? throw new ArgumentNullException(nameof(parsers));
        if (_parsers.Count == 0)
        {
            throw new ArgumentException("At least one parser must be configured.", nameof(parsers));
        }
    }

    public string Name => $"Composite({string.Join(", ", _parsers.Select(static x => x.Name))})";

    public bool TryParse(ReadOnlyMemory<byte> payload, LogSourceContext sourceContext, out LogEntry? entry)
    {
        foreach (var parser in _parsers)
        {
            if (parser.TryParse(payload, sourceContext, out entry) && entry is not null)
            {
                return true;
            }
        }

        entry = null;
        return false;
    }
}
