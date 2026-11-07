using System.Text.Json.Serialization;

namespace WinGuiMcp.Perception;

public sealed class UemNode
{
    [JsonPropertyName("patternValues")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object?>? PatternValues { get; init; }

    [JsonPropertyName("ref")]
    public required string Ref { get; init; }

    [JsonPropertyName("source")]
    public required string Source { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    [JsonPropertyName("automationId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AutomationId { get; init; }

    [JsonPropertyName("className")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClassName { get; init; }

    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Value { get; init; }

    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; init; }

    [JsonPropertyName("states")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? States { get; init; }

    [JsonPropertyName("patterns")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Patterns { get; init; }

    [JsonPropertyName("children")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<UemNode>? Children { get; init; }

    public UemNode CloneWithoutChildren() => new()
    {
        PatternValues = PatternValues is null ? null : new(PatternValues),
        Ref = Ref,
        Source = Source,
        Role = Role,
        Name = Name,
        AutomationId = AutomationId,
        ClassName = ClassName,
        Value = Value,
        Enabled = Enabled,
        States = States is null ? null : new List<string>(States),
        Patterns = Patterns is null ? null : new List<string>(Patterns)
    };
}

public sealed class GetPropertiesEnvelope
{
    [JsonPropertyName("patternValues")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object?>? PatternValues { get; init; }

    [JsonPropertyName("ok")]
    public bool Ok { get; init; } = true;

    [JsonPropertyName("ref")]
    public required string Ref { get; init; }

    [JsonPropertyName("source")]
    public required string Source { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    [JsonPropertyName("automationId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AutomationId { get; init; }

    [JsonPropertyName("className")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClassName { get; init; }

    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Value { get; init; }

    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; init; }

    [JsonPropertyName("states")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? States { get; init; }

    [JsonPropertyName("patterns")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Patterns { get; init; }

    public static GetPropertiesEnvelope From(UemNode node) => new()
    {
        PatternValues = node.PatternValues is null ? null : new(node.PatternValues),
        Ref = node.Ref,
        Source = node.Source,
        Role = node.Role,
        Name = node.Name,
        AutomationId = node.AutomationId,
        ClassName = node.ClassName,
        Value = node.Value,
        Enabled = node.Enabled,
        States = node.States is null ? null : new List<string>(node.States),
        Patterns = node.Patterns is null ? null : new List<string>(node.Patterns)
    };
}

public sealed class SnapshotEnvelope
{
    [JsonPropertyName("ok")]
    public bool Ok { get; init; } = true;

    [JsonPropertyName("snapshotId")]
    public required string SnapshotId { get; init; }

    [JsonPropertyName("handle")]
    public required int Handle { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("processName")]
    public required string ProcessName { get; init; }

    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }

    [JsonPropertyName("tree")]
    public required UemNode Tree { get; init; }
}

public sealed class WindowInfo
{
    public required int Handle { get; init; }
    public required string Title { get; init; }
    public required string ProcessName { get; init; }
    public required int Pid { get; init; }
    public required bool IsForeground { get; init; }
    public required bool IsMinimized { get; init; }
}
