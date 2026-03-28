using System.Text;
using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Models;
using SamLabs.Beobachter.Infrastructure.Parsing;
using Xunit;

namespace SamLabs.Beobachter.Tests.Infrastructure.Parsing;

public sealed class Log4jXmlParserTests
{
    private readonly Log4jXmlParser _parser = new();

    [Fact]
    public void TryParse_ParsesBasicLog4jEvent()
    {
        const string xml = """
            <log4j:event logger="App.Service.Auth" timestamp="1184286222308" level="ERROR" thread="7"
                         xmlns:log4j="http://jakarta.apache.org/log4j/">
              <log4j:message>Authentication failed</log4j:message>
              <log4j:locationInfo class="App.Service.AuthManager" method="Login" file="AuthManager.cs" line="42" />
            </log4j:event>
            """;

        var ok = _parser.TryParse(
            Encoding.UTF8.GetBytes(xml),
            new LogSourceContext { ReceiverId = "udp-1", DefaultLoggerName = "DefaultLogger" },
            out var entry);

        Assert.True(ok);
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Error, entry!.Level);
        Assert.Equal("App.Service.Auth", entry.LoggerName);
        Assert.Equal("Authentication failed", entry.Message);
        Assert.Equal("7", entry.ThreadName);
        Assert.Equal("App.Service.AuthManager", entry.CallSiteClass);
        Assert.Equal("Login", entry.CallSiteMethod);
        Assert.Equal("AuthManager.cs", entry.SourceFileName);
        Assert.Equal((uint)42, entry.SourceFileLineNumber);
        Assert.Equal("udp-1", entry.ReceiverId);
    }

    [Fact]
    public void TryParse_UsesIntegerLevelNormalization()
    {
        const string xml = """
            <log4j:event logger="App.Service" timestamp="1184286222308" level="10001" thread="9"
                         xmlns:log4j="http://jakarta.apache.org/log4j/">
              <log4j:message>Debug-level payload with integer level</log4j:message>
            </log4j:event>
            """;

        var ok = _parser.TryParse(
            Encoding.UTF8.GetBytes(xml),
            new LogSourceContext { ReceiverId = "tcp-1", DefaultLoggerName = "DefaultLogger" },
            out var entry);

        Assert.True(ok);
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Debug, entry!.Level);
        Assert.Equal(10001, entry.RawLevelValue);
        Assert.Equal("10001", entry.RawLevelName);
    }

    [Fact]
    public void TryParse_ParsesSequenceAndProperties()
    {
        const string xml = """
            <log4j:event logger="App.Service" timestamp="1184286222308" level="INFO" thread="12"
                         xmlns:log4j="http://jakarta.apache.org/log4j/"
                         xmlns:nlog="http://nlog-project.org">
              <log4j:message>Hello</log4j:message>
              <nlog:eventSequenceNumber>12345</nlog:eventSequenceNumber>
              <log4j:properties>
                <log4j:data name="tenant" value="alpha" />
                <log4j:data name="exceptions" value="oops" />
                <log4j:data name="log4net:HostName" value="api-host-01" />
              </log4j:properties>
            </log4j:event>
            """;

        var ok = _parser.TryParse(
            Encoding.UTF8.GetBytes(xml),
            new LogSourceContext { ReceiverId = "file-1", DefaultLoggerName = "DefaultLogger" },
            out var entry);

        Assert.True(ok);
        Assert.NotNull(entry);
        Assert.Equal(12345, entry!.SequenceNumber);
        Assert.Equal("alpha", entry.Properties["tenant"]);
        Assert.Equal("oops", entry.Exception);
        Assert.Equal("api-host-01", entry.HostName);
    }

    [Fact]
    public void TryParse_ReturnsFalseForInvalidXml()
    {
        const string xml = "<log4j:event><log4j:message>broken";

        var ok = _parser.TryParse(
            Encoding.UTF8.GetBytes(xml),
            new LogSourceContext { ReceiverId = "udp-1", DefaultLoggerName = "DefaultLogger" },
            out var entry);

        Assert.False(ok);
        Assert.Null(entry);
    }

    [Fact]
    public void TryParse_ParsesLog4j2XmlLayoutEvent()
    {
        const string xml = """
            <Event xmlns="http://logging.apache.org/log4j/2.0/events"
                   loggerName="Orders.Api.Controllers.CheckoutController"
                   level="WARN"
                   threadName="worker-3"
                   timeMillis="1700000000123">
              <Message>Payment gateway timeout</Message>
              <Source class="Orders.Api.Controllers.CheckoutController" method="Post" file="CheckoutController.cs" line="88" />
              <ContextMap>
                <item key="tenant" value="beta" />
                <item key="hostName" value="app-node-02" />
              </ContextMap>
            </Event>
            """;

        var ok = _parser.TryParse(
            Encoding.UTF8.GetBytes(xml),
            new LogSourceContext { ReceiverId = "tcp-2", DefaultLoggerName = "DefaultLogger" },
            out var entry);

        Assert.True(ok);
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Warn, entry!.Level);
        Assert.Equal("Orders.Api.Controllers.CheckoutController", entry.LoggerName);
        Assert.Equal("Payment gateway timeout", entry.Message);
        Assert.Equal("worker-3", entry.ThreadName);
        Assert.Equal("Orders.Api.Controllers.CheckoutController", entry.CallSiteClass);
        Assert.Equal("Post", entry.CallSiteMethod);
        Assert.Equal("CheckoutController.cs", entry.SourceFileName);
        Assert.Equal((uint)88, entry.SourceFileLineNumber);
        Assert.Equal("beta", entry.Properties["tenant"]);
        Assert.Equal("app-node-02", entry.HostName);
    }

    [Fact]
    public void TryParse_ParsesLog4j2InstantAndThrownMessage()
    {
        const string xml = """
            <Event xmlns="http://logging.apache.org/log4j/2.0/events"
                   loggerName="Billing.Worker"
                   level="ERROR"
                   thread="billing-thread">
              <Instant epochSecond="1700000100" nanoOfSecond="500000000" />
              <Message>Charge failed</Message>
              <Thrown message="System.InvalidOperationException: payment already captured" />
            </Event>
            """;

        var ok = _parser.TryParse(
            Encoding.UTF8.GetBytes(xml),
            new LogSourceContext { ReceiverId = "udp-2", DefaultLoggerName = "DefaultLogger" },
            out var entry);

        Assert.True(ok);
        Assert.NotNull(entry);
        Assert.Equal(LogLevel.Error, entry!.Level);
        Assert.Equal("Billing.Worker", entry.LoggerName);
        Assert.Equal("Charge failed", entry.Message);
        Assert.Equal("billing-thread", entry.ThreadName);
        Assert.Equal("System.InvalidOperationException: payment already captured", entry.Exception);
        Assert.NotEqual(default, entry.Timestamp);
    }
}
