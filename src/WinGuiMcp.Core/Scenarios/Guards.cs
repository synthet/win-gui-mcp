using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using WinGuiMcp.Platform;

namespace WinGuiMcp.Scenarios;

public sealed record WindowOwner(int Hwnd, int Pid, string ProcessName, string ClassName, string Title);

/// <summary>Resolves windows and screen points to their owning process (root window).</summary>
public static partial class WindowOwnership
{
    public static WindowOwner Foreground() => Describe(NativeMethods.GetForegroundWindow());

    public static WindowOwner AtPoint(int x, int y)
    {
        var hwnd = NativeMethods.WindowFromPoint(new NativeMethods.POINT { X = x, Y = y });
        var root = hwnd == 0 ? 0 : GetAncestor(hwnd, 2 /* GA_ROOT */);
        return Describe(root == 0 ? hwnd : root);
    }

    public static WindowOwner Describe(nint hwnd)
    {
        if (hwnd == 0 || !NativeMethods.IsWindow(hwnd))
            return new WindowOwner((int)hwnd, 0, "", "", "");
        NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
        var name = "";
        try
        {
            using var p = Process.GetProcessById((int)pid);
            name = p.ProcessName + ".exe";
        }
        catch (ArgumentException) { }
        catch (InvalidOperationException) { }
        Span<char> buf = stackalloc char[256];
        var len = GetClassNameW(hwnd, buf, buf.Length);
        return new WindowOwner((int)hwnd, (int)pid, name, len > 0 ? new string(buf[..len]) : "", WindowEnumerator.GetWindowTitle(hwnd));
    }

    [LibraryImport("user32.dll")]
    private static partial nint GetAncestor(nint hwnd, uint flags);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int GetClassNameW(nint hwnd, Span<char> name, int max);
}

/// <summary>
/// Step modifier <c>guard:</c> — checks the foreground window before the step acts, e.g.
/// <c>guard: { process: lightroom.exe, class: NonActivateWindow, waitMs: 2000 }</c> so typed digits can never
/// land in the app as shortcuts after a dropped click.
/// </summary>
public static class ForegroundGuard
{
    public static void Check(JsonObject guard, JsonObject scope)
    {
        var g = (JsonObject)ScenarioInterpolation.Apply(guard, scope)!;
        var process = g["process"]?.ToString();
        var cls = g["class"]?.ToString();
        var titleLike = g["titleLike"]?.ToString();
        var handle = ScenarioExpression.TryNum(g["handle"], out var h) ? (int)h : (int?)null;
        var waitMs = ScenarioExpression.TryNum(g["waitMs"], out var w) ? (int)w : 0;

        var sw = Stopwatch.StartNew();
        WindowOwner fg;
        string? why;
        do
        {
            fg = WindowOwnership.Foreground();
            why = Mismatch(fg, process, cls, titleLike, handle);
            if (why is null) return;
            Thread.Sleep(100);
        } while (sw.ElapsedMilliseconds < waitMs);

        throw new ScenarioException(ScenarioErrorCodes.GuardFailed,
            $"Foreground guard failed: {why}. Nothing was sent.",
            new Dictionary<string, object?> { ["foreground"] = fg });
    }

    private static string? Mismatch(WindowOwner fg, string? process, string? cls, string? titleLike, int? handle)
    {
        if (process is not null && !SameProcess(fg.ProcessName, process))
            return $"foreground process is '{fg.ProcessName}', expected '{process}'";
        if (cls is not null && !string.Equals(fg.ClassName, cls, StringComparison.Ordinal))
            return $"foreground class is '{fg.ClassName}', expected '{cls}'";
        if (titleLike is not null && !Regex.IsMatch(fg.Title, titleLike, RegexOptions.IgnoreCase))
            return $"foreground title '{fg.Title}' does not match /{titleLike}/";
        if (handle is not null && fg.Hwnd != handle)
            return $"foreground handle is {fg.Hwnd}, expected {handle}";
        return null;
    }

    internal static bool SameProcess(string actual, string expected) =>
        string.Equals(Path.GetFileNameWithoutExtension(actual), Path.GetFileNameWithoutExtension(expected), StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Keeps actuating steps inside the scenario's declared <c>target</c> and away from restricted system apps
/// (.agent/SAFETY.md). Windows are resolved by handle, or by hit-testing x/y for coordinate input.
/// </summary>
public static class TargetGuard
{
    public static readonly string[] DeniedProcesses =
        ["control.exe", "SystemSettings.exe", "mmc.exe", "regedit.exe", "gpedit.exe", "secpol.exe", "taskmgr.exe", "consent.exe", "UserAccountControlSettings.exe"];

    public static void EnsureWindowAllowed(ScenarioTarget? target, WindowOwner owner, string step)
    {
        if (owner.Pid == 0)
            throw new ScenarioException(ScenarioErrorCodes.TargetNotAllowed,
                $"{step}: window {owner.Hwnd} no longer exists (stale handle); take a new snapshot or list_windows.");
        if (DeniedProcesses.Any(d => ForegroundGuard.SameProcess(owner.ProcessName, d)))
            throw new ScenarioException(ScenarioErrorCodes.TargetNotAllowed,
                $"{step}: '{owner.ProcessName}' is a restricted system app; scenarios never actuate it.");
        if (target is null || (target.Processes.Count == 0 && target.TitlePattern is null))
            throw new ScenarioException(ScenarioErrorCodes.TargetNotAllowed,
                $"{step}: this scenario declares no target, so it may not actuate windows. Add target: {{ processes: [app.exe] }}.");
        if (target.Processes.Count > 0 && !target.Processes.Any(p => ForegroundGuard.SameProcess(owner.ProcessName, p)))
            throw new ScenarioException(ScenarioErrorCodes.TargetNotAllowed,
                $"{step}: window belongs to '{owner.ProcessName}', which is not in target.processes ({string.Join(", ", target.Processes)}).",
                new Dictionary<string, object?> { ["window"] = owner });
        if (target.TitlePattern is { } pattern && !Regex.IsMatch(owner.Title, pattern, RegexOptions.IgnoreCase) && owner.Title.Length > 0
            && target.Processes.Count == 0)
            throw new ScenarioException(ScenarioErrorCodes.TargetNotAllowed,
                $"{step}: window title '{owner.Title}' does not match target.titlePattern /{pattern}/.");
    }

    public static void EnsureLaunchAllowed(ScenarioTarget? target, string? path, string? aumid, string step)
    {
        var name = Path.GetFileName(path ?? "") ?? "";
        if ((path ?? "").StartsWith("ms-settings:", StringComparison.OrdinalIgnoreCase) ||
            (aumid ?? "").Contains("windows.immersivecontrolpanel", StringComparison.OrdinalIgnoreCase) ||
            DeniedProcesses.Any(d => ForegroundGuard.SameProcess(name, d)))
            throw new ScenarioException(ScenarioErrorCodes.TargetNotAllowed, $"{step}: launching '{path ?? aumid}' is not allowed (restricted system app).");
        if (target is null || target.Processes.Count == 0 || name.Length == 0)
            return;
        if (!target.Processes.Any(p => ForegroundGuard.SameProcess(name, p)))
            throw new ScenarioException(ScenarioErrorCodes.TargetNotAllowed,
                $"{step}: '{name}' is not in target.processes ({string.Join(", ", target.Processes)}).");
    }
}
