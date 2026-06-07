using WinGuiMcp.Platform;

namespace WinGuiMcp.Session;

public sealed class SnapshotSession
{
    public static readonly TimeSpan DefaultIdleTimeout = TimeSpan.FromMinutes(30);

    private readonly IClock _clock;
    private readonly TimeSpan _idleTimeout;

    public SnapshotSession(IClock clock, TimeSpan? idleTimeout = null)
    {
        _clock = clock;
        _idleTimeout = idleTimeout ?? DefaultIdleTimeout;
    }

    public RefTable RefTable { get; } = new();
    public DateTimeOffset LastActivity { get; private set; }

    private readonly Dictionary<int, Guid> _snapshotsByWindow = new();
    private readonly Dictionary<int, nint> _windowRegistry = new();
    private readonly Dictionary<nint, int> _handleToIndex = new();
    private int _nextWindowIndex = 1;

    public void TouchActivity() => LastActivity = _clock.UtcNow;

    public void EnsureNotExpired()
    {
        if (LastActivity != default && _clock.UtcNow - LastActivity > _idleTimeout)
            Clear();
    }

    public void Clear()
    {
        _snapshotsByWindow.Clear();
        _windowRegistry.Clear();
        _handleToIndex.Clear();
        _nextWindowIndex = 1;
        RefTable.Clear();
        LastActivity = _clock.UtcNow;
    }

    public int RegisterWindow(nint handle)
    {
        TouchActivity();
        if (_handleToIndex.TryGetValue(handle, out var existing))
            return existing;

        var index = _nextWindowIndex++;
        _handleToIndex[handle] = index;
        _windowRegistry[index] = handle;
        return index;
    }

    public nint GetHandle(int windowIndex)
    {
        if (!_windowRegistry.TryGetValue(windowIndex, out var handle))
            throw new Common.ToolException(Common.ErrorCodes.WindowGone, $"Window index {windowIndex} is not registered.");
        return handle;
    }

    public int? TryGetWindowIndex(nint handle) =>
        _handleToIndex.TryGetValue(handle, out var index) ? index : null;

    public Guid SetSnapshotId(int windowIndex, Guid snapshotId)
    {
        TouchActivity();
        RefTable.ClearForWindow(windowIndex);
        _snapshotsByWindow[windowIndex] = snapshotId;
        return snapshotId;
    }

    public Guid? GetCurrentSnapshotId(int windowIndex) =>
        _snapshotsByWindow.TryGetValue(windowIndex, out var id) ? id : null;

    public void RemoveWindow(nint handle)
    {
        if (!_handleToIndex.TryGetValue(handle, out var index))
            return;

        RefTable.ClearForWindow(index);
        _snapshotsByWindow.Remove(index);
        _windowRegistry.Remove(index);
        _handleToIndex.Remove(handle);
    }
}
