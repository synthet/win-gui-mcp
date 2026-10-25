namespace WinGuiMcp.Session;

public readonly record struct Bbox(int X, int Y, int Width, int Height)
{
    public (int X, int Y) Center => (X + Width / 2, Y + Height / 2);

    public static Bbox FromRectangle(int left, int top, int right, int bottom)
    {
        const int minCoord = -100000;
        const int maxCoord = 100000;
        var l = Math.Clamp(left, minCoord, maxCoord);
        var t = Math.Clamp(top, minCoord, maxCoord);
        var r = Math.Clamp(right, minCoord, maxCoord);
        var b = Math.Clamp(bottom, minCoord, maxCoord);
        return new(l, t, Math.Max(0, r - l), Math.Max(0, b - t));
    }

    public static Bbox FromDoubles(double left, double top, double right, double bottom)
    {
        if (!double.IsFinite(left) || !double.IsFinite(top) || !double.IsFinite(right) || !double.IsFinite(bottom))
            return new(0, 0, 0, 0);
        const double minCoord = -100000;
        const double maxCoord = 100000;
        var l = (int)Math.Clamp(left, minCoord, maxCoord);
        var t = (int)Math.Clamp(top, minCoord, maxCoord);
        var r = (int)Math.Clamp(right, minCoord, maxCoord);
        var b = (int)Math.Clamp(bottom, minCoord, maxCoord);
        return new(l, t, Math.Max(0, r - l), Math.Max(0, b - t));
    }
}
