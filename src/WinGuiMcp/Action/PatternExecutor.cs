using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Exceptions;
using Microsoft.Extensions.Logging;
using WinGuiMcp.Common;
using WinGuiMcp.Platform;
using WinGuiMcp.Session;

namespace WinGuiMcp.Action;

public sealed class PatternExecutor
{
    private readonly InputSimulator _inputSimulator;
    private readonly WindowEnumerator _windowEnumerator;
    private readonly ILogger<PatternExecutor>? _logger;

    public PatternExecutor(
        InputSimulator inputSimulator,
        WindowEnumerator windowEnumerator,
        ILogger<PatternExecutor>? logger = null)
    {
        _inputSimulator = inputSimulator;
        _windowEnumerator = windowEnumerator;
        _logger = logger;
    }

    public ActionResult Click(RefEntry entry, string button, int clickCount)
    {
        if (!entry.Element.IsEnabled)
            throw new ToolException(ErrorCodes.ElementNotEnabled, $"Element {entry.Ref} is not enabled.");

        try
        {
            if (entry.Element.IsOffscreen)
                throw new ToolException(ErrorCodes.VisibilityMismatch, $"Element {entry.Ref} is offscreen.");
        }
        catch (PropertyNotSupportedException)
        {
            // Some dialog controls (e.g. Save As filename) omit IsOffscreen.
        }

        if (button == "left" && clickCount == 1)
        {
            if (entry.Element.Patterns.Invoke.IsSupported)
            {
                entry.Element.Patterns.Invoke.Pattern.Invoke();
                return new ActionResult { Method = "InvokePattern" };
            }

            if (entry.Element.Patterns.Toggle.IsSupported)
            {
                entry.Element.Patterns.Toggle.Pattern.Toggle();
                return new ActionResult { Method = "TogglePattern" };
            }

            if (entry.Element.Patterns.SelectionItem.IsSupported)
            {
                entry.Element.Patterns.SelectionItem.Pattern.Select();
                return new ActionResult { Method = "SelectionItemPattern" };
            }

            if (entry.Element.Patterns.ExpandCollapse.IsSupported)
            {
                var ec = entry.Element.Patterns.ExpandCollapse.Pattern;
                if (ec.ExpandCollapseState.Value == ExpandCollapseState.Collapsed)
                    ec.Expand();
                else
                    ec.Collapse();
                return new ActionResult { Method = "ExpandCollapsePattern" };
            }
        }

        _windowEnumerator.FocusWindow(entry.Hwnd);
        EnsureBboxInCapturedFrame(entry);
        var (x, y) = entry.Bbox.Center;
        _inputSimulator.ClickAt(x, y, button, clickCount);
        return new ActionResult { Method = "SendInput" };
    }

    public ActionResult SetValue(RefEntry entry, string value)
    {
        if (entry.Element.Patterns.Value.IsSupported)
        {
            var valPattern = entry.Element.Patterns.Value.Pattern;
            var isReadOnly = false;
            try { isReadOnly = valPattern.IsReadOnly.Value; } catch { }
            if (!isReadOnly)
            {
                try
                {
                    entry.Element.Focus();
                    valPattern.SetValue(value);
                    return new ActionResult { Method = "ValuePattern" };
                }
                catch
                {
                    // Fall through if ValuePattern.SetValue fails.
                }
            }
        }

        if (entry.Element.Patterns.RangeValue.IsSupported)
        {
            if (double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var num) ||
                double.TryParse(value, out num))
            {
                entry.Element.Focus();
                entry.Element.Patterns.RangeValue.Pattern.SetValue(num);
                return new ActionResult { Method = "RangeValuePattern" };
            }
            throw new ToolException(ErrorCodes.InvalidArgument, $"Value '{value}' cannot be parsed as a number for RangeValuePattern.");
        }

        throw new ToolException(ErrorCodes.PatternNotSupported, $"Element {entry.Ref} does not support writable ValuePattern or RangeValuePattern.");
    }

    public ActionResult Toggle(RefEntry entry)
    {
        if (!entry.Element.Patterns.Toggle.IsSupported)
            throw new ToolException(ErrorCodes.PatternNotSupported, $"Element {entry.Ref} does not support TogglePattern.");
        entry.Element.Patterns.Toggle.Pattern.Toggle();
        return new ActionResult { Method = "TogglePattern" };
    }

    public ActionResult Select(RefEntry entry)
    {
        if (!entry.Element.Patterns.SelectionItem.IsSupported)
            throw new ToolException(ErrorCodes.PatternNotSupported, $"Element {entry.Ref} does not support SelectionItemPattern.");
        entry.Element.Patterns.SelectionItem.Pattern.Select();
        return new ActionResult { Method = "SelectionItemPattern" };
    }

    public ActionResult Expand(RefEntry entry)
    {
        if (!entry.Element.Patterns.ExpandCollapse.IsSupported)
            throw new ToolException(ErrorCodes.PatternNotSupported, $"Element {entry.Ref} does not support ExpandCollapsePattern.");
        entry.Element.Patterns.ExpandCollapse.Pattern.Expand();
        return new ActionResult { Method = "ExpandCollapsePattern" };
    }

    public ActionResult Collapse(RefEntry entry)
    {
        if (!entry.Element.Patterns.ExpandCollapse.IsSupported)
            throw new ToolException(ErrorCodes.PatternNotSupported, $"Element {entry.Ref} does not support ExpandCollapsePattern.");
        entry.Element.Patterns.ExpandCollapse.Pattern.Collapse();
        return new ActionResult { Method = "ExpandCollapsePattern" };
    }

    public string GetText(RefEntry entry)
    {
        if (entry.Element.Patterns.Text.IsSupported)
        {
            return entry.Element.Patterns.Text.Pattern.DocumentRange?.GetText(-1) ?? string.Empty;
        }
        if (entry.Element.Patterns.Value.IsSupported)
        {
            return entry.Element.Patterns.Value.Pattern.Value.Value ?? string.Empty;
        }
        return entry.Element.Name ?? string.Empty;
    }

    public ActionResult TypeText(RefEntry? entry, string text, bool clearFirst, bool submit, string method = "auto")
    {
        var normMethod = (method ?? "auto").ToLowerInvariant();
        if (normMethod is not ("auto" or "pattern" or "keyboard"))
            throw new ToolException(ErrorCodes.InvalidArgument, $"Invalid method '{method}'. Expected auto, pattern, or keyboard.");

        if (entry is not null && normMethod is "auto" or "pattern")
        {
            if (entry.Element.Patterns.Value.IsSupported)
            {
                var valPattern = entry.Element.Patterns.Value.Pattern;
                var isReadOnly = false;
                try { isReadOnly = valPattern.IsReadOnly.Value; } catch { }

                if (!isReadOnly)
                {
                    try
                    {
                        entry.Element.Focus();
                        var value = clearFirst
                            ? text
                            : (valPattern.Value.Value ?? string.Empty) + text;
                        valPattern.SetValue(value);
                        if (submit)
                            _inputSimulator.SendEnter();
                        return new ActionResult { Method = "ValuePattern" };
                    }
                    catch when (normMethod == "auto")
                    {
                        // Fall through to keyboard input when ValuePattern fails in auto mode.
                    }
                }
                else if (normMethod == "pattern")
                {
                    throw new ToolException(ErrorCodes.PatternNotSupported, $"Element {entry.Ref} ValuePattern is read-only.");
                }
            }
            else if (normMethod == "pattern")
            {
                throw new ToolException(ErrorCodes.PatternNotSupported, $"Element {entry.Ref} does not support ValuePattern.");
            }
        }

        if (entry is not null)
        {
            _windowEnumerator.FocusWindow(entry.Hwnd);
            var focused = false;
            try
            {
                entry.Element.Focus();
                focused = entry.Element.Properties.HasKeyboardFocus.TryGetValue(out var hf) && hf;
            }
            catch { }

            if (!focused && entry.Bbox.Width > 0 && entry.Bbox.Height > 0)
            {
                EnsureBboxInCapturedFrame(entry);
                var (cx, cy) = entry.Bbox.Center;
                _inputSimulator.ClickAt(cx, cy, "left", 1);
            }
        }

        if (clearFirst)
            _inputSimulator.SelectAll();

        _inputSimulator.TypeUnicode(text);
        if (submit)
            _inputSimulator.SendEnter();

        return new ActionResult { Method = "SendInput" };
    }

    public ActionResult Scroll(RefEntry? entry, int? pointX, int? pointY, string direction, int amount)
    {
        var normDir = direction.ToLowerInvariant();
        if (normDir is not ("up" or "down" or "left" or "right"))
            throw new ToolException(ErrorCodes.InvalidArgument, $"Invalid scroll direction '{direction}'. Expected up, down, left, or right.");

        if (entry is not null && entry.Element.Patterns.Scroll.IsSupported)
        {
            try
            {
                var scrollPattern = entry.Element.Patterns.Scroll.Pattern;
                var hAmount = normDir switch
                {
                    "left" => ScrollAmount.SmallDecrement,
                    "right" => ScrollAmount.SmallIncrement,
                    _ => ScrollAmount.NoAmount
                };
                var vAmount = normDir switch
                {
                    "up" => ScrollAmount.SmallDecrement,
                    "down" => ScrollAmount.SmallIncrement,
                    _ => ScrollAmount.NoAmount
                };
                for (var i = 0; i < Math.Max(1, amount); i++)
                    scrollPattern.Scroll(hAmount, vAmount);
                return new ActionResult { Method = "ScrollPattern" };
            }
            catch
            {
                // Fall back to mouse wheel simulation.
            }
        }

        int x, y;
        if (entry is not null)
        {
            _windowEnumerator.FocusWindow(entry.Hwnd);
            EnsureBboxInCapturedFrame(entry);
            (x, y) = entry.Bbox.Center;
        }
        else if (pointX.HasValue && pointY.HasValue)
        {
            x = pointX.Value;
            y = pointY.Value;
        }
        else
        {
            throw new ToolException(ErrorCodes.InvalidArgument, "Either ref or point (x, y) is required for scroll.");
        }

        _inputSimulator.ScrollWheel(x, y, normDir, Math.Max(1, amount));
        return new ActionResult { Method = "SendInput" };
    }

    public ActionResult Drag(
        RefEntry? sourceEntry,
        int? startX,
        int? startY,
        RefEntry? targetEntry,
        int? endX,
        int? endY,
        string button = "left")
    {
        int sx, sy, ex, ey;
        if (sourceEntry is not null)
        {
            _windowEnumerator.FocusWindow(sourceEntry.Hwnd);
            EnsureBboxInCapturedFrame(sourceEntry);
            (sx, sy) = sourceEntry.Bbox.Center;
        }
        else if (startX.HasValue && startY.HasValue)
        {
            sx = startX.Value;
            sy = startY.Value;
        }
        else
        {
            throw new ToolException(ErrorCodes.InvalidArgument, "Source ref or (startX, startY) is required for drag.");
        }

        if (targetEntry is not null)
        {
            EnsureBboxInCapturedFrame(targetEntry);
            (ex, ey) = targetEntry.Bbox.Center;
        }
        else if (endX.HasValue && endY.HasValue)
        {
            ex = endX.Value;
            ey = endY.Value;
        }
        else
        {
            throw new ToolException(ErrorCodes.InvalidArgument, "Target ref or (endX, endY) is required for drag.");
        }

        _inputSimulator.Drag(sx, sy, ex, ey, button);
        return new ActionResult { Method = "SendInput" };
    }

    private void EnsureBboxInCapturedFrame(RefEntry entry)
    {
        if (!NativeMethods.GetWindowRect(entry.Hwnd, out var frame) || frame.Width <= 0 || frame.Height <= 0)
            throw new ToolException(ErrorCodes.CaptureFailed, "Could not read the window frame before SendInput.");

        var overflow = FrameReconciler.Measure(entry.Bbox, frame.Left, frame.Top, frame.Right, frame.Bottom);
        if (overflow.Max > FrameReconciler.LogThresholdPx)
        {
            _logger?.LogWarning(
                "UIA bbox vs capture frame disagree by {OverflowPx} px (left={Left}, top={Top}, right={Right}, bottom={Bottom}) hwnd={Hwnd}",
                overflow.Max,
                overflow.Left,
                overflow.Top,
                overflow.Right,
                overflow.Bottom,
                entry.Hwnd);
        }

        if (!FrameReconciler.CenterInside(entry.Bbox, frame.Left, frame.Top, frame.Right, frame.Bottom))
        {
            throw new ToolException(
                ErrorCodes.VisibilityMismatch,
                $"Element {entry.Ref} bbox center is outside the captured window frame.");
        }
    }
}

public sealed class ActionResult
{
    public required string Method { get; init; }
}
