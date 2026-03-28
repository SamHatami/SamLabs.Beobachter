using System.Text;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Infrastructure.Parsing;
using Xunit;

namespace SamLabs.Beobachter.Tests.Infrastructure.Parsing;

public sealed class CsvParserTests
{
    private readonly CsvParser _parser = new();

    [Fact]
    public void TryParse_ParsesDefaultCsvShape()
    {
        const string line = "12,2026/03/28 11:22:33.456,ERROR,7,Orders.Api.Checkout,Submit,\"Payment failed, timeout\",System.TimeoutException,CheckoutService.cs:88";

        var ok = _parser.TryParse(
            Encoding.UTF8.GetBytes(line),
            new LogSourceContext { ReceiverId = "file-csv-1", DefaultLoggerName = "DefaultCsv" },
            out var entry);

        Assert.True(ok);
        Assert.NotNull(entry);
        Assert.Equal(12, entry!.SequenceNumber);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("Orders.Api.Checkout", entry.LoggerName);
        Assert.Equal("Payment failed, timeout", entry.Message);
        Assert.Equal("Submit", entry.CallSiteMethod);
        Assert.Equal("CheckoutService.cs", entry.SourceFileName);
        Assert.Equal((uint)88, entry.SourceFileLineNumber);
    }

    [Fact]
    public void TryParse_ReturnsFalseForHeaderLine()
    {
        const string header = "sequence,time,level,thread,class,method,message,exception,file";

        var ok = _parser.TryParse(
            Encoding.UTF8.GetBytes(header),
            new LogSourceContext { ReceiverId = "file-csv-1", DefaultLoggerName = "DefaultCsv" },
            out var entry);

        Assert.False(ok);
        Assert.Null(entry);
    }

    [Fact]
    public void TryParse_ReturnsFalseForXmlPayload()
    {
        const string xml = "<log4j:event />";

        var ok = _parser.TryParse(
            Encoding.UTF8.GetBytes(xml),
            new LogSourceContext { ReceiverId = "file-csv-1", DefaultLoggerName = "DefaultCsv" },
            out var entry);

        Assert.False(ok);
        Assert.Null(entry);
    }
}
