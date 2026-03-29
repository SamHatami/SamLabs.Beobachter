using System.Text;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Infrastructure.Parsing;
using Xunit;

namespace SamLabs.Beobachter.Tests.Infrastructure.Parsing;

public sealed class PlainTextParserTests
{
    private readonly PlainTextParser _parser = new();

    [Fact]
    public void TryParse_ParsesBracketFormat()
    {
        const string line = "[2026-03-28 11:30:00.123 +01:00] [WARN] [Billing.Worker] [thr-3] Retry scheduled";

        var ok = _parser.TryParse(
            Encoding.UTF8.GetBytes(line),
            new LogSourceContext { ReceiverId = "plain-1", DefaultLoggerName = "Fallback" },
            out var entry);

        Assert.True(ok);
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Warn, entry!.Level);
        Assert.Equal("Billing.Worker", entry.LoggerName);
        Assert.Equal("thr-3", entry.ThreadName);
        Assert.Equal("Retry scheduled", entry.Message);
    }

    [Fact]
    public void TryParse_ParsesDashFormat()
    {
        const string line = "2026-03-28T11:35:00.000+01:00 ERROR Payments.Api - Charge failed";

        var ok = _parser.TryParse(
            Encoding.UTF8.GetBytes(line),
            new LogSourceContext { ReceiverId = "plain-2", DefaultLoggerName = "Fallback" },
            out var entry);

        Assert.True(ok);
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Error, entry!.Level);
        Assert.Equal("Payments.Api", entry.LoggerName);
        Assert.Equal("Charge failed", entry.Message);
    }

    [Fact]
    public void TryParse_UsesDefaultLoggerForUnknownShape()
    {
        const string line = "raw text with no structure";

        var ok = _parser.TryParse(
            Encoding.UTF8.GetBytes(line),
            new LogSourceContext { ReceiverId = "plain-3", DefaultLoggerName = "FallbackLogger" },
            out var entry);

        Assert.True(ok);
        Assert.NotNull(entry);
        Assert.Equal("FallbackLogger", entry!.LoggerName);
        Assert.Equal(LogLevel.Info, entry.Level);
        Assert.Equal(line, entry.Message);
    }

    [Fact]
    public void TryParse_ParsesPipeDelimitedFormat()
    {
        const string line = "2026-03-29T16:39:57.123+01:00 | ERROR | Demo.Payments.Checkout | Payment failed (#15)";

        var ok = _parser.TryParse(
            Encoding.UTF8.GetBytes(line),
            new LogSourceContext { ReceiverId = "plain-4", DefaultLoggerName = "FallbackLogger" },
            out var entry);

        Assert.True(ok);
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Error, entry!.Level);
        Assert.Equal("Demo.Payments.Checkout", entry.LoggerName);
        Assert.Equal("Payment failed (#15)", entry.Message);
    }
}
