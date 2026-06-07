using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using WinGuiMcp.Common;
using WinGuiMcp.Platform;
using WinGuiMcp.Session;

namespace WinGuiMcp.Perception;

public sealed class UiaSnapshotBuilder : IDisposable
{
    private const int MaxDepth = 32;

    private static readonly HashSet<ControlType> InteractiveTypes =
    [
        ControlType.Button,
        ControlType.CheckBox,
        ControlType.ComboBox,
        ControlType.Edit,
        ControlType.Hyperlink,
        ControlType.ListItem,
        ControlType.MenuItem,
        ControlType.RadioButton,
        ControlType.Slider,
        ControlType.Spinner,
        ControlType.TabItem,
        ControlType.TreeItem,
        ControlType.DataItem,
        ControlType.Document
    ];

    private readonly UIA3Automation _automation = new();

    public SnapshotEnvelope Build(
        SnapshotSession session,
        nint handle,
        string scope,
        string detail,
        bool includeOffscreen)
    {
        if (handle == 0)
            throw new ToolException(ErrorCodes.InvalidArgument, "handle must be a non-zero HWND integer.");

        AutomationElement root;
        try
        {
            root = _automation.FromHandle(handle);
        }
        catch (Exception ex)
        {
            throw new ToolException(ErrorCodes.WindowGone, $"Could not attach to window: {ex.Message}");
        }

        if (scope == "focused-control")
        {
            var focused = _automation.FocusedElement();
            if (focused is not null)
                root = focused;
        }

        var windowIndex = session.RegisterWindow(handle);
        var snapshotId = Guid.NewGuid();
        session.SetSnapshotId(windowIndex, snapshotId);

        var elementIndex = 0;
        var tree = WalkElement(root, windowIndex, ref elementIndex, snapshotId, handle, detail, includeOffscreen, 0, session);

        NativeMethods.GetWindowThreadProcessId(handle, out var pid);

        return new SnapshotEnvelope
        {
            SnapshotId = snapshotId.ToString(),
            Handle = (int)handle,
            Title = WindowEnumerator.GetWindowTitle(handle),
            ProcessName = GetProcessName((int)pid),
            Timestamp = DateTime.UtcNow.ToString("O"),
            Tree = tree
        };
    }

    private UemNode WalkElement(
        AutomationElement element,
        int windowIndex,
        ref int elementIndex,
        Guid snapshotId,
        nint hwnd,
        string detail,
        bool includeOffscreen,
        int depth,
        SnapshotSession session)
    {
        elementIndex++;
        var refValue = RefFormat.Format(windowIndex, elementIndex);
        var patterns = GetSupportedPatterns(element);
        var rect = element.BoundingRectangle;
        var bbox = Bbox.FromRectangle(rect.Left, rect.Top, rect.Right, rect.Bottom);

        string? value = null;
        try
        {
            if (element.Patterns.Value.IsSupported)
                value = element.Patterns.Value.Pattern.Value.Value;
        }
        catch
        {
            // Value may be unavailable.
        }

        var isOffscreen = TryGetIsOffscreen(element);
        var states = new List<string>();
        if (isOffscreen)
            states.Add("offscreen");
        if (element.Properties.HasKeyboardFocus.TryGetValue(out var hasFocus) && hasFocus)
            states.Add("focused");

        var children = new List<UemNode>();
        if (depth < MaxDepth)
        {
            foreach (var child in element.FindAllChildren())
            {
                try
                {
                    if (!includeOffscreen && TryGetIsOffscreen(child))
                        continue;

                    if (!ShouldIncludeNode(child, detail, includeOffscreen))
                        continue;

                    if (ShouldHoist(child))
                    {
                        var visibleChildren = child.FindAllChildren()
                            .Where(c => includeOffscreen || !TryGetIsOffscreen(c))
                            .Where(c => ShouldIncludeNode(c, detail, includeOffscreen))
                            .ToList();

                        if (visibleChildren.Count == 1)
                        {
                            children.Add(WalkElement(visibleChildren[0], windowIndex, ref elementIndex, snapshotId, hwnd, detail, includeOffscreen, depth + 1, session));
                            continue;
                        }
                    }

                    children.Add(WalkElement(child, windowIndex, ref elementIndex, snapshotId, hwnd, detail, includeOffscreen, depth + 1, session));
                }
                catch
                {
                    // Skip children that fail UIA property queries (transient Explorer/Notepad nodes).
                }
            }
        }
        else
        {
            states.Add("truncated");
        }

        var node = CreateNode(refValue, element, value, patterns, states, children, includeEnabledWhenFalse: !TryGetIsEnabled(element));

        session.RefTable.Add(new RefEntry
        {
            Ref = refValue,
            Hwnd = hwnd,
            Element = element,
            Bbox = bbox,
            Source = "uia",
            Patterns = patterns,
            SnapshotId = snapshotId,
            Node = node
        });

        return node;
    }

    private static UemNode CreateNode(
        string refValue,
        AutomationElement element,
        string? value,
        List<string> patterns,
        List<string> states,
        List<UemNode> children,
        bool includeEnabledWhenFalse)
    {
        var node = new UemNode
        {
            Ref = refValue,
            Source = "uia",
            Role = TryGetControlType(element)?.ToString() ?? "Unknown",
            Name = NullIfEmpty(TryGetName(element)),
            AutomationId = NullIfEmpty(TryGetAutomationId(element)),
            ClassName = NullIfEmpty(TryGetClassName(element)),
            Value = string.IsNullOrEmpty(value) ? null : value,
            Enabled = includeEnabledWhenFalse ? false : null,
            States = states.Count > 0 ? states : null,
            Patterns = patterns.Count > 0 ? patterns : null,
            Children = children.Count > 0 ? children : null
        };

        return node;
    }

    private bool ShouldIncludeNode(AutomationElement element, string detail, bool includeOffscreen)
    {
        try
        {
            if (!includeOffscreen && TryGetIsOffscreen(element))
                return false;

            var controlType = TryGetControlType(element);
            if (controlType is not null && InteractiveTypes.Contains(controlType.Value))
                return true;

            if (HasInteractionPattern(element))
                return true;

            var name = TryGetName(element);
            var automationId = TryGetAutomationId(element);
            if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(automationId))
                return true;

            if (detail == "content")
                return true;

            return element.FindAllChildren().Any(c => ShouldIncludeNode(c, detail, includeOffscreen));
        }
        catch
        {
            return false;
        }
    }

    private static bool ShouldHoist(AutomationElement element)
    {
        var controlType = TryGetControlType(element);
        if (controlType != ControlType.Pane && controlType != ControlType.Group)
            return false;

        if (!string.IsNullOrEmpty(TryGetName(element)) || !string.IsNullOrEmpty(TryGetAutomationId(element)))
            return false;

        return !HasInteractionPattern(element);
    }

    private static bool HasInteractionPattern(AutomationElement element) =>
        element.Patterns.Invoke.IsSupported ||
        element.Patterns.Value.IsSupported ||
        element.Patterns.Toggle.IsSupported ||
        element.Patterns.Selection.IsSupported ||
        element.Patterns.ExpandCollapse.IsSupported ||
        element.Patterns.Scroll.IsSupported;

    private static List<string> GetSupportedPatterns(AutomationElement element)
    {
        var patterns = new List<string>();
        if (element.Patterns.Invoke.IsSupported) patterns.Add("Invoke");
        if (element.Patterns.Value.IsSupported) patterns.Add("Value");
        if (element.Patterns.Toggle.IsSupported) patterns.Add("Toggle");
        if (element.Patterns.Selection.IsSupported) patterns.Add("Selection");
        if (element.Patterns.ExpandCollapse.IsSupported) patterns.Add("ExpandCollapse");
        if (element.Patterns.Scroll.IsSupported) patterns.Add("Scroll");
        return patterns;
    }

    private static string GetProcessName(int pid)
    {
        try
        {
            return Process.GetProcessById(pid).ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

    private static string TryGetName(AutomationElement element) => TryGetString(() => element.Name);

    private static string TryGetAutomationId(AutomationElement element) => TryGetString(() => element.AutomationId);

    private static string TryGetClassName(AutomationElement element) => TryGetString(() => element.ClassName);

    private static bool TryGetIsEnabled(AutomationElement element)
    {
        try
        {
            return element.Properties.IsEnabled.IsSupported && element.IsEnabled;
        }
        catch
        {
            return true;
        }
    }

    private static ControlType? TryGetControlType(AutomationElement element)
    {
        try
        {
            return element.ControlType;
        }
        catch
        {
            return null;
        }
    }

    private static string TryGetString(Func<string> getter)
    {
        try
        {
            return getter() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool TryGetIsOffscreen(AutomationElement element)
    {
        try
        {
            return element.Properties.IsOffscreen.IsSupported && element.IsOffscreen;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose() => _automation.Dispose();
}
