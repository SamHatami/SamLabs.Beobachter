using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Core.Queries;
using SamLabs.Beobachter.Core.Services;
using Xunit;

namespace SamLabs.Beobachter.Tests.Core;

public sealed class LogQueryEvaluatorTests
{
    private readonly LogQueryEvaluator _evaluator = new();

    [Fact]
    public void Matches_AppliesStructuredFieldFilters()
    {
        var entry = new LogEntry
        {
            Level = LogLevel.Error,
            ReceiverId = "udp-prod",
            LoggerName = "Orders.Api.Checkout",
            ThreadName = "worker-7",
            Message = "Checkout failed",
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["tenant"] = "alpha",
                ["traceId"] = "abcd1234"
            }
        };

        var query = new LogQuery
        {
            MinimumLevel = LogLevel.Warn,
            ReceiverId = "udp-prod",
            LoggerContains = "Checkout",
            ThreadContains = "worker",
            PropertyContains = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["tenant"] = "alp",
                ["traceId"] = "abcd"
            }
        };

        Assert.True(_evaluator.Matches(entry, query));
    }

    [Fact]
    public void Matches_TextFallbackSearchesMessageLoggerThreadExceptionAndProperties()
    {
        var entry = new LogEntry
        {
            Level = LogLevel.Info,
            ReceiverId = "tcp-dev",
            LoggerName = "Billing.Worker",
            ThreadName = "thread-99",
            Message = "Charge created",
            Exception = "TimeoutException",
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["tenant"] = "beta"
            }
        };

        Assert.True(_evaluator.Matches(entry, new LogQuery { TextContains = "worker" }));
        Assert.True(_evaluator.Matches(entry, new LogQuery { TextContains = "thread-99" }));
        Assert.True(_evaluator.Matches(entry, new LogQuery { TextContains = "timeout" }));
        Assert.True(_evaluator.Matches(entry, new LogQuery { TextContains = "tenant" }));
        Assert.True(_evaluator.Matches(entry, new LogQuery { TextContains = "beta" }));
        Assert.False(_evaluator.Matches(entry, new LogQuery { TextContains = "does-not-exist" }));
    }
}
