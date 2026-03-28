using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Infrastructure.Parsing;
using Xunit;

namespace SamLabs.Beobachter.Tests.Infrastructure.Parsing;

public sealed class ParserPipelineFactoryTests
{
    [Fact]
    public void Create_UsesRequestedOrder()
    {
        var factory = new ParserPipelineFactory([new NamedParser("A"), new NamedParser("B"), new NamedParser("C")]);

        var parser = factory.Create(["C", "A"]);

        Assert.Contains("C", parser.Name, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("A", parser.Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_FallsBackToAllRegisteredWhenRequestUnknown()
    {
        var factory = new ParserPipelineFactory([new NamedParser("A"), new NamedParser("B")]);

        var parser = factory.Create(["X"]);

        Assert.Contains("A", parser.Name, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("B", parser.Name, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class NamedParser(string name) : ILogParser
    {
        public string Name => name;

        public bool TryParse(ReadOnlyMemory<byte> payload, LogSourceContext sourceContext, out LogEntry? entry)
        {
            entry = null;
            return false;
        }
    }
}
