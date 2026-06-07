using System.ComponentModel;
using ModelContextProtocol.Server;
using WinGuiMcp.Common;

namespace WinGuiMcp.Tools;

[McpServerToolType]
public sealed class Phase1Tools
{
    private readonly ToolService _tools;

    public Phase1Tools(ToolService tools)
    {
        _tools = tools;
    }

    [McpServerTool(Name = "list_windows")]
    [Description("Returns top-level visible windows.")]
    public Task<string> ListWindows(string? filter = null, bool includeMinimized = false) =>
        Execute(() => _tools.ListWindows(filter, includeMinimized));

    [McpServerTool(Name = "launch_app")]
    [Description("Starts an application by executable path or Application User Model ID.")]
    public Task<string> LaunchApp(
        string? path = null,
        string? appUserModelId = null,
        string? args = null,
        int waitForWindowMs = 10000) =>
        Execute(() => _tools.LaunchApp(path, appUserModelId, args, waitForWindowMs));

    [McpServerTool(Name = "focus_window")]
    [Description("Brings a window to the foreground.")]
    public Task<string> FocusWindow(int handle) =>
        Execute(() => _tools.FocusWindow(handle));

    [McpServerTool(Name = "snapshot")]
    [Description("Returns a pruned UIA accessibility tree with stable refs.")]
    public Task<string> Snapshot(
        int handle,
        string scope = "window",
        string detail = "control",
        bool includeOffscreen = false,
        bool useVision = false) =>
        Execute(() =>
        {
            if (useVision)
                throw new ToolException(ErrorCodes.InvalidArgument, "useVision is not supported in Phase 1.");
            return _tools.Snapshot(handle, scope, detail, includeOffscreen);
        });

    [McpServerTool(Name = "find_element")]
    [Description("Locates an element in the snapshot index for the given window.")]
    public Task<string> FindElement(
        int handle,
        string by,
        string value,
        int nth = 0,
        string match = "contains") =>
        Execute(() => _tools.FindElement(handle, by, value, nth, match));

    [McpServerTool(Name = "get_properties")]
    [Description("Returns full UEM record for a ref.")]
    public Task<string> GetProperties(string @ref) =>
        Execute(() => _tools.GetProperties(@ref));

    [McpServerTool(Name = "click")]
    [Description("Clicks an element by ref or physical point.")]
    public Task<string> Click(
        string? @ref = null,
        int? x = null,
        int? y = null,
        string button = "left",
        int clickCount = 1,
        bool includeSnapshot = false) =>
        Execute(() => _tools.Click(@ref, x, y, button, clickCount, includeSnapshot));

    [McpServerTool(Name = "type_text")]
    [Description("Types text into the focused control or a specific ref.")]
    public Task<string> TypeText(
        string text,
        string? @ref = null,
        bool clearFirst = false,
        bool submit = false,
        bool includeSnapshot = false) =>
        Execute(() => _tools.TypeText(text, @ref, clearFirst, submit, includeSnapshot));

    [McpServerTool(Name = "set_value")]
    [Description("Sets control value directly via UIA ValuePattern.")]
    public Task<string> SetValue(
        string @ref,
        string value,
        bool includeSnapshot = false) =>
        Execute(() => _tools.SetValue(@ref, value, includeSnapshot));

    [McpServerTool(Name = "hotkey")]
    [Description("Sends a key chord to the foreground window.")]
    public Task<string> Hotkey(
        string keys,
        int? handle = null,
        bool includeSnapshot = false) =>
        Execute(() => _tools.Hotkey(keys, handle, includeSnapshot));

    [McpServerTool(Name = "screenshot")]
    [Description("Captures a window or full screen.")]
    public Task<string> Screenshot(
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

    [McpServerTool(Name = "wait_for")]
    [Description("Waits until a condition holds or timeout.")]
    public Task<string> WaitFor(
        int handle,
        string condition,
        string? target = null,
        int timeoutMs = 5000) =>
        Execute(() => _tools.WaitFor(handle, condition, target, timeoutMs));

    private static Task<string> Execute(Func<object> action)
    {
        try
        {
            var result = action();
            return Task.FromResult(ToolResponse.Success(result));
        }
        catch (ToolException ex)
        {
            return Task.FromResult(ToolResponse.Error(ex.Error));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Internal(ex.Message));
        }
    }
}
