using WinGuiMcp.Common;
using WinGuiMcp.Tools;

namespace WinGuiMcp.Tests;

[Collection("Phase1Gate")]
public class Phase1SampleGateTests
{
    [Fact]
    public void WinFormsGate_LaunchClickAndFill()
    {
        var exe = SamplePathHelper.RequireSampleExe(
            "WinGuiMcp.WinFormsSample",
            "WinGuiMcp.WinFormsSample.exe");

        using var tools = TestServiceFactoryExtensions.CreateDisposable();
        using var launchedApp = new LaunchedAppScope();
        var launch = tools.Service.LaunchApp(exe, null, null, 15000);
        launchedApp.Track(launch);
        var handle = GateTestHelpers.ReadHandle(launch);

        tools.Service.FocusWindow(handle);
        tools.Service.Snapshot(handle, "window", "control", includeOffscreen: false);

        var buttonRef = GateTestHelpers.ReadRef(tools.Service.FindElement(handle, "name", "Click Me", nth: 0, match: "exact"));
        tools.Service.Click(buttonRef, null, null, "left", 1, includeSnapshot: false);

        var inputRef = GateTestHelpers.ReadRef(tools.Service.FindElement(handle, "name", "InputTextBox", nth: 0, match: "exact"));
        tools.Service.SetValue(inputRef, "Hello WinGuiMcp", includeSnapshot: false);
        tools.Service.Click(buttonRef, null, null, "left", 1, includeSnapshot: false);

        var snapshot = tools.Service.Snapshot(handle, "window", "control", includeOffscreen: false);
        var treeJson = ToolResponse.Serialize(snapshot.Tree);
        Assert.Contains("Output: Hello WinGuiMcp", treeJson, StringComparison.Ordinal);
    }

    [Fact]
    public void WpfGate_MenuDialogAndVerify()
    {
        var exe = SamplePathHelper.RequireSampleExe(
            "WinGuiMcp.WpfSample",
            "WinGuiMcp.WpfSample.exe");

        using var tools = TestServiceFactoryExtensions.CreateDisposable();
        using var launchedApp = new LaunchedAppScope();
        var launch = tools.Service.LaunchApp(exe, null, null, 15000);
        launchedApp.Track(launch);
        var handle = GateTestHelpers.ReadHandle(launch);

        tools.Service.FocusWindow(handle);
        tools.Service.Snapshot(handle, "window", "control", includeOffscreen: true);

        tools.Service.Hotkey("alt+f", handle, includeSnapshot: false);
        Thread.Sleep(250);
        tools.Service.Hotkey("o", handle, includeSnapshot: false);
        Thread.Sleep(250);

        var dialogHandle = GateTestHelpers.WaitForWindow(tools.Service, "Sample Dialog", TimeSpan.FromSeconds(8));
        launchedApp.TrackHandle(dialogHandle);
        tools.Service.FocusWindow(dialogHandle);
        tools.Service.Snapshot(dialogHandle, "window", "control", includeOffscreen: false);

        var inputRef = GateTestHelpers.ReadRef(tools.Service.FindElement(dialogHandle, "automationId", "dialogInput", nth: 0, match: "exact"));
        tools.Service.SetValue(inputRef, "Gate OK", includeSnapshot: false);

        var okRef = GateTestHelpers.ReadRef(tools.Service.FindElement(dialogHandle, "automationId", "dialogOk", nth: 0, match: "exact"));
        tools.Service.Click(okRef, null, null, "left", 1, includeSnapshot: false);

        tools.Service.FocusWindow(handle);
        var snapshot = tools.Service.Snapshot(handle, "window", "control", includeOffscreen: false);
        var treeJson = ToolResponse.Serialize(snapshot.Tree);
        Assert.Contains("Dialog result: Gate OK", treeJson, StringComparison.Ordinal);
    }
}
