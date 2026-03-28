using SamLabs.Beobachter.Core.Interfaces;

namespace SamLabs.Beobachter.Infrastructure.Parsing;

public sealed class ParserPipelineFactory
{
    private readonly IReadOnlyList<ILogParser> _registeredParsers;

    public ParserPipelineFactory(IEnumerable<ILogParser> registeredParsers)
    {
        _registeredParsers = registeredParsers?.ToArray() ?? throw new ArgumentNullException(nameof(registeredParsers));
        if (_registeredParsers.Count == 0)
        {
            throw new ArgumentException("At least one parser must be registered.", nameof(registeredParsers));
        }
    }

    public ILogParser Create(IReadOnlyList<string>? parserOrder = null)
    {
        var selected = ResolveParserOrder(parserOrder);
        if (selected.Count == 1)
        {
            return selected[0];
        }

        return new CompositeLogParser(selected);
    }

    private IReadOnlyList<ILogParser> ResolveParserOrder(IReadOnlyList<string>? parserOrder)
    {
        if (parserOrder is null || parserOrder.Count == 0)
        {
            return _registeredParsers;
        }

        var selected = new List<ILogParser>(parserOrder.Count);
        foreach (var requestedName in parserOrder)
        {
            var match = _registeredParsers.FirstOrDefault(
                parser => parser.Name.Equals(requestedName, StringComparison.OrdinalIgnoreCase));

            if (match is not null && !selected.Contains(match))
            {
                selected.Add(match);
            }
        }

        return selected.Count == 0 ? _registeredParsers : selected;
    }
}
