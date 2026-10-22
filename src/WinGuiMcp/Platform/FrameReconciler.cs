using WinGuiMcp.Session;

namespace WinGuiMcp.Platform;

public static class FrameReconciler
{
    public const int LogThresholdPx = 2;

    public readonly record struct Overflow(int Left, int Top, int Right, int Bottom)
    {
        public int Max => Math.Max(Math.Max(Left, Top), Math.Max(Right, Bottom));
    }

    public static Overflow Measure(Bbox bbox, int frameLeft, int frameTop, int frameRight, int frameBottom) =>
        new(
            Math.Max(0, frameLeft - bbox.X),
            Math.Max(0, frameTop - bbox.Y),
            Math.Max(0, bbox.X + bbox.Width - frameRight),
            Math.Max(0, bbox.Y + bbox.Height - frameBottom));

    public static bool CenterInside(Bbox bbox, int frameLeft, int frameTop, int frameRight, int frameBottom)
    {
        var (x, y) = bbox.Center;
        return x >= frameLeft && y >= frameTop && x < frameRight && y < frameBottom;
    }
}
