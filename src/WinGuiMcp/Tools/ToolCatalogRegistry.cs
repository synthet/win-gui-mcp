using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using WinGuiMcp.Capture;
using WinGuiMcp.Common;

namespace WinGuiMcp.Tools;

public sealed record WinGuiPluginDescriptor(
    string Id,
    string Name,
    string Category,
    string Description,
    IReadOnlyList<string> ToolNames,
    bool IsExternal = false);

public sealed record WinGuiFieldDescriptor(
    string CanonicalValue,
    string ParameterName,
    string Description,
    string[] Aliases,
    string[] SupportedTools);

public sealed record WinGuiToolDescriptor(
    string Name,
    string PluginId,
    string Category,
    string Description,
    bool ReadOnly,
    string[] Aliases,
    Dictionary<string, string[]> ParameterAliases,
    JsonObject InputSchema);

public static class ToolCatalogRegistry
{
    private static readonly List<WinGuiPluginDescriptor> BuiltInPlugins =
    [
        new(
            "window",
            "Window & Process Management Plugin",
            "window",
            "Top-level window enumeration, application launching, foreground activation, window state/geometry, modal popup inspection, and clean window closing.",
            ["list_windows", "launch_app", "focus_window", "close_window", "set_window_state", "get_active_modal"]),
        new(
            "perception",
            "UIA Perception & Locator Plugin",
            "perception",
            "Pruned UIA accessibility snapshots with stable UEM refs, attribute locators, element property inspection, text extraction, hit-testing, and OS wait conditions.",
            ["snapshot", "find_element", "get_properties", "get_text", "inspect_at", "wait_for"]),
        new(
            "action",
            "UIA Pattern & Input Action Plugin",
            "action",
            "Deterministic UIA pattern execution and Win32 SendInput fallbacks for click, drag, scroll, text entry, value setting, hotkeys, single keys, toggle, select, expand, and collapse.",
            ["click", "drag", "scroll", "type_text", "set_value", "hotkey", "press_key", "toggle", "select", "expand", "collapse"]),
        new(
            "capture",
            "Screen & Window Capture Plugin",
            "capture",
            "Window and screen region image capture with automatic PrintWindow and Desktop DC fallbacks for hardware-accelerated surfaces.",
            ["screenshot"]),
        new(
            "discovery",
            "MCP Progressive Tool Discovery & Dispatch Plugin",
            "discovery",
            "BM25 + UIA pattern/property alias search, tool schema inspection, and validated dynamic tool dispatch.",
            ["search_tools", "describe_tool", "execute_tool"])
    ];

    public static readonly IReadOnlyList<WinGuiFieldDescriptor> FieldDescriptors =
    [
        new(
            "automationId",
            "by",
            "UIA AutomationId property locator",
            ["AutomationId", "automation_id", "AutomationElement.AutomationIdProperty", "aid", "uiaId"],
            ["find_element", "snapshot", "get_properties"]),
        new(
            "name",
            "by",
            "UIA Name / Accessible label locator",
            ["Name", "label", "title", "LegacyIAccessible.Name", "AutomationElement.NameProperty", "accessibleName"],
            ["find_element", "snapshot", "get_properties", "get_text"]),
        new(
            "role",
            "by",
            "UIA ControlType / Role locator",
            ["ControlType", "control_type", "role", "LocalizedControlType", "AutomationElement.ControlTypeProperty"],
            ["find_element", "snapshot", "get_properties"]),
        new(
            "className",
            "by",
            "Win32 / WPF ClassName property locator",
            ["ClassName", "class_name", "AutomationElement.ClassNameProperty", "win32Class"],
            ["find_element", "snapshot", "get_properties"]),
        new(
            "text",
            "by",
            "Element Value or Text content locator",
            ["Value", "value", "Text", "text", "ValuePattern.Value", "TextPattern"],
            ["find_element", "get_text", "set_value", "type_text"])
    ];

    private static readonly Dictionary<string, string> ToolAliasMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["search"] = "search_tools",
        ["describe"] = "describe_tool",
        ["dispatch"] = "execute_tool",
        ["execute"] = "execute_tool",
        ["windows"] = "list_windows",
        ["enumerate_windows"] = "list_windows",
        ["start_app"] = "launch_app",
        ["open_app"] = "launch_app",
        ["activate_window"] = "focus_window",
        ["WindowPattern.Close"] = "close_window",
        ["Window.Close"] = "close_window",
        ["resize_window"] = "set_window_state",
        ["move_window"] = "set_window_state",
        ["check_modal"] = "get_active_modal",
        ["uia_tree"] = "snapshot",
        ["accessibility_tree"] = "snapshot",
        ["tree"] = "snapshot",
        ["find"] = "find_element",
        ["query_element"] = "find_element",
        ["locate_element"] = "find_element",
        ["inspect_element"] = "get_properties",
        ["InvokePattern.Invoke"] = "click",
        ["Invoke.Invoke"] = "click",
        ["invoke"] = "click",
        ["ScrollPattern.Scroll"] = "scroll",
        ["Scroll.Scroll"] = "scroll",
        ["mouse_wheel"] = "scroll",
        ["wheel"] = "scroll",
        ["send_keys"] = "type_text",
        ["input_text"] = "type_text",
        ["type"] = "type_text",
        ["ValuePattern.SetValue"] = "set_value",
        ["RangeValuePattern.SetValue"] = "set_value",
        ["Value.SetValue"] = "set_value",
        ["RangeValue.SetValue"] = "set_value",
        ["shortcut"] = "hotkey",
        ["keybinding"] = "hotkey",
        ["send_key"] = "press_key",
        ["key"] = "press_key",
        ["capture"] = "screenshot",
        ["capture_window"] = "screenshot",
        ["TogglePattern.Toggle"] = "toggle",
        ["Toggle.Toggle"] = "toggle",
        ["SelectionItemPattern.Select"] = "select",
        ["SelectionItem.Select"] = "select",
        ["ExpandCollapsePattern.Expand"] = "expand",
        ["ExpandCollapse.Expand"] = "expand",
        ["ExpandCollapsePattern.Collapse"] = "collapse",
        ["ExpandCollapse.Collapse"] = "collapse",
        ["read_text"] = "get_text",
        ["TextPattern.GetText"] = "get_text",
        ["hit_test"] = "inspect_at",
        ["element_from_point"] = "inspect_at"
    };

    private static readonly IReadOnlyList<WinGuiToolDescriptor> ToolDescriptors = BuildToolDescriptors();
    private static readonly List<WinGuiPluginDescriptor> ExternalPlugins = [];
    private static bool _pluginsLoaded;

    public static string NormalizeLocatorBy(string by)
    {
        var norm = Regex.Replace(by ?? string.Empty, @"[^\p{L}\p{Nd}]", "").ToLowerInvariant();
        return norm switch
        {
            "automationid" or "automationelementautomationidproperty" or "aid" or "uiaid" => "automationId",
            "name" or "label" or "title" or "legacyiaccessiblename" or "automationelementnameproperty" or "accessiblename" => "name",
            "role" or "controltype" or "localizedcontroltype" or "automationelementcontroltypeproperty" => "role",
            "classname" or "automationelementclassnameproperty" or "win32class" => "className",
            "text" or "value" or "valuepatternvalue" or "textpattern" => "text",
            _ => by ?? string.Empty
        };
    }

    public static void EnsurePluginsLoaded(string? customDir = null)
    {
        if (_pluginsLoaded && customDir is null)
            return;

        _pluginsLoaded = true;
        var dirs = new List<string>();
        if (!string.IsNullOrWhiteSpace(customDir))
            dirs.Add(customDir);

        var envDir = Environment.GetEnvironmentVariable("WINGUI_MCP_PLUGINS_DIR");
        if (!string.IsNullOrWhiteSpace(envDir))
            dirs.Add(envDir);

        dirs.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins"));
        dirs.Add(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\plugins")));
        dirs.Add(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\plugins")));

        foreach (var dir in dirs.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.GetFiles(dir, "*.plugin.json", SearchOption.TopDirectoryOnly))
                TryLoadPluginManifest(file);
        }
    }

    public static bool TryLoadPluginManifest(string path)
    {
        try
        {
            var root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
            var id = root?["id"]?.GetValue<string>()?.Trim();
            if (string.IsNullOrWhiteSpace(id)) return false;

            if (root!["toolAliases"] is JsonObject aliases)
            {
                foreach (var (alias, targetNode) in aliases)
                {
                    if (targetNode is JsonValue val && val.TryGetValue<string>(out var target))
                        ToolAliasMap[alias] = target;
                }
            }

            ExternalPlugins.RemoveAll(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            ExternalPlugins.Add(new WinGuiPluginDescriptor(
                id,
                root["name"]?.GetValue<string>() ?? id,
                root["category"]?.GetValue<string>() ?? "custom",
                root["description"]?.GetValue<string>() ?? "External declarative plugin.",
                [],
                IsExternal: true));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static (string ResolvedTool, JsonObject NormalizedArguments) ResolveToolAndArguments(string rawTool, JsonObject? rawArguments)
    {
        EnsurePluginsLoaded();
        var args = rawArguments is not null ? (JsonObject)rawArguments.DeepClone() : new JsonObject();
        var trimmed = (rawTool ?? string.Empty).Trim();
        if (trimmed.StartsWith("wingui.", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed["wingui.".Length..];

        var resolvedTool = ToolAliasMap.TryGetValue(trimmed, out var mapped) ? mapped : trimmed;

        // Check locator-scoped tool aliases: e.g. "find.AutomationId", "find_by_automationId", "find.Name", "find.ControlType"
        if (trimmed.StartsWith("find.", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("find_by_", StringComparison.OrdinalIgnoreCase))
        {
            var suffix = trimmed.StartsWith("find.", StringComparison.OrdinalIgnoreCase)
                ? trimmed["find.".Length..]
                : trimmed["find_by_".Length..];
            var normalizedBy = NormalizeLocatorBy(suffix);
            if (normalizedBy is "automationId" or "name" or "role" or "className" or "text")
            {
                resolvedTool = "find_element";
                if (!args.ContainsKey("by"))
                    args["by"] = normalizedBy;
            }
        }

        var normalizedArgs = NormalizeArgumentsForTool(resolvedTool, args);
        return (resolvedTool, normalizedArgs);
    }

    public static JsonObject NormalizeArgumentsForTool(string canonicalTool, JsonObject rawArgs)
    {
        var result = new JsonObject();
        foreach (var (key, value) in rawArgs)
        {
            var canonicalKey = ResolveParameterKey(canonicalTool, key);
            var normalizedValue = value?.DeepClone();

            if (canonicalTool == "find_element" &&
                canonicalKey == "by" &&
                normalizedValue is JsonValue byVal &&
                byVal.TryGetValue<string>(out var byText))
            {
                normalizedValue = JsonValue.Create(NormalizeLocatorBy(byText));
            }

            if (result.TryGetPropertyValue(canonicalKey, out var existing))
            {
                if (existing?.ToJsonString() != normalizedValue?.ToJsonString())
                    throw new ToolException(ErrorCodes.InvalidArgument, $"Conflicting values supplied for parameter '{canonicalKey}' via alias '{key}'.");
                continue;
            }

            result[canonicalKey] = normalizedValue;
        }
        return result;
    }

    private static string ResolveParameterKey(string canonicalTool, string key)
    {
        if (key.Equals("hwnd", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("windowHandle", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("window_handle", StringComparison.OrdinalIgnoreCase))
            return "handle";

        if (canonicalTool != "drag" &&
            (key.Equals("elementRef", StringComparison.OrdinalIgnoreCase) ||
             key.Equals("element_ref", StringComparison.OrdinalIgnoreCase) ||
             key.Equals("element", StringComparison.OrdinalIgnoreCase) ||
             key.Equals("targetRef", StringComparison.OrdinalIgnoreCase)))
            return "ref";

        if (canonicalTool == "find_element" &&
            (key.Equals("property", StringComparison.OrdinalIgnoreCase) ||
             key.Equals("field", StringComparison.OrdinalIgnoreCase) ||
             key.Equals("attribute", StringComparison.OrdinalIgnoreCase)))
            return "by";

        if (canonicalTool is "describe_tool" or "execute_tool" &&
            (key.Equals("action_id", StringComparison.OrdinalIgnoreCase) ||
             key.Equals("actionId", StringComparison.OrdinalIgnoreCase) ||
             key.Equals("name", StringComparison.OrdinalIgnoreCase) ||
             key.Equals("toolName", StringComparison.OrdinalIgnoreCase) ||
             key.Equals("id", StringComparison.OrdinalIgnoreCase)))
            return "tool";

        if (canonicalTool == "search_tools")
        {
            if (key.Equals("read_only_only", StringComparison.OrdinalIgnoreCase))
                return "readOnlyOnly";
            if (key.Equals("include_schemas", StringComparison.OrdinalIgnoreCase))
                return "includeSchemas";
        }

        if (key.Equals("dry_run", StringComparison.OrdinalIgnoreCase))
            return "dryRun";
        if (key.Equals("include_snapshot", StringComparison.OrdinalIgnoreCase))
            return "includeSnapshot";
        if (key.Equals("clear_first", StringComparison.OrdinalIgnoreCase))
            return "clearFirst";

        return key;
    }

    public static object SearchTools(
        string query,
        string? category = null,
        bool readOnlyOnly = false,
        bool includeSchemas = false,
        int limit = 10)
    {
        EnsurePluginsLoaded();
        limit = Math.Clamp(limit, 1, 50);
        var q = (query ?? string.Empty).Trim();
        var tokens = Tokenize(q);

        var matchedFields = new List<object>();
        var fieldToolBonus = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(q))
        {
            foreach (var f in FieldDescriptors)
            {
                double fScore = 0;
                if (f.CanonicalValue.Equals(q, StringComparison.OrdinalIgnoreCase) ||
                    f.Aliases.Any(a => a.Equals(q, StringComparison.OrdinalIgnoreCase)))
                    fScore += 14.0;
                foreach (var t in tokens)
                {
                    if (f.CanonicalValue.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                        f.Aliases.Any(a => a.Contains(t, StringComparison.OrdinalIgnoreCase)))
                        fScore += 4.0;
                }
                if (fScore > 0)
                {
                    matchedFields.Add(new
                    {
                        canonicalValue = f.CanonicalValue,
                        parameterName = f.ParameterName,
                        description = f.Description,
                        aliases = f.Aliases,
                        supportedTools = f.SupportedTools,
                        score = Math.Round(fScore, 3)
                    });
                    foreach (var st in f.SupportedTools)
                        fieldToolBonus[st] = Math.Max(fieldToolBonus.GetValueOrDefault(st), fScore * 0.5);
                }
            }
        }

        var scored = new List<(WinGuiToolDescriptor Tool, double Score)>();
        foreach (var d in ToolDescriptors)
        {
            if (readOnlyOnly && !d.ReadOnly) continue;
            if (!string.IsNullOrWhiteSpace(category) &&
                !d.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
                !d.PluginId.Equals(category, StringComparison.OrdinalIgnoreCase))
                continue;

            var score = string.IsNullOrWhiteSpace(q) ? 1.0 : ScoreTool(d, q, tokens);
            if (fieldToolBonus.TryGetValue(d.Name, out var bonus))
                score += bonus;

            if (score > 0)
                scored.Add((d, Math.Round(score, 3)));
        }

        var top = scored
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Tool.Name, StringComparer.Ordinal)
            .Take(limit)
            .Select(x => new
            {
                id = $"wingui.{x.Tool.Name}",
                name = x.Tool.Name,
                plugin = x.Tool.PluginId,
                category = x.Tool.Category,
                description = x.Tool.Description,
                readOnly = x.Tool.ReadOnly,
                aliases = x.Tool.Aliases,
                score = x.Score,
                inputSchema = includeSchemas ? x.Tool.InputSchema : null
            })
            .ToList();

        return new
        {
            ok = true,
            query = q,
            totalMatched = scored.Count,
            tools = top,
            fieldMatches = matchedFields,
            plugins = BuiltInPlugins.Concat(ExternalPlugins).Select(p => new
            {
                id = p.Id,
                name = p.Name,
                category = p.Category,
                toolCount = p.ToolNames.Count,
                isExternal = p.IsExternal
            }).ToList()
        };
    }

    public static object DescribeTool(string tool)
    {
        EnsurePluginsLoaded();
        if (string.IsNullOrWhiteSpace(tool))
            throw new ToolException(ErrorCodes.InvalidArgument, "Argument 'tool' is required.");

        var (resolvedName, injectedArgs) = ResolveToolAndArguments(tool, null);
        var descriptor = ToolDescriptors.FirstOrDefault(d => d.Name.Equals(resolvedName, StringComparison.OrdinalIgnoreCase))
            ?? throw new ToolException(ErrorCodes.InvalidArgument, $"Unknown tool or alias '{tool}'. Use search_tools to discover available tools.");

        return new
        {
            ok = true,
            id = $"wingui.{descriptor.Name}",
            name = descriptor.Name,
            requestedName = tool,
            plugin = descriptor.PluginId,
            category = descriptor.Category,
            description = descriptor.Description,
            readOnly = descriptor.ReadOnly,
            aliases = descriptor.Aliases,
            parameterAliases = descriptor.ParameterAliases,
            injectedDefaults = injectedArgs,
            inputSchema = descriptor.InputSchema
        };
    }

    public static async Task<object> ExecuteToolAsync(
        ToolService tools,
        string tool,
        JsonObject? arguments = null,
        bool dryRun = false,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tool))
            throw new ToolException(ErrorCodes.InvalidArgument, "Argument 'tool' is required.");

        var (resolvedTool, normArgs) = ResolveToolAndArguments(tool, arguments);
        if (resolvedTool.Equals("execute_tool", StringComparison.OrdinalIgnoreCase))
            throw new ToolException(ErrorCodes.InvalidArgument, "Recursive calls to execute_tool are not allowed.");

        var descriptor = ToolDescriptors.FirstOrDefault(d => d.Name.Equals(resolvedTool, StringComparison.OrdinalIgnoreCase))
            ?? throw new ToolException(ErrorCodes.InvalidArgument, $"Unknown tool or alias '{tool}'.");

        ValidateRequiredArguments(descriptor, normArgs);

        if (dryRun)
        {
            return new
            {
                ok = true,
                dryRun = true,
                valid = true,
                requestedTool = tool,
                resolvedTool = descriptor.Name,
                normalizedArguments = normArgs
            };
        }

        if (descriptor.Name == "wait_for")
        {
            return await tools.WaitForAsync(
                GetInt(normArgs, "handle")!.Value,
                GetString(normArgs, "condition")!,
                GetString(normArgs, "target"),
                GetInt(normArgs, "timeoutMs") ?? 5000,
                GetInt(normArgs, "settleMs") ?? 200,
                GetLong(normArgs, "minBytes") ?? 1L,
                GetString(normArgs, "changedSinceUtc"),
                ct);
        }

        if (descriptor.Name is "search_tools" or "describe_tool")
        {
            return descriptor.Name == "search_tools"
                ? SearchTools(
                    GetString(normArgs, "query")!,
                    GetString(normArgs, "category"),
                    GetBool(normArgs, "readOnlyOnly") ?? false,
                    GetBool(normArgs, "includeSchemas") ?? false,
                    GetInt(normArgs, "limit") ?? 10)
                : DescribeTool(GetString(normArgs, "tool")!);
        }

        return await tools.Dispatcher.InvokeAsync(() => DispatchSync(tools, descriptor.Name, normArgs));
    }

    private static object DispatchSync(ToolService tools, string name, JsonObject args) => name switch
    {
        "list_windows" => tools.ListWindows(GetString(args, "filter"), GetBool(args, "includeMinimized") ?? false),
        "launch_app" => tools.LaunchApp(GetString(args, "path"), GetString(args, "appUserModelId"), GetString(args, "args"), GetInt(args, "waitForWindowMs") ?? 10000),
        "focus_window" => tools.FocusWindow(GetInt(args, "handle")!.Value),
        "close_window" => tools.CloseWindow(GetInt(args, "handle")!.Value),
        "set_window_state" => tools.SetWindowState(GetInt(args, "handle")!.Value, GetString(args, "state") ?? "normal", GetInt(args, "x"), GetInt(args, "y"), GetInt(args, "width"), GetInt(args, "height")),
        "get_active_modal" => tools.GetActiveModal(GetInt(args, "handle")!.Value),
        "snapshot" => tools.Snapshot(GetInt(args, "handle")!.Value, GetString(args, "scope") ?? "window", GetString(args, "detail") ?? "control", GetBool(args, "includeOffscreen") ?? false, GetString(args, "rootRef"), GetInt(args, "maxDepth") ?? 32),
        "find_element" => tools.FindElement(GetInt(args, "handle")!.Value, GetString(args, "by")!, GetString(args, "value")!, GetInt(args, "nth") ?? 0, GetString(args, "match") ?? "contains"),
        "get_properties" => tools.GetProperties(GetString(args, "ref")!),
        "click" => tools.Click(GetString(args, "ref"), GetInt(args, "x"), GetInt(args, "y"), GetString(args, "button") ?? "left", GetInt(args, "clickCount") ?? 1, GetBool(args, "includeSnapshot") ?? false),
        "drag" => tools.Drag(GetString(args, "sourceRef"), GetInt(args, "startX"), GetInt(args, "startY"), GetString(args, "targetRef"), GetInt(args, "endX"), GetInt(args, "endY"), GetString(args, "button") ?? "left", GetBool(args, "includeSnapshot") ?? false),
        "scroll" => tools.Scroll(GetString(args, "ref"), GetInt(args, "x"), GetInt(args, "y"), GetString(args, "direction") ?? "down", GetInt(args, "amount") ?? 3, GetBool(args, "includeSnapshot") ?? false),
        "type_text" => tools.TypeText(GetString(args, "text")!, GetString(args, "ref"), GetBool(args, "clearFirst") ?? false, GetBool(args, "submit") ?? false, GetBool(args, "includeSnapshot") ?? false, GetString(args, "method") ?? "auto"),
        "set_value" => tools.SetValue(GetString(args, "ref")!, GetString(args, "value")!, GetBool(args, "includeSnapshot") ?? false),
        "hotkey" => tools.Hotkey(GetString(args, "keys")!, GetInt(args, "handle"), GetBool(args, "includeSnapshot") ?? false),
        "press_key" => tools.PressKey(GetString(args, "key")!, GetInt(args, "handle"), GetInt(args, "count") ?? 1, GetBool(args, "includeSnapshot") ?? false),
        "screenshot" => tools.Screenshot(
            GetInt(args, "handle"),
            GetInt(args, "regionX") is not null && GetInt(args, "regionY") is not null && GetInt(args, "regionWidth") is not null && GetInt(args, "regionHeight") is not null
                ? new CaptureRegion { X = GetInt(args, "regionX")!.Value, Y = GetInt(args, "regionY")!.Value, Width = GetInt(args, "regionWidth")!.Value, Height = GetInt(args, "regionHeight")!.Value }
                : null,
            GetDouble(args, "scale") ?? 1.0),
        "toggle" => tools.Toggle(GetString(args, "ref")!, GetBool(args, "includeSnapshot") ?? false),
        "select" => tools.Select(GetString(args, "ref")!, GetBool(args, "includeSnapshot") ?? false),
        "expand" => tools.Expand(GetString(args, "ref")!, GetBool(args, "includeSnapshot") ?? false),
        "collapse" => tools.Collapse(GetString(args, "ref")!, GetBool(args, "includeSnapshot") ?? false),
        "get_text" => tools.GetText(GetString(args, "ref")!),
        "inspect_at" => tools.InspectAt(GetInt(args, "x")!.Value, GetInt(args, "y")!.Value),
        _ => throw new ToolException(ErrorCodes.InvalidArgument, $"Unsupported tool '{name}'.")
    };

    private static void ValidateRequiredArguments(WinGuiToolDescriptor descriptor, JsonObject args)
    {
        if (descriptor.InputSchema["required"] is not JsonArray reqArr) return;
        foreach (var req in reqArr.Select(r => r?.GetValue<string>()).Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            if (!args.ContainsKey(req!) || args[req!] is null)
                throw new ToolException(ErrorCodes.InvalidArgument, $"Missing required argument '{req}' for tool '{descriptor.Name}'.");
        }
    }

    private static string? GetString(JsonObject args, string key) =>
        args[key] is JsonValue v && v.TryGetValue<string>(out var s) ? s : args[key]?.ToString();

    private static int? GetInt(JsonObject args, string key) =>
        args[key] is JsonValue v && v.TryGetValue<int>(out var i) ? i :
        args[key] is JsonValue vd && vd.TryGetValue<double>(out var d) ? (int)d : null;

    private static long? GetLong(JsonObject args, string key) =>
        args[key] is JsonValue v && v.TryGetValue<long>(out var l) ? l :
        args[key] is JsonValue vd && vd.TryGetValue<double>(out var d) ? (long)d : null;

    private static double? GetDouble(JsonObject args, string key) =>
        args[key] is JsonValue v && v.TryGetValue<double>(out var d) ? d : null;

    private static bool? GetBool(JsonObject args, string key) =>
        args[key] is JsonValue v && v.TryGetValue<bool>(out var b) ? b : null;

    private static double ScoreTool(WinGuiToolDescriptor d, string q, string[] tokens)
    {
        double score = 0;
        if (d.Name.Equals(q, StringComparison.OrdinalIgnoreCase) || $"wingui.{d.Name}".Equals(q, StringComparison.OrdinalIgnoreCase))
            score += 15.0;
        else if (d.Aliases.Any(a => a.Equals(q, StringComparison.OrdinalIgnoreCase)))
            score += 14.0;
        else if (d.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            score += 8.0;

        foreach (var t in tokens)
        {
            if (d.Name.Contains(t, StringComparison.OrdinalIgnoreCase)) score += 4.0;
            if (d.Aliases.Any(a => a.Contains(t, StringComparison.OrdinalIgnoreCase))) score += 3.5;
            if (d.Category.Contains(t, StringComparison.OrdinalIgnoreCase)) score += 2.5;
            if (d.Description.Contains(t, StringComparison.OrdinalIgnoreCase)) score += 1.5;
        }
        return score;
    }

    private static string[] Tokenize(string text) =>
        Regex.Split(text ?? string.Empty, @"[^\p{L}\p{Nd}]+")
            .Where(t => t.Length >= 2)
            .Select(t => t.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static IReadOnlyList<WinGuiToolDescriptor> BuildToolDescriptors()
    {
        WinGuiToolDescriptor Make(string name, string plugin, bool readOnly, string desc, string schemaJson, Dictionary<string, string[]>? paramAliases = null)
        {
            var aliases = ToolAliasMap
                .Where(kv => kv.Value.Equals(name, StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Key)
                .ToList();
            if (name == "find_element")
                aliases.AddRange(["find.AutomationId", "find.Name", "find.ControlType", "find.ClassName"]);

            return new WinGuiToolDescriptor(
                name,
                plugin,
                plugin,
                desc,
                readOnly,
                aliases.ToArray(),
                paramAliases ?? new Dictionary<string, string[]>(),
                JsonNode.Parse(schemaJson)!.AsObject());
        }

        var handleAlias = new Dictionary<string, string[]> { ["handle"] = ["hwnd", "windowHandle"] };
        var refAlias = new Dictionary<string, string[]> { ["ref"] = ["elementRef", "element", "targetRef"] };

        return
        [
            Make("list_windows", "window", true, "Returns top-level visible windows.", """{"type":"object","properties":{"filter":{"type":"string"},"includeMinimized":{"type":"boolean"}}}"""),
            Make("launch_app", "window", false, "Starts an application by executable path or Application User Model ID.", """{"type":"object","properties":{"path":{"type":"string"},"appUserModelId":{"type":"string"},"args":{"type":"string"},"waitForWindowMs":{"type":"integer"}}}"""),
            Make("focus_window", "window", false, "Brings a window to the foreground.", """{"type":"object","required":["handle"],"properties":{"handle":{"type":"integer"}}}""", handleAlias),
            Make("close_window", "window", false, "Closes a window cleanly via WindowPattern.Close or WM_CLOSE.", """{"type":"object","required":["handle"],"properties":{"handle":{"type":"integer"}}}""", handleAlias),
            Make("set_window_state", "window", false, "Sets window state (minimize, maximize, restore, normal) and optionally moves/resizes the window.", """{"type":"object","required":["handle"],"properties":{"handle":{"type":"integer"},"state":{"type":"string"},"x":{"type":"integer"},"y":{"type":"integer"},"width":{"type":"integer"},"height":{"type":"integer"}}}""", handleAlias),
            Make("get_active_modal", "window", true, "Checks whether a window is disabled by or owns an active modal dialog popup.", """{"type":"object","required":["handle"],"properties":{"handle":{"type":"integer"}}}""", handleAlias),
            Make("snapshot", "perception", true, "Returns a pruned UIA accessibility tree with stable refs.", """{"type":"object","required":["handle"],"properties":{"handle":{"type":"integer"},"scope":{"type":"string"},"detail":{"type":"string"},"includeOffscreen":{"type":"boolean"},"rootRef":{"type":"string"},"maxDepth":{"type":"integer"}}}""", handleAlias),
            Make("find_element", "perception", true, "Locates an element in the snapshot index by AutomationId, Name, ControlType/role, ClassName, or text.", """{"type":"object","required":["handle","by","value"],"properties":{"handle":{"type":"integer"},"by":{"type":"string"},"value":{"type":"string"},"nth":{"type":"integer"},"match":{"type":"string"}}}""",
                new Dictionary<string, string[]> { ["handle"] = ["hwnd", "windowHandle"], ["by"] = ["property", "field", "attribute", "AutomationId", "Name", "ControlType", "ClassName"] }),
            Make("get_properties", "perception", true, "Returns full UEM record for a ref.", """{"type":"object","required":["ref"],"properties":{"ref":{"type":"string"}}}""", refAlias),
            Make("click", "action", false, "Clicks an element by ref (InvokePattern/Point) or physical screen coordinates.", """{"type":"object","properties":{"ref":{"type":"string"},"x":{"type":"integer"},"y":{"type":"integer"},"button":{"type":"string"},"clickCount":{"type":"integer"},"includeSnapshot":{"type":"boolean"}}}""", refAlias),
            Make("drag", "action", false, "Drags from a source element or screen coordinates to a target element or screen coordinates.", """{"type":"object","properties":{"sourceRef":{"type":"string"},"startX":{"type":"integer"},"startY":{"type":"integer"},"targetRef":{"type":"string"},"endX":{"type":"integer"},"endY":{"type":"integer"},"button":{"type":"string"},"includeSnapshot":{"type":"boolean"}}}"""),
            Make("scroll", "action", false, "Scrolls an element via ScrollPattern or mouse wheel at the target coordinates.", """{"type":"object","properties":{"ref":{"type":"string"},"x":{"type":"integer"},"y":{"type":"integer"},"direction":{"type":"string"},"amount":{"type":"integer"},"includeSnapshot":{"type":"boolean"}}}""", refAlias),
            Make("type_text", "action", false, "Types text into the focused control or a specific ref (auto-falls back to keyboard simulation if ValuePattern is read-only).", """{"type":"object","required":["text"],"properties":{"text":{"type":"string"},"ref":{"type":"string"},"clearFirst":{"type":"boolean"},"submit":{"type":"boolean"},"includeSnapshot":{"type":"boolean"},"method":{"type":"string"}}}""", refAlias),
            Make("set_value", "action", false, "Sets control value directly via UIA ValuePattern or RangeValuePattern.", """{"type":"object","required":["ref","value"],"properties":{"ref":{"type":"string"},"value":{"type":"string"},"includeSnapshot":{"type":"boolean"}}}""", refAlias),
            Make("hotkey", "action", false, "Sends a key chord to the foreground or specified window.", """{"type":"object","required":["keys"],"properties":{"keys":{"type":"string"},"handle":{"type":"integer"},"includeSnapshot":{"type":"boolean"}}}""", handleAlias),
            Make("press_key", "action", false, "Presses an individual key (e.g. Tab, Enter, Escape, ArrowUp, ArrowDown, ArrowLeft, ArrowRight) one or more times.", """{"type":"object","required":["key"],"properties":{"key":{"type":"string"},"handle":{"type":"integer"},"count":{"type":"integer"},"includeSnapshot":{"type":"boolean"}}}""", handleAlias),
            Make("screenshot", "capture", true, "Captures a window or full screen (automatically falls back to PrintWindow / Desktop DC for GPU/DirectX windows).", """{"type":"object","properties":{"handle":{"type":"integer"},"regionX":{"type":"integer"},"regionY":{"type":"integer"},"regionWidth":{"type":"integer"},"regionHeight":{"type":"integer"},"scale":{"type":"number"}}}""", handleAlias),
            Make("wait_for", "perception", true, "Waits until a UI or file condition holds or timeout.", """{"type":"object","required":["handle","condition"],"properties":{"handle":{"type":"integer"},"condition":{"type":"string"},"target":{"type":"string"},"timeoutMs":{"type":"integer"},"settleMs":{"type":"integer"},"minBytes":{"type":"integer"},"changedSinceUtc":{"type":"string"}}}""", handleAlias),
            Make("toggle", "action", false, "Toggles a check box or toggle button via TogglePattern.", """{"type":"object","required":["ref"],"properties":{"ref":{"type":"string"},"includeSnapshot":{"type":"boolean"}}}""", refAlias),
            Make("select", "action", false, "Selects a list item, tab item, or radio button via SelectionItemPattern.", """{"type":"object","required":["ref"],"properties":{"ref":{"type":"string"},"includeSnapshot":{"type":"boolean"}}}""", refAlias),
            Make("expand", "action", false, "Expands a combo box, tree item, or expander via ExpandCollapsePattern.", """{"type":"object","required":["ref"],"properties":{"ref":{"type":"string"},"includeSnapshot":{"type":"boolean"}}}""", refAlias),
            Make("collapse", "action", false, "Collapses a combo box, tree item, or expander via ExpandCollapsePattern.", """{"type":"object","required":["ref"],"properties":{"ref":{"type":"string"},"includeSnapshot":{"type":"boolean"}}}""", refAlias),
            Make("get_text", "perception", true, "Reads text from an element via TextPattern, ValuePattern, or Name.", """{"type":"object","required":["ref"],"properties":{"ref":{"type":"string"}}}""", refAlias),
            Make("inspect_at", "perception", true, "Inspects the UIA element at the given screen coordinates.", """{"type":"object","required":["x","y"],"properties":{"x":{"type":"integer"},"y":{"type":"integer"}}}"""),
            Make("search_tools", "discovery", true, "Searches WinGuiMcp tools, plugins, and UIA pattern/property aliases using BM25 and keyword ranking.", """{"type":"object","required":["query"],"properties":{"query":{"type":"string"},"category":{"type":"string"},"readOnlyOnly":{"type":"boolean"},"includeSchemas":{"type":"boolean"},"limit":{"type":"integer"}}}"""),
            Make("describe_tool", "discovery", true, "Returns full inputSchema, plugin metadata, tool aliases, and UIA parameter aliases for a selected tool.", """{"type":"object","required":["tool"],"properties":{"tool":{"type":"string"}}}"""),
            Make("execute_tool", "discovery", false, "Validates and dispatches any WinGuiMcp tool by canonical name or UIA pattern/property alias, normalizing argument aliases.", """{"type":"object","required":["tool"],"properties":{"tool":{"type":"string"},"arguments":{"type":"object"},"dryRun":{"type":"boolean"}}}""")
        ];
    }
}
