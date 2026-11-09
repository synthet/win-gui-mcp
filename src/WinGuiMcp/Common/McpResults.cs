using System.Text.Json;
using ModelContextProtocol.Protocol;
using WinGuiMcp.Capture;

namespace WinGuiMcp.Common;

internal static class McpResults
{
    public static CallToolResult Create(object payload)
    {
        if (payload is ScreenshotEnvelope screenshot)
        {
            // Pixels travel as an image block; the JSON carries only metadata so clients
            // render the image instead of receiving a huge base64 text payload.
            var metadata = ToolResponse.Serialize(screenshot.Metadata());
            return new CallToolResult
            {
                IsError = false,
                Content =
                [
                    ImageContentBlock.FromBytes(screenshot.ImageBytes, screenshot.MimeType),
                    new TextContentBlock { Text = metadata }
                ],
                StructuredContent = JsonSerializer.Deserialize<JsonElement>(metadata)
            };
        }

        var json = ToolResponse.Serialize(payload);
        return new CallToolResult
        {
            IsError = payload is ToolError,
            Content = [new TextContentBlock { Text = json }],
            StructuredContent = JsonSerializer.Deserialize<JsonElement>(json)
        };
    }
}
