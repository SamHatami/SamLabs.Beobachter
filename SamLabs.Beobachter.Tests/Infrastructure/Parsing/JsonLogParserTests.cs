using System.Text;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Infrastructure.Parsing;
using Xunit;

namespace SamLabs.Beobachter.Tests.Infrastructure.Parsing;

public sealed class JsonLogParserTests
{
    private readonly JsonLogParser _parser = new();

    [Fact]
    public void TryParse_ParsesStructuredJsonWithPropertyBag()
    {
        const string json = """
            {
              "timestamp": "2026-03-28T12:10:11Z",
              "level": "WARN",
              "logger": "Orders.Api.Checkout",
              "thread": "worker-7",
              "message": "Payment gateway timeout",
              "sequence": 42,
              "exception": "System.TimeoutException: gateway timeout",
              "hostName": "node-1",
              "properties": {
                "tenant": "acme",
                "statusCode": 504
              }
            }
            """;

        var ok = _parser.TryParse(
            Encoding.UTF8.GetBytes(json),
            new LogSourceContext { ReceiverId = "udp-json", DefaultLoggerName = "DefaultLogger" },
            out var entry);

        Assert.True(ok);
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Warn, entry!.Level);
        Assert.Equal("Orders.Api.Checkout", entry.LoggerName);
        Assert.Equal("Payment gateway timeout", entry.Message);
        Assert.Equal(42, entry.SequenceNumber);
        Assert.Equal("node-1", entry.HostName);
        Assert.Equal("acme", entry.Properties["tenant"]);
        Assert.Equal("504", entry.Properties["statusCode"]);
    }

    [Fact]
    public void TryParse_ParsesSerilogStylePayload()
    {
        const string json = """
            {
              "@t": "2026-03-28T13:45:00.0000000Z",
              "@l": "Error",
              "@m": "Failed to capture payment",
              "@x": "System.InvalidOperationException: payment already captured",
              "SourceContext": "Billing.Worker",
              "OrderId": 123,
              "ElapsedMs": 42.7
            }
            """;

        var ok = _parser.TryParse(
            Encoding.UTF8.GetBytes(json),
            new LogSourceContext { ReceiverId = "tcp-json", DefaultLoggerName = "DefaultLogger" },
            out var entry);

        Assert.True(ok);
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Error, entry!.Level);
        Assert.Equal("Billing.Worker", entry.LoggerName);
        Assert.Equal("Failed to capture payment", entry.Message);
        Assert.Equal("System.InvalidOperationException: payment already captured", entry.Exception);
        Assert.Equal("123", entry.Properties["OrderId"]);
        Assert.Equal("42.7", entry.Properties["ElapsedMs"]);
    }

    [Fact]
    public void TryParse_UsesNumericLevelNormalization()
    {
        const string json = """
            {
              "time": 1760000000123,
              "level": 60001,
              "message": "Error level by numeric range"
            }
            """;

        var ok = _parser.TryParse(
            Encoding.UTF8.GetBytes(json),
            new LogSourceContext { ReceiverId = "file-json", DefaultLoggerName = "DefaultLogger" },
            out var entry);

        Assert.True(ok);
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Error, entry!.Level);
        Assert.Equal(60001, entry.RawLevelValue);
    }

    [Fact]
    public void TryParse_ReturnsFalseForNonJsonOrInvalidPayload()
    {
        var plainTextOk = _parser.TryParse(
            Encoding.UTF8.GetBytes("not json"),
            new LogSourceContext { ReceiverId = "r1", DefaultLoggerName = "DefaultLogger" },
            out var plainEntry);

        Assert.False(plainTextOk);
        Assert.Null(plainEntry);

        var invalidJsonOk = _parser.TryParse(
            Encoding.UTF8.GetBytes("{\"message\":\"broken\""),
            new LogSourceContext { ReceiverId = "r1", DefaultLoggerName = "DefaultLogger" },
            out var invalidEntry);

        Assert.False(invalidJsonOk);
        Assert.Null(invalidEntry);
    }
}
