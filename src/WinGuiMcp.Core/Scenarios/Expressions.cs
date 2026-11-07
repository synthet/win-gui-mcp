using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace WinGuiMcp.Scenarios;

/// <summary>
/// Safe expression language for scenarios (docs/scenarios.md). No code execution.
/// Literals: numbers, 'text' / "text", true, false, null. Paths: name, a.b, a[0], params.x, steps.id.output.
/// Operators: || &amp;&amp; ! == != &lt; &lt;= &gt; &gt;= + - * / and parentheses.
/// Functions: abs(x) round(x[,n]) min(a,b) max(a,b) len(x) contains(haystack, needle) startsWith(s,p) number(x) string(x).
/// </summary>
public static class ScenarioExpression
{
    public static JsonNode? Evaluate(string expression, JsonObject scope) => new Parser(expression, scope).ParseAll();

    public static bool IsTruthy(JsonNode? v) => v switch
    {
        null => false,
        JsonValue jv when jv.TryGetValue(out bool b) => b,
        JsonValue jv when TryNum(jv, out var d) => d != 0,
        JsonValue jv when jv.TryGetValue(out string? s) => !string.IsNullOrEmpty(s),
        JsonArray a => a.Count > 0,
        JsonObject o => o.Count > 0,
        _ => true
    };

    internal static bool TryNum(JsonNode? n, out double d)
    {
        d = 0;
        if (n is not JsonValue v) return false;
        if (v.TryGetValue(out double x)) { d = x; return true; }
        if (v.TryGetValue(out int i)) { d = i; return true; }
        if (v.TryGetValue(out long l)) { d = l; return true; }
        if (v.TryGetValue(out decimal m)) { d = (double)m; return true; }
        return false;
    }

    internal static string Text(JsonNode? n) => n switch
    {
        null => "null",
        JsonValue v when v.TryGetValue(out string? s) => s ?? "",
        JsonValue v when TryNum(v, out var d) => d.ToString("R", CultureInfo.InvariantCulture),
        _ => n.ToJsonString()
    };

    private sealed class Parser(string src, JsonObject scope)
    {
        private int _pos;

        public JsonNode? ParseAll()
        {
            var v = Or();
            Skip();
            if (_pos < src.Length)
                throw Error($"unexpected '{src[_pos]}'");
            return v;
        }

        private JsonNode? Or()
        {
            var left = And();
            while (Eat("||")) { var right = And(); left = Bool(IsTruthy(left) || IsTruthy(right)); }
            return left;
        }

        private JsonNode? And()
        {
            var left = Equality();
            while (Eat("&&")) { var right = Equality(); left = Bool(IsTruthy(left) && IsTruthy(right)); }
            return left;
        }

        private JsonNode? Equality()
        {
            var left = Compare();
            while (true)
            {
                if (Eat("==")) left = Bool(AreEqual(left, Compare()));
                else if (Eat("!=")) left = Bool(!AreEqual(left, Compare()));
                else return left;
            }
        }

        private JsonNode? Compare()
        {
            var left = Additive();
            while (true)
            {
                string? op = Eat("<=") ? "<=" : Eat(">=") ? ">=" : Eat("<") ? "<" : Eat(">") ? ">" : null;
                if (op is null) return left;
                var a = Num(left, op); var b = Num(Additive(), op);
                left = Bool(op switch { "<" => a < b, "<=" => a <= b, ">" => a > b, _ => a >= b });
            }
        }

        private JsonNode? Additive()
        {
            var left = Term();
            while (true)
            {
                if (Eat("+"))
                {
                    var right = Term();
                    left = TryNum(left, out var a) && TryNum(right, out var b) ? JsonValue.Create(a + b) : JsonValue.Create(Text(left) + Text(right));
                }
                else if (Peek('-') && !PeekAt(1, '-')) { _pos++; left = JsonValue.Create(Num(left, "-") - Num(Term(), "-")); }
                else return left;
            }
        }

        private JsonNode? Term()
        {
            var left = Unary();
            while (true)
            {
                if (Eat("*")) left = JsonValue.Create(Num(left, "*") * Num(Unary(), "*"));
                else if (Eat("/")) { var d = Num(Unary(), "/"); left = JsonValue.Create(Num(left, "/") / d); }
                else return left;
            }
        }

        private JsonNode? Unary()
        {
            if (Eat("!")) return Bool(!IsTruthy(Unary()));
            if (Eat("-")) return JsonValue.Create(-Num(Unary(), "-"));
            return Primary();
        }

        private JsonNode? Primary()
        {
            Skip();
            if (_pos >= src.Length) throw Error("unexpected end");
            var c = src[_pos];
            if (c == '(') { _pos++; var v = Or(); Expect(')'); return v; }
            if (c is '\'' or '"') return JsonValue.Create(ReadString(c));
            if (char.IsDigit(c) || (c == '.' && _pos + 1 < src.Length && char.IsDigit(src[_pos + 1]))) return JsonValue.Create(ReadNumber());
            if (char.IsLetter(c) || c == '_' || c == '$')
            {
                var name = ReadIdent();
                Skip();
                if (Peek('(')) { _pos++; return Call(name, Args()); }
                return name switch
                {
                    "true" => Bool(true),
                    "false" => Bool(false),
                    "null" => null,
                    _ => Path(Lookup(scope, name))
                };
            }
            throw Error($"unexpected '{c}'");
        }

        private JsonNode? Path(JsonNode? current)
        {
            while (true)
            {
                if (Peek('.') ) { _pos++; current = Lookup(current, ReadIdent()); }
                else if (Peek('['))
                {
                    _pos++;
                    var key = Or();
                    Expect(']');
                    current = TryNum(key, out var idx) && current is JsonArray arr
                        ? (idx >= 0 && idx < arr.Count ? arr[(int)idx] : null)
                        : Lookup(current, Text(key));
                }
                else return current;
            }
        }

        private static JsonNode? Lookup(JsonNode? node, string key) =>
            node is JsonObject o && o.TryGetPropertyValue(key, out var v) ? v : null;

        private List<JsonNode?> Args()
        {
            var args = new List<JsonNode?>();
            Skip();
            if (Peek(')')) { _pos++; return args; }
            while (true)
            {
                args.Add(Or());
                if (Eat(",")) continue;
                Expect(')');
                return args;
            }
        }

        private JsonNode? Call(string fn, List<JsonNode?> a)
        {
            double N(int i) => Num(i < a.Count ? a[i] : null, fn);
            return fn switch
            {
                "abs" => JsonValue.Create(Math.Abs(N(0))),
                "round" => JsonValue.Create(Math.Round(N(0), a.Count > 1 ? (int)N(1) : 0, MidpointRounding.AwayFromZero)),
                "min" => JsonValue.Create(Math.Min(N(0), N(1))),
                "max" => JsonValue.Create(Math.Max(N(0), N(1))),
                "number" => JsonValue.Create(TryNum(a[0], out var d) ? d
                    : double.Parse(Text(a[0]), NumberStyles.Float, CultureInfo.InvariantCulture)),
                "string" => JsonValue.Create(Text(a.Count > 0 ? a[0] : null)),
                "len" => JsonValue.Create((double)(a.Count == 0 ? 0 : a[0] switch
                {
                    JsonArray arr => arr.Count,
                    JsonObject obj => obj.Count,
                    null => 0,
                    var x => Text(x).Length
                })),
                "contains" => Bool(a.Count >= 2 && (a[0] is JsonArray arr2
                    ? arr2.Any(x => AreEqual(x, a[1]))
                    : Text(a[0]).Contains(Text(a[1]), StringComparison.OrdinalIgnoreCase))),
                "startsWith" => Bool(a.Count >= 2 && Text(a[0]).StartsWith(Text(a[1]), StringComparison.OrdinalIgnoreCase)),
                _ => throw Error($"unknown function '{fn}'")
            };
        }

        private static bool AreEqual(JsonNode? a, JsonNode? b)
        {
            if (a is null || b is null) return a is null && b is null;
            if (TryNum(a, out var x) && TryNum(b, out var y)) return Math.Abs(x - y) < 1e-9;
            if (a is JsonValue && b is JsonValue) return string.Equals(Text(a), Text(b), StringComparison.Ordinal);
            return JsonNode.DeepEquals(a, b);
        }

        private double Num(JsonNode? n, string op)
        {
            if (TryNum(n, out var d)) return d;
            if (n is JsonValue v && v.TryGetValue(out string? s) && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return d;
            throw Error($"'{op}' needs a number, got {Text(n)}");
        }

        private static JsonNode Bool(bool b) => JsonValue.Create(b);

        private string ReadIdent()
        {
            Skip();
            var start = _pos;
            // Hyphenated step ids are reached with steps['my-id'] so that a-b stays a subtraction.
            while (_pos < src.Length && (char.IsLetterOrDigit(src[_pos]) || src[_pos] is '_' or '$'))
                _pos++;
            if (_pos == start) throw Error("expected a name");
            return src[start.._pos];
        }

        private double ReadNumber()
        {
            var start = _pos;
            while (_pos < src.Length && (char.IsDigit(src[_pos]) || src[_pos] is '.' or 'e' or 'E' ||
                   (src[_pos] is '+' or '-' && _pos > start && src[_pos - 1] is 'e' or 'E')))
                _pos++;
            return double.Parse(src[start.._pos], NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private string ReadString(char quote)
        {
            _pos++;
            var sb = new StringBuilder();
            while (_pos < src.Length && src[_pos] != quote)
            {
                if (src[_pos] == '\\' && _pos + 1 < src.Length) _pos++;
                sb.Append(src[_pos++]);
            }
            Expect(quote);
            return sb.ToString();
        }

        private void Skip() { while (_pos < src.Length && char.IsWhiteSpace(src[_pos])) _pos++; }
        private bool Peek(char c) { Skip(); return _pos < src.Length && src[_pos] == c; }
        private bool PeekAt(int offset, char c) => _pos + offset < src.Length && src[_pos + offset] == c;

        private bool Eat(string token)
        {
            Skip();
            if (string.CompareOrdinal(src, _pos, token, 0, token.Length) != 0) return false;
            // don't let "<" eat the first char of "<=", or "=" style prefixes
            if (token is "<" or ">" or "!" && PeekAt(1, '=')) return false;
            _pos += token.Length;
            return true;
        }

        private void Expect(char c)
        {
            Skip();
            if (_pos >= src.Length || src[_pos] != c) throw Error($"expected '{c}'");
            _pos++;
        }

        private ScenarioException Error(string what) =>
            new(ScenarioErrorCodes.ScenarioInvalid, $"Expression '{src}': {what} at position {_pos}.");
    }
}

/// <summary>${...} interpolation over arguments, and the $.a.b[0] result paths used by <c>save:</c>.</summary>
public static partial class ScenarioInterpolation
{
    [GeneratedRegex(@"\$\{([^}]*)\}")]
    private static partial Regex Placeholder();

    /// <summary>
    /// A string that is exactly one "${expr}" becomes the typed value; strings with embedded placeholders
    /// become text. Objects and arrays are interpolated recursively.
    /// </summary>
    public static JsonNode? Apply(JsonNode? node, JsonObject scope) => node switch
    {
        JsonObject o => new JsonObject(o.Select(kv => KeyValuePair.Create(kv.Key, Apply(kv.Value, scope)))),
        JsonArray a => new JsonArray(a.Select(x => Apply(x, scope)).ToArray()),
        JsonValue v when v.TryGetValue(out string? s) && s is not null => ApplyString(s, scope),
        _ => node?.DeepClone()
    };

    public static JsonNode? ApplyString(string s, JsonObject scope)
    {
        var matches = Placeholder().Matches(s);
        if (matches.Count == 0)
            return JsonValue.Create(s);
        if (matches.Count == 1 && matches[0].Index == 0 && matches[0].Length == s.Length)
            return ScenarioExpression.Evaluate(matches[0].Groups[1].Value, scope)?.DeepClone();
        return JsonValue.Create(Placeholder().Replace(s, m => ScenarioExpression.Text(ScenarioExpression.Evaluate(m.Groups[1].Value, scope))));
    }

    /// <summary>Selects from a step result with "$", "$.a.b", "$.a[0].b", or "$.masks[-1]".</summary>
    public static JsonNode? Select(JsonNode? root, string path)
    {
        if (!path.StartsWith('$'))
            throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, $"save path '{path}' must start with '$'.");
        var current = root;
        var i = 1;
        while (i < path.Length && current is not null)
        {
            if (path[i] == '.')
            {
                var start = ++i;
                while (i < path.Length && path[i] is not '.' and not '[') i++;
                var key = path[start..i];
                current = current is JsonObject o ? FindKey(o, key) : null;
            }
            else if (path[i] == '[')
            {
                var end = path.IndexOf(']', i);
                if (end < 0) throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, $"save path '{path}' has an unclosed '['.");
                var token = path[(i + 1)..end].Trim('\'', '"');
                current = int.TryParse(token, out var idx) && current is JsonArray arr
                    ? (idx < 0 ? (arr.Count + idx >= 0 ? arr[arr.Count + idx] : null) : (idx < arr.Count ? arr[idx] : null))
                    : current is JsonObject o2 ? FindKey(o2, token) : null;
                i = end + 1;
            }
            else
                throw new ScenarioException(ScenarioErrorCodes.ScenarioInvalid, $"save path '{path}' is malformed at {i}.");
        }
        return current?.DeepClone();
    }

    // Tool results are camelCase JSON, but authors often write the C# name; accept either.
    private static JsonNode? FindKey(JsonObject o, string key) =>
        o.TryGetPropertyValue(key, out var v) ? v
        : o.FirstOrDefault(kv => string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase)).Value;
}
