using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using WinGuiMcp;
using WinGuiMcp.Scenarios;
using WinGuiMcp.Tools;

var compactMode = args.Any(a => string.Equals(a, "--compact-tools", StringComparison.OrdinalIgnoreCase)) ||
    string.Equals(Environment.GetEnvironmentVariable("MCP_TOOL_SEARCH_MODE"), "compact", StringComparison.OrdinalIgnoreCase);

// CLI: `WinGuiMcp scenario run|resume|validate|list ...` runs the same engine as the run_scenario tools.
if (args.Length > 0 && args[0] == "scenario")
{
    var cli = new ServiceCollection();
    cli.AddWinGuiCore(o => o.WarnIfNotPerMonitorV2 = false);
    cli.AddScenarioEngine();
    using var provider = cli.BuildServiceProvider();
    return await ScenarioCommands.RunCliAsync(provider.GetRequiredService<ScenarioEngine>(), args[1..], Console.Out, CancellationToken.None);
}

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddWinGuiCore();
builder.Services.AddScenarioEngine();
builder.Services.AddSingleton<Phase1Tools>();
builder.Services.AddSingleton<ScenarioTools>();
builder.Services.AddSingleton<CompactDiscoveryTools>();

var mcpBuilder = builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "win-gui-mcp",
            Version = "0.1.0"
        };
    })
    .WithStdioServerTransport();

if (compactMode)
    mcpBuilder.WithTools<CompactDiscoveryTools>();
else
    mcpBuilder.WithTools<Phase1Tools>();
mcpBuilder.WithTools<ScenarioTools>();

await builder.Build().RunAsync();
return 0;
