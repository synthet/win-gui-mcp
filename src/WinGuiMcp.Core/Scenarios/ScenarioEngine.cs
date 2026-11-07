using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using WinGuiMcp.Common;

namespace WinGuiMcp.Scenarios;

// ---------------------------------------------------------------------- extension points

/// <summary>Supplies scenario steps. Namespace "" serves bare names (win-gui tools); "lr" serves "lr.*".</summary>
public interface IScenarioStepProvider
{
    string Namespace { get; }
    /// <summary>Null when the provider has no step of that name.</summary>
    StepDescriptor? Describe(string name);
    IEnumerable<StepDescriptor> List();
    Task<StepResult> ExecuteAsync(StepInvocation step, CancellationToken ct);
}

public sealed record StepDescriptor(string Name, string Description, bool ReadOnly);

public sealed record StepInvocation(string Name, JsonObject Args, bool DryRun, ScenarioTarget? Target, string RunDir);

public sealed record StepResult(bool Ok, JsonNode? Output, string? ErrorCode = null, string? ErrorMessage = null, bool GuardViolation = false)
{
    public static StepResult Success(JsonNode? output) => new(true, output);
    public static StepResult Failure(string code, string message, JsonNode? output = null, bool guard = false) => new(false, output, code, message, guard);
}

public sealed class ScenarioEngineOptions
{
    public string RunsDir { get; set; } = Environment.GetEnvironmentVariable("WINGUI_SCENARIO_RUNS")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinGuiMcp", "runs");
    public List<string> ScenarioRoots { get; } = [];
    public TimeSpan ResumeTokenLifetime { get; set; } = TimeSpan.FromHours(12);
    /// <summary>Optional: capture the screen to a PNG on failure (on_error.screenshot). Returns the path.</summary>
    public Func<string, string?>? ErrorCapture { get; set; }
}

public sealed class ScenarioRunOptions
{
    public Dictionary<string, JsonNode?> Params { get; init; } = new(StringComparer.Ordinal);
    public bool DryRun { get; init; }
    /// <summary>Write steps (non-read-only) run only when true. Without it they stop the run before acting.</summary>
    public bool AllowWrites { get; init; }
}

public static class ScenarioStatus
{
    public const string Passed = "passed", Failed = "failed", Blocked = "blocked", Paused = "paused", Invalid = "invalid", Aborted = "aborted";

    /// <summary>CLI exit codes (docs/scenarios.md): 0 passed, 1 failed, 2 blocked by a guard, 3 paused, 4 invalid.</summary>
    public static int ExitCode(string status) => status switch
    {
        Passed => 0, Failed => 1, Blocked => 2, Paused => 3, Invalid => 4, Aborted => 1, _ => 1
    };
}

public sealed record RunOutcome(
    string RunId,
    string Scenario,
    string Status,
    int ExitCode,
    bool DryRun,
    int StepsRun,
    int StepsTotal,
    string? ResumeToken,
    string? CheckpointMessage,
    string? RunDir,
    string? ReceiptPath,
    string? ErrorCode,
    string? ErrorMessage,
    int? FailedStep,
    JsonObject Vars);

// ---------------------------------------------------------------------- engine

public sealed class ScenarioEngine
{
    private readonly IReadOnlyList<IScenarioStepProvider> _providers;
    private readonly ScenarioEngineOptions _options;
    private static readonly AsyncLocal<int> Depth = new();

    public ScenarioEngine(IEnumerable<IScenarioStepProvider> providers, ScenarioEngineOptions options)
    {
        _providers = providers.ToList();
        _options = options;
    }

    public ScenarioEngineOptions Options => _options;

    /// <summary>True while a scenario step is executing on this async flow (blocks run_scenario recursion).</summary>
    public static bool InsideRun => Depth.Value > 0;

    public IEnumerable<(string Name, StepDescriptor Step)> ListSteps() =>
        _providers.SelectMany(p => p.List().Select(s => (p.Namespace.Length == 0 ? s.Name : $"{p.Namespace}.{s.Name}", s)));

    public string ResolvePath(string pathOrName)
    {
        if (File.Exists(pathOrName))
            return Path.GetFullPath(pathOrName);
        foreach (var root in _options.ScenarioRoots)
            foreach (var candidate in new[] { pathOrName, pathOrName + ".scenario.yaml", pathOrName + ".scenario.yml", pathOrName + ".scenario.json" })
            {
                var p = Path.Combine(root, candidate);
                if (File.Exists(p))
                    return Path.GetFullPath(p);
            }
        throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, $"Scenario '{pathOrName}' not found (searched {string.Join(", ", _options.ScenarioRoots)}).");
    }

    public IReadOnlyList<object> ListScenarios()
    {
        var found = new List<object>();
        foreach (var root in _options.ScenarioRoots.Where(Directory.Exists))
            foreach (var file in Directory.EnumerateFiles(root, "*.scenario.*", SearchOption.AllDirectories)
                         .Where(f => f.EndsWith(".yaml") || f.EndsWith(".yml") || f.EndsWith(".json")))
            {
                try
                {
                    var doc = ScenarioLoader.Load(file);
                    found.Add(new
                    {
                        name = doc.Name, path = doc.SourcePath, description = doc.Description, steps = doc.Steps.Count,
                        writes = doc.Writes, @params = doc.Params.Values.Select(p => new { p.Name, p.Type, p.Required, p.Secret, p.Description })
                    });
                }
                catch (ScenarioException ex)
                {
                    found.Add(new { path = file, invalid = ex.Message });
                }
            }
        return found;
    }

    /// <summary>Static validation: params resolve and every action maps to a control step or a provider step.</summary>
    public IReadOnlyList<string> Validate(ScenarioDocument doc, IReadOnlyDictionary<string, JsonNode?> args)
    {
        var problems = new List<string>();
        try { ResolveParams(doc, args); }
        catch (ScenarioException ex) { problems.Add(ex.Message); }
        foreach (var s in doc.Steps)
        {
            ValidateAction(doc, s.Action, s.Args, $"step {s.Index + 1}", problems);
            if (s.Raw["guard"] is { } g && g is not JsonObject)
                problems.Add($"step {s.Index + 1}: guard must be a mapping");
        }
        return problems;
    }

    private void ValidateAction(ScenarioDocument doc, string action, JsonNode? args, string where, List<string> problems)
    {
        if (ScenarioLoader.ControlActions.Contains(action))
        {
            if (action == "wait_until")
            {
                var inner = (args as JsonObject)?["do"] as JsonObject;
                if (inner is null || inner.Count != 1 || (args as JsonObject)?["until"] is null)
                    problems.Add($"{where}: wait_until needs 'do' (one step) and 'until' (an expression)");
                else
                    ValidateAction(doc, inner.First().Key, inner.First().Value, where + " (do)", problems);
            }
            if (action == "call")
            {
                try
                {
                    var child = ScenarioLoader.Load(ResolveChild(doc, (args as JsonObject)?["scenario"]?.ToString() ?? ""));
                    if (child.Steps.Any(c => c.Action == "checkpoint"))
                        problems.Add($"{where}: called scenario '{child.Name}' contains a checkpoint; checkpoints are only allowed in the top-level scenario");
                }
                catch (ScenarioException ex) { problems.Add($"{where}: {ex.Message}"); }
            }
            return;
        }
        if (FindStep(action) is null)
            problems.Add($"{where}: unknown step '{action}'");
    }

    public Task<RunOutcome> RunAsync(ScenarioDocument doc, ScenarioRunOptions run, CancellationToken ct)
    {
        var problems = Validate(doc, run.Params);
        var runId = $"{Slug(doc.Name)}-{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}-{RandomNumberGenerator.GetHexString(4).ToLowerInvariant()}";
        if (problems.Count > 0)
            return Task.FromResult(new RunOutcome(runId, doc.Name, ScenarioStatus.Invalid, 4, run.DryRun, 0, doc.Steps.Count, null, null, null, null,
                ScenarioErrorCodes.ScenarioInvalid, string.Join("; ", problems), null, []));

        var state = new RunState
        {
            RunId = runId, ScenarioPath = doc.SourcePath, ScenarioSha256 = doc.Sha256, DryRun = run.DryRun, AllowWrites = run.AllowWrites,
            Params = ResolveParams(doc, run.Params), Vars = [], StepOutputs = [], NextIndex = 0,
            SecretNames = doc.Params.Values.Where(p => p.Secret).Select(p => p.Name).ToList()
        };
        return ExecuteAsync(doc, state, receipt: [], ct);
    }

    public async Task<RunOutcome> ResumeAsync(string token, bool approve, IReadOnlyDictionary<string, JsonNode?>? secretParams, CancellationToken ct)
    {
        var (runDir, state) = RunStore.Claim(_options.RunsDir, token, _options.ResumeTokenLifetime);
        var doc = ScenarioLoader.Load(state.ScenarioPath);
        if (doc.Sha256 != state.ScenarioSha256)
            throw new ScenarioException(ScenarioErrorCodes.ResumeTokenInvalid,
                $"{Path.GetFileName(state.ScenarioPath)} changed since the run paused; start a new run.");
        var receipt = RunStore.LoadReceiptSteps(runDir);
        if (!approve)
        {
            receipt.Add(new JsonObject { ["index"] = state.NextIndex, ["action"] = "checkpoint", ["status"] = "rejected" });
            return Finish(doc, state, receipt, ScenarioStatus.Aborted, state.NextIndex, ScenarioErrorCodes.GuardFailed, "Checkpoint rejected; run stopped.", null);
        }
        foreach (var name in state.SecretNames)
        {
            if (secretParams is null || !secretParams.TryGetValue(name, out var value))
                throw new ScenarioException(ScenarioErrorCodes.ResumeTokenInvalid, $"Secret param '{name}' is not stored; pass it again to resume.");
            state.Params[name] = value?.DeepClone();
        }
        state.NextIndex++; // continue after the checkpoint
        return await ExecuteAsync(doc, state, receipt, ct).ConfigureAwait(false);
    }

    private async Task<RunOutcome> ExecuteAsync(ScenarioDocument doc, RunState state, JsonArray receipt, CancellationToken ct)
    {
        var runDir = Path.Combine(_options.RunsDir, state.RunId);
        Directory.CreateDirectory(runDir);
        var secrets = state.SecretNames.Select(n => ScenarioExpression.Text(state.Params.GetValueOrDefault(n))).Where(s => s.Length > 0).ToArray();

        for (var i = state.NextIndex; i < doc.Steps.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var step = doc.Steps[i];
            var scope = Scope(state);
            var entry = new JsonObject { ["index"] = i + 1, ["id"] = step.Id, ["action"] = step.Action };
            receipt.Add(entry);
            var sw = Stopwatch.StartNew();
            try
            {
                if (step.Raw["when"] is { } when && !ScenarioExpression.IsTruthy(Eval(when, scope)))
                {
                    entry["status"] = "skipped";
                    continue;
                }

                if (step.Action == "checkpoint")
                {
                    var message = ScenarioExpression.Text(ScenarioInterpolation.Apply(
                        step.Args is JsonObject co ? co["message"] : step.Args, scope));
                    entry["message"] = Redact(message, secrets);
                    if (state.DryRun)
                    {
                        entry["status"] = "checkpoint (dry run, not paused)";
                        continue;
                    }
                    entry["status"] = "paused";
                    state.NextIndex = i;
                    var token = RunStore.Pause(runDir, state);
                    var paused = Finish(doc, state, receipt, ScenarioStatus.Paused, i, null, null, null, secrets);
                    return paused with { ResumeToken = token, CheckpointMessage = message };
                }

                var outcome = await RunStepAsync(doc, step.Action, step.Args, step.Raw, state, scope, runDir, entry, secrets, ct).ConfigureAwait(false);
                entry["ms"] = sw.ElapsedMilliseconds;
                if (outcome is { } failure)
                {
                    if (step.Raw["continueOnError"]?.GetValue<bool>() == true)
                    {
                        entry["status"] = "failed (continued)";
                        continue;
                    }
                    var capture = failure.Status == ScenarioStatus.Failed ? CaptureOnError(doc, runDir, i) : null;
                    if (capture is not null) entry["errorCapture"] = capture;
                    return Finish(doc, state, receipt, failure.Status, i, failure.Code, Redact(failure.Message, secrets), i + 1, secrets);
                }
                entry["status"] ??= "ok";
            }
            catch (ScenarioException ex)
            {
                entry["status"] = "failed";
                entry["error"] = Redact(ex.Message, secrets);
                var status = ex.Code is ScenarioErrorCodes.TargetNotAllowed or ScenarioErrorCodes.WritesNotAllowed or ScenarioErrorCodes.GuardFailed
                    ? ScenarioStatus.Blocked
                    : ex.Code == ScenarioErrorCodes.ScenarioInvalid ? ScenarioStatus.Invalid : ScenarioStatus.Failed;
                return Finish(doc, state, receipt, status, i, ex.Code, Redact(ex.Message, secrets), i + 1, secrets);
            }
        }

        return Finish(doc, state, receipt, ScenarioStatus.Passed, doc.Steps.Count, null, null, null, secrets);
    }

    private sealed record StepFailure(string Status, string Code, string Message);

    private async Task<StepFailure?> RunStepAsync(ScenarioDocument doc, string action, JsonNode? rawArgs, JsonObject raw, RunState state,
        JsonObject scope, string runDir, JsonObject entry, string[] secrets, CancellationToken ct)
    {
        switch (action)
        {
            case "set":
                if (rawArgs is not JsonObject assignments)
                    throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, "set needs a mapping of variable: value");
                foreach (var (name, value) in assignments)
                    state.Vars[name] = ScenarioInterpolation.Apply(value, scope);
                return null;

            case "log":
                entry["message"] = Redact(ScenarioExpression.Text(ScenarioInterpolation.Apply(rawArgs is JsonObject lo ? lo["message"] : rawArgs, scope)), secrets);
                return null;

            case "fail":
                return new StepFailure(ScenarioStatus.Failed, ScenarioErrorCodes.AssertionFailed,
                    ScenarioExpression.Text(ScenarioInterpolation.Apply(rawArgs is JsonObject fo ? fo["message"] : rawArgs, scope)));

            case "assert":
            {
                var expr = rawArgs is JsonObject ao ? ao["expression"]?.ToString() : rawArgs?.ToString();
                var message = rawArgs is JsonObject am ? am["message"]?.ToString() : null;
                if (string.IsNullOrWhiteSpace(expr))
                    throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, "assert needs an expression");
                if (state.DryRun)
                {
                    entry["status"] = "assert (dry run, not evaluated)";
                    return null;
                }
                if (!ScenarioExpression.IsTruthy(ScenarioExpression.Evaluate(expr, scope)))
                    return new StepFailure(ScenarioStatus.Failed, ScenarioErrorCodes.AssertionFailed,
                        message is null ? $"assert failed: {expr}" : $"{ScenarioExpression.Text(ScenarioInterpolation.ApplyString(message, scope))} ({expr})");
                return null;
            }

            case "wait_until":
            {
                var o = (JsonObject)rawArgs!;
                var inner = (JsonObject)o["do"]!;
                var until = o["until"]!.ToString();
                var timeout = TimeSpan.FromMilliseconds(Num(o["timeoutMs"], 10000));
                var interval = TimeSpan.FromMilliseconds(Num(o["intervalMs"], 500));
                var started = Stopwatch.StartNew();
                var attempts = 0;
                while (true)
                {
                    attempts++;
                    var innerEntry = new JsonObject();
                    var fail = await RunStepAsync(doc, inner.First().Key, inner.First().Value, raw, state, Scope(state), runDir, innerEntry, secrets, ct).ConfigureAwait(false);
                    if (fail is not null && fail.Status != ScenarioStatus.Failed)
                        return fail;
                    if (state.DryRun || (fail is null && ScenarioExpression.IsTruthy(ScenarioExpression.Evaluate(until, Scope(state)))))
                    {
                        entry["attempts"] = attempts;
                        return null;
                    }
                    if (started.Elapsed >= timeout)
                        return new StepFailure(ScenarioStatus.Failed, ErrorCodes.Timeout,
                            $"wait_until '{until}' still false after {attempts} attempts ({timeout.TotalSeconds:0.#} s){(fail is null ? "" : $"; last error: {fail.Message}")}");
                    await Task.Delay(interval, ct).ConfigureAwait(false);
                }
            }

            case "call":
            {
                var o = rawArgs as JsonObject ?? throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, "call needs {scenario, params}");
                var child = ScenarioLoader.Load(ResolveChild(doc, o["scenario"]?.ToString() ?? ""));
                var childParams = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
                if (ScenarioInterpolation.Apply(o["params"], scope) is JsonObject cp)
                    foreach (var (k, v) in cp) childParams[k] = v?.DeepClone();
                var childState = new RunState
                {
                    RunId = state.RunId, ScenarioPath = child.SourcePath, ScenarioSha256 = child.Sha256, DryRun = state.DryRun,
                    AllowWrites = state.AllowWrites, Params = ResolveParams(child, childParams), Vars = [], StepOutputs = [], NextIndex = 0,
                    SecretNames = child.Params.Values.Where(p => p.Secret).Select(p => p.Name).ToList()
                };
                var childReceipt = new JsonArray();
                var result = await ExecuteAsync(child, childState, childReceipt, ct).ConfigureAwait(false);
                entry["steps"] = childReceipt;
                state.StepOutputs[raw["id"]?.ToString() ?? $"call{entry["index"]}"] = new JsonObject { ["ok"] = result.Status == ScenarioStatus.Passed, ["output"] = childState.Vars.DeepClone() };
                if (raw["save"] is JsonObject save)
                    foreach (var (name, path) in save)
                        state.Vars[name] = ScenarioInterpolation.Select(childState.Vars, path?.ToString() ?? "$");
                return result.Status == ScenarioStatus.Passed ? null
                    : new StepFailure(result.Status, result.ErrorCode ?? ScenarioErrorCodes.StepFailed, $"called scenario '{child.Name}': {result.ErrorMessage}");
            }
        }

        // ------------------------------------------------ provider step
        var (provider, descriptor, local) = FindStep(action)
            ?? throw new ScenarioException(ScenarioErrorCodes.StepNotFound, $"unknown step '{action}'");
        var args = ScenarioInterpolation.Apply(rawArgs ?? new JsonObject(), scope) as JsonObject
            ?? throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, $"{action}: arguments must be a mapping");
        entry["args"] = JsonNode.Parse(Redact(args.ToJsonString(), secrets));
        entry["readOnly"] = descriptor.ReadOnly;

        if (!descriptor.ReadOnly && !state.AllowWrites && !state.DryRun)
            throw new ScenarioException(ScenarioErrorCodes.WritesNotAllowed,
                $"'{action}' changes the target app; run again with allowWrites (CLI --allow-writes) to let this scenario write.");

        if (raw["guard"] is JsonObject guard && !state.DryRun)
            ForegroundGuard.Check(guard, scope);

        var retry = raw["retry"] as JsonObject;
        var attemptsAllowed = (int)Num(retry?["attempts"], 1);
        var backoff = retry?["backoffMs"];
        StepResult result2 = StepResult.Failure(ScenarioErrorCodes.StepFailed, "not run");
        for (var attempt = 1; attempt <= Math.Max(1, attemptsAllowed); attempt++)
        {
            Depth.Value++;
            try
            {
                result2 = await provider.ExecuteAsync(new StepInvocation(local, (JsonObject)args.DeepClone(), state.DryRun, doc.Target, runDir), ct).ConfigureAwait(false);
            }
            finally
            {
                Depth.Value--;
            }
            if (result2.Ok || result2.GuardViolation || attempt >= attemptsAllowed)
                break;
            var delay = backoff is JsonArray ba ? Num(ba[Math.Min(attempt - 1, ba.Count - 1)], 500) : Num(backoff, 500);
            await Task.Delay(TimeSpan.FromMilliseconds(delay), ct).ConfigureAwait(false);
            entry["retries"] = attempt;
        }

        entry["output"] = Truncate(result2.Output, secrets);
        if (raw["id"]?.ToString() is { } id)
            state.StepOutputs[id] = new JsonObject { ["ok"] = result2.Ok, ["output"] = result2.Output?.DeepClone() };

        if (!result2.Ok)
        {
            entry["status"] = "failed";
            entry["error"] = Redact($"{result2.ErrorCode}: {result2.ErrorMessage}", secrets);
            return new StepFailure(result2.GuardViolation ? ScenarioStatus.Blocked : ScenarioStatus.Failed,
                result2.ErrorCode ?? ScenarioErrorCodes.StepFailed,
                result2.ErrorMessage?.StartsWith(action + ":", StringComparison.Ordinal) == true ? result2.ErrorMessage : $"{action}: {result2.ErrorMessage}");
        }

        if (raw["save"] is JsonObject saves)
            foreach (var (name, path) in saves)
                // In a dry run nothing produced real output, so later steps get a placeholder and are still schema-checked.
                state.Vars[name] = state.DryRun ? JsonValue.Create($"<dry-run:{name}>") : ScenarioInterpolation.Select(result2.Output, path?.ToString() ?? "$");

        if (raw["expect"] is { } expect && !state.DryRun)
        {
            var s2 = Scope(state);
            s2["result"] = result2.Output?.DeepClone();
            var exprText = expect is JsonObject eo ? eo["expression"]?.ToString() : expect.ToString();
            if (!ScenarioExpression.IsTruthy(ScenarioExpression.Evaluate(exprText ?? "false", s2)))
                return new StepFailure(ScenarioStatus.Failed, ScenarioErrorCodes.AssertionFailed, $"{action}: expect failed: {exprText}");
        }
        return null;
    }

    private (IScenarioStepProvider Provider, StepDescriptor Step, string Local)? FindStep(string action)
    {
        var dot = action.IndexOf('.');
        var ns = dot > 0 ? action[..dot] : "";
        var local = dot > 0 ? action[(dot + 1)..] : action;
        foreach (var p in _providers.Where(p => p.Namespace == ns))
            if (p.Describe(local) is { } d)
                return (p, d, local);
        return null;
    }

    private RunOutcome Finish(ScenarioDocument doc, RunState state, JsonArray receipt, string status, int stepsRun,
        string? code, string? message, int? failedStep, string[]? secrets = null)
    {
        var runDir = Path.Combine(_options.RunsDir, state.RunId);
        Directory.CreateDirectory(runDir);
        var vars = JsonNode.Parse(Redact(state.Vars.ToJsonString(), secrets ?? []))!.AsObject();
        var receiptPath = Path.Combine(runDir, "receipt.json");
        var doc2 = new JsonObject
        {
            ["schema"] = "wingui-scenario-receipt-v1",
            ["runId"] = state.RunId,
            ["scenario"] = doc.Name,
            ["scenarioPath"] = doc.SourcePath,
            ["scenarioSha256"] = doc.Sha256,
            ["status"] = status,
            ["dryRun"] = state.DryRun,
            ["allowWrites"] = state.AllowWrites,
            ["finishedUtc"] = DateTimeOffset.UtcNow.ToString("O"),
            ["error"] = code is null ? null : new JsonObject { ["code"] = code, ["message"] = message, ["step"] = failedStep },
            ["params"] = RedactParams(state),
            ["vars"] = vars,
            ["steps"] = JsonNode.Parse(Redact(receipt.ToJsonString(), secrets ?? []))
        };
        File.WriteAllText(receiptPath, doc2.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return new RunOutcome(state.RunId, doc.Name, status, ScenarioStatus.ExitCode(status), state.DryRun, stepsRun, doc.Steps.Count,
            null, null, runDir, receiptPath, code, message, failedStep, vars);
    }

    private string? CaptureOnError(ScenarioDocument doc, string runDir, int index)
    {
        if (doc.OnError?["screenshot"]?.GetValue<bool>() != true || _options.ErrorCapture is null)
            return null;
        try { return _options.ErrorCapture(Path.Combine(runDir, $"error-step{index + 1}.png")); }
        catch (Exception) { return null; }
    }

    private static JsonObject Scope(RunState state)
    {
        var scope = new JsonObject();
        foreach (var (k, v) in state.Vars) scope[k] = v?.DeepClone();
        scope["params"] = new JsonObject(state.Params.Select(kv => KeyValuePair.Create(kv.Key, kv.Value?.DeepClone())));
        scope["steps"] = state.StepOutputs.DeepClone();
        return scope;
    }

    private static JsonNode? Eval(JsonNode when, JsonObject scope) =>
        when is JsonValue v && v.TryGetValue(out string? s) && s is not null && !s.Contains("${")
            ? ScenarioExpression.Evaluate(s, scope)
            : ScenarioInterpolation.Apply(when, scope);

    private static Dictionary<string, JsonNode?> ResolveParams(ScenarioDocument doc, IReadOnlyDictionary<string, JsonNode?> given)
    {
        var unknown = given.Keys.Where(k => !doc.Params.ContainsKey(k)).ToArray();
        if (unknown.Length > 0)
            throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, $"unknown param(s): {string.Join(", ", unknown)}");
        var resolved = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        foreach (var p in doc.Params.Values)
        {
            var value = given.TryGetValue(p.Name, out var g) ? g?.DeepClone() : p.Default?.DeepClone();
            if (value is null)
            {
                if (p.Required) throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, $"param '{p.Name}' is required");
                resolved[p.Name] = null;
                continue;
            }
            if (p.Type is "number" or "integer")
            {
                if (!ScenarioExpression.TryNum(value, out var d) &&
                    !double.TryParse(ScenarioExpression.Text(value), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d))
                    throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, $"param '{p.Name}' must be a number");
                if (p.Min is { } min && d < min || p.Max is { } max && d > max)
                    throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, $"param '{p.Name}'={d} is outside [{p.Min}, {p.Max}]");
                value = p.Type == "integer"
                    ? (d is >= int.MinValue and <= int.MaxValue ? JsonValue.Create((int)d) : JsonValue.Create((long)d))
                    : JsonValue.Create(d);
            }
            else if (p.Type == "boolean" && value is JsonValue bv && bv.TryGetValue(out string? bs))
                value = JsonValue.Create(bs is "true" or "1" or "yes");
            resolved[p.Name] = value;
        }
        return resolved;
    }

    private static JsonObject RedactParams(RunState state) =>
        new(state.Params.Select(kv => KeyValuePair.Create(kv.Key, state.SecretNames.Contains(kv.Key) ? (JsonNode?)"***" : kv.Value?.DeepClone())));

    private static string Redact(string text, string[] secrets)
    {
        foreach (var s in secrets.Where(s => s.Length >= 3))
            text = text.Replace(s, "***", StringComparison.Ordinal);
        return text;
    }

    private static JsonNode? Truncate(JsonNode? output, string[] secrets)
    {
        if (output is null) return null;
        var json = Redact(output.ToJsonString(), secrets);
        // Strip inline images: receipts keep paths, not pixels.
        json = System.Text.RegularExpressions.Regex.Replace(json, "\"(imageBase64|data)\":\"[A-Za-z0-9+/=]{200,}\"", "\"$1\":\"(stripped)\"");
        return json.Length <= 4000 ? JsonNode.Parse(json) : JsonValue.Create(json[..4000] + "…(truncated)");
    }

    private static string ResolveChild(ScenarioDocument parent, string child)
    {
        if (string.IsNullOrWhiteSpace(child))
            throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, "call needs 'scenario'");
        var p = Path.IsPathRooted(child) ? child : Path.Combine(Path.GetDirectoryName(parent.SourcePath)!, child);
        foreach (var c in new[] { p, p + ".scenario.yaml", p + ".scenario.json" })
            if (File.Exists(c)) return c;
        throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, $"called scenario '{child}' not found next to {Path.GetFileName(parent.SourcePath)}");
    }

    private static double Num(JsonNode? n, double fallback) => ScenarioExpression.TryNum(n, out var d) ? d : fallback;

    private static string Slug(string s) =>
        new string(s.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-').ToArray()).Trim('-');
}

// ---------------------------------------------------------------------- run state and resume tokens

public sealed class RunState
{
    public required string RunId { get; set; }
    public required string ScenarioPath { get; set; }
    public required string ScenarioSha256 { get; set; }
    public bool DryRun { get; set; }
    public bool AllowWrites { get; set; }
    public required Dictionary<string, JsonNode?> Params { get; set; }
    public required JsonObject Vars { get; set; }
    public required JsonObject StepOutputs { get; set; }
    public int NextIndex { get; set; }
    public List<string> SecretNames { get; set; } = [];
}

internal static class RunStore
{
    /// <summary>Saves state (secrets removed) and returns a single-use token "runId.secret".</summary>
    public static string Pause(string runDir, RunState state)
    {
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(18)).ToLowerInvariant();
        var json = new JsonObject
        {
            ["runId"] = state.RunId,
            ["scenarioPath"] = state.ScenarioPath,
            ["scenarioSha256"] = state.ScenarioSha256,
            ["dryRun"] = state.DryRun,
            ["allowWrites"] = state.AllowWrites,
            ["params"] = new JsonObject(state.Params.Where(kv => !state.SecretNames.Contains(kv.Key))
                .Select(kv => KeyValuePair.Create(kv.Key, kv.Value?.DeepClone()))),
            ["secretNames"] = new JsonArray(state.SecretNames.Select(s => (JsonNode?)JsonValue.Create(s)).ToArray()),
            ["vars"] = state.Vars.DeepClone(),
            ["stepOutputs"] = state.StepOutputs.DeepClone(),
            ["nextIndex"] = state.NextIndex,
            ["tokenSha256"] = Hash(secret),
            ["pausedUtc"] = DateTimeOffset.UtcNow.ToString("O")
        };
        File.WriteAllText(Path.Combine(runDir, "state.json"), json.ToJsonString());
        return $"{state.RunId}.{secret}";
    }

    public static (string RunDir, RunState State) Claim(string runsDir, string token, TimeSpan lifetime)
    {
        var dot = token.LastIndexOf('.');
        if (dot <= 0)
            throw new ScenarioException(ScenarioErrorCodes.ResumeTokenInvalid, "Malformed resume token.");
        var runId = token[..dot];
        if (runId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || runId.Contains(".."))
            throw new ScenarioException(ScenarioErrorCodes.ResumeTokenInvalid, "Malformed resume token.");
        var runDir = Path.Combine(runsDir, runId);
        var statePath = Path.Combine(runDir, "state.json");
        if (!File.Exists(statePath))
            throw new ScenarioException(ScenarioErrorCodes.ResumeTokenInvalid, "No paused run for this token (already resumed, or unknown).");
        var j = JsonNode.Parse(File.ReadAllText(statePath))!.AsObject();
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(j["tokenSha256"]!.ToString()), Convert.FromHexString(Hash(token[(dot + 1)..]))))
            throw new ScenarioException(ScenarioErrorCodes.ResumeTokenInvalid, "Resume token does not match.");
        if (DateTimeOffset.Parse(j["pausedUtc"]!.ToString(), System.Globalization.CultureInfo.InvariantCulture) + lifetime < DateTimeOffset.UtcNow)
            throw new ScenarioException(ScenarioErrorCodes.ResumeTokenInvalid, "Resume token expired; start a new run.");
        File.Delete(statePath); // single use

        var state = new RunState
        {
            RunId = runId,
            ScenarioPath = j["scenarioPath"]!.ToString(),
            ScenarioSha256 = j["scenarioSha256"]!.ToString(),
            DryRun = j["dryRun"]!.GetValue<bool>(),
            AllowWrites = j["allowWrites"]!.GetValue<bool>(),
            Params = j["params"]!.AsObject().ToDictionary(kv => kv.Key, kv => kv.Value?.DeepClone(), StringComparer.Ordinal),
            SecretNames = j["secretNames"]!.AsArray().Select(s => s!.ToString()).ToList(),
            Vars = j["vars"]!.AsObject().DeepClone().AsObject(),
            StepOutputs = j["stepOutputs"]!.AsObject().DeepClone().AsObject(),
            NextIndex = j["nextIndex"]!.GetValue<int>()
        };
        return (runDir, state);
    }

    public static JsonArray LoadReceiptSteps(string runDir)
    {
        var path = Path.Combine(runDir, "receipt.json");
        return File.Exists(path) && JsonNode.Parse(File.ReadAllText(path))?["steps"] is JsonArray steps
            ? steps.DeepClone().AsArray()
            : [];
    }

    private static string Hash(string s) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(s))).ToLowerInvariant();
}
