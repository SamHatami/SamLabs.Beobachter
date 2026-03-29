using System.Text.Json.Serialization;
using SamLabs.Beobachter.Core.Settings;

namespace SamLabs.Beobachter.Infrastructure.Settings;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(LogLevelColorOverrides))]
[JsonSerializable(typeof(LogLevelColorOverride))]
[JsonSerializable(typeof(ReceiverDefinitions))]
[JsonSerializable(typeof(WorkspaceSettings))]
[JsonSerializable(typeof(UiLayoutSettings))]
internal partial class SettingsJsonContext : JsonSerializerContext;
