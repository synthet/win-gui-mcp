using WinGuiMcp.Platform;
using WinGuiMcp.Session;

namespace WinGuiMcp.Tests;

public class CorrectnessRegressionTests
{
    [Fact]
    public void FileReadiness_RequiresFreshTimestampAndQuietInterval()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wingui-fresh-{Guid.NewGuid():N}.bin");
        try
        {
            File.WriteAllBytes(path, [1, 2, 3]);
            File.SetLastWriteTimeUtc(path, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var time = new ManualTime();
            var readiness = new FileReadiness(time);
            Assert.False(readiness.Observe(path, 200, 1, DateTimeOffset.UtcNow.AddMinutes(-1)));
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
            Assert.False(readiness.Observe(path, 200, 1, DateTimeOffset.UtcNow.AddMinutes(-1)));
            time.Advance(201);
            Assert.True(readiness.Observe(path, 200, 1, DateTimeOffset.UtcNow.AddMinutes(-1)));
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(1));
            Assert.False(readiness.Observe(path, 200, 1, DateTimeOffset.UtcNow.AddMinutes(-1)));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task WaitForAsync_CancellationIsObservedWithoutDesktopAccess()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = TestServiceFactory.Create();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.WaitForAsync(0, "file-stable", "missing-file", 5000, cancellationToken: cancellation.Token));
    }

    private sealed class ManualTime : TimeProvider
    {
        private long _ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => _ticks;
        public void Advance(int milliseconds) => _ticks += TimeSpan.FromMilliseconds(milliseconds).Ticks;
    }

    [Fact]
    public void RefAllocation_DoesNotAliasAfterSnapshotRefresh()
    {
        var session = new SnapshotSession(new SystemClock());
        var window = session.RegisterWindow(42);
        session.SetSnapshotId(window, Guid.NewGuid());
        var first = session.AllocateElementRef(window);
        session.SetSnapshotId(window, Guid.NewGuid());
        var second = session.AllocateElementRef(window);
        Assert.NotEqual(first, second);
        Assert.True(WinGuiMcp.Common.RefFormat.IsValid(first));
        Assert.True(WinGuiMcp.Common.RefFormat.IsValid(second));
    }

    [Fact]
    public void PatternValues_SurviveFlatPropertyEnvelope()
    {
        var node = new WinGuiMcp.Perception.UemNode
        {
            Ref = "w1e1", Source = "uia", Role = "Slider", Value = "0.75",
            PatternValues = new() { ["rangeValue"] = new { value = 0.75, minimum = -5.0, maximum = 5.0 } }
        };
        var json = WinGuiMcp.Common.ToolResponse.Serialize(WinGuiMcp.Perception.GetPropertiesEnvelope.From(node.CloneWithoutChildren()));
        using var document = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal(0.75, document.RootElement.GetProperty("patternValues").GetProperty("rangeValue").GetProperty("value").GetDouble());
    }

    [Fact]
    public void Clear_DoesNotRecycleWindowIdentity()
    {
        var session = new SnapshotSession(new SystemClock());
        var before = session.RegisterWindow(42);
        session.Clear();
        var after = session.RegisterWindow(42);
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void FileStable_RejectsEmptyFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wingui-empty-{Guid.NewGuid():N}.bin");
        try
        {
            File.WriteAllBytes(path, []);
            Assert.False(OsWaitConditions.IsFileStable(path, 50));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void SanitizeDouble_ReplacesInfinityAndNaNWithNull()
    {
        Assert.Null(WinGuiMcp.Perception.UiaSnapshotBuilder.SanitizeDouble(double.PositiveInfinity));
        Assert.Null(WinGuiMcp.Perception.UiaSnapshotBuilder.SanitizeDouble(double.NegativeInfinity));
        Assert.Null(WinGuiMcp.Perception.UiaSnapshotBuilder.SanitizeDouble(double.NaN));
        Assert.Equal(42.5, WinGuiMcp.Perception.UiaSnapshotBuilder.SanitizeDouble(42.5));
    }

    [Fact]
    public void Bbox_ClampsInfiniteAndExtremeCoordinates()
    {
        var infBbox = Bbox.FromDoubles(double.PositiveInfinity, 0, 100, 100);
        Assert.Equal(new Bbox(0, 0, 0, 0), infBbox);

        var rectBbox = Bbox.FromRectangle(10, 20, 110, 70);
        Assert.Equal(new Bbox(10, 20, 100, 50), rectBbox);
    }

    [Fact]
    public void WindowCapture_IsBlankBitmap_DetectsAllBlackVsNonBlank()
    {
        using var black = new System.Drawing.Bitmap(64, 64);
        using (var g = System.Drawing.Graphics.FromImage(black))
        {
            g.Clear(System.Drawing.Color.Black);
        }
        Assert.True(WinGuiMcp.Capture.WindowCapture.IsBlankBitmap(black));

        using var painted = new System.Drawing.Bitmap(64, 64);
        using (var g = System.Drawing.Graphics.FromImage(painted))
        {
            g.Clear(System.Drawing.Color.White);
        }
        Assert.False(WinGuiMcp.Capture.WindowCapture.IsBlankBitmap(painted));
    }

    [Fact]
    public async Task ToolCatalogRegistry_SearchDescribeAndDryRunExecute_ResolveUiaPatternAndPropertyAliases()
    {
        var searchJson = WinGuiMcp.Common.ToolResponse.Serialize(
            WinGuiMcp.Tools.ToolCatalogRegistry.SearchTools("RangeValuePattern.SetValue", includeSchemas: true));
        using (var doc = System.Text.Json.JsonDocument.Parse(searchJson))
        {
            var tools = doc.RootElement.GetProperty("tools");
            Assert.True(tools.GetArrayLength() > 0);
            Assert.Equal("set_value", tools[0].GetProperty("name").GetString());
        }

        var (resolvedTool, normalizedArgs) = WinGuiMcp.Tools.ToolCatalogRegistry.ResolveToolAndArguments(
            "find.AutomationId",
            System.Text.Json.Nodes.JsonNode.Parse("""{"hwnd":100,"value":"HeaderList"}""")!.AsObject());
        Assert.Equal("find_element", resolvedTool);
        Assert.Equal(100, normalizedArgs["handle"]!.GetValue<int>());
        Assert.Equal("automationId", normalizedArgs["by"]!.GetValue<string>());

        var service = TestServiceFactory.Create();
        var dryRunObj = await WinGuiMcp.Tools.ToolCatalogRegistry.ExecuteToolAsync(
            service,
            "SelectionItemPattern.Select",
            System.Text.Json.Nodes.JsonNode.Parse("""{"elementRef":"w1e5"}""")!.AsObject(),
            dryRun: true);
        var dryRunJson = WinGuiMcp.Common.ToolResponse.Serialize(dryRunObj);
        using (var doc = System.Text.Json.JsonDocument.Parse(dryRunJson))
        {
            Assert.True(doc.RootElement.GetProperty("dryRun").GetBoolean());
            Assert.Equal("select", doc.RootElement.GetProperty("resolvedTool").GetString());
            Assert.Equal("w1e5", doc.RootElement.GetProperty("normalizedArguments").GetProperty("ref").GetString());
        }
    }
}
