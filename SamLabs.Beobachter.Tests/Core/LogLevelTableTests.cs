using SamLabs.Beobachter.Core.Enums;
using SamLabs.Beobachter.Core.Services;
using Xunit;

namespace SamLabs.Beobachter.Tests.Core;

public sealed class LogLevelTableTests
{
    [Theory]
    [InlineData(0, LogLevel.Trace)]
    [InlineData(10000, LogLevel.Trace)]
    [InlineData(10001, LogLevel.Debug)]
    [InlineData(30000, LogLevel.Debug)]
    [InlineData(30001, LogLevel.Info)]
    [InlineData(40001, LogLevel.Warn)]
    [InlineData(60001, LogLevel.Error)]
    [InlineData(70001, LogLevel.Fatal)]
    [InlineData(110000, LogLevel.Fatal)]
    public void FromNumeric_MapsLegacyRanges(int rawValue, LogLevel expected)
    {
        var actual = LogLevelTable.FromNumeric(rawValue, fallback: LogLevel.None);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FromName_UsesCaseInsensitiveLookup()
    {
        Assert.Equal(LogLevel.Warn, LogLevelTable.FromName("warning"));
        Assert.Equal(LogLevel.Debug, LogLevelTable.FromName("DeBuG"));
        Assert.Equal(LogLevel.Info, LogLevelTable.FromName("information"));
    }

    [Fact]
    public void Normalize_PrefersNumericValueOverStringName()
    {
        var normalized = LogLevelTable.Normalize(rawLevelName: "ERROR", rawLevelValue: 10001);
        Assert.Equal(LogLevel.Debug, normalized.Level);
        Assert.False(normalized.UsedFallback);
    }

    [Fact]
    public void Normalize_UsesFallbackForUnknownInputs()
    {
        var normalized = LogLevelTable.Normalize(rawLevelName: "custom", rawLevelValue: null, fallback: LogLevel.Info);
        Assert.Equal(LogLevel.Info, normalized.Level);
        Assert.True(normalized.UsedFallback);
    }
}
