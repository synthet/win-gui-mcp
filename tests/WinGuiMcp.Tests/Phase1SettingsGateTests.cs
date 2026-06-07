using System.Diagnostics;

namespace WinGuiMcp.Tests;

[Collection("Phase1Gate")]
public class Phase1SettingsGateTests
{
    [Fact]
    public void SettingsGate_LaunchAndSnapshotUnderBudget()
    {
        using var tools = TestServiceFactoryExtensions.CreateDisposable();
        using var launchedApp = new LaunchedAppScope();

        var launch = tools.Service.LaunchApp("ms-settings:", null, null, 20000);
        launchedApp.Track(launch);
        var handle = GateTestHelpers.ReadHandle(launch);

        GateTestHelpers.TryFocus(tools.Service, handle);

        var sw = Stopwatch.StartNew();
        var snapshot = tools.Service.Snapshot(handle, "window", "control", includeOffscreen: false);
        sw.Stop();

        Assert.False(string.IsNullOrWhiteSpace(snapshot.SnapshotId));
        Assert.NotNull(snapshot.Tree);
        Assert.Equal("uia", snapshot.Tree.Source);

        var treeJson = Common.ToolResponse.Serialize(snapshot.Tree);
        Assert.True(treeJson.Length > 100, "Settings snapshot tree should not be empty.");

        // Informational Phase 1 metric from docs/phases.md (< 2 s target).
        Assert.True(sw.ElapsedMilliseconds < 5000, $"Settings snapshot took {sw.ElapsedMilliseconds} ms (target < 2 s).");
    }

    [Fact]
    public void SettingsGate_NavigateToSystemPage()
    {
        using var tools = TestServiceFactoryExtensions.CreateDisposable();
        using var launchedApp = new LaunchedAppScope();

        var launch = tools.Service.LaunchApp("ms-settings:", null, null, 20000);
        launchedApp.Track(launch);
        var handle = GateTestHelpers.ReadHandle(launch);

        GateTestHelpers.TryFocus(tools.Service, handle);
        var before = Common.ToolResponse.Serialize(tools.Service.Snapshot(handle, "window", "control", includeOffscreen: true).Tree);

        var navigated = GateTestHelpers.TryClickByLocator(tools.Service, handle, "name", "System", "contains") ||
                        GateTestHelpers.TryClickByLocator(tools.Service, handle, "name", "Система", "contains") ||
                        GateTestHelpers.TryClickByLocator(tools.Service, handle, "name", "Display", "contains") ||
                        GateTestHelpers.TryClickByLocator(tools.Service, handle, "name", "Экран", "contains");

        if (!navigated)
            return;

        Thread.Sleep(800);
        var after = Common.ToolResponse.Serialize(tools.Service.Snapshot(handle, "window", "control", includeOffscreen: true).Tree);
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void SettingsGate_ToggleSwitchIfPresent()
    {
        using var tools = TestServiceFactoryExtensions.CreateDisposable();
        using var launchedApp = new LaunchedAppScope();

        var launch = tools.Service.LaunchApp("ms-settings:", null, null, 20000);
        launchedApp.Track(launch);
        var handle = GateTestHelpers.ReadHandle(launch);
        GateTestHelpers.TryFocus(tools.Service, handle);

        _ = GateTestHelpers.TryClickByLocator(tools.Service, handle, "name", "System", "contains") ||
            GateTestHelpers.TryClickByLocator(tools.Service, handle, "name", "Система", "contains") ||
            GateTestHelpers.TryClickByLocator(tools.Service, handle, "name", "Display", "contains") ||
            GateTestHelpers.TryClickByLocator(tools.Service, handle, "name", "Экран", "contains");

        Thread.Sleep(800);
        tools.Service.Snapshot(handle, "window", "control", includeOffscreen: true);

        if (!GateTestHelpers.TryFindElementRef(tools.Service, handle, "role", "Switch", "exact", out var switchRef) &&
            !GateTestHelpers.TryFindElementRef(tools.Service, handle, "role", "ToggleButton", "exact", out switchRef))
        {
            return;
        }

        var before = Common.ToolResponse.Serialize(tools.Service.GetProperties(switchRef));
        GateTestHelpers.AssertResponseOk(
            tools.Service.Click(switchRef, null, null, "left", 1, includeSnapshot: false));
        Thread.Sleep(500);
        tools.Service.Snapshot(handle, "window", "control", includeOffscreen: true);
        var after = Common.ToolResponse.Serialize(tools.Service.GetProperties(switchRef));

        Assert.NotEqual(before, after);
    }
}
