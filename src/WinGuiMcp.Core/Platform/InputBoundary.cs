using System.Diagnostics;
using System.Runtime.InteropServices;
using WinGuiMcp.Common;

namespace WinGuiMcp.Platform;

/// <summary>
/// SendInput does not report UIPI failures. Compare integrity and the input desktop before injection.
/// </summary>
public static class InputBoundary
{
    public const int MediumRid = 0x2000;
    public const int HighRid = 0x3000;

    // WTSINFOEXW is 8-aligned because it contains LARGE_INTEGER, so SessionFlags
    // sits at byte 16 (Level, 4 bytes of padding, SessionId, SessionState, SessionFlags).
    // wtsapi32.h: WTS_SESSIONSTATE_LOCK = 0, WTS_SESSIONSTATE_UNLOCK = 1 on Windows 8+.
    private const int SessionFlagsOffset = 16;

    public readonly record struct DesktopProbe(bool Opened, string? Name, bool SessionLocked);

    public static bool BlocksLowerIntegrity(int serverRid, int targetRid) =>
        serverRid >= 0 && targetRid > serverRid;

    public static bool IsLockedSession(int sessionFlags) =>
        sessionFlags == 0;

    public static bool IsSecureOrLocked(DesktopProbe probe)
    {
        if (probe.SessionLocked || !probe.Opened)
            return true;

        return probe.Name is { Length: > 0 } name &&
               !name.Equals("Default", StringComparison.OrdinalIgnoreCase);
    }

    public static ToolException CreateUipiBlocked(string processName, int pid, bool serverElevated) =>
        new(
            ErrorCodes.UIPIBlocked,
            $"Cannot interact with elevated process '{processName}' (PID {pid}). Run WinGuiMcp as Administrator.",
            new Dictionary<string, object?>
            {
                ["targetProcess"] = processName,
                ["targetPid"] = pid,
                ["serverElevated"] = serverElevated
            });

    public static void ThrowIfInteractiveDesktop()
    {
        var probe = ProbeDesktop();
        if (!IsSecureOrLocked(probe))
            return;

        var message = probe.SessionLocked
            ? "Workstation is locked. Unlock the session before automation can continue."
            : "The secure desktop is active (UAC or Winlogon). Dismiss the prompt before automation can continue.";
        throw new ToolException(ErrorCodes.SecureDesktop, message);
    }

    public static void ThrowIfBlocked(nint targetHwnd)
    {
        ThrowIfInteractiveDesktop();

        if (targetHwnd == 0)
            return;

        var serverRid = CurrentIntegrityRid.Value;
        if (!TryReadTarget(targetHwnd, serverRid, out var blocked))
            return;

        if (blocked is not null)
            throw blocked;
    }

    private static readonly Lazy<int> CurrentIntegrityRid = new(ReadCurrentIntegrity);

    private static DesktopProbe ProbeDesktop()
    {
        var locked = QuerySessionLocked();
        var desktop = NativeMethods.OpenInputDesktop(0, false, NativeMethods.DESKTOP_READOBJECTS);
        if (desktop == IntPtr.Zero)
            return new DesktopProbe(false, null, locked);

        try
        {
            return new DesktopProbe(true, ReadDesktopName(desktop), locked);
        }
        finally
        {
            NativeMethods.CloseDesktop(desktop);
        }
    }

    private static bool QuerySessionLocked()
    {
        if (!NativeMethods.WTSQuerySessionInformationW(
                IntPtr.Zero,
                NativeMethods.WTS_CURRENT_SESSION,
                NativeMethods.WTSSessionInfoEx,
                out var buffer,
                out var bytes))
        {
            return false;
        }

        try
        {
            if (buffer == IntPtr.Zero || bytes < SessionFlagsOffset + sizeof(int))
                return false;

            if (Marshal.ReadInt32(buffer, 0) != 1)
                return false;

            var flags = Marshal.ReadInt32(buffer, SessionFlagsOffset);
            if (flags == unchecked((int)0xFFFFFFFF))
                return false;

            return IsLockedSession(flags);
        }
        finally
        {
            NativeMethods.WTSFreeMemory(buffer);
        }
    }

    private static string? ReadDesktopName(IntPtr desktop)
    {
        _ = NativeMethods.GetUserObjectInformationW(desktop, NativeMethods.UOI_NAME, IntPtr.Zero, 0, out var needed);
        if (needed == 0 || needed > 512)
            return null;

        var buffer = Marshal.AllocHGlobal((int)needed);
        try
        {
            if (!NativeMethods.GetUserObjectInformationW(desktop, NativeMethods.UOI_NAME, buffer, needed, out _))
                return null;

            return Marshal.PtrToStringUni(buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool TryReadTarget(nint hwnd, int serverRid, out ToolException? blocked)
    {
        blocked = null;
        var pid = 0u;
        _ = NativeMethods.GetWindowThreadProcessId(hwnd, out pid);
        if (pid == 0)
            throw new ToolException(ErrorCodes.WindowGone, "Target window has no process.");

        var processName = DescribeProcess((int)pid);
        var process = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (process == IntPtr.Zero)
        {
            if (Marshal.GetLastPInvokeError() == NativeMethods.ErrorAccessDenied)
            {
                blocked = CreateUipiBlocked(processName, (int)pid, serverRid >= HighRid);
                return true;
            }

            throw new ToolException(ErrorCodes.WindowGone, "Target process is no longer available.");
        }

        try
        {
            var read = ReadIntegrity(process);
            if (read.AccessDenied)
            {
                blocked = CreateUipiBlocked(processName, (int)pid, serverRid >= HighRid);
                return true;
            }

            if (!read.Ok)
                return false;

            if (BlocksLowerIntegrity(serverRid, read.Rid))
                blocked = CreateUipiBlocked(processName, (int)pid, serverRid >= HighRid);

            return true;
        }
        finally
        {
            NativeMethods.CloseHandle(process);
        }
    }

    private static int ReadCurrentIntegrity()
    {
        var read = ReadIntegrity(NativeMethods.GetCurrentProcess());
        if (!read.Ok)
            throw new ToolException(ErrorCodes.InternalError, "Could not read the server integrity level.");

        return read.Rid;
    }

    private readonly record struct IntegrityRead(bool Ok, int Rid, bool AccessDenied);

    private static IntegrityRead ReadIntegrity(IntPtr processHandle)
    {
        if (!NativeMethods.OpenProcessToken(processHandle, NativeMethods.TOKEN_QUERY, out var token))
            return new IntegrityRead(false, -1, Marshal.GetLastPInvokeError() == NativeMethods.ErrorAccessDenied);

        try
        {
            NativeMethods.GetTokenInformation(token, NativeMethods.TokenIntegrityLevel, IntPtr.Zero, 0, out var needed);
            if (needed == 0 || needed > 1024)
                return new IntegrityRead(false, -1, false);

            var buffer = Marshal.AllocHGlobal((int)needed);
            try
            {
                if (!NativeMethods.GetTokenInformation(token, NativeMethods.TokenIntegrityLevel, buffer, needed, out _))
                    return new IntegrityRead(false, -1, Marshal.GetLastPInvokeError() == NativeMethods.ErrorAccessDenied);

                var sid = Marshal.ReadIntPtr(buffer);
                if (sid == IntPtr.Zero)
                    return new IntegrityRead(false, -1, false);

                var count = Marshal.ReadByte(NativeMethods.GetSidSubAuthorityCount(sid));
                if (count == 0)
                    return new IntegrityRead(false, -1, false);

                var rid = Marshal.ReadInt32(NativeMethods.GetSidSubAuthority(sid, (uint)(count - 1)));
                return new IntegrityRead(true, rid, false);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        finally
        {
            NativeMethods.CloseHandle(token);
        }
    }

    private static string DescribeProcess(int pid)
    {
        try
        {
            var name = Process.GetProcessById(pid).ProcessName;
            if (string.IsNullOrWhiteSpace(name))
                return "unknown";

            return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name : name + ".exe";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return "unknown";
        }
    }
}
