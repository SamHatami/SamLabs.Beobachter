using System.Collections.Generic;
using SamLabs.Beobachter.Core.Models;

namespace SamLabs.Beobachter.Application.Services;

public interface ISampleLogEntryGenerator
{
    IReadOnlyList<LogEntry> CreateBatch(int startSequenceNumber, int count);
}
