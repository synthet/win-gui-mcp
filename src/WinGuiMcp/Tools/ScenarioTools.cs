using System.ComponentModel;
using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using WinGuiMcp.Common;
using WinGuiMcp.Scenarios;

namespace WinGuiMcp.Tools;

/// <summary>Scenario runner tools (docs/tools.md § Scenarios, docs/scenarios.md, ADR-0001).</summary>
[McpServerToolType]
public sealed class ScenarioTools
{
    private readonly ScenarioEngine _engine;

    public ScenarioTools(ScenarioEngine engine) => _engine = engine;

    [McpServerTool(Name = "run_scenario", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(JsonObject))]
    [Description("Runs a declarative *.scenario.yaml/json step file (path or name under the scenario roots). Write steps need allowWrites; dryRun validates every step without acting. Stops at a checkpoint with a resumeToken.")]
    public Task<CallToolResult> RunScenario(
        string scenario,
        JsonObject? @params = null,
        bool dryRun = false,
        bool allowWrites = false,
        CancellationToken cancellationToken = default) =>
        Run(() => ScenarioCommands.RunAsync(_engine, scenario, @params, dryRun, allowWrites, cancellationToken));

    [McpServerTool(Name = "resume_scenario", ReadOnly = false, Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(JsonObject))]
    [Description("Continues a run paused at a checkpoint (approve: true) or stops it (approve: false). Tokens are single-use; secret params must be passed again.")]
    public Task<CallToolResult> ResumeScenario(
        string resumeToken,
        bool approve = true,
        JsonObject? secretParams = null,
        CancellationToken cancellationToken = default) =>
        Run(() => ScenarioCommands.ResumeAsync(_engine, resumeToken, approve, secretParams, cancellationToken));

    [McpServerTool(Name = "validate_scenario", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(JsonObject))]
    [Description("Checks a scenario file: structure, params, and that every step exists. Does not touch the desktop.")]
    public Task<CallToolResult> ValidateScenario(string scenario, JsonObject? @params = null) =>
        Run(() => Task.FromResult(ScenarioCommands.Validate(_engine, scenario, @params)));

    [McpServerTool(Name = "list_scenarios", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(JsonObject))]
    [Description("Lists scenario files under the scenario roots with their params and descriptions.")]
    public Task<CallToolResult> ListScenarios() =>
        Run(() => Task.FromResult(ScenarioCommands.List(_engine)));

    private static async Task<CallToolResult> Run(Func<Task<object>> action)
    {
        try
        {
            return McpResults.Create(await action());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ScenarioException ex)
        {
            return McpResults.Create(ToolError.Create(ex.Code, ex.Message, ex.Details));
        }
        catch (ToolException ex)
        {
            return McpResults.Create(ex.Error);
        }
        catch (Exception ex)
        {
            return McpResults.Create(ToolError.Create(ErrorCodes.InternalError, ex.Message));
        }
    }
}
