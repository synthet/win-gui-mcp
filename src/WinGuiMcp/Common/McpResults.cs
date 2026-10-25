using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace WinGuiMcp.Common;

internal static class McpResults
{
    public static CallToolResult Create(object payload)
    {
        var json = ToolResponse.Serialize(payload);
        return new CallToolResult
        {
            IsError = payload is ToolError,
            Content = [new TextContentBlock { Text = json }],
            StructuredContent = JsonSerializer.Deserialize<JsonElement>(json)
        };
    }
}
