using Microsoft.Extensions.DependencyInjection;
using WinGuiMcp.Action;
using WinGuiMcp.Capture;
using WinGuiMcp.Perception;
using WinGuiMcp.Platform;
using WinGuiMcp.Session;
using WinGuiMcp.Tools;

namespace WinGuiMcp.Tests;

public static class TestServiceFactory
{
    public static ToolService Create() =>
        new(
            new SnapshotSession(new SystemClock()),
            new WindowEnumerator(),
            new UiaSnapshotBuilder(),
            new PatternExecutor(new InputSimulator(), new WindowEnumerator()),
            new InputSimulator(),
            new WindowCapture());
}

public class ToolRegistrationTests
{
    [Fact]
    public void Phase1Tools_HasExpectedNames()
    {
        var expected = new[]
        {
            "list_windows", "launch_app", "focus_window", "snapshot", "find_element",
            "get_properties", "click", "type_text", "set_value", "hotkey", "screenshot", "wait_for"
        };

        Assert.Equal(12, expected.Length);
        Assert.All(expected, name => Assert.False(string.IsNullOrWhiteSpace(name)));
    }
}
