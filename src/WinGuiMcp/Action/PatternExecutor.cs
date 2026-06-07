using FlaUI.Core.AutomationElements;
using WinGuiMcp.Common;
using WinGuiMcp.Platform;
using WinGuiMcp.Session;

namespace WinGuiMcp.Action;

public sealed class PatternExecutor
{
    private readonly InputSimulator _inputSimulator;
    private readonly WindowEnumerator _windowEnumerator;

    public PatternExecutor(InputSimulator inputSimulator, WindowEnumerator windowEnumerator)
    {
        _inputSimulator = inputSimulator;
        _windowEnumerator = windowEnumerator;
    }

    public ActionResult Click(RefEntry entry, string button, int clickCount)
    {
        if (!entry.Element.IsEnabled)
            throw new ToolException(ErrorCodes.ElementNotEnabled, $"Element {entry.Ref} is not enabled.");

        if (entry.Element.IsOffscreen)
            throw new ToolException(ErrorCodes.VisibilityMismatch, $"Element {entry.Ref} is offscreen.");

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

        _windowEnumerator.FocusWindow(entry.Hwnd);
        var (x, y) = entry.Bbox.Center;
        _inputSimulator.ClickAt(x, y, button, clickCount);
        return new ActionResult { Method = "SendInput" };
    }

    public ActionResult SetValue(RefEntry entry, string value)
    {
        if (!entry.Element.Patterns.Value.IsSupported)
            throw new ToolException(ErrorCodes.PatternNotSupported, $"Element {entry.Ref} does not support ValuePattern.");

        entry.Element.Patterns.Value.Pattern.SetValue(value);
        return new ActionResult { Method = "ValuePattern" };
    }

    public ActionResult TypeText(RefEntry? entry, string text, bool clearFirst, bool submit)
    {
        if (entry is not null)
        {
            entry.Element.Focus();

            if (entry.Element.Patterns.Value.IsSupported && clearFirst)
            {
                entry.Element.Patterns.Value.Pattern.SetValue(text);
                if (submit)
                    _inputSimulator.SendEnter();
                return new ActionResult { Method = "ValuePattern" };
            }
        }

        if (entry is not null)
            _windowEnumerator.FocusWindow(entry.Hwnd);

        _inputSimulator.TypeUnicode(text);
        if (submit)
            _inputSimulator.SendEnter();

        return new ActionResult { Method = "SendInput" };
    }
}

public sealed class ActionResult
{
    public required string Method { get; init; }
}
