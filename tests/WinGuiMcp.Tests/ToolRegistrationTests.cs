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

// The tool-name check that lived here only asserted a local array; ToolSurfaceSnapshotTests
// now locks the real [McpServerTool] surface and catalog schemas against golden/tool-surface.json.
