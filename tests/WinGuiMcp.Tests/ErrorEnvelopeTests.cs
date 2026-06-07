using System.Text.Json;
using WinGuiMcp.Common;

namespace WinGuiMcp.Tests;

public class ErrorEnvelopeTests
{
    [Fact]
    public void Error_SerializesWithRequiredFields()
    {
        var error = ToolError.Create(ErrorCodes.InvalidArgument, "bad input", new Dictionary<string, object?> { ["field"] = "handle" });
        var json = ToolResponse.Error(error);
        using var doc = JsonDocument.Parse(json);

        Assert.False(doc.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("InvalidArgument", doc.RootElement.GetProperty("code").GetString());
        Assert.Equal("bad input", doc.RootElement.GetProperty("message").GetString());
        Assert.Equal("handle", doc.RootElement.GetProperty("details").GetProperty("field").GetString());
    }
}
