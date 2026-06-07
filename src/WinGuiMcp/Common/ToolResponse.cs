using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinGuiMcp.Common;

public static class ToolResponse
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static string Serialize(object value) =>
        JsonSerializer.Serialize(value, JsonOptions);

    public static string Success(object payload) => Serialize(payload);

    public static string Error(ToolError error) => Serialize(error);

    public static string Internal(string message) =>
        Error(ToolError.Create(ErrorCodes.InternalError, message));
}
