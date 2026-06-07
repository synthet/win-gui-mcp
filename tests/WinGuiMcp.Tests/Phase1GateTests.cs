using System.Text.Json;
using WinGuiMcp.Common;
using WinGuiMcp.Perception;
using WinGuiMcp.Tools;

namespace WinGuiMcp.Tests;

[CollectionDefinition("Phase1Gate", DisableParallelization = true)]
public sealed class Phase1GateCollection : ICollectionFixture<Phase1GateCollection>;

public class Phase1GateTests
{
    [Fact]
    public void CalculatorGate_LaunchSnapshotAndListWindows()
    {
        using var tools = TestServiceFactoryExtensions.CreateDisposable();

        var launch = tools.Service.LaunchApp("calc.exe", null, null, 15000);
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

        TryClickCalculatorDigit(tools.Service, handle, snapshot);
    }

    private static void TryClickCalculatorDigit(ToolService tools, int handle, SnapshotEnvelope snapshot)
    {
        string? digitRef = null;
        foreach (var name in new[] { "2", "Two", "Два" })
        {
            try
            {
                var found = tools.FindElement(handle, "name", name, nth: 0, match: "exact");
                var foundJson = ToolResponse.Serialize(found);
                using var doc = JsonDocument.Parse(foundJson);
                if (!doc.RootElement.GetProperty("ok").GetBoolean())
                    continue;
                digitRef = doc.RootElement.GetProperty("ref").GetString();
                break;
            }
            catch (ToolException)
            {
                // Try next localized name.
            }
        }

        if (digitRef is null)
            return;

        var click = tools.Click(digitRef, null, null, "left", 1, includeSnapshot: false);
        var clickJson = JsonSerializer.Serialize(click);
        Assert.True(
            clickJson.Contains("InvokePattern", StringComparison.OrdinalIgnoreCase) ||
            clickJson.Contains("SendInput", StringComparison.OrdinalIgnoreCase),
            clickJson);
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
