using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using YamlDotNet.RepresentationModel;

namespace WinGuiMcp.Scenarios;

public static class ScenarioErrorCodes
{
    public const string ScenarioInvalid = "ScenarioInvalid";
    public const string StepNotFound = "StepNotFound";
    public const string AssertionFailed = "AssertionFailed";
    public const string TargetNotAllowed = "TargetNotAllowed";
    public const string WritesNotAllowed = "WritesNotAllowed";
    public const string GuardFailed = "GuardFailed";
    public const string ResumeTokenInvalid = "ResumeTokenInvalid";
    public const string StepFailed = "StepFailed";
}

public sealed class ScenarioException(string code, string message, Dictionary<string, object?>? details = null) : Exception(message)
{
    public string Code { get; } = code;
    public Dictionary<string, object?>? Details { get; } = details;
}

public sealed record ScenarioParam(string Name, string Type, bool Required, JsonNode? Default, bool Secret, double? Min, double? Max, string? Description);

public sealed record ScenarioTarget(IReadOnlyList<string> Processes, string? TitlePattern);

public sealed record ScenarioStep(int Index, string? Id, string Action, JsonNode? Args, JsonObject Raw);

public sealed record ScenarioDocument(
    string Name,
    int Version,
    string? Description,
    ScenarioTarget? Target,
    IReadOnlyDictionary<string, ScenarioParam> Params,
    IReadOnlyList<string> Writes,
    JsonObject? OnError,
    IReadOnlyList<ScenarioStep> Steps,
    string SourcePath,
    string Sha256);

/// <summary>Loads *.scenario.yaml / *.scenario.json files. See docs/scenarios.md and schemas/scenario.schema.json.</summary>
public static class ScenarioLoader
{
    /// <summary>Keys a step may carry besides its single action key.</summary>
    public static readonly HashSet<string> StepModifiers = new(StringComparer.Ordinal)
        { "id", "save", "retry", "when", "note", "expect", "guard", "timeoutMs", "continueOnError" };

    public static readonly HashSet<string> ControlActions = new(StringComparer.Ordinal)
        { "set", "assert", "wait_until", "checkpoint", "call", "log", "fail" };

    private static readonly HashSet<string> TopLevelKeys = new(StringComparer.Ordinal)
        { "scenario", "version", "description", "target", "params", "writes", "on_error", "steps", "profile" };

    public static ScenarioDocument Load(string path)
    {
        var full = Path.GetFullPath(path);
        if (!File.Exists(full))
            throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, $"Scenario file not found: {full}");
        var bytes = File.ReadAllBytes(full);
        var text = Encoding.UTF8.GetString(bytes);
        var node = full.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? JsonNode.Parse(text) : YamlToJson(text, full);
        return Parse(node as JsonObject ?? throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, $"{full}: top level must be a mapping."),
            full, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
    }

    public static ScenarioDocument Parse(JsonObject root, string sourcePath, string sha256)
    {
        var errors = new List<string>();
        foreach (var key in root.Select(kv => kv.Key).Where(k => !TopLevelKeys.Contains(k)))
            errors.Add($"unknown top-level key '{key}'");

        var name = Str(root["scenario"]);
        if (string.IsNullOrWhiteSpace(name)) errors.Add("'scenario' (a name) is required");
        var version = root["version"] is { } v && ScenarioExpression.TryNum(v, out var vd) ? (int)vd : 1;

        ScenarioTarget? target = null;
        if (root["target"] is JsonObject t)
        {
            var procs = t["processes"] is JsonArray pa ? pa.Select(p => Str(p) ?? "").Where(p => p.Length > 0).ToArray() : [];
            target = new ScenarioTarget(procs, Str(t["titlePattern"]));
        }
        else if (root["target"] is not null) errors.Add("'target' must be a mapping with 'processes' and/or 'titlePattern'");

        var parameters = new Dictionary<string, ScenarioParam>(StringComparer.Ordinal);
        if (root["params"] is JsonObject ps)
        {
            foreach (var (pname, pdef) in ps)
            {
                var d = pdef as JsonObject ?? new JsonObject { ["default"] = pdef?.DeepClone() };
                var type = Str(d["type"]) ?? "string";
                if (type is not ("string" or "number" or "integer" or "boolean" or "object" or "array"))
                    errors.Add($"param '{pname}': unknown type '{type}'");
                parameters[pname] = new ScenarioParam(pname, type, d["required"]?.GetValue<bool>() ?? false, d["default"]?.DeepClone(),
                    d["secret"]?.GetValue<bool>() ?? false, Num(d["min"]), Num(d["max"]), Str(d["description"]));
            }
        }

        var writes = root["writes"] is JsonArray wa ? wa.Select(w => Str(w) ?? "").ToArray() : [];
        var steps = new List<ScenarioStep>();
        if (root["steps"] is not JsonArray stepArray || stepArray.Count == 0)
            errors.Add("'steps' must be a non-empty list");
        else
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < stepArray.Count; i++)
            {
                if (stepArray[i] is not JsonObject s)
                {
                    errors.Add($"step {i + 1}: must be a mapping");
                    continue;
                }
                var actions = s.Select(kv => kv.Key).Where(k => !StepModifiers.Contains(k)).ToArray();
                if (actions.Length != 1)
                {
                    errors.Add($"step {i + 1}: needs exactly one action key, found {(actions.Length == 0 ? "none" : string.Join(", ", actions))}");
                    continue;
                }
                var id = Str(s["id"]);
                if (id is not null && !ids.Add(id)) errors.Add($"step {i + 1}: duplicate id '{id}'");
                if (s["save"] is { } save && save is not JsonObject) errors.Add($"step {i + 1}: 'save' must map variable names to $ paths");
                var action = actions[0];
                if (action == "checkpoint" && s[action] is not (JsonObject or JsonValue)) errors.Add($"step {i + 1}: checkpoint needs a message");
                steps.Add(new ScenarioStep(i, id, action, s[action]?.DeepClone(), s));
            }
        }

        if (errors.Count > 0)
            throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, $"{Path.GetFileName(sourcePath)}: " + string.Join("; ", errors),
                new Dictionary<string, object?> { ["errors"] = errors, ["file"] = sourcePath });

        return new ScenarioDocument(name!, version, Str(root["description"]), target, parameters, writes,
            root["on_error"] as JsonObject, steps, sourcePath, sha256);
    }

    private static string? Str(JsonNode? n) => n is JsonValue v && v.TryGetValue(out string? s) ? s : n?.ToString();
    private static double? Num(JsonNode? n) => ScenarioExpression.TryNum(n, out var d) ? d : null;

    // ------------------------------------------------------------------ YAML → JSON

    public static JsonNode? YamlToJson(string yaml, string? sourceName = null)
    {
        var stream = new YamlStream();
        try
        {
            stream.Load(new StringReader(yaml));
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, $"{sourceName ?? "yaml"}: {ex.Message}");
        }
        return stream.Documents.Count == 0 ? null : YamlNodeToJson(stream.Documents[0].RootNode);
    }

    private static JsonNode? YamlNodeToJson(YamlNode node) => node switch
    {
        YamlMappingNode m => new JsonObject(m.Children.Select(kv =>
            KeyValuePair.Create(((YamlScalarNode)kv.Key).Value ?? "", YamlNodeToJson(kv.Value)))),
        YamlSequenceNode s => new JsonArray(s.Children.Select(YamlNodeToJson).ToArray()),
        YamlScalarNode sc => Scalar(sc),
        _ => null
    };

    private static JsonNode? Scalar(YamlScalarNode s)
    {
        var text = s.Value ?? "";
        if (s.Style is not YamlDotNet.Core.ScalarStyle.Plain)
            return JsonValue.Create(text);
        switch (text)
        {
            case "" or "~" or "null" or "Null" or "NULL": return null;
            case "true" or "True" or "TRUE": return JsonValue.Create(true);
            case "false" or "False" or "FALSE": return JsonValue.Create(false);
        }
        if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var l))
            return l is >= int.MinValue and <= int.MaxValue ? JsonValue.Create((int)l) : JsonValue.Create(l);
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && !text.StartsWith('.') && text.Any(char.IsDigit))
            return JsonValue.Create(d);
        return JsonValue.Create(text);
    }
}
