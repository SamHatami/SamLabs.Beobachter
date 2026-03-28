using SamLabs.Beobachter.Core.Enums;

namespace SamLabs.Beobachter.Core.Models;

public readonly record struct NormalizedLogLevel(
    LogLevel Level,
    string? RawLevelName,
    int? RawLevelValue,
    bool UsedFallback);
