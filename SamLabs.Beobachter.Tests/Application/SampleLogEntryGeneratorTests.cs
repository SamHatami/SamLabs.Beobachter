using System.Collections.Generic;
using SamLabs.Beobachter.Application.Services;
using SamLabs.Beobachter.Core.Models;
using Xunit;

namespace SamLabs.Beobachter.Tests.Application;

public sealed class SampleLogEntryGeneratorTests
{
    [Fact]
    public void CreateBatch_ReturnsRequestedCountWithIncrementingSequence()
    {
        ISampleLogEntryGenerator generator = new SampleLogEntryGenerator();

        IReadOnlyList<LogEntry> entries = generator.CreateBatch(41, 4);

        Assert.Equal(4, entries.Count);
        Assert.Equal(41, entries[0].SequenceNumber);
        Assert.Equal(42, entries[1].SequenceNumber);
        Assert.Equal(43, entries[2].SequenceNumber);
        Assert.Equal(44, entries[3].SequenceNumber);
        Assert.All(entries, static entry => Assert.Equal("sample", entry.ReceiverId));
    }

    [Fact]
    public void CreateBatch_WithNonPositiveCount_ReturnsEmpty()
    {
        ISampleLogEntryGenerator generator = new SampleLogEntryGenerator();

        IReadOnlyList<LogEntry> entries = generator.CreateBatch(1, 0);

        Assert.Empty(entries);
    }
}
