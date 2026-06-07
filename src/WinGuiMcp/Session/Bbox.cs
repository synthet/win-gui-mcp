namespace WinGuiMcp.Session;

public readonly record struct Bbox(int X, int Y, int Width, int Height)
{
    public (int X, int Y) Center => (X + Width / 2, Y + Height / 2);

    public static Bbox FromRectangle(int left, int top, int right, int bottom) =>
        new(left, top, right - left, bottom - top);
}
