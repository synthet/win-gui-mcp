using System.Text.Json.Nodes;
using WinGuiMcp.Capture;
using WinGuiMcp.Common;
using WinGuiMcp.Session;
using WinGuiMcp.Tools;

namespace WinGuiMcp.Scenarios;

/// <summary>
/// Exposes every win-gui tool (tools.md) as a scenario step under its own name, dispatched through
/// <see cref="ToolCatalogRegistry.ExecuteToolAsync"/> so aliases and argument normalisation match execute_tool.
/// Actuating steps pass <see cref="TargetGuard"/> first. <c>screenshot</c> with <c>outputPath</c> writes a PNG
/// (relative paths land in the run directory) instead of returning base64.
/// </summary>
public sealed class WinGuiToolStepProvider : IScenarioStepProvider
{
    private static readonly HashSet<string> NotSteps = new(StringComparer.Ordinal) { "search_tools", "describe_tool", "execute_tool" };

    private readonly ToolService _tools;
    private readonly SnapshotSession _session;
    private readonly WindowCapture _capture;

    public WinGuiToolStepProvider(ToolService tools, SnapshotSession session, WindowCapture capture)
    {
        _tools = tools;
        _session = session;
        _capture = capture;
    }

    public string Namespace => "";

    public StepDescriptor? Describe(string name)
    {
        if (NotSteps.Contains(name))
            return null;
        try
        {
            var d = ToolResponse.Serialize(ToolCatalogRegistry.DescribeTool(name));
            var node = JsonNode.Parse(d)!;
            return node["name"]?.ToString() is { } canonical && !NotSteps.Contains(canonical)
                ? new StepDescriptor(name, node["description"]?.ToString() ?? "", node["readOnly"]?.GetValue<bool>() ?? false)
                : null;
        }
        catch (ToolException)
        {
            return null;
        }
    }

    public IEnumerable<StepDescriptor> List()
    {
        foreach (var n in ToolCatalogRegistry.ToolNames)
            if (Describe(n) is { } d)
                yield return d;
    }

    public async Task<StepResult> ExecuteAsync(StepInvocation step, CancellationToken ct)
    {
        var descriptor = Describe(step.Name)!;
        try
        {
            if (!descriptor.ReadOnly && !step.DryRun)
                Guard(step);

            if (step.Name == "screenshot" && step.Args["outputPath"]?.ToString() is { } outputPath)
                return StepResult.Success(await _tools.Dispatcher.InvokeAsync(() => CaptureToFile(step, outputPath), ct).ConfigureAwait(false));

            var result = await ToolCatalogRegistry.ExecuteToolAsync(_tools, step.Name, step.Args, step.DryRun, ct).ConfigureAwait(false);
            return StepResult.Success(JsonNode.Parse(ToolResponse.Serialize(result)));
        }
        catch (ToolException ex)
        {
            return StepResult.Failure(ex.Error.Code, ex.Error.Message, ex.Error.Details is null ? null : JsonNode.Parse(ToolResponse.Serialize(ex.Error.Details)));
        }
        catch (ScenarioException ex)
        {
            return StepResult.Failure(ex.Code, ex.Message, guard: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StepResult.Failure(ErrorCodes.InternalError, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private void Guard(StepInvocation step)
    {
        var a = step.Args;
        if (step.Name == "launch_app")
        {
            TargetGuard.EnsureLaunchAllowed(step.Target, a["path"]?.ToString(), a["appUserModelId"]?.ToString(), step.Name);
            return;
        }

        var owners = new List<WindowOwner>();
        foreach (var key in new[] { "handle", "hwnd", "windowHandle" })
            if (ScenarioExpression.TryNum(a[key], out var h))
                owners.Add(WindowOwnership.Describe((nint)(long)h));
        foreach (var key in new[] { "ref", "sourceRef", "targetRef" })
            if (a[key]?.ToString() is { Length: > 0 } r)
                owners.Add(WindowOwnership.Describe(_session.GetHandle(RefFormat.Parse(r).WindowIndex)));
        foreach (var (xk, yk) in new[] { ("x", "y"), ("startX", "startY"), ("endX", "endY") })
            if (ScenarioExpression.TryNum(a[xk], out var x) && ScenarioExpression.TryNum(a[yk], out var y))
                owners.Add(WindowOwnership.AtPoint((int)x, (int)y));
        if (owners.Count == 0)
            owners.Add(WindowOwnership.Foreground()); // keyboard input without a target goes to the foreground

        foreach (var owner in owners)
            TargetGuard.EnsureWindowAllowed(step.Target, owner, step.Name);
    }

    private JsonNode CaptureToFile(StepInvocation step, string outputPath)
    {
        var a = step.Args;
        var path = Path.IsPathRooted(outputPath) ? outputPath : Path.Combine(step.RunDir, outputPath);
        nint? handle = ScenarioExpression.TryNum(a["handle"], out var h) ? (nint)(long)h : null;
        CaptureRegion? region = ScenarioExpression.TryNum(a["regionX"], out var rx) && ScenarioExpression.TryNum(a["regionY"], out var ry) &&
                                ScenarioExpression.TryNum(a["regionWidth"], out var rw) && ScenarioExpression.TryNum(a["regionHeight"], out var rh)
            ? new CaptureRegion { X = (int)rx, Y = (int)ry, Width = (int)rw, Height = (int)rh }
            : null;
        var scale = ScenarioExpression.TryNum(a["scale"], out var s) ? s : 1.0;
        if (step.DryRun)
            return new JsonObject { ["ok"] = true, ["dryRun"] = true, ["path"] = path };
        var r = _capture.CaptureToFile(handle, region, scale, path);
        return new JsonObject { ["ok"] = true, ["path"] = r.Path, ["width"] = r.Width, ["height"] = r.Height, ["sha256"] = r.Sha256 };
    }
}
