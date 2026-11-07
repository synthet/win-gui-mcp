namespace WinGuiMcp.Platform;

/// <summary>Public read-only view of the current foreground window, for hosts outside Core.</summary>
public static class ForegroundInfo
{
    public static (int Hwnd, string Title) Get()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        return ((int)hwnd, WindowEnumerator.GetWindowTitle(hwnd));
    }
}
