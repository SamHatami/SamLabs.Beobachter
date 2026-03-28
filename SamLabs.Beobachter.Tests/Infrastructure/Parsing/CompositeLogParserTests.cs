using System.Text;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Interfaces;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Infrastructure.Parsing;
using Xunit;

namespace SamLabs.Beobachter.Tests.Infrastructure.Parsing;

public sealed class CompositeLogParserTests
{
    [Fact]
    public void TryParse_UsesFirstParserThatSucceeds()
    {
        var parser = new CompositeLogParser(
        [
            new FailingParser("first"),
            new SuccessParser("second")
        ]);

        var ok = parser.TryParse(
            Encoding.UTF8.GetBytes("anything"),
            new LogSourceContext { ReceiverId = "r1", DefaultLoggerName = "Default" },
            out var entry);

        Assert.True(ok);
        Assert.NotNull(entry);
        Assert.Equal("second", entry!.Message);
    }

    [Fact]
    public void TryParse_ReturnsFalseWhenNoParserHandlesPayload()
    {
        var parser = new CompositeLogParser([new FailingParser("first"), new FailingParser("second")]);

        var ok = parser.TryParse(
            Encoding.UTF8.GetBytes("anything"),
            new LogSourceContext { ReceiverId = "r1", DefaultLoggerName = "Default" },
            out var entry);

        Assert.False(ok);
        Assert.Null(entry);
    }

    [Fact]
    public void TryParse_ComposesRealParsersInOrder()
    {
        var parser = new CompositeLogParser([new Log4jXmlParser(), new JsonLogParser(), new CsvParser(), new PlainTextParser()]);
        const string csv = "1,2026/03/28 12:00:00.000,INFO,1,Orders.Api,Run,CSV works,,Orders.cs:10";

        var ok = parser.TryParse(
            Encoding.UTF8.GetBytes(csv),
            new LogSourceContext { ReceiverId = "r2", DefaultLoggerName = "Default" },
            out var entry);

        Assert.True(ok);
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Info, entry!.Level);
        Assert.Equal("CSV works", entry.Message);
    }

    [Fact]
    public void TryParse_ComposesJsonParserForStructuredPayload()
    {
        var parser = new CompositeLogParser([new Log4jXmlParser(), new JsonLogParser(), new CsvParser(), new PlainTextParser()]);
        const string json = """{"@l":"Warn","@m":"structured warning","SourceContext":"Orders.Api","tenant":"alpha"}""";

        var ok = parser.TryParse(
            Encoding.UTF8.GetBytes(json),
            new LogSourceContext { ReceiverId = "r3", DefaultLoggerName = "Default" },
            out var entry);

        Assert.True(ok);
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Warn, entry!.Level);
        Assert.Equal("structured warning", entry.Message);
        Assert.Equal("alpha", entry.Properties["tenant"]);
    }

    private sealed class FailingParser(string name) : ILogParser
    {
        public string Name => name;

        public bool TryParse(ReadOnlyMemory<byte> payload, LogSourceContext sourceContext, out LogEntry? entry)
        {
            entry = null;
            return false;
        }
    }

    private sealed class SuccessParser(string message) : ILogParser
    {
        public string Name => "success";

        public bool TryParse(ReadOnlyMemory<byte> payload, LogSourceContext sourceContext, out LogEntry? entry)
        {
            entry = new LogEntry
            {
                ReceiverId = sourceContext.ReceiverId,
                LoggerName = sourceContext.DefaultLoggerName,
                RootLoggerName = sourceContext.DefaultLoggerName,
                Message = message,
                Level = LogLevel.Info
            };
            return true;
        }
    }
}
