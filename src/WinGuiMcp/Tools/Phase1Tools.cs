using System.ComponentModel;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using WinGuiMcp.Common;
using WinGuiMcp.Platform;

namespace WinGuiMcp.Tools;

[McpServerToolType]
public sealed class Phase1Tools
{
    private readonly ToolService _tools;

    public Phase1Tools(ToolService tools)
    {
        _tools = tools;
    }

    [McpServerTool(Name = "list_windows", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Returns top-level visible windows.")]
    public Task<CallToolResult> ListWindows(string? filter = null, bool includeMinimized = false) =>
        Execute(() => _tools.ListWindows(filter, includeMinimized));

    [McpServerTool(Name = "launch_app", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Starts an application by executable path or Application User Model ID.")]
    public Task<CallToolResult> LaunchApp(
        string? path = null,
        string? appUserModelId = null,
        string? args = null,
        int waitForWindowMs = 10000) =>
        Execute(() => _tools.LaunchApp(path, appUserModelId, args, waitForWindowMs));

    [McpServerTool(Name = "focus_window", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Brings a window to the foreground.")]
    public Task<CallToolResult> FocusWindow(int handle) =>
        Execute(() => _tools.FocusWindow(handle));

    [McpServerTool(Name = "close_window", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Closes a window cleanly via WindowPattern.Close or WM_CLOSE.")]
    public Task<CallToolResult> CloseWindow(int handle) =>
        Execute(() => _tools.CloseWindow(handle));

    [McpServerTool(Name = "set_window_state", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Sets window state (minimize, maximize, restore, normal) and optionally moves/resizes the window.")]
    public Task<CallToolResult> SetWindowState(
        int handle,
        string state = "normal",
        int? x = null,
        int? y = null,
        int? width = null,
        int? height = null) =>
        Execute(() => _tools.SetWindowState(handle, state, x, y, width, height));

    [McpServerTool(Name = "get_active_modal", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Checks whether a window is disabled by or owns an active modal dialog popup.")]
    public Task<CallToolResult> GetActiveModal(int handle) =>
        Execute(() => _tools.GetActiveModal(handle));

    [McpServerTool(Name = "snapshot", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Returns a pruned UIA accessibility tree with stable refs.")]
    public Task<CallToolResult> Snapshot(
        int handle,
        string scope = "window",
        string detail = "control",
        bool includeOffscreen = false,
        bool useVision = false,
        string? rootRef = null,
        int maxDepth = 32) =>
        Execute(() =>
        {
            if (useVision)
                throw new ToolException(ErrorCodes.InvalidArgument, "useVision is not supported in Phase 1.");
            return _tools.Snapshot(handle, scope, detail, includeOffscreen, rootRef, maxDepth);
        });

    [McpServerTool(Name = "find_element", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Locates an element in the snapshot index for the given window.")]
    public Task<CallToolResult> FindElement(
        int handle,
        string by,
        string value,
        int nth = 0,
        string match = "contains") =>
        Execute(() => _tools.FindElement(handle, by, value, nth, match));

    [McpServerTool(Name = "get_properties", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Returns full UEM record for a ref.")]
    public Task<CallToolResult> GetProperties(string @ref) =>
        Execute(() => _tools.GetProperties(@ref));

    [McpServerTool(Name = "click", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Clicks an element by ref or physical point.")]
    public Task<CallToolResult> Click(
        string? @ref = null,
        int? x = null,
        int? y = null,
        string button = "left",
        int clickCount = 1,
        bool includeSnapshot = false) =>
        Execute(() => _tools.Click(@ref, x, y, button, clickCount, includeSnapshot));

    [McpServerTool(Name = "drag", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Drags from a source element or screen coordinates to a target element or screen coordinates.")]
    public Task<CallToolResult> Drag(
        string? sourceRef = null,
        int? startX = null,
        int? startY = null,
        string? targetRef = null,
        int? endX = null,
        int? endY = null,
        string button = "left",
        bool includeSnapshot = false) =>
        Execute(() => _tools.Drag(sourceRef, startX, startY, targetRef, endX, endY, button, includeSnapshot));

    [McpServerTool(Name = "scroll", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Scrolls an element via ScrollPattern or mouse wheel at the target coordinates.")]
    public Task<CallToolResult> Scroll(
        string? @ref = null,
        int? x = null,
        int? y = null,
        string direction = "down",
        int amount = 3,
        bool includeSnapshot = false) =>
        Execute(() => _tools.Scroll(@ref, x, y, direction, amount, includeSnapshot));

    [McpServerTool(Name = "type_text", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Types text into the focused control or a specific ref (auto-falls back to keyboard simulation if ValuePattern is read-only).")]
    public Task<CallToolResult> TypeText(
        string text,
        string? @ref = null,
        bool clearFirst = false,
        bool submit = false,
        bool includeSnapshot = false,
        string method = "auto") =>
        Execute(() => _tools.TypeText(text, @ref, clearFirst, submit, includeSnapshot, method));

    [McpServerTool(Name = "set_value", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Sets control value directly via UIA ValuePattern or RangeValuePattern.")]
    public Task<CallToolResult> SetValue(
        string @ref,
        string value,
        bool includeSnapshot = false) =>
        Execute(() => _tools.SetValue(@ref, value, includeSnapshot));

    [McpServerTool(Name = "hotkey", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Sends a key chord to the foreground window.")]
    public Task<CallToolResult> Hotkey(
        string keys,
        int? handle = null,
        bool includeSnapshot = false) =>
        Execute(() => _tools.Hotkey(keys, handle, includeSnapshot));

    [McpServerTool(Name = "press_key", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Presses an individual key (e.g. Tab, Enter, Escape, ArrowUp, ArrowDown, ArrowLeft, ArrowRight, Delete, Backspace, F1-F12) one or more times.")]
    public Task<CallToolResult> PressKey(
        string key,
        int? handle = null,
        int count = 1,
        bool includeSnapshot = false) =>
        Execute(() => _tools.PressKey(key, handle, count, includeSnapshot));

    [McpServerTool(Name = "screenshot", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Captures a window or full screen (automatically falls back to PrintWindow / Desktop DC for GPU/DirectX windows).")]
    public Task<CallToolResult> Screenshot(
        int? handle = null,
        int? regionX = null,
        int? regionY = null,
        int? regionWidth = null,
        int? regionHeight = null,
        double scale = 1.0) =>
        Execute(() =>
        {
            Capture.CaptureRegion? region = null;
            if (regionX is not null || regionY is not null || regionWidth is not null || regionHeight is not null)
            {
                if (regionX is null || regionY is null || regionWidth is null || regionHeight is null)
                    throw new ToolException(ErrorCodes.InvalidArgument, "region requires x, y, width, and height.");

                region = new Capture.CaptureRegion
                {
                    X = regionX.Value,
                    Y = regionY.Value,
                    Width = regionWidth.Value,
                    Height = regionHeight.Value
                };
            }

            return _tools.Screenshot(handle, region, scale);
        });

    [McpServerTool(Name = "wait_for", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Waits until a condition holds or timeout.")]
    public Task<CallToolResult> WaitFor(
        int handle,
        string condition,
        string? target = null,
        int timeoutMs = 5000,
        int settleMs = 200,
        long minBytes = 1,
        string? changedSinceUtc = null,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => _tools.WaitForAsync(handle, condition, target, timeoutMs, settleMs, minBytes, changedSinceUtc, cancellationToken));

    [McpServerTool(Name = "toggle", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Toggles a check box or toggle button via TogglePattern.")]
    public Task<CallToolResult> Toggle(string @ref, bool includeSnapshot = false) =>
        Execute(() => _tools.Toggle(@ref, includeSnapshot));

    [McpServerTool(Name = "select", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Selects a list item, tab item, or radio button via SelectionItemPattern.")]
    public Task<CallToolResult> Select(string @ref, bool includeSnapshot = false) =>
        Execute(() => _tools.Select(@ref, includeSnapshot));

    [McpServerTool(Name = "expand", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Expands a combo box, tree item, or expander via ExpandCollapsePattern.")]
    public Task<CallToolResult> Expand(string @ref, bool includeSnapshot = false) =>
        Execute(() => _tools.Expand(@ref, includeSnapshot));

    [McpServerTool(Name = "collapse", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Collapses a combo box, tree item, or expander via ExpandCollapsePattern.")]
    public Task<CallToolResult> Collapse(string @ref, bool includeSnapshot = false) =>
        Execute(() => _tools.Collapse(@ref, includeSnapshot));

    [McpServerTool(Name = "get_text", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Reads text from an element via TextPattern, ValuePattern, or Name.")]
    public Task<CallToolResult> GetText(string @ref) =>
        Execute(() => _tools.GetText(@ref));

    [McpServerTool(Name = "inspect_at", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Inspects the UIA element at the given screen coordinates.")]
    public Task<CallToolResult> InspectAt(int x, int y) =>
        Execute(() => _tools.InspectAt(x, y));

    [McpServerTool(Name = "search_tools", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Searches WinGuiMcp tools, plugins, and UIA pattern/property aliases using BM25 and keyword ranking.")]
    public Task<CallToolResult> SearchTools(
        string query,
        string? category = null,
        bool readOnlyOnly = false,
        bool includeSchemas = false,
        int limit = 10) =>
        ExecuteAsync(() => Task.FromResult(ToolCatalogRegistry.SearchTools(query, category, readOnlyOnly, includeSchemas, limit)));

    [McpServerTool(Name = "describe_tool", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Returns full inputSchema, plugin metadata, tool aliases, and UIA parameter aliases for a selected tool.")]
    public Task<CallToolResult> DescribeTool(string tool) =>
        ExecuteAsync(() => Task.FromResult(ToolCatalogRegistry.DescribeTool(tool)));

    [McpServerTool(Name = "execute_tool", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Validates and dispatches any WinGuiMcp tool by canonical name or UIA pattern/property alias, normalizing argument aliases.")]
    public Task<CallToolResult> ExecuteTool(
        string tool,
        System.Text.Json.Nodes.JsonObject? arguments = null,
        bool dryRun = false,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => ToolCatalogRegistry.ExecuteToolAsync(_tools, tool, arguments, dryRun, cancellationToken));

    private async Task<CallToolResult> Execute(Func<object> action)
    {
        try
        {
            var result = await _tools.Dispatcher.InvokeAsync(action);
            return McpResults.Create(result);
        }
        catch (ToolException ex)
        {
            var fgHwnd = NativeMethods.GetForegroundWindow();
            var details = ex.Error.Details ?? new Dictionary<string, object?>();
            if (!details.ContainsKey("foregroundHwnd"))
                details["foregroundHwnd"] = (int)fgHwnd;
            if (!details.ContainsKey("foregroundTitle"))
                details["foregroundTitle"] = WindowEnumerator.GetWindowTitle(fgHwnd);

            var enriched = new ToolError
            {
                Ok = false,
                Code = ex.Error.Code,
                Message = ex.Error.Message,
                Details = details
            };
            return McpResults.Create(enriched);
        }
        catch (Exception ex)
        {
            return McpResults.Create(ToolError.Create(ErrorCodes.InternalError, ex.Message));
        }
    }

    private static async Task<CallToolResult> ExecuteAsync(Func<Task<object>> action)
    {
        try { return McpResults.Create(await action()); }
        catch (OperationCanceledException) { throw; }
        catch (ToolException ex) { return McpResults.Create(ex.Error); }
        catch (Exception ex) { return McpResults.Create(ToolError.Create(ErrorCodes.InternalError, ex.Message)); }
    }
}

[McpServerToolType]
public sealed class CompactDiscoveryTools
{
    private readonly Phase1Tools _inner;

    public CompactDiscoveryTools(Phase1Tools inner)
    {
        _inner = inner;
    }

    [McpServerTool(Name = "search_tools", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Searches WinGuiMcp tools, plugins, and UIA pattern/property aliases using BM25 and keyword ranking.")]
    public Task<CallToolResult> SearchTools(
        string query,
        string? category = null,
        bool readOnlyOnly = false,
        bool includeSchemas = false,
        int limit = 10) =>
        _inner.SearchTools(query, category, readOnlyOnly, includeSchemas, limit);

    [McpServerTool(Name = "describe_tool", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Returns full inputSchema, plugin metadata, tool aliases, and UIA parameter aliases for a selected tool.")]
    public Task<CallToolResult> DescribeTool(string tool) =>
        _inner.DescribeTool(tool);

    [McpServerTool(Name = "execute_tool", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(System.Text.Json.Nodes.JsonObject))]
    [Description("Validates and dispatches any WinGuiMcp tool by canonical name or UIA pattern/property alias, normalizing argument aliases.")]
    public Task<CallToolResult> ExecuteTool(
        string tool,
        System.Text.Json.Nodes.JsonObject? arguments = null,
        bool dryRun = false,
        CancellationToken cancellationToken = default) =>
        _inner.ExecuteTool(tool, arguments, dryRun, cancellationToken);
}
