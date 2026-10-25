using System.Diagnostics;
using WinGuiMcp.Action;
using WinGuiMcp.Capture;
using WinGuiMcp.Common;
using WinGuiMcp.Perception;
using WinGuiMcp.Platform;
using WinGuiMcp.Session;

namespace WinGuiMcp.Tools;

public sealed class ToolService
{
    private readonly SnapshotSession _session;
    private readonly WindowEnumerator _windowEnumerator;
    private readonly UiaSnapshotBuilder _snapshotBuilder;
    private readonly PatternExecutor _patternExecutor;
    private readonly InputSimulator _inputSimulator;
    private readonly WindowCapture _windowCapture;
    public UiaDispatcher Dispatcher { get; }

    public ToolService(
        SnapshotSession session,
        WindowEnumerator windowEnumerator,
        UiaSnapshotBuilder snapshotBuilder,
        PatternExecutor patternExecutor,
        InputSimulator inputSimulator,
        WindowCapture windowCapture,
        UiaDispatcher? dispatcher = null)
    {
        _session = session;
        _windowEnumerator = windowEnumerator;
        _snapshotBuilder = snapshotBuilder;
        _patternExecutor = patternExecutor;
        _inputSimulator = inputSimulator;
        _windowCapture = windowCapture;
        Dispatcher = dispatcher ?? UiaDispatcher.Shared;
    }

    public object ListWindows(string? filter, bool includeMinimized)
    {
        _session.EnsureNotExpired();
        _session.TouchActivity();

        var windows = _windowEnumerator.ListWindows(filter, includeMinimized)
            .Select(w => new
            {
                handle = w.Handle,
                title = w.Title,
                processName = w.ProcessName,
                pid = w.Pid,
                isForeground = w.IsForeground,
                isMinimized = w.IsMinimized
            });

        return new { ok = true, windows };
    }

    public object LaunchApp(string? path, string? appUserModelId, string? args, int waitForWindowMs)
    {
        _session.EnsureNotExpired();
        var result = _windowEnumerator.LaunchApp(path, appUserModelId, args, waitForWindowMs);
        return new { ok = true, handle = result.Handle, title = result.Title, pid = result.Pid };
    }

    public object FocusWindow(int handle)
    {
        _session.EnsureNotExpired();
        _windowEnumerator.ValidateHandle(handle);
        _windowEnumerator.FocusWindow(handle);
        _session.TouchActivity();
        return new { ok = true, handle, focused = true };
    }

    public object CloseWindow(int handle)
    {
        _session.EnsureNotExpired();
        _windowEnumerator.ValidateHandle(handle);
        InputBoundary.ThrowIfBlocked((nint)handle);

        var closedViaPattern = false;
        try
        {
            var root = _snapshotBuilder.Automation.FromHandle((nint)handle);
            if (root is not null && root.Patterns.Window.IsSupported)
            {
                root.Patterns.Window.Pattern.Close();
                closedViaPattern = true;
            }
        }
        catch { }

        if (!closedViaPattern)
            NativeMethods.PostMessageW((nint)handle, NativeMethods.WM_CLOSE, 0, 0);

        _session.TouchActivity();
        return new { ok = true, handle, method = closedViaPattern ? "WindowPattern" : "WM_CLOSE" };
    }

    public object SetWindowState(int handle, string state, int? x = null, int? y = null, int? width = null, int? height = null)
    {
        _session.EnsureNotExpired();
        _windowEnumerator.ValidateHandle(handle);
        InputBoundary.ThrowIfBlocked((nint)handle);

        var normState = (state ?? "normal").ToLowerInvariant();
        var cmd = normState switch
        {
            "minimize" or "minimized" => NativeMethods.SW_MINIMIZE,
            "maximize" or "maximized" => NativeMethods.SW_MAXIMIZE,
            "restore" or "normal" => NativeMethods.SW_RESTORE,
            _ => throw new ToolException(ErrorCodes.InvalidArgument, $"Invalid window state '{state}'. Expected minimize, maximize, restore, or normal.")
        };

        NativeMethods.ShowWindow((nint)handle, cmd);

        if (x.HasValue || y.HasValue || width.HasValue || height.HasValue)
        {
            if (!NativeMethods.GetWindowRect((nint)handle, out var rect))
                throw new ToolException(ErrorCodes.InternalError, "Could not read current window bounds.");

            var nx = x ?? rect.Left;
            var ny = y ?? rect.Top;
            var nw = width ?? rect.Width;
            var nh = height ?? rect.Height;
            if (nw <= 0 || nh <= 0)
                throw new ToolException(ErrorCodes.InvalidArgument, "Window width and height must be positive.");

            NativeMethods.MoveWindow((nint)handle, nx, ny, nw, nh, true);
        }

        _session.TouchActivity();
        return new { ok = true, handle, state = normState };
    }

    public object GetActiveModal(int handle)
    {
        _session.EnsureNotExpired();
        _windowEnumerator.ValidateHandle(handle);
        _session.TouchActivity();

        var hwnd = (nint)handle;
        var isMainEnabled = NativeMethods.IsWindowEnabled(hwnd);
        var popup = NativeMethods.GetLastActivePopup(hwnd);
        var hasPopup = popup != 0 && popup != hwnd && NativeMethods.IsWindow(popup) && NativeMethods.IsWindowVisible(popup);

        if (hasPopup)
        {
            return new
            {
                ok = true,
                handle,
                isMainWindowEnabled = isMainEnabled,
                hasModal = true,
                modalHandle = (int)popup,
                modalTitle = WindowEnumerator.GetWindowTitle(popup)
            };
        }

        return new
        {
            ok = true,
            handle,
            isMainWindowEnabled = isMainEnabled,
            hasModal = !isMainEnabled,
            modalHandle = (int?)null,
            modalTitle = (string?)null
        };
    }

    public SnapshotEnvelope Snapshot(
        int handle,
        string scope,
        string detail,
        bool includeOffscreen,
        string? rootRef = null,
        int maxDepth = 32)
    {
        _session.EnsureNotExpired();
        _windowEnumerator.ValidateHandle(handle);
        return _snapshotBuilder.Build(_session, handle, scope, detail, includeOffscreen, rootRef, maxDepth);
    }

    public object FindElement(int handle, string by, string value, int nth, string match)
    {
        _session.EnsureNotExpired();
        _windowEnumerator.ValidateHandle(handle);

        var windowIndex = _session.TryGetWindowIndex(handle);
        if (windowIndex is null || _session.GetCurrentSnapshotId(windowIndex.Value) is null)
            Snapshot(handle, "window", "control", includeOffscreen: false);

        var registeredIndex = _session.RegisterWindow(handle);
        var snapshotId = _session.GetCurrentSnapshotId(registeredIndex)!;

        var entry = _session.RefTable.FindElement(registeredIndex, snapshotId.Value, by, value, match, nth);
        return new { ok = true, entry.Ref, role = entry.Node.Role, name = entry.Node.Name };
    }

    public GetPropertiesEnvelope GetProperties(string refValue)
    {
        _session.EnsureNotExpired();
        _session.TouchActivity();

        var (windowIndex, _) = RefFormat.Parse(refValue);
        var snapshotId = _session.GetCurrentSnapshotId(windowIndex);
        var node = _snapshotBuilder.ReadProperties(_session.RefTable.Resolve(refValue, snapshotId));
        return GetPropertiesEnvelope.From(node);
    }

    public object Click(string? refValue, int? pointX, int? pointY, string button, int clickCount, bool includeSnapshot)
    {
        _session.EnsureNotExpired();
        ActionResult result;

        if (!string.IsNullOrEmpty(refValue))
        {
            var (windowIndex, _) = RefFormat.Parse(refValue);
            var snapshotId = _session.GetCurrentSnapshotId(windowIndex);
            var entry = _session.RefTable.Resolve(refValue, snapshotId);
            result = _patternExecutor.Click(entry, button, clickCount);
        }
        else if (pointX.HasValue && pointY.HasValue)
        {
            _inputSimulator.ClickAt(pointX.Value, pointY.Value, button, clickCount);
            result = new ActionResult { Method = "SendInput" };
        }
        else
        {
            throw new ToolException(ErrorCodes.InvalidArgument, "Either ref or point is required.");
        }

        _session.TouchActivity();
        return BuildActionResponse(result.Method, refValue, includeSnapshot);
    }

    public object Drag(
        string? sourceRef,
        int? startX,
        int? startY,
        string? targetRef,
        int? endX,
        int? endY,
        string button,
        bool includeSnapshot)
    {
        _session.EnsureNotExpired();
        RefEntry? sourceEntry = null;
        RefEntry? targetEntry = null;

        if (!string.IsNullOrEmpty(sourceRef))
        {
            var (wIdx, _) = RefFormat.Parse(sourceRef);
            sourceEntry = _session.RefTable.Resolve(sourceRef, _session.GetCurrentSnapshotId(wIdx));
        }

        if (!string.IsNullOrEmpty(targetRef))
        {
            var (wIdx, _) = RefFormat.Parse(targetRef);
            targetEntry = _session.RefTable.Resolve(targetRef, _session.GetCurrentSnapshotId(wIdx));
        }

        var result = _patternExecutor.Drag(sourceEntry, startX, startY, targetEntry, endX, endY, button);
        _session.TouchActivity();
        return BuildActionResponse(result.Method, sourceRef ?? targetRef, includeSnapshot);
    }

    public object Scroll(
        string? refValue,
        int? pointX,
        int? pointY,
        string direction,
        int amount,
        bool includeSnapshot)
    {
        _session.EnsureNotExpired();
        RefEntry? entry = null;

        if (!string.IsNullOrEmpty(refValue))
        {
            var (wIdx, _) = RefFormat.Parse(refValue);
            entry = _session.RefTable.Resolve(refValue, _session.GetCurrentSnapshotId(wIdx));
        }

        var result = _patternExecutor.Scroll(entry, pointX, pointY, direction, amount);
        _session.TouchActivity();
        return BuildActionResponse(result.Method, refValue, includeSnapshot);
    }

    public object TypeText(string text, string? refValue, bool clearFirst, bool submit, bool includeSnapshot, string method = "auto")
    {
        _session.EnsureNotExpired();
        RefEntry? entry = null;

        if (!string.IsNullOrEmpty(refValue))
        {
            var (windowIndex, _) = RefFormat.Parse(refValue);
            var snapshotId = _session.GetCurrentSnapshotId(windowIndex);
            entry = _session.RefTable.Resolve(refValue, snapshotId);
        }

        var result = _patternExecutor.TypeText(entry, text, clearFirst, submit, method);
        _session.TouchActivity();
        return BuildActionResponse(result.Method, refValue, includeSnapshot);
    }

    public object SetValue(string refValue, string value, bool includeSnapshot)
    {
        _session.EnsureNotExpired();
        var (windowIndex, _) = RefFormat.Parse(refValue);
        var snapshotId = _session.GetCurrentSnapshotId(windowIndex);
        var entry = _session.RefTable.Resolve(refValue, snapshotId);
        _patternExecutor.SetValue(entry, value);
        _session.TouchActivity();
        return BuildActionResponse("ValuePattern", refValue, includeSnapshot, includeMethod: false);
    }

    public object Hotkey(string keys, int? handle, bool includeSnapshot)
    {
        _session.EnsureNotExpired();

        if (handle is > 0)
            _windowEnumerator.FocusWindow(handle.Value);

        _inputSimulator.SendHotkey(keys);
        _session.TouchActivity();
        return BuildActionResponse("SendInput", null, includeSnapshot);
    }

    public object PressKey(string key, int? handle, int count = 1, bool includeSnapshot = false)
    {
        _session.EnsureNotExpired();

        if (handle is > 0)
            _windowEnumerator.FocusWindow(handle.Value);

        for (var i = 0; i < Math.Max(1, count); i++)
            _inputSimulator.SendHotkey(key);

        _session.TouchActivity();
        return BuildActionResponse("SendInput", null, includeSnapshot);
    }

    public object Screenshot(int? handle, CaptureRegion? region, double scale)
    {
        _session.EnsureNotExpired();
        _session.TouchActivity();

        if (handle is 0)
            throw new ToolException(ErrorCodes.InvalidArgument, "handle must be a non-zero HWND integer.");

        var result = _windowCapture.Capture(handle, region, scale);
        return new
        {
            ok = true,
            imageBase64 = result.ImageBase64,
            mimeType = result.MimeType,
            width = result.Width,
            height = result.Height
        };
    }

    public object Toggle(string refValue, bool includeSnapshot)
    {
        _session.EnsureNotExpired();
        var (windowIndex, _) = RefFormat.Parse(refValue);
        var snapshotId = _session.GetCurrentSnapshotId(windowIndex);
        var entry = _session.RefTable.Resolve(refValue, snapshotId);
        var result = _patternExecutor.Toggle(entry);
        _session.TouchActivity();
        return BuildActionResponse(result.Method, refValue, includeSnapshot);
    }

    public object Select(string refValue, bool includeSnapshot)
    {
        _session.EnsureNotExpired();
        var (windowIndex, _) = RefFormat.Parse(refValue);
        var snapshotId = _session.GetCurrentSnapshotId(windowIndex);
        var entry = _session.RefTable.Resolve(refValue, snapshotId);
        var result = _patternExecutor.Select(entry);
        _session.TouchActivity();
        return BuildActionResponse(result.Method, refValue, includeSnapshot);
    }

    public object Expand(string refValue, bool includeSnapshot)
    {
        _session.EnsureNotExpired();
        var (windowIndex, _) = RefFormat.Parse(refValue);
        var snapshotId = _session.GetCurrentSnapshotId(windowIndex);
        var entry = _session.RefTable.Resolve(refValue, snapshotId);
        var result = _patternExecutor.Expand(entry);
        _session.TouchActivity();
        return BuildActionResponse(result.Method, refValue, includeSnapshot);
    }

    public object Collapse(string refValue, bool includeSnapshot)
    {
        _session.EnsureNotExpired();
        var (windowIndex, _) = RefFormat.Parse(refValue);
        var snapshotId = _session.GetCurrentSnapshotId(windowIndex);
        var entry = _session.RefTable.Resolve(refValue, snapshotId);
        var result = _patternExecutor.Collapse(entry);
        _session.TouchActivity();
        return BuildActionResponse(result.Method, refValue, includeSnapshot);
    }

    public object GetText(string refValue)
    {
        _session.EnsureNotExpired();
        var (windowIndex, _) = RefFormat.Parse(refValue);
        var snapshotId = _session.GetCurrentSnapshotId(windowIndex);
        var entry = _session.RefTable.Resolve(refValue, snapshotId);
        var text = _patternExecutor.GetText(entry);
        _session.TouchActivity();
        return new { ok = true, @ref = refValue, text };
    }

    public object InspectAt(int x, int y)
    {
        _session.EnsureNotExpired();
        _session.TouchActivity();
        var element = _snapshotBuilder.Automation.FromPoint(new System.Drawing.Point(x, y));
        if (element is null)
            throw new ToolException(ErrorCodes.ElementNotFound, $"No element found at point ({x}, {y}).");

        return new
        {
            ok = true,
            role = element.ControlType.ToString(),
            name = element.Name,
            automationId = element.AutomationId,
            className = element.ClassName,
            isEnabled = element.IsEnabled,
            isOffscreen = element.IsOffscreen
        };
    }

    public object WaitFor(int handle, string condition, string? target, int timeoutMs) =>
        WaitForAsync(handle, condition, target, timeoutMs).GetAwaiter().GetResult();

    public async Task<object> WaitForAsync(int handle, string condition, string? target, int timeoutMs,
        int settleMs = 200, long minBytes = 1, string? changedSinceUtc = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (timeoutMs < 0 || timeoutMs > 3600000 || settleMs < 0 || settleMs > 60000 || minBytes < 0)
            throw new ToolException(ErrorCodes.InvalidArgument, "Invalid timeout, quiet duration, or minimum file size.");
        DateTimeOffset? changedSince = null;
        if (changedSinceUtc is not null)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(changedSinceUtc, @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|[+-]\d{2}:\d{2})$") ||
                !DateTimeOffset.TryParse(changedSinceUtc, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal, out var timestamp))
                throw new ToolException(ErrorCodes.InvalidArgument, "changedSinceUtc must be an ISO timestamp.");
            changedSince = timestamp;
        }
        string[] known = ["process-exists", "file-exists", "file-stable", "window-active", "window-closed", "element-exists", "element-enabled", "text-visible", "element-gone", "element-hidden", "value-equals"];
        if (!known.Contains(condition)) throw new ToolException(ErrorCodes.InvalidArgument, $"Unknown condition: {condition}");
        var osLevel = OsWaitConditions.IsOsLevelCondition(condition);
        if (!osLevel) await Dispatcher.InvokeAsync(() => { _session.EnsureNotExpired(); _windowEnumerator.ValidateHandle(handle); return true; }, cancellationToken);
        else if (string.IsNullOrWhiteSpace(target))
            throw new ToolException(ErrorCodes.InvalidArgument, "target is required for OS-level waits.");
        var readiness = new FileReadiness();
        var stopwatch = Stopwatch.StartNew();
        var delayMs = 50;
        while (stopwatch.ElapsedMilliseconds <= timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (osLevel)
            {
                var ready = condition == "file-stable"
                    ? readiness.Observe(target!, settleMs, minBytes, changedSince)
                    : OsWaitConditions.TryEvaluate(condition, target, out _);
                if (ready) return new { ok = true, condition, target, elapsedMs = (int)stopwatch.ElapsedMilliseconds };
            }
            else
            {
                if (condition != "window-closed" && !NativeMethods.IsWindow(handle))
                    throw new ToolException(ErrorCodes.WindowGone, "Window closed while waiting.");
                try
                {
                    var outcome = await Dispatcher.InvokeAsync(() => EvaluateWaitCondition(handle, condition, target), cancellationToken);
                    if (outcome is not null)
                    {
                        if (RefFormat.IsValid(outcome.Value.Ref)) return new { ok = true, outcome.Value.Ref, elapsedMs = (int)stopwatch.ElapsedMilliseconds };
                        return new { ok = true, condition, target, elapsedMs = (int)stopwatch.ElapsedMilliseconds };
                    }
                }
                catch (ToolException ex) when (ex.Error.Code == ErrorCodes.ElementNotFound) { }
            }
            var remaining = timeoutMs - (int)stopwatch.ElapsedMilliseconds;
            if (remaining <= 0) break;
            await Task.Delay(Math.Min(delayMs, remaining), cancellationToken);
            delayMs = Math.Min(delayMs * 2, 500);
        }
        throw new ToolException(ErrorCodes.Timeout, $"wait_for timed out after {timeoutMs} ms.");
    }

    private (string Ref, string Role, string? Name)? EvaluateWaitCondition(int handle, string condition, string? target)
    {
        return condition switch
        {
            "window-active" when NativeMethods.GetForegroundWindow() == (nint)handle => ("active", "Window", "active"),
            "window-closed" when !NativeMethods.IsWindow(handle) => ("closed", "Window", "closed"),
            "element-exists" => FindForWait(handle, target, requireEnabled: false, requireVisible: false),
            "element-enabled" => FindForWait(handle, target, requireEnabled: true, requireVisible: false),
            "text-visible" => FindForWait(handle, target, requireEnabled: false, requireVisible: true),
            "element-gone" => FindGoneForWait(handle, target),
            "element-hidden" => FindGoneForWait(handle, target),
            "value-equals" => FindValueEqualsForWait(handle, target),
            _ => throw new ToolException(ErrorCodes.InvalidArgument, $"Unknown condition: {condition}")
        };
    }

    private (string Ref, string Role, string? Name)? FindForWait(int handle, string? target, bool requireEnabled, bool requireVisible)
    {
        if (string.IsNullOrWhiteSpace(target))
            throw new ToolException(ErrorCodes.InvalidArgument, "target is required for this condition.");

        var windowIndex = _session.TryGetWindowIndex(handle);
        var snapshotId = windowIndex.HasValue ? _session.GetCurrentSnapshotId(windowIndex.Value) : null;

        // 2. Targeted live UIA search without full snapshot rebuild
        FlaUI.Core.AutomationElements.AutomationElement? root;
        try
        {
            root = _snapshotBuilder.Automation.FromHandle((nint)handle);
            if (root is null) return null;
        }
        catch
        {
            return null;
        }

        var cf = _snapshotBuilder.Automation.ConditionFactory;
        var match = root.FindFirstDescendant(cf.ByName(target).Or(cf.ByAutomationId(target)));
        if (match is null)
        {
            match = root.FindAllDescendants().FirstOrDefault(e =>
                SafeProbe(() => e.Name?.Contains(target, StringComparison.OrdinalIgnoreCase) == true) ||
                SafeProbe(() => e.AutomationId?.Contains(target, StringComparison.OrdinalIgnoreCase) == true));
        }

        if (match is null)
            return null;

        if (requireEnabled && !match.IsEnabled)
            return null;

        if (requireVisible && match.IsOffscreen)
            return null;

        // Take a single snapshot only upon successful match to assign fresh valid refs
        var envelope = Snapshot(handle, "window", "control", includeOffscreen: true);
        var registeredWinIdx = _session.TryGetWindowIndex(handle)!;
        var snapGuid = Guid.Parse(envelope.SnapshotId);
        try
        {
            var freshEntry = _session.RefTable.FindElement(registeredWinIdx.Value, snapGuid, "name", target, "contains", 0);
            return (freshEntry.Ref, freshEntry.Node.Role, freshEntry.Node.Name);
        }
        catch
        {
            var node = _snapshotBuilder.RegisterElement(_session, handle, match);
            return (node.Ref, node.Role, node.Name);
        }
    }

    private (string Ref, string Role, string? Name)? FindGoneForWait(int handle, string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
            throw new ToolException(ErrorCodes.InvalidArgument, "target is required for this condition.");

        FlaUI.Core.AutomationElements.AutomationElement? root;
        try
        {
            root = _snapshotBuilder.Automation.FromHandle((nint)handle);
            if (root is null) return ("gone", "Element", target);
        }
        catch
        {
            return ("gone", "Element", target);
        }

        var cf = _snapshotBuilder.Automation.ConditionFactory;
        var match = root.FindFirstDescendant(cf.ByName(target).Or(cf.ByAutomationId(target)));
        if (match is null || match.IsOffscreen)
            return ("gone", "Element", target);

        return null;
    }

    private (string Ref, string Role, string? Name)? FindValueEqualsForWait(int handle, string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
            throw new ToolException(ErrorCodes.InvalidArgument, "target is required for this condition.");

        FlaUI.Core.AutomationElements.AutomationElement? root;
        try
        {
            root = _snapshotBuilder.Automation.FromHandle((nint)handle);
            if (root is null) return null;
        }
        catch
        {
            return null;
        }

        var match = root.FindAllDescendants().FirstOrDefault(e =>
            SafeProbe(() => e.Patterns.Value.IsSupported && e.Patterns.Value.Pattern.Value.Value == target) ||
            SafeProbe(() => e.Name == target));

        if (match is not null)
        {
            var node = _snapshotBuilder.RegisterElement(_session, handle, match);
            return (node.Ref, node.Role, node.Name);
        }

        return null;
    }

    private static bool SafeProbe(Func<bool> probe)
    {
        try
        {
            return probe();
        }
        catch (FlaUI.Core.Exceptions.PropertyNotSupportedException)
        {
            return false;
        }
        catch (FlaUI.Core.Exceptions.PatternNotSupportedException)
        {
            return false;
        }
    }

    private object BuildActionResponse(string method, string? refValue, bool includeSnapshot, bool includeMethod = true)
    {
        if (!includeSnapshot)
        {
            if (includeMethod)
                return new { ok = true, method };
            return new { ok = true };
        }

        if (string.IsNullOrEmpty(refValue))
            return includeMethod ? new { ok = true, method } : new { ok = true };

        var (windowIndex, _) = RefFormat.Parse(refValue);
        var handle = (int)_session.GetHandle(windowIndex);
        var snapshot = Snapshot(handle, "window", "control", includeOffscreen: false);

        if (includeMethod)
        {
            return new
            {
                ok = true,
                method,
                snapshotDelta = snapshot
            };
        }

        return new { ok = true, snapshotDelta = snapshot };
    }
}
