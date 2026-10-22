using System.Diagnostics;

namespace WinGuiMcp.Platform;

/// <summary>
/// OS-level wait predicates (process / filesystem) used by <c>wait_for</c> when no window handle is required.
/// </summary>
public static class OsWaitConditions
{
    public static bool IsOsLevelCondition(string condition) =>
        condition is "process-exists" or "file-exists" or "file-stable";

    public static bool TryEvaluate(string condition, string? target, out string outcomeRef)
    {
        outcomeRef = condition;
        if (string.IsNullOrWhiteSpace(target))
            return false;

        return condition switch
        {
            "process-exists" => IsProcessRunning(target),
            "file-exists" => File.Exists(NormalizePath(target)),
            "file-stable" => IsFileStable(NormalizePath(target)),
            _ => false
        };
    }

    private static string NormalizePath(string path) =>
        Environment.ExpandEnvironmentVariables(path.Trim());

    private static bool IsProcessRunning(string processName)
    {
        var name = processName.Trim();
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            name = name[..^4];

        return Process.GetProcessesByName(name).Length > 0;
    }

    /// <summary>
    /// File exists and size is unchanged across two samples (export / write completion).
    /// </summary>
    public static bool IsFileStable(string path, int settleMs = 200)
    {
        if (!File.Exists(path))
            return false;

        long size1 = new FileInfo(path).Length;
        Thread.Sleep(Math.Clamp(settleMs, 50, 2000));
        if (!File.Exists(path))
            return false;

        long size2 = new FileInfo(path).Length;
        return size1 == size2;
    }
}
