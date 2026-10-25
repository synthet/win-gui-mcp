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

        var first = new FileInfo(path);
        long size1 = first.Length;
        if (size1 == 0) return false;
        var modified = first.LastWriteTimeUtc;
        Thread.Sleep(Math.Clamp(settleMs, 50, 2000));
        if (!File.Exists(path))
            return false;

        var second = new FileInfo(path);
        return size1 == second.Length && modified == second.LastWriteTimeUtc;
    }
}

/// <summary>Per-wait observation state; no sleeps, desktop calls, or image-format assumptions.</summary>
public sealed class FileReadiness(TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    private (long Length, DateTime Modified)? _last;
    private long _stableSince;

    public bool Observe(string path, int settleMs = 200, long minBytes = 1, DateTimeOffset? changedSinceUtc = null)
    {
        if (settleMs < 0 || settleMs > 60000 || minBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(settleMs), "Quiet duration must be 0..60000 ms and minBytes nonnegative.");
        var file = new FileInfo(Environment.ExpandEnvironmentVariables(path));
        if (!file.Exists || file.Length < minBytes || (changedSinceUtc.HasValue && file.LastWriteTimeUtc <= changedSinceUtc.Value.UtcDateTime))
        {
            _last = null;
            return false;
        }
        var current = (file.Length, file.LastWriteTimeUtc);
        if (_last != current)
        {
            _last = current;
            _stableSince = _time.GetTimestamp();
        }
        return _time.GetElapsedTime(_stableSince).TotalMilliseconds >= settleMs;
    }
}
