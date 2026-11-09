using System.Text.Json;
using ModelContextProtocol.Protocol;
using WinGuiMcp.Capture;
using WinGuiMcp.Common;
using WinGuiMcp.Perception;
using WinGuiMcp.Platform;
using WinGuiMcp.Tools;

namespace WinGuiMcp.Tests;

/// <summary>
/// Regressions for problems seen in agent transcripts: unviewable base64 screenshots,
/// keystrokes landing in the wrong window, and launch_app returning a helper window.
/// </summary>
public class AgentErgonomicsTests
{
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    private static ScreenshotEnvelope Envelope() => new()
    {
        ImageBase64 = Convert.ToBase64String(TinyPng),
        ImageBytes = TinyPng,
        MimeType = "image/png",
        Width = 1,
        Height = 1,
        Origin = new ScreenPoint(720, 232),
        Scale = 0.5
    };

    [Fact]
    public void Screenshot_ReturnsImageBlockAndMetadataWithoutBase64()
    {
        var result = McpResults.Create(Envelope());

        Assert.False(result.IsError);
        var image = Assert.Single(result.Content.OfType<ImageContentBlock>());
        Assert.Equal("image/png", image.MimeType);
        Assert.Equal(TinyPng, image.DecodedData.ToArray());

        var text = Assert.Single(result.Content.OfType<TextContentBlock>()).Text;
        Assert.DoesNotContain("imageBase64", text);
        Assert.DoesNotContain(Convert.ToBase64String(TinyPng), text);

        var structured = Assert.IsType<JsonElement>(result.StructuredContent);
        Assert.False(structured.TryGetProperty("imageBase64", out _));
        Assert.Equal(720, structured.GetProperty("origin").GetProperty("x").GetInt32());
        Assert.Equal(232, structured.GetProperty("origin").GetProperty("y").GetInt32());
        Assert.Equal(0.5, structured.GetProperty("scale").GetDouble());
    }

    [Fact]
    public void Screenshot_InProcessSerializationKeepsBase64()
    {
        using var doc = JsonDocument.Parse(ToolResponse.Serialize(Envelope()));
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal(Convert.ToBase64String(TinyPng), doc.RootElement.GetProperty("imageBase64").GetString());
        Assert.False(doc.RootElement.TryGetProperty("imageBytes", out _));
    }

    [Fact]
    public void ResolveRegion_FullImageWhenNoRegion()
    {
        var crop = WindowCapture.ResolveRegion(800, 600, null);
        Assert.Equal(new System.Drawing.Rectangle(0, 0, 800, 600), crop);
    }

    [Fact]
    public void ResolveRegion_ClampsToSource()
    {
        var crop = WindowCapture.ResolveRegion(800, 600, new CaptureRegion { X = 700, Y = -5, Width = 500, Height = 100 });
        Assert.Equal(new System.Drawing.Rectangle(700, 0, 100, 100), crop);
    }

    private static LaunchCandidate Candidate(
        int handle,
        string title,
        long area,
        bool realTitle = true,
        bool cloaked = false,
        bool tool = false,
        bool isNew = true,
        bool minimized = false,
        bool foreground = false) =>
        new(
            new WindowInfo
            {
                Handle = handle,
                Title = title,
                ProcessName = "app",
                Pid = 1,
                IsForeground = foreground,
                IsMinimized = minimized
            },
            area,
            cloaked,
            tool,
            realTitle,
            isNew);

    [Fact]
    public void PickMainWindow_SkipsZeroSizeHelperWithSyntheticTitle()
    {
        // Seen with a GPUI app: a hidden zero-size window titled with the process name came first.
        var picked = WindowEnumerator.PickMainWindow(
        [
            Candidate(1, "synthet-commander", area: 0, realTitle: false),
            Candidate(2, "Synthet Commander", area: 1200 * 800)
        ]);

        Assert.Equal(2, picked?.Handle);
    }

    [Fact]
    public void PickMainWindow_PrefersRealTitleOverLargerUntitledWindow()
    {
        var picked = WindowEnumerator.PickMainWindow(
        [
            Candidate(1, "app", area: 4000 * 2000, realTitle: false),
            Candidate(2, "Document - App", area: 800 * 600)
        ]);

        Assert.Equal(2, picked?.Handle);
    }

    [Fact]
    public void PickMainWindow_PrefersVisibleNonToolNewWindows()
    {
        var picked = WindowEnumerator.PickMainWindow(
        [
            Candidate(1, "Cloaked", area: 900_000, cloaked: true),
            Candidate(2, "Palette", area: 900_000, tool: true),
            Candidate(3, "Old window", area: 900_000, isNew: false),
            Candidate(4, "New window", area: 400_000)
        ]);

        Assert.Equal(4, picked?.Handle);
    }

    [Fact]
    public void PickMainWindow_PrefersForegroundOverLargerRestoredWindow()
    {
        // Windows 11 Notepad restores a previous session: a larger restored window opens next to the
        // new untitled one. Typing into the restored window would edit the user's document.
        var picked = WindowEnumerator.PickMainWindow(
        [
            Candidate(1, "notes.txt - Notepad", area: 1600 * 1000),
            Candidate(2, "Untitled - Notepad", area: 900 * 700, foreground: true)
        ]);

        Assert.Equal(2, picked?.Handle);
    }

    [Fact]
    public void PickMainWindow_ReturnsNullWhenOnlyZeroSizeWindows()
    {
        Assert.Null(WindowEnumerator.PickMainWindow([Candidate(1, "helper", area: 0)]));
    }

    [Fact]
    public async Task TypeText_WithInvalidHandle_FailsInsteadOfTypingIntoForeground()
    {
        // Empty text: even a regression cannot send keystrokes to the developer's foreground window.
        var result = await new Phase1Tools(TestServiceFactory.Create()).TypeText(string.Empty, handle: int.MaxValue);

        Assert.True(result.IsError);
        var structured = Assert.IsType<JsonElement>(result.StructuredContent);
        Assert.Equal(ErrorCodes.WindowGone, structured.GetProperty("code").GetString());
    }
}
