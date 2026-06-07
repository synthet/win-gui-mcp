using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using WinGuiMcp.Common;

namespace WinGuiMcp.Tests;

/// <summary>
/// Closes windows and processes opened by integration gate tests.
/// </summary>
internal sealed class LaunchedAppScope : IDisposable
{
    private const uint WmClose = 0x0010;

    private readonly HashSet<int> _handles = [];
    private readonly HashSet<int> _pids = [];
    private bool _disposed;

    public void Track(object launchResult)
    {
        var json = ToolResponse.Serialize(launchResult);
        using var doc = JsonDocument.Parse(json);
        TrackHandle(doc.RootElement.GetProperty("handle").GetInt32());

        if (doc.RootElement.TryGetProperty("pid", out var pidElement))
            TrackPid(pidElement.GetInt32());
    }

    public void TrackHandle(int handle)
    {
        if (handle != 0)
            _handles.Add(handle);
    }

    public void TrackPid(int pid)
    {
        if (pid > 0)
            _pids.Add(pid);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var handle in _handles)
            TryCloseWindow(handle);

        Thread.Sleep(250);

        foreach (var handle in _handles)
        {
            if (IsWindow(handle))
                TryCloseWindow(handle);
        }

        Thread.Sleep(250);

        foreach (var pid in _pids)
            TryTerminateProcess(pid);

        foreach (var handle in _handles)
        {
            if (!IsWindow(handle))
                continue;

            GetWindowThreadProcessId(handle, out var pid);
            if (pid > 0)
                TryTerminateProcess((int)pid);
        }
    }

    private static void TryCloseWindow(int handle)
    {
        try
        {
            if (!IsWindow(handle))
                return;

            _ = SendMessage(handle, WmClose, IntPtr.Zero, IntPtr.Zero);
        }
        catch
        {
            // Best-effort cleanup for test hygiene.
        }
    }

    private static void TryTerminateProcess(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            if (process.HasExited)
                return;

            if (!process.CloseMainWindow())
                Thread.Sleep(300);

            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (ArgumentException)
        {
            // Process already exited.
        }
        catch (InvalidOperationException)
        {
            // Process exited between checks.
        }
        catch
        {
            // Best-effort cleanup for test hygiene.
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
