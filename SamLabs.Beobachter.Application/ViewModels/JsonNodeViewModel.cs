using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace SamLabs.Beobachter.Application.ViewModels;

public sealed class JsonNodeViewModel
{
    public JsonNodeViewModel(string displayKey, string preview, JsonValueKind valueKind, IReadOnlyList<JsonNodeViewModel>? children)
    {
        DisplayKey = displayKey;
        Preview = preview;
        ValueKind = valueKind;
        Children = children;
    }

    public string DisplayKey { get; }

    public string Preview { get; }

    public JsonValueKind ValueKind { get; }

    public IReadOnlyList<JsonNodeViewModel>? Children { get; }

    public bool HasChildren => Children is { Count: > 0 };

    public static IReadOnlyList<JsonNodeViewModel> Build(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return [];
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);
            return [BuildNode(document.RootElement, displayKey: string.Empty)];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static JsonNodeViewModel BuildNode(JsonElement element, string displayKey)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                List<JsonNodeViewModel> children = element
                    .EnumerateObject()
                    .Select(static p => BuildNode(p.Value, p.Name))
                    .ToList();
                string preview = children.Count == 1 ? "{ 1 field }" : $"{{ {children.Count} fields }}";
                return new JsonNodeViewModel(displayKey, preview, JsonValueKind.Object, children);
            }
            case JsonValueKind.Array:
            {
                List<JsonNodeViewModel> children = element
                    .EnumerateArray()
                    .Select((item, index) => BuildNode(item, $"[{index.ToString(CultureInfo.InvariantCulture)}]"))
                    .ToList();
                string preview = children.Count == 1 ? "[ 1 item ]" : $"[ {children.Count} items ]";
                return new JsonNodeViewModel(displayKey, preview, JsonValueKind.Array, children);
            }
            case JsonValueKind.String:
                return new JsonNodeViewModel(displayKey, $"\"{element.GetString()}\"", JsonValueKind.String, null);
            case JsonValueKind.Number:
                return new JsonNodeViewModel(displayKey, element.GetRawText(), JsonValueKind.Number, null);
            case JsonValueKind.True:
                return new JsonNodeViewModel(displayKey, "true", JsonValueKind.True, null);
            case JsonValueKind.False:
                return new JsonNodeViewModel(displayKey, "false", JsonValueKind.False, null);
            case JsonValueKind.Null:
                return new JsonNodeViewModel(displayKey, "null", JsonValueKind.Null, null);
            default:
                return new JsonNodeViewModel(displayKey, element.GetRawText(), element.ValueKind, null);
        }
    }
}
