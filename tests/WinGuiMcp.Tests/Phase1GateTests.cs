using System.Text.Json;
using WinGuiMcp.Common;
using WinGuiMcp.Perception;
using WinGuiMcp.Tools;

namespace WinGuiMcp.Tests;

[CollectionDefinition("Phase1Gate", DisableParallelization = true)]
public sealed class Phase1GateCollection : ICollectionFixture<Phase1GateCollection>;

[Collection("Phase1Gate")]
public class Phase1GateTests
{
    [Fact]
    public void CalculatorGate_LaunchSnapshotAndListWindows()
    {
        using var tools = TestServiceFactoryExtensions.CreateDisposable();
        using var launchedApp = new LaunchedAppScope();

        var launch = tools.Service.LaunchApp("calc.exe", null, null, 15000);
        launchedApp.Track(launch);
        var launchJson = JsonSerializer.Serialize(launch);
        using var launchDoc = JsonDocument.Parse(launchJson);
        Assert.True(launchDoc.RootElement.GetProperty("ok").GetBoolean());

        var handle = launchDoc.RootElement.GetProperty("handle").GetInt32();
        Assert.NotEqual(0, handle);

        var snapshot = tools.Service.Snapshot(handle, "window", "control", false);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.SnapshotId));
        Assert.NotNull(snapshot.Tree);
        Assert.Equal("uia", snapshot.Tree.Source);

        var windows = tools.Service.ListWindows(null, includeMinimized: true);
        var windowsJson = JsonSerializer.Serialize(windows);
        using var windowsDoc = JsonDocument.Parse(windowsJson);
        Assert.True(windowsDoc.RootElement.GetProperty("ok").GetBoolean());

        _ = GateTestHelpers.TryClickButton(tools.Service, handle, "2", "Two", "Два");
    }

    [Fact]
    public void CalculatorGate_ArithmeticFlow2Plus3Equals5()
    {
        using var tools = TestServiceFactoryExtensions.CreateDisposable();
        using var launchedApp = new LaunchedAppScope();

        var launch = tools.Service.LaunchApp("calc.exe", null, null, 15000);
        launchedApp.Track(launch);
        var handle = GateTestHelpers.ReadHandle(launch);

        GateTestHelpers.TryFocus(tools.Service, handle);
        tools.Service.Snapshot(handle, "window", "control", includeOffscreen: false);

        if (!GateTestHelpers.TryClickButton(tools.Service, handle, "2", "Two", "Два"))
            return;

        Assert.True(GateTestHelpers.TryClickButton(tools.Service, handle, "+", "Plus", "Плюс"));
        Assert.True(GateTestHelpers.TryClickButton(tools.Service, handle, "3", "Three", "Три"));
        Assert.True(GateTestHelpers.TryClickButton(tools.Service, handle, "=", "Equals", "Равно"));

        Assert.True(
            GateTestHelpers.TreeContainsText(tools.Service, handle, "5") ||
            GateTestHelpers.TreeContainsText(tools.Service, handle, "Display is 5"),
            "Calculator result 5 not found in snapshot tree.");
    }

    [Fact]
    public void CalculatorGate_SnapshotTokenBudget()
    {
        using var tools = TestServiceFactoryExtensions.CreateDisposable();
        using var launchedApp = new LaunchedAppScope();

        var launch = tools.Service.LaunchApp("calc.exe", null, null, 15000);
        launchedApp.Track(launch);
        var handle = GateTestHelpers.ReadHandle(launch);

        GateTestHelpers.TryFocus(tools.Service, handle);
        var snapshot = tools.Service.Snapshot(handle, "window", "control", includeOffscreen: false);
        var treeJson = ToolResponse.Serialize(snapshot.Tree);

        // Informational Phase 1 metric from docs/phases.md (< 8k tokens ~ 32k chars heuristic).
        Assert.True(treeJson.Length < 64_000, $"Calculator tree JSON length {treeJson.Length} exceeds budget heuristic.");
    }
}

public sealed class DisposableToolService : IDisposable
{
    public ToolService Service { get; }
    private readonly UiaSnapshotBuilder _snapshotBuilder;

    public DisposableToolService(ToolService service, UiaSnapshotBuilder snapshotBuilder)
    {
        Service = service;
        _snapshotBuilder = snapshotBuilder;
    }

    public void Dispose() => _snapshotBuilder.Dispose();
}

public static class TestServiceFactoryExtensions
{
    public static DisposableToolService CreateDisposable()
    {
        var snapshotBuilder = new UiaSnapshotBuilder();
        var windowEnumerator = new Platform.WindowEnumerator();
        var service = new ToolService(
            new Session.SnapshotSession(new Platform.SystemClock()),
            windowEnumerator,
            snapshotBuilder,
            new Action.PatternExecutor(new Action.InputSimulator(), windowEnumerator),
            new Action.InputSimulator(),
            new Capture.WindowCapture());

        return new DisposableToolService(service, snapshotBuilder);
    }
}
