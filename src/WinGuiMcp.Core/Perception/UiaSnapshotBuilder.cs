using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using WinGuiMcp.Common;
using WinGuiMcp.Platform;
using WinGuiMcp.Session;

namespace WinGuiMcp.Perception;

public sealed class UiaSnapshotBuilder : IDisposable
{
    private const int DefaultMaxDepth = 32;

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

    private readonly Lazy<UIA3Automation> _automation = new(() => new UIA3Automation());

    public UIA3Automation Automation => _automation.Value;

    public CacheRequest CreateCacheRequest()
    {
        var cr = new CacheRequest
        {
            AutomationElementMode = AutomationElementMode.Full,
            TreeScope = TreeScope.Element | TreeScope.Children
        };

        cr.Add(Automation.PropertyLibrary.Element.BoundingRectangle);
        cr.Add(Automation.PropertyLibrary.Element.Name);
        cr.Add(Automation.PropertyLibrary.Element.AutomationId);
        cr.Add(Automation.PropertyLibrary.Element.ControlType);
        cr.Add(Automation.PropertyLibrary.Element.ClassName);
        cr.Add(Automation.PropertyLibrary.Element.IsEnabled);
        cr.Add(Automation.PropertyLibrary.Element.HasKeyboardFocus);
        cr.Add(Automation.PropertyLibrary.Element.IsOffscreen);

        cr.Add(Automation.PatternLibrary.InvokePattern);
        cr.Add(Automation.PatternLibrary.ValuePattern);
        cr.Add(Automation.PatternLibrary.TogglePattern);
        cr.Add(Automation.PatternLibrary.SelectionPattern);
        cr.Add(Automation.PatternLibrary.SelectionItemPattern);
        cr.Add(Automation.PatternLibrary.ExpandCollapsePattern);
        cr.Add(Automation.PatternLibrary.ScrollPattern);
        cr.Add(Automation.PatternLibrary.RangeValuePattern);
        cr.Add(Automation.PatternLibrary.TextPattern);

        return cr;
    }

    public SnapshotEnvelope Build(
        SnapshotSession session,
        nint handle,
        string scope,
        string detail,
        bool includeOffscreen,
        string? rootRef = null,
        int maxDepth = DefaultMaxDepth)
    {
        if (handle == 0)
            throw new ToolException(ErrorCodes.InvalidArgument, "handle must be a non-zero HWND integer.");

        AutomationElement root;
        if (!string.IsNullOrEmpty(rootRef))
        {
            var (wIdx, _) = RefFormat.Parse(rootRef);
            var snapId = session.GetCurrentSnapshotId(wIdx);
            var entry = session.RefTable.Resolve(rootRef, snapId);
            root = entry.Element;
        }
        else
        {
            try
            {
                root = Automation.FromHandle(handle);
            }
            catch (Exception ex)
            {
                throw new ToolException(ErrorCodes.WindowGone, $"Could not attach to window: {ex.Message}");
            }

            if (scope == "focused-control")
            {
                var focused = Automation.FocusedElement();
                if (focused is not null)
                    root = focused;
            }
        }

        var windowIndex = session.RegisterWindow(handle);
        var snapshotId = Guid.NewGuid();
        session.SetSnapshotId(windowIndex, snapshotId);

        var elementIndex = 0;
        var tree = WalkElement(root, windowIndex, ref elementIndex, snapshotId, handle, detail, includeOffscreen, 0, maxDepth, session);

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
        int maxDepth,
        SnapshotSession session)
    {
        elementIndex++;
        var refValue = session.AllocateElementRef(windowIndex);
        var patterns = GetSupportedPatterns(element);
        var bbox = TryGetBbox(element);

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
        try
        {
            if (element.Properties.HasKeyboardFocus.TryGetValue(out var hasFocus) && hasFocus)
                states.Add("focused");
        }
        catch { }

        var children = new List<UemNode>();
        if (depth < maxDepth)
        {
            AutomationElement[] rawChildren = [];
            try
            {
                rawChildren = element.FindAllChildren();
            }
            catch { }

            foreach (var child in rawChildren)
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
                            children.Add(WalkElement(visibleChildren[0], windowIndex, ref elementIndex, snapshotId, hwnd, detail, includeOffscreen, depth + 1, maxDepth, session));
                            continue;
                        }
                    }

                    children.Add(WalkElement(child, windowIndex, ref elementIndex, snapshotId, hwnd, detail, includeOffscreen, depth + 1, maxDepth, session));
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
            Value = IsProtected(element) ? null : string.IsNullOrEmpty(value) ? ReadRangeValue(element) : value,
            PatternValues = ReadPatternValues(element),
            Enabled = includeEnabledWhenFalse ? false : null,
            States = states.Count > 0 ? states : null,
            Patterns = patterns.Count > 0 ? patterns : null,
            Children = children.Count > 0 ? children : null
        };

        return node;
    }

    public UemNode ReadProperties(RefEntry entry)
    {
        var element = entry.Element;
        var states = new List<string>();
        if (TryGetIsOffscreen(element)) states.Add("offscreen");
        try
        {
            if (element.Properties.HasKeyboardFocus.TryGetValue(out var focused) && focused) states.Add("focused");
        }
        catch { }

        string? value = null;
        try
        {
            if (!IsProtected(element) && element.Patterns.Value.IsSupported)
                value = element.Patterns.Value.Pattern.Value.Value;
        }
        catch { }

        return CreateNode(entry.Ref, element, value, GetSupportedPatterns(element), states, [], !TryGetIsEnabled(element));
    }

    public UemNode RegisterElement(SnapshotSession session, nint handle, AutomationElement element)
    {
        var windowIndex = session.RegisterWindow(handle);
        var snapshotId = session.GetCurrentSnapshotId(windowIndex) ?? session.SetSnapshotId(windowIndex, Guid.NewGuid());
        var index = 0;
        return WalkElement(element, windowIndex, ref index, snapshotId, handle, "control", true, 0, 0, session);
    }

    private static bool IsProtected(AutomationElement element)
    {
        try
        {
            return element.Properties.IsPassword.TryGetValue(out var protectedContent) && protectedContent;
        }
        catch
        {
            return false;
        }
    }

    public static double? SanitizeDouble(double val) => double.IsFinite(val) ? val : null;

    private static Bbox TryGetBbox(AutomationElement element)
    {
        try
        {
            var rect = element.BoundingRectangle;
            return Bbox.FromRectangle(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }
        catch
        {
            return new Bbox(0, 0, 0, 0);
        }
    }

    private static string? ReadRangeValue(AutomationElement element)
    {
        try
        {
            if (!element.Patterns.RangeValue.IsSupported) return null;
            var val = element.Patterns.RangeValue.Pattern.Value.Value;
            return double.IsFinite(val) ? val.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, object?>? ReadPatternValues(AutomationElement element)
    {
        if (IsProtected(element)) return null;
        var values = new Dictionary<string, object?>();
        try
        {
            if (element.Patterns.RangeValue.IsSupported)
            {
                var range = element.Patterns.RangeValue.Pattern;
                values["rangeValue"] = new
                {
                    value = SanitizeDouble(range.Value.Value),
                    minimum = SanitizeDouble(range.Minimum.Value),
                    maximum = SanitizeDouble(range.Maximum.Value),
                    smallChange = SanitizeDouble(range.SmallChange.Value),
                    readOnly = range.IsReadOnly.Value
                };
            }
        }
        catch { }

        try
        {
            if (element.Patterns.Toggle.IsSupported)
                values["toggle"] = element.Patterns.Toggle.Pattern.ToggleState.Value.ToString();
        }
        catch { }

        try
        {
            if (element.Patterns.SelectionItem.IsSupported)
                values["selected"] = element.Patterns.SelectionItem.Pattern.IsSelected.Value;
        }
        catch { }

        try
        {
            if (element.Patterns.ExpandCollapse.IsSupported)
                values["expandCollapse"] = element.Patterns.ExpandCollapse.Pattern.ExpandCollapseState.Value.ToString();
        }
        catch { }

        return values.Count == 0 ? null : values;
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

    private static bool HasInteractionPattern(AutomationElement element)
    {
        try
        {
            return element.Patterns.Invoke.IsSupported ||
                   element.Patterns.Value.IsSupported ||
                   element.Patterns.Toggle.IsSupported ||
                   element.Patterns.Selection.IsSupported ||
                   element.Patterns.SelectionItem.IsSupported ||
                   element.Patterns.ExpandCollapse.IsSupported ||
                   element.Patterns.Scroll.IsSupported ||
                   element.Patterns.RangeValue.IsSupported;
        }
        catch
        {
            return false;
        }
    }

    private static List<string> GetSupportedPatterns(AutomationElement element)
    {
        var patterns = new List<string>();
        try { if (element.Patterns.Invoke.IsSupported) patterns.Add("Invoke"); } catch { }
        try { if (element.Patterns.Value.IsSupported) patterns.Add("Value"); } catch { }
        try { if (element.Patterns.Toggle.IsSupported) patterns.Add("Toggle"); } catch { }
        try { if (element.Patterns.Selection.IsSupported) patterns.Add("Selection"); } catch { }
        try { if (element.Patterns.SelectionItem.IsSupported) patterns.Add("SelectionItem"); } catch { }
        try { if (element.Patterns.ExpandCollapse.IsSupported) patterns.Add("ExpandCollapse"); } catch { }
        try { if (element.Patterns.Scroll.IsSupported) patterns.Add("Scroll"); } catch { }
        try { if (element.Patterns.RangeValue.IsSupported) patterns.Add("RangeValue"); } catch { }
        try { if (element.Patterns.Text.IsSupported) patterns.Add("Text"); } catch { }
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

    public void Dispose() { if (_automation.IsValueCreated) _automation.Value.Dispose(); }
}
