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

    public ToolService(
        SnapshotSession session,
        WindowEnumerator windowEnumerator,
        UiaSnapshotBuilder snapshotBuilder,
        PatternExecutor patternExecutor,
        InputSimulator inputSimulator,
        WindowCapture windowCapture)
    {
        _session = session;
        _windowEnumerator = windowEnumerator;
        _snapshotBuilder = snapshotBuilder;
        _patternExecutor = patternExecutor;
        _inputSimulator = inputSimulator;
        _windowCapture = windowCapture;
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

    public SnapshotEnvelope Snapshot(int handle, string scope, string detail, bool includeOffscreen)
    {
        _session.EnsureNotExpired();
        _windowEnumerator.ValidateHandle(handle);
        return _snapshotBuilder.Build(_session, handle, scope, detail, includeOffscreen);
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
        var node = _session.RefTable.GetProperties(refValue, snapshotId);
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

    public object TypeText(string text, string? refValue, bool clearFirst, bool submit, bool includeSnapshot)
    {
        _session.EnsureNotExpired();
        RefEntry? entry = null;

        if (!string.IsNullOrEmpty(refValue))
        {
            var (windowIndex, _) = RefFormat.Parse(refValue);
            var snapshotId = _session.GetCurrentSnapshotId(windowIndex);
            entry = _session.RefTable.Resolve(refValue, snapshotId);
        }

        var result = _patternExecutor.TypeText(entry, text, clearFirst, submit);
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

    public object WaitFor(int handle, string condition, string? target, int timeoutMs)
    {
        _session.EnsureNotExpired();
        _windowEnumerator.ValidateHandle(handle);

        var sw = Stopwatch.StartNew();
        var delayMs = 100;

        while (sw.ElapsedMilliseconds <= timeoutMs)
        {
            if (!NativeMethods.IsWindow(handle))
                throw new ToolException(ErrorCodes.WindowGone, "Window closed while waiting.");

            try
            {
                var outcome = EvaluateWaitCondition(handle, condition, target);
                if (outcome is not null)
                {
                    return new { ok = true, outcome.Value.Ref, elapsedMs = (int)sw.ElapsedMilliseconds };
                }
            }
            catch (ToolException)
            {
                // Keep polling for element conditions.
            }

            Thread.Sleep(delayMs);
            delayMs = Math.Min(delayMs * 2, 500);
        }

        throw new ToolException(ErrorCodes.Timeout, $"wait_for timed out after {timeoutMs} ms.");
    }

    private (string Ref, string Role, string? Name)? EvaluateWaitCondition(int handle, string condition, string? target)
    {
        return condition switch
        {
            "window-active" when NativeMethods.GetForegroundWindow() == (nint)handle => ("active", "Window", "active"),
            "element-exists" => FindForWait(handle, target),
            "element-enabled" => FindEnabledForWait(handle, target),
            "text-visible" => FindForWait(handle, target),
            _ => throw new ToolException(ErrorCodes.InvalidArgument, $"Unknown condition: {condition}")
        };
    }

    private (string Ref, string Role, string? Name)? FindForWait(int handle, string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
            throw new ToolException(ErrorCodes.InvalidArgument, "target is required for this condition.");

        var envelope = Snapshot(handle, "window", "control", includeOffscreen: true);
        var windowIndex = _session.TryGetWindowIndex(handle)!;
        var snapshotId = Guid.Parse(envelope.SnapshotId);

        try
        {
            var entry = _session.RefTable.FindElement(windowIndex.Value, snapshotId, "name", target, "contains", 0);
            return (entry.Ref, entry.Node.Role, entry.Node.Name);
        }
        catch (ToolException ex) when (ex.Error.Code == ErrorCodes.ElementNotFound)
        {
            return null;
        }
    }

    private (string Ref, string Role, string? Name)? FindEnabledForWait(int handle, string? target)
    {
        var found = FindForWait(handle, target);
        if (found is null)
            return null;

        var (windowIndex, _) = RefFormat.Parse(found.Value.Ref);
        var snapshotId = _session.GetCurrentSnapshotId(windowIndex);
        var entry = _session.RefTable.Resolve(found.Value.Ref, snapshotId);
        return entry.Element.IsEnabled ? found : null;
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
