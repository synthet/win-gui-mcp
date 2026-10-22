using System.Text.Json;
using WinGuiMcp.Common;
using WinGuiMcp.Platform;
using WinGuiMcp.Session;

namespace WinGuiMcp.Tests;

public class ActuationGuardTests
{
    [Theory]
    [InlineData(InputBoundary.MediumRid, InputBoundary.HighRid, true)]
    [InlineData(InputBoundary.HighRid, InputBoundary.MediumRid, false)]
    [InlineData(InputBoundary.MediumRid, InputBoundary.MediumRid, false)]
    [InlineData(-1, InputBoundary.HighRid, false)]
    public void IntegrityComparison_BlocksOnlyHigherTargets(int serverRid, int targetRid, bool blocked)
    {
        Assert.Equal(blocked, InputBoundary.BlocksLowerIntegrity(serverRid, targetRid));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(-1, false)]
    public void SessionFlags_LockIsZeroOnWindows8Plus(int flags, bool locked)
    {
        Assert.Equal(locked, InputBoundary.IsLockedSession(flags));
    }

    [Fact]
    public void DesktopProbe_SecureWhenLockedClosedOrWinlogon()
    {
        Assert.True(InputBoundary.IsSecureOrLocked(new InputBoundary.DesktopProbe(true, "Default", SessionLocked: true)));
        Assert.True(InputBoundary.IsSecureOrLocked(new InputBoundary.DesktopProbe(false, null, SessionLocked: false)));
        Assert.True(InputBoundary.IsSecureOrLocked(new InputBoundary.DesktopProbe(true, "Winlogon", SessionLocked: false)));
        Assert.False(InputBoundary.IsSecureOrLocked(new InputBoundary.DesktopProbe(true, "Default", SessionLocked: false)));
        Assert.False(InputBoundary.IsSecureOrLocked(new InputBoundary.DesktopProbe(true, null, SessionLocked: false)));
    }

    [Fact]
    public void UipiBlocked_MatchesErrorCatalogShape()
    {
        var error = InputBoundary.CreateUipiBlocked("Taskmgr.exe", 1234, serverElevated: false);
        using var doc = JsonDocument.Parse(ToolResponse.Error(error.Error));

        Assert.Equal(ErrorCodes.UIPIBlocked, doc.RootElement.GetProperty("code").GetString());
        Assert.Contains("Run WinGuiMcp as Administrator", doc.RootElement.GetProperty("message").GetString(), StringComparison.Ordinal);
        Assert.Equal("Taskmgr.exe", doc.RootElement.GetProperty("details").GetProperty("targetProcess").GetString());
        Assert.Equal(1234, doc.RootElement.GetProperty("details").GetProperty("targetPid").GetInt32());
        Assert.False(doc.RootElement.GetProperty("details").GetProperty("serverElevated").GetBoolean());
    }

    [Fact]
    public void CurrentSession_IsAnInteractiveDesktop()
    {
        InputBoundary.ThrowIfInteractiveDesktop();
    }

    [Fact]
    public void FrameReconciler_AllowsCenterInsideAndMeasuresOverflow()
    {
        var bbox = new Bbox(10, 20, 40, 30);
        var overflow = FrameReconciler.Measure(bbox, frameLeft: 0, frameTop: 0, frameRight: 100, frameBottom: 80);

        Assert.Equal(0, overflow.Max);
        Assert.True(FrameReconciler.CenterInside(bbox, 0, 0, 100, 80));
    }

    [Fact]
    public void FrameReconciler_ReportsEdgeDisagreementAboveTwoPixels()
    {
        var bbox = new Bbox(-5, 0, 20, 10);
        var overflow = FrameReconciler.Measure(bbox, 0, 0, 100, 50);

        Assert.Equal(5, overflow.Left);
        Assert.Equal(5, overflow.Max);
        Assert.True(FrameReconciler.CenterInside(bbox, 0, 0, 100, 50));
    }

    [Fact]
    public void FrameReconciler_RejectsCenterOutsideFrameIncludingNegativeMonitors()
    {
        var outside = new Bbox(90, 0, 30, 10);
        Assert.False(FrameReconciler.CenterInside(outside, 0, 0, 100, 50));

        var secondary = new Bbox(-1900, 100, 40, 20);
        Assert.True(FrameReconciler.CenterInside(secondary, -1920, 0, 0, 1080));

        var pastRightEdge = new Bbox(90, 0, 20, 10);
        Assert.False(FrameReconciler.CenterInside(pastRightEdge, 0, 0, 100, 50));
    }
}
