using WinGuiMcp.Common;
using WinGuiMcp.Perception;
using WinGuiMcp.Platform;
using WinGuiMcp.Session;

namespace WinGuiMcp.Tests;

public class RefTableTests
{
    [Theory]
    [InlineData("w1e1", true)]
    [InlineData("w12e42", true)]
    [InlineData("w1", false)]
    [InlineData("e1", false)]
    public void RefFormat_ValidatesPattern(string value, bool expected) =>
        Assert.Equal(expected, RefFormat.IsValid(value));

    [Fact]
    public void Resolve_ReturnsStaleForOldSnapshot()
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
            Patterns = ["Invoke"],
            SnapshotId = oldSnapshot,
            Node = node
        });

        var ex = Assert.Throws<ToolException>(() => table.Resolve("w1e1", Guid.NewGuid()));
        Assert.Equal(ErrorCodes.ElementNotFound, ex.Error.Code);
        Assert.True(ex.Error.Details?["stale"] as bool?);
    }

    [Fact]
    public void FindElement_ReturnsNthMatch()
    {
        var table = new RefTable();
        var snapshotId = Guid.NewGuid();

        for (var i = 1; i <= 2; i++)
        {
            var node = new UemNode { Ref = $"w1e{i}", Source = "uia", Role = "Button", Name = "Save" };
            table.Add(new RefEntry
            {
                Ref = node.Ref,
                Hwnd = 1,
                Element = null!,
                Bbox = new Bbox(0, 0, 10, 10),
                Source = "uia",
                Patterns = [],
                SnapshotId = snapshotId,
                Node = node
            });
        }

        var second = table.FindElement(1, snapshotId, "name", "Save", "exact", 1);
        Assert.Equal("w1e2", second.Ref);
    }
}

public class SnapshotSessionTests
{
    [Fact]
    public void IdleTimeout_ClearsSession()
    {
        var clock = new FakeClock(DateTimeOffset.UtcNow);
        var session = new SnapshotSession(clock, TimeSpan.FromMinutes(1));
        session.RegisterWindow(100);
        session.SetSnapshotId(1, Guid.NewGuid());

        clock.Advance(TimeSpan.FromMinutes(2));
        session.EnsureNotExpired();

        Assert.Null(session.GetCurrentSnapshotId(1));
        Assert.Null(session.TryGetWindowIndex(100));
    }

    private sealed class FakeClock(DateTimeOffset start) : IClock
    {
        private DateTimeOffset _now = start;
        public DateTimeOffset UtcNow => _now;
        public void Advance(TimeSpan delta) => _now = _now.Add(delta);
    }
}
