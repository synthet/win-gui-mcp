using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using WinGuiMcp.Capture;

namespace WinGuiMcp.Scenarios;

public static class ScenarioServiceCollectionExtensions
{
    /// <summary>
    /// Registers the scenario engine with the win-gui tool provider. Hosts add their own providers with
    /// <see cref="AddScenarioStepProvider{T}"/> (lightroom-mcp registers "lr.*").
    /// </summary>
    public static IServiceCollection AddScenarioEngine(this IServiceCollection services, Action<ScenarioEngineOptions>? configure = null)
    {
        services.AddSingleton(sp =>
        {
            var o = new ScenarioEngineOptions();
            o.ScenarioRoots.Add(Path.Combine(Environment.CurrentDirectory, "scenarios"));
            o.ScenarioRoots.Add(Path.Combine(AppContext.BaseDirectory, "scenarios"));
            var capture = sp.GetRequiredService<WindowCapture>();
            o.ErrorCapture = path => capture.CaptureToFile(null, null, 1.0, path).Path;
            configure?.Invoke(o);
            return o;
        });
        services.AddSingleton<IScenarioStepProvider, WinGuiToolStepProvider>();
        services.AddSingleton(sp => new ScenarioEngine(sp.GetServices<IScenarioStepProvider>(), sp.GetRequiredService<ScenarioEngineOptions>()));
        return services;
    }

    public static IServiceCollection AddScenarioStepProvider<T>(this IServiceCollection services) where T : class, IScenarioStepProvider =>
        services.AddSingleton<IScenarioStepProvider, T>();
}

/// <summary>
/// Shared logic behind the run_scenario / resume_scenario / validate_scenario / list_scenarios tools and the
/// <c>scenario</c> CLI subcommand, so both surfaces behave identically.
/// </summary>
public static class ScenarioCommands
{
    public static async Task<object> RunAsync(ScenarioEngine engine, string scenario, JsonObject? parameters, bool dryRun, bool allowWrites, CancellationToken ct)
    {
        if (ScenarioEngine.InsideRun)
            throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, "run_scenario cannot be called from inside a scenario step; use call:.");
        var doc = ScenarioLoader.Load(engine.ResolvePath(scenario));
        var outcome = await engine.RunAsync(doc, new ScenarioRunOptions
        {
            Params = ToDict(parameters),
            DryRun = dryRun,
            AllowWrites = allowWrites
        }, ct).ConfigureAwait(false);
        return Shape(outcome);
    }

    public static async Task<object> ResumeAsync(ScenarioEngine engine, string token, bool approve, JsonObject? secretParams, CancellationToken ct) =>
        Shape(await engine.ResumeAsync(token, approve, ToDict(secretParams), ct).ConfigureAwait(false));

    public static object Validate(ScenarioEngine engine, string scenario, JsonObject? parameters)
    {
        var doc = ScenarioLoader.Load(engine.ResolvePath(scenario));
        var problems = engine.Validate(doc, ToDict(parameters));
        return new
        {
            ok = problems.Count == 0,
            scenario = doc.Name,
            path = doc.SourcePath,
            steps = doc.Steps.Count,
            writes = doc.Steps.Count(s => !ScenarioLoader.ControlActions.Contains(s.Action) && engine.ListSteps().Any(x => x.Name == s.Action && !x.Step.ReadOnly)),
            problems
        };
    }

    public static object List(ScenarioEngine engine) =>
        new { ok = true, roots = engine.Options.ScenarioRoots, scenarios = engine.ListScenarios() };

    public static object Shape(RunOutcome o) => new
    {
        ok = o.Status is ScenarioStatus.Passed or ScenarioStatus.Paused,
        status = o.Status,
        exitCode = o.ExitCode,
        scenario = o.Scenario,
        runId = o.RunId,
        dryRun = o.DryRun,
        stepsRun = o.StepsRun,
        stepsTotal = o.StepsTotal,
        resumeToken = o.ResumeToken,
        checkpoint = o.CheckpointMessage,
        error = o.ErrorCode is null ? null : new { code = o.ErrorCode, message = o.ErrorMessage, step = o.FailedStep },
        receipt = o.ReceiptPath,
        vars = o.Vars
    };

    /// <summary>
    /// CLI: <c>scenario run &lt;file&gt; [--param k=v]... [--dry-run] [--allow-writes]</c>,
    /// <c>scenario resume &lt;token&gt; [--reject] [--param secret=v]</c>, <c>scenario validate &lt;file&gt;</c>, <c>scenario list</c>.
    /// Prints JSON; exit code 0 passed, 1 failed, 2 blocked, 3 paused, 4 invalid.
    /// </summary>
    public static async Task<int> RunCliAsync(ScenarioEngine engine, string[] args, TextWriter output, CancellationToken ct)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            output.WriteLine("usage: scenario run <file> [--param k=v]... [--dry-run] [--allow-writes]\n       scenario resume <token> [--reject] [--param secret=v]\n       scenario validate <file> [--param k=v]...\n       scenario list");
            return args.Length == 0 ? 4 : 0;
        }
        var verb = args[0];
        var rest = args.Skip(1).ToList();
        var parameters = new JsonObject();
        for (var i = 0; i < rest.Count; i++)
        {
            if (rest[i] is "--param" or "-p" && i + 1 < rest.Count)
            {
                var kv = rest[i + 1];
                var eq = kv.IndexOf('=');
                if (eq <= 0) { output.WriteLine($"bad --param '{kv}', use key=value"); return 4; }
                parameters[kv[..eq]] = ParseCliValue(kv[(eq + 1)..]);
                rest.RemoveAt(i + 1);
                rest.RemoveAt(i);
                i--;
            }
        }
        var flags = rest.Where(a => a.StartsWith("--", StringComparison.Ordinal)).ToHashSet();
        var positional = rest.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToList();
        try
        {
            object result;
            int code;
            switch (verb)
            {
                case "run" when positional.Count == 1:
                    result = await RunAsync(engine, positional[0], parameters, flags.Contains("--dry-run"), flags.Contains("--allow-writes"), ct).ConfigureAwait(false);
                    code = ((dynamic)result).exitCode;
                    break;
                case "resume" when positional.Count == 1:
                    result = await ResumeAsync(engine, positional[0], !flags.Contains("--reject"), parameters, ct).ConfigureAwait(false);
                    code = ((dynamic)result).exitCode;
                    break;
                case "validate" when positional.Count == 1:
                    result = Validate(engine, positional[0], parameters);
                    code = ((dynamic)result).ok ? 0 : 4;
                    break;
                case "list":
                    result = List(engine);
                    code = 0;
                    break;
                default:
                    output.WriteLine($"unknown or incomplete command: scenario {string.Join(' ', args)}");
                    return 4;
            }
            output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            return code;
        }
        catch (ScenarioException ex)
        {
            output.WriteLine(JsonSerializer.Serialize(new { ok = false, code = ex.Code, message = ex.Message, details = ex.Details },
                new JsonSerializerOptions { WriteIndented = true }));
            return ex.Code == ScenarioErrorCodes.ScenarioInvalid ? 4 : ex.Code == ScenarioErrorCodes.ResumeTokenInvalid ? 4 : 1;
        }
    }

    private static JsonNode? ParseCliValue(string raw)
    {
        if (raw is "true" or "false") return JsonValue.Create(raw == "true");
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return JsonValue.Create(d);
        if (raw.StartsWith('{') || raw.StartsWith('['))
            try { return JsonNode.Parse(raw); } catch (JsonException) { }
        return JsonValue.Create(raw);
    }

    private static Dictionary<string, JsonNode?> ToDict(JsonObject? o) =>
        o is null ? new(StringComparer.Ordinal) : o.ToDictionary(kv => kv.Key, kv => kv.Value?.DeepClone(), StringComparer.Ordinal);
}
