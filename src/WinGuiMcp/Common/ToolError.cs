using System.Text.Json.Serialization;

namespace WinGuiMcp.Common;

public sealed class ToolError
{
    [JsonPropertyName("ok")]
    public bool Ok { get; init; }

    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object?>? Details { get; init; }

    public static ToolError Create(string code, string message, Dictionary<string, object?>? details = null) =>
        new()
        {
            Ok = false,
            Code = code,
            Message = message,
            Details = details
        };
}
