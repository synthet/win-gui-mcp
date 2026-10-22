using System.Diagnostics;
using System.Runtime.InteropServices;
using WinGuiMcp.Common;
using WinGuiMcp.Perception;

namespace WinGuiMcp.Platform;

public sealed class WindowEnumerator
{
    public void ValidateHandle(nint handle)
    {
        if (handle == 0)
            throw new ToolException(ErrorCodes.InvalidArgument, "handle must be a non-zero HWND integer.");
        if (!NativeMethods.IsWindow(handle))
            throw new ToolException(ErrorCodes.WindowGone, "Window handle is no longer valid.");
    }

    public IReadOnlyList<WindowInfo> ListWindows(string? filter, bool includeMinimized)
    {
        var foreground = NativeMethods.GetForegroundWindow();
        var results = new List<WindowInfo>();

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (hWnd == NativeMethods.GetShellWindow())
                return true;

            if (!NativeMethods.IsWindowVisible(hWnd))
                return true;

            var minimized = NativeMethods.IsIconic(hWnd);
            if (minimized && !includeMinimized)
                return true;

            var title = GetWindowTitle(hWnd);
            NativeMethods.GetWindowThreadProcessId(hWnd, out var pid);
            var processName = GetProcessName((int)pid);

            if (string.IsNullOrWhiteSpace(title))
            {
                if (string.IsNullOrWhiteSpace(processName))
                    return true;
                title = processName;
            }

            if (!MatchesFilter(filter, title, processName))
                return true;

            results.Add(new WindowInfo
            {
                Handle = (int)hWnd,
                Title = title,
                ProcessName = processName,
                Pid = (int)pid,
                IsForeground = hWnd == foreground,
                IsMinimized = minimized
            });

            return true;
        }, IntPtr.Zero);

        return results;
    }

    public void FocusWindow(nint handle)
    {
        ValidateHandle(handle);
        InputBoundary.ThrowIfBlocked(handle);

        if (NativeMethods.IsIconic(handle))
            NativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);

        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == handle)
            return;

        var foregroundThread = NativeMethods.GetWindowThreadProcessId(foreground, out _);
        var targetThread = NativeMethods.GetWindowThreadProcessId(handle, out _);
        var currentThread = NativeMethods.GetCurrentThreadId();

        _ = NativeMethods.AttachThreadInput(currentThread, foregroundThread, true);
        _ = NativeMethods.AttachThreadInput(targetThread, foregroundThread, true);

        try
        {
            NativeMethods.ShowWindow(handle, NativeMethods.SW_SHOW);
            NativeMethods.BringWindowToTop(handle);

            // ALT trick helps bypass foreground lock on some Windows versions.
            SendAltTap();
            if (!NativeMethods.SetForegroundWindow(handle))
            {
                SendAltTap();
                NativeMethods.SetForegroundWindow(handle);
            }

            if (NativeMethods.GetForegroundWindow() != handle)
                throw new ToolException(ErrorCodes.FocusFailed, "Could not bring window to foreground.");
        }
        finally
        {
            _ = NativeMethods.AttachThreadInput(targetThread, foregroundThread, false);
            _ = NativeMethods.AttachThreadInput(currentThread, foregroundThread, false);
        }
    }

    public LaunchResult LaunchApp(string? path, string? appUserModelId, string? args, int waitForWindowMs)
    {
        if (string.IsNullOrWhiteSpace(path) && string.IsNullOrWhiteSpace(appUserModelId))
            throw new ToolException(ErrorCodes.InvalidArgument, "Either path or appUserModelId is required.");

        Process? process = null;
        var shellUriLaunch = IsShellUri(path);
        try
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                var startInfo = new ProcessStartInfo { UseShellExecute = true };

                if (IsSettingsPath(path))
                {
                    startInfo.FileName = "explorer.exe";
                    startInfo.Arguments = path;
                }
                else
                {
                    startInfo.FileName = path;
                    startInfo.Arguments = args ?? string.Empty;
                }

                process = Process.Start(startInfo);
            }
            else
            {
                process = Process.Start(new ProcessStartInfo
                {
                    FileName = $"shell:AppsFolder\\{appUserModelId}",
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            throw new ToolException(ErrorCodes.LaunchFailed, $"Failed to launch application: {ex.Message}");
        }

        if (process is null && !shellUriLaunch)
            throw new ToolException(ErrorCodes.LaunchFailed, "Process.Start returned null.");

        var processId = process?.Id ?? -1;
        var processNameHint = process is null ? string.Empty : SafeProcessName(process);

        var deadline = DateTime.UtcNow.AddMilliseconds(waitForWindowMs);
        while (DateTime.UtcNow < deadline)
        {
            var match = FindLaunchedWindow(path, appUserModelId, processId, processNameHint);
            if (match is not null)
                return match;

            try
            {
                if (processId > 0 &&
                    !PreferProcessDiscovery(path) &&
                    process is not null &&
                    !process.HasExited &&
                    process.MainWindowHandle != IntPtr.Zero)
                {
                    var handle = process.MainWindowHandle;
                    return new LaunchResult
                    {
                        Handle = (int)handle,
                        Title = GetWindowTitle(handle),
                        Pid = processId
                    };
                }
            }
            catch (InvalidOperationException)
            {
                // Launcher process may exit before the real app window appears.
            }

            Thread.Sleep(200);
        }

        throw new ToolException(ErrorCodes.Timeout, "Application launched but no window appeared within waitForWindowMs.");
    }

    private LaunchResult? FindLaunchedWindow(string? path, string? appUserModelId, int launcherPid, string processNameHint)
    {
        var windows = ListWindows(null, includeMinimized: true);

        var byLauncherPid = launcherPid > 0
            ? windows.FirstOrDefault(w => w.Pid == launcherPid)
            : null;
        if (byLauncherPid is not null)
            return ToLaunchResult(byLauncherPid);

        var expectedProcess = ResolveExpectedProcessName(path, appUserModelId, processNameHint);
        if (!string.IsNullOrEmpty(expectedProcess))
        {
            var byProcess = windows
                .Where(w => w.ProcessName.Equals(expectedProcess, StringComparison.OrdinalIgnoreCase) ||
                            w.ProcessName.Contains(expectedProcess, StringComparison.OrdinalIgnoreCase))
                .OrderBy(w => w.IsMinimized)
                .ThenByDescending(w => w.IsForeground)
                .ThenByDescending(w => w.Handle)
                .FirstOrDefault();

            if (byProcess is not null)
                return ToLaunchResult(byProcess);
        }

        if (IsCalculatorPath(path) || IsSettingsPath(path))
        {
            var frameHost = windows
                .Where(w => w.ProcessName.Equals("ApplicationFrameHost", StringComparison.OrdinalIgnoreCase))
                .OrderBy(w => w.IsMinimized)
                .ThenByDescending(w => w.IsForeground)
                .FirstOrDefault();

            if (frameHost is not null)
                return ToLaunchResult(frameHost);
        }

        var titleHint = ResolveTitleHint(path);
        if (!string.IsNullOrEmpty(titleHint))
        {
            var byTitle = windows
                .Where(w => w.Title.Contains(titleHint, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(w => w.IsForeground)
                .FirstOrDefault();

            if (byTitle is not null)
                return ToLaunchResult(byTitle);
        }

        return null;
    }

    private static bool PreferProcessDiscovery(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (path.StartsWith("ms-settings", StringComparison.OrdinalIgnoreCase))
            return true;

        var exe = Path.GetFileName(path);
        return exe.Equals("calc.exe", StringComparison.OrdinalIgnoreCase);
    }

    private static LaunchResult ToLaunchResult(WindowInfo window) =>
        new() { Handle = window.Handle, Title = window.Title, Pid = window.Pid };

    private static string? ResolveExpectedProcessName(string? path, string? appUserModelId, string processNameHint)
    {
        if (!string.IsNullOrWhiteSpace(processNameHint))
            return processNameHint;

        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (path.StartsWith("ms-settings", StringComparison.OrdinalIgnoreCase))
            return "SystemSettings";

        return Path.GetFileNameWithoutExtension(path).ToLowerInvariant() switch
        {
            "calc" => "CalculatorApp",
            "notepad" => "Notepad",
            "explorer" => "explorer",
            _ => Path.GetFileNameWithoutExtension(path)
        };
    }

    private static string? ResolveTitleHint(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (path.StartsWith("ms-settings", StringComparison.OrdinalIgnoreCase))
            return "settings";

        return Path.GetFileNameWithoutExtension(path).ToLowerInvariant() switch
        {
            "calc" => "calc",
            "notepad" => "notepad",
            "explorer" => "explorer",
            _ => null
        };
    }

    private static bool IsCalculatorPath(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        Path.GetFileName(path).Equals("calc.exe", StringComparison.OrdinalIgnoreCase);

    private static bool IsSettingsPath(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        path.StartsWith("ms-settings", StringComparison.OrdinalIgnoreCase);

    private static bool IsShellUri(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (IsSettingsPath(path))
            return true;

        return path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetWindowTitle(nint handle)
    {
        var length = NativeMethods.GetWindowTextLengthW(handle);
        if (length <= 0)
            return string.Empty;

        Span<char> buffer = stackalloc char[length + 1];
        _ = NativeMethods.GetWindowTextW(handle, buffer, buffer.Length);
        return buffer[..length].ToString();
    }

    private static string SafeProcessName(Process process)
    {
        try
        {
            return process.ProcessName;
        }
        catch (InvalidOperationException)
        {
            return string.Empty;
        }
    }

    private static void SendAltTap()
    {
        InputBoundary.ThrowIfInteractiveDesktop();

        var down = new NativeMethods.INPUT
        {
            Type = NativeMethods.INPUT_KEYBOARD,
            U = new NativeMethods.InputUnion
            {
                Keyboard = new NativeMethods.KEYBDINPUT { wVk = NativeMethods.VK_MENU }
            }
        };
        var up = down;
        up.U.Keyboard.dwFlags = NativeMethods.KEYEVENTF_KEYUP;
        _ = NativeMethods.SendInput(1, [down], System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
        _ = NativeMethods.SendInput(1, [up], System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static string GetProcessName(int pid)
    {
        try
        {
            return Process.GetProcessById(pid).ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool MatchesFilter(string? filter, string title, string processName)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return true;

        return title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
               processName.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }
}
