using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using WinGuiMcp.Action;
using WinGuiMcp.Capture;
using WinGuiMcp.Perception;
using WinGuiMcp.Platform;
using WinGuiMcp.Session;
using WinGuiMcp.Tools;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<DpiManager>();
builder.Services.AddSingleton<WindowEnumerator>();
builder.Services.AddSingleton<SnapshotSession>();
builder.Services.AddSingleton<UiaSnapshotBuilder>();
builder.Services.AddSingleton<InputSimulator>();
builder.Services.AddSingleton<PatternExecutor>();
builder.Services.AddSingleton<WindowCapture>();
builder.Services.AddSingleton<ToolService>();
builder.Services.AddSingleton<Phase1Tools>();

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "win-gui-mcp",
            Version = "0.1.0"
        };
    })
    .WithStdioServerTransport()
    .WithTools<Phase1Tools>();

await builder.Build().RunAsync();
