using System.Text.Json;
using WinGuiMcp.Common;
using WinGuiMcp.Perception;
using WinGuiMcp.Session;

namespace WinGuiMcp.Tests;

public class GetPropertiesTests
{
    [Fact]
    public void GetProperties_ReturnsFlatUemEnvelope()
    {
        var session = new SnapshotSession(new Platform.SystemClock());
        var snapshotId = Guid.NewGuid();
        var windowIndex = session.RegisterWindow(42);
        session.SetSnapshotId(windowIndex, snapshotId);

        var node = new UemNode
        {
            Ref = "w1e3",
            Source = "uia",
            Role = "Button",
            Name = "Save",
            Patterns = ["Invoke"]
        };

        session.RefTable.Add(new RefEntry
        {
            Ref = node.Ref,
            Hwnd = 42,
            Element = null!,
            Bbox = new Bbox(0, 0, 10, 10),
            Source = "uia",
            Patterns = ["Invoke"],
            SnapshotId = snapshotId,
            Node = node
        });

        var envelope = GetPropertiesEnvelope.From(session.RefTable.GetProperties("w1e3", snapshotId));
        var json = ToolResponse.Serialize(envelope);

        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("w1e3", doc.RootElement.GetProperty("ref").GetString());
        Assert.Equal("Button", doc.RootElement.GetProperty("role").GetString());
        Assert.Equal("Save", doc.RootElement.GetProperty("name").GetString());
        Assert.False(doc.RootElement.TryGetProperty("node", out _));
    }

    [Fact]
    public void GetProperties_StaleRef_ReturnsElementNotFoundWithStaleFlag()
    {
        var table = new RefTable();
        var oldSnapshot = Guid.NewGuid();
        var node = new UemNode { Ref = "w1e1", Source = "uia", Role = "Button", Name = "OK" };

        table.Add(new RefEntry
        {
            Ref = "w1e1",
            Hwnd = 123,
            Element = null!,
            Bbox = new Bbox(0, 0, 10, 10),
            Source = "uia",
            Patterns = [],
            SnapshotId = oldSnapshot,
            Node = node
        });

        var ex = Assert.Throws<ToolException>(() => table.GetProperties("w1e1", Guid.NewGuid()));
        Assert.Equal(ErrorCodes.ElementNotFound, ex.Error.Code);
        Assert.True(ex.Error.Details?["stale"] as bool?);
    }
}
