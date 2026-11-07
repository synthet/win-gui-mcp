namespace WinGuiMcp.Platform;

public sealed class DpiManager
{
    public (int X, int Y) GetCenter(Session.Bbox bbox) =>
        (bbox.X + bbox.Width / 2, bbox.Y + bbox.Height / 2);
}
