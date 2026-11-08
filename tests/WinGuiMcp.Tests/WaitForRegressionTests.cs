using System.Text.Json;
using ModelContextProtocol.Protocol;
using WinGuiMcp.Platform;
using WinGuiMcp.Tools;

namespace WinGuiMcp.Tests;

/// <summary>
/// Regression: "window-active" and "window-closed" were `when`-guarded switch arms, so a window that was not yet
/// active (or not yet closed) fell through to "Unknown condition" instead of being polled until timeout.
/// Found by the first live scenario run (notepad-roundtrip, 2026-11-07).
/// </summary>
public class WaitForRegressionTests
{
    [Theory]
    [InlineData("window-closed")]
    [InlineData("window-active")]
    public async Task WindowConditionNotYetTrue_WaitsUntilTimeout_NotUnknownCondition(string condition)
    {
        // The shell window stays open and is never the foreground window while tests run.
        var shell = (int)NativeMethods.GetShellWindow();
        if (shell == 0)
            return; // no interactive shell (service session)

        var started = DateTime.UtcNow;
        var result = Assert.IsType<CallToolResult>(await new Phase1Tools(TestServiceFactory.Create()).WaitFor(shell, condition, timeoutMs: 400, settleMs: 0));

        Assert.True(result.IsError);
        var payload = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(result.StructuredContent));
        Assert.Equal("Timeout", payload.GetProperty("code").GetString());
        Assert.True(DateTime.UtcNow - started >= TimeSpan.FromMilliseconds(350), "should have polled until the timeout");
    }
}
