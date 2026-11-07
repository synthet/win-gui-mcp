using System.Text.Json.Nodes;
using WinGuiMcp.Scenarios;

namespace WinGuiMcp.Tests;

internal sealed class FakeProvider : IScenarioStepProvider
{
    public List<StepInvocation> Calls { get; } = [];
    public int FlakyFailures { get; set; }
    public double Value { get; set; }

    public string Namespace => "fake";

    public StepDescriptor? Describe(string name) => name switch
    {
        "read" => new StepDescriptor("read", "returns the current value", ReadOnly: true),
        "write" => new StepDescriptor("write", "sets the value", ReadOnly: false),
        "flaky" => new StepDescriptor("flaky", "fails a few times", ReadOnly: true),
        "echo" => new StepDescriptor("echo", "returns its args", ReadOnly: true),
        _ => null
    };

    public IEnumerable<StepDescriptor> List() => new[] { "read", "write", "flaky", "echo" }.Select(n => Describe(n)!);

    public Task<StepResult> ExecuteAsync(StepInvocation step, CancellationToken ct)
    {
        Calls.Add(step);
        switch (step.Name)
        {
            case "read":
                Value += 1;
                return Task.FromResult(StepResult.Success(new JsonObject { ["value"] = Value, ["items"] = new JsonArray(1, 2, 3) }));
            case "write":
                if (!step.DryRun && ScenarioExpression.TryNum(step.Args["value"], out var v)) Value = v;
                return Task.FromResult(StepResult.Success(new JsonObject { ["ok"] = true, ["dryRun"] = step.DryRun }));
            case "flaky":
                return Task.FromResult(FlakyFailures-- > 0
                    ? StepResult.Failure("Timeout", "not yet")
                    : StepResult.Success(new JsonObject { ["ready"] = true }));
            default:
                return Task.FromResult(StepResult.Success(step.Args.DeepClone()));
        }
    }
}

public sealed class ScenarioEngineTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("wingui-scn-").FullName;
    private readonly FakeProvider _fake = new();
    private readonly ScenarioEngine _engine;

    public ScenarioEngineTests()
    {
        var options = new ScenarioEngineOptions { RunsDir = Path.Combine(_dir, "runs") };
        options.ScenarioRoots.Add(_dir);
        _engine = new ScenarioEngine([_fake], options);
    }

    public void Dispose() => Directory.Delete(_dir, true);

    private ScenarioDocument Write(string name, string yaml)
    {
        var path = Path.Combine(_dir, name + ".scenario.yaml");
        File.WriteAllText(path, yaml);
        return ScenarioLoader.Load(path);
    }

    private Task<RunOutcome> Run(ScenarioDocument doc, bool allowWrites = false, bool dryRun = false, Dictionary<string, JsonNode?>? p = null) =>
        _engine.RunAsync(doc, new ScenarioRunOptions { AllowWrites = allowWrites, DryRun = dryRun, Params = p ?? new() }, CancellationToken.None);

    // ------------------------------------------------------------- expressions and YAML

    [Theory]
    [InlineData("1 + 2 * 3", 7.0)]
    [InlineData("(1 + 2) * 3", 9.0)]
    [InlineData("a - b", 3.0)]
    [InlineData("abs(-0.25) + round(0.126, 2)", 0.38)]
    [InlineData("len(items)", 3.0)]
    [InlineData("items[1] + obj.inner.x", 12.0)]
    public void Expression_EvaluatesArithmeticPathsAndFunctions(string expr, double expected)
    {
        var scope = JsonNode.Parse("""{"a":5,"b":2,"items":[1,2,3],"obj":{"inner":{"x":10}}}""")!.AsObject();
        Assert.Equal(expected, ScenarioExpression.Evaluate(expr, scope)!.GetValue<double>(), 6);
    }

    [Theory]
    [InlineData("a > 1 && b <= 2", true)]
    [InlineData("!(a == 5) || name != 'x'", true)]
    [InlineData("contains(name, 'DSC') && startsWith(name, 'dsc')", true)]
    [InlineData("missing == null", true)]
    [InlineData("a >= 6", false)]
    public void Expression_EvaluatesBooleans(string expr, bool expected)
    {
        var scope = JsonNode.Parse("""{"a":5,"b":2,"name":"DSC_8750.NEF"}""")!.AsObject();
        Assert.Equal(expected, ScenarioExpression.IsTruthy(ScenarioExpression.Evaluate(expr, scope)));
    }

    [Fact]
    public void Expression_RejectsUnknownFunctions()
    {
        var ex = Assert.Throws<ScenarioException>(() => ScenarioExpression.Evaluate("system('calc')", []));
        Assert.Equal(ScenarioErrorCodes.ScenarioInvalid, ex.Code);
    }

    [Fact]
    public void Interpolation_WholePlaceholderKeepsType_EmbeddedBecomesText()
    {
        var scope = JsonNode.Parse("""{"ev":0.5,"params":{"photo":"DSC_8750"}}""")!.AsObject();
        Assert.Equal(0.5, ScenarioInterpolation.ApplyString("${ev}", scope)!.GetValue<double>());
        Assert.Equal("set 0.5 on DSC_8750", ScenarioInterpolation.ApplyString("set ${ev} on ${params.photo}", scope)!.GetValue<string>());
    }

    [Fact]
    public void Yaml_PlainScalarsAreTyped_QuotedStayStrings()
    {
        var node = ScenarioLoader.YamlToJson("a: 5\nb: '5'\nc: true\nd: 0.25\ne: ~\nf: DSC_8750\n")!;
        Assert.Equal(5, node["a"]!.GetValue<int>());
        Assert.Equal("5", node["b"]!.GetValue<string>());
        Assert.True(node["c"]!.GetValue<bool>());
        Assert.Equal(0.25, node["d"]!.GetValue<double>());
        Assert.Null(node["e"]);
        Assert.Equal("DSC_8750", node["f"]!.GetValue<string>());
    }

    [Fact]
    public void Loader_ReportsAllStructuralProblems()
    {
        var path = Path.Combine(_dir, "bad.scenario.yaml");
        File.WriteAllText(path, "scenario: bad\nbogus: 1\nsteps:\n  - fake.read: {}\n    fake.write: {}\n  - id: x\n");
        var ex = Assert.Throws<ScenarioException>(() => ScenarioLoader.Load(path));
        Assert.Contains("unknown top-level key 'bogus'", ex.Message);
        Assert.Contains("found fake.read, fake.write", ex.Message);
        Assert.Contains("found none", ex.Message);
    }

    // ------------------------------------------------------------- engine

    [Fact]
    public async Task Run_SavesVariablesAndPassesAssertions()
    {
        var doc = Write("pass", """
            scenario: pass
            params:
              want: { type: number, default: 2 }
            steps:
              - fake.read: {}
              - id: second
                fake.read: {}
                save: { v: $.value, first: '$.items[0]' }
              - assert: "v == params.want && first == 1"
              - set: { doubled: "${v * 2}" }
              - fake.echo: { msg: "got ${doubled}" }
                expect: "result.msg == 'got 4'"
            """);
        var outcome = await Run(doc);
        Assert.Equal(ScenarioStatus.Passed, outcome.Status);
        Assert.Equal(0, outcome.ExitCode);
        Assert.Equal(4.0, outcome.Vars["doubled"]!.GetValue<double>());
        Assert.True(File.Exists(outcome.ReceiptPath));
    }

    [Fact]
    public async Task Run_FailedAssertionExitsOne()
    {
        var doc = Write("fail", "scenario: fail\nsteps:\n  - fake.read: {}\n    save: { v: $.value }\n  - assert: { expression: 'v > 10', message: 'value was ${v}' }\n");
        var outcome = await Run(doc);
        Assert.Equal(ScenarioStatus.Failed, outcome.Status);
        Assert.Equal(1, outcome.ExitCode);
        Assert.Equal(ScenarioErrorCodes.AssertionFailed, outcome.ErrorCode);
        Assert.Contains("value was 1", outcome.ErrorMessage);
    }

    [Fact]
    public async Task Run_WriteStepsAreBlockedWithoutAllowWrites()
    {
        var doc = Write("writes", "scenario: writes\nsteps:\n  - fake.read: {}\n  - fake.write: { value: 9 }\n");
        var blocked = await Run(doc);
        Assert.Equal(ScenarioStatus.Blocked, blocked.Status);
        Assert.Equal(2, blocked.ExitCode);
        Assert.Equal(ScenarioErrorCodes.WritesNotAllowed, blocked.ErrorCode);
        Assert.DoesNotContain(_fake.Calls, c => c.Name == "write");

        var allowed = await Run(doc, allowWrites: true);
        Assert.Equal(ScenarioStatus.Passed, allowed.Status);
        Assert.Equal(9, _fake.Value);
    }

    [Fact]
    public async Task DryRun_NeverActuates()
    {
        var doc = Write("dry", "scenario: dry\nsteps:\n  - fake.write: { value: 9 }\n  - checkpoint: ok?\n  - assert: 'false'\n");
        var outcome = await Run(doc, dryRun: true);
        Assert.Equal(ScenarioStatus.Passed, outcome.Status);
        Assert.All(_fake.Calls, c => Assert.True(c.DryRun));
        Assert.Equal(0, _fake.Value);
    }

    [Fact]
    public async Task Checkpoint_PausesAndResumesOnceFromTheNextStep()
    {
        var doc = Write("pause", """
            scenario: pause
            steps:
              - fake.read: {}
                save: { before: $.value }
              - checkpoint: { message: "Value is ${before}; continue?" }
              - fake.read: {}
                save: { after: $.value }
            """);
        var paused = await Run(doc);
        Assert.Equal(ScenarioStatus.Paused, paused.Status);
        Assert.Equal(3, paused.ExitCode);
        Assert.Equal("Value is 1; continue?", paused.CheckpointMessage);
        Assert.Single(_fake.Calls);

        var resumed = await _engine.ResumeAsync(paused.ResumeToken!, approve: true, null, CancellationToken.None);
        Assert.Equal(ScenarioStatus.Passed, resumed.Status);
        Assert.Equal(1.0, resumed.Vars["before"]!.GetValue<double>());   // vars survive the pause
        Assert.Equal(2.0, resumed.Vars["after"]!.GetValue<double>());
        Assert.Equal(2, _fake.Calls.Count);                                // step 1 not repeated

        var again = await Assert.ThrowsAsync<ScenarioException>(() => _engine.ResumeAsync(paused.ResumeToken!, true, null, CancellationToken.None));
        Assert.Equal(ScenarioErrorCodes.ResumeTokenInvalid, again.Code);   // single use
    }

    [Fact]
    public async Task Resume_RefusesChangedScenarioAndForgedToken()
    {
        var doc = Write("edit", "scenario: edit\nsteps:\n  - checkpoint: go?\n  - fake.read: {}\n");
        var paused = await Run(doc);
        var forged = paused.ResumeToken![..^4] + "beef";
        Assert.Equal(ScenarioErrorCodes.ResumeTokenInvalid,
            (await Assert.ThrowsAsync<ScenarioException>(() => _engine.ResumeAsync(forged, true, null, CancellationToken.None))).Code);

        var paused2 = await Run(doc);
        File.AppendAllText(doc.SourcePath, "  - fake.read: {}\n");
        var ex = await Assert.ThrowsAsync<ScenarioException>(() => _engine.ResumeAsync(paused2.ResumeToken!, true, null, CancellationToken.None));
        Assert.Contains("changed", ex.Message);
    }

    [Fact]
    public async Task Resume_RejectStopsTheRun()
    {
        var doc = Write("reject", "scenario: reject\nsteps:\n  - checkpoint: go?\n  - fake.read: {}\n");
        var paused = await Run(doc);
        var stopped = await _engine.ResumeAsync(paused.ResumeToken!, approve: false, null, CancellationToken.None);
        Assert.Equal(ScenarioStatus.Aborted, stopped.Status);
        Assert.Empty(_fake.Calls);
    }

    [Fact]
    public async Task Retry_RetriesThenSucceeds()
    {
        _fake.FlakyFailures = 2;
        var doc = Write("retry", "scenario: retry\nsteps:\n  - fake.flaky: {}\n    retry: { attempts: 3, backoffMs: [1, 1] }\n");
        Assert.Equal(ScenarioStatus.Passed, (await Run(doc)).Status);
        Assert.Equal(3, _fake.Calls.Count);
    }

    [Fact]
    public async Task WaitUntil_PollsAStepUntilTheConditionHolds()
    {
        var doc = Write("wait", "scenario: wait\nsteps:\n  - wait_until: { do: { fake.read: {} }, until: 'v >= 3', intervalMs: 1, timeoutMs: 2000 }\n    save: { v: $.value }\n");
        var outcome = await Run(doc);
        Assert.True(outcome.Status == ScenarioStatus.Passed, outcome.ErrorMessage);
        Assert.Equal(3, _fake.Calls.Count);
    }

    [Fact]
    public async Task Secrets_AreRedactedFromReceiptsAndNotStoredOnPause()
    {
        var doc = Write("secret", """
            scenario: secret
            params:
              token: { type: string, secret: true, required: true }
            steps:
              - fake.echo: { auth: "Bearer ${params.token}" }
              - checkpoint: go?
            """);
        var paused = await Run(doc, p: new() { ["token"] = "s3cr3t-value" });
        var runDir = Path.GetDirectoryName(paused.ReceiptPath)!;
        Assert.DoesNotContain("s3cr3t-value", File.ReadAllText(paused.ReceiptPath!));
        Assert.DoesNotContain("s3cr3t-value", File.ReadAllText(Path.Combine(runDir, "state.json")));

        var noSecret = await Assert.ThrowsAsync<ScenarioException>(() => _engine.ResumeAsync(paused.ResumeToken!, true, null, CancellationToken.None));
        Assert.Contains("Secret param 'token'", noSecret.Message);
    }

    [Fact]
    public async Task UnknownStepOrParam_IsInvalidBeforeAnythingRuns()
    {
        var doc = Write("invalid", "scenario: invalid\nsteps:\n  - fake.read: {}\n  - fake.nope: {}\n");
        var outcome = await Run(doc, p: new() { ["extra"] = 1 });
        Assert.Equal(ScenarioStatus.Invalid, outcome.Status);
        Assert.Equal(4, outcome.ExitCode);
        Assert.Contains("unknown step 'fake.nope'", outcome.ErrorMessage);
        Assert.Contains("unknown param", outcome.ErrorMessage);
        Assert.Empty(_fake.Calls);
    }

    [Fact]
    public async Task Call_RunsSubScenarioAndRejectsNestedCheckpoints()
    {
        Write("child", "scenario: child\nparams:\n  n: { type: number }\nsteps:\n  - set: { out: '${params.n + 1}' }\n");
        Write("child-pause", "scenario: child-pause\nsteps:\n  - checkpoint: nope\n");
        var parent = Write("parent", "scenario: parent\nsteps:\n  - call: { scenario: child, params: { n: 41 } }\n    save: { answer: $.out }\n");
        var outcome = await Run(parent);
        Assert.Equal(ScenarioStatus.Passed, outcome.Status);
        Assert.Equal(42.0, outcome.Vars["answer"]!.GetValue<double>());

        var bad = Write("parent2", "scenario: parent2\nsteps:\n  - call: { scenario: child-pause }\n");
        Assert.Contains("checkpoint", (await Run(bad)).ErrorMessage);
    }

    // ------------------------------------------------------------- guards

    [Theory]
    [InlineData("SystemSettings.exe")]
    [InlineData("control.exe")]
    [InlineData("regedit.exe")]
    public void TargetGuard_AlwaysDeniesRestrictedApps(string process)
    {
        var owner = new WindowOwner(1, 1, process, "x", "x");
        var target = new ScenarioTarget([process], null);
        Assert.Equal(ScenarioErrorCodes.TargetNotAllowed,
            Assert.Throws<ScenarioException>(() => TargetGuard.EnsureWindowAllowed(target, owner, "click")).Code);
    }

    [Fact]
    public void TargetGuard_RequiresDeclaredTargetAndMatchingProcess()
    {
        var owner = new WindowOwner(1, 1, "notepad.exe", "Notepad", "Untitled - Notepad");
        Assert.Throws<ScenarioException>(() => TargetGuard.EnsureWindowAllowed(null, owner, "click"));
        Assert.Throws<ScenarioException>(() => TargetGuard.EnsureWindowAllowed(new ScenarioTarget(["lightroom.exe"], null), owner, "click"));
        TargetGuard.EnsureWindowAllowed(new ScenarioTarget(["notepad"], null), owner, "click");
        Assert.Throws<ScenarioException>(() => TargetGuard.EnsureLaunchAllowed(null, "ms-settings:display", null, "launch_app"));
    }
}
