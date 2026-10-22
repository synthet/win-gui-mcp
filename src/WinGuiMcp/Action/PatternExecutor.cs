using FlaUI.Core.AutomationElements;
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

        if (entry.Element.Patterns.Invoke.IsSupported)
        {
            for (var i = 0; i < clickCount; i++)
                entry.Element.Patterns.Invoke.Pattern.Invoke();
            return new ActionResult { Method = "InvokePattern" };
        }

        if (entry.Element.Patterns.Toggle.IsSupported)
        {
            for (var i = 0; i < clickCount; i++)
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
            if (ec.ExpandCollapseState.Value == FlaUI.Core.Definitions.ExpandCollapseState.Collapsed)
                ec.Expand();
            else
                ec.Collapse();
            return new ActionResult { Method = "ExpandCollapsePattern" };
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
            entry.Element.Focus();
            entry.Element.Patterns.Value.Pattern.SetValue(value);
            return new ActionResult { Method = "ValuePattern" };
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

        throw new ToolException(ErrorCodes.PatternNotSupported, $"Element {entry.Ref} does not support ValuePattern or RangeValuePattern.");
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

    public ActionResult TypeText(RefEntry? entry, string text, bool clearFirst, bool submit)
    {
        if (entry is not null)
        {
            entry.Element.Focus();

            if (entry.Element.Patterns.Value.IsSupported)
            {
                var value = clearFirst
                    ? text
                    : (entry.Element.Patterns.Value.Pattern.Value.Value ?? string.Empty) + text;
                entry.Element.Patterns.Value.Pattern.SetValue(value);
                if (submit)
                    _inputSimulator.SendEnter();
                return new ActionResult { Method = "ValuePattern" };
            }
        }

        if (entry is not null)
            _windowEnumerator.FocusWindow(entry.Hwnd);

        if (clearFirst)
            _inputSimulator.SelectAll();

        _inputSimulator.TypeUnicode(text);
        if (submit)
            _inputSimulator.SendEnter();

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
