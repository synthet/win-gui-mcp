using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;
using WinGuiMcp.Tools;

namespace WinGuiMcp.Tests;

/// <summary>
/// Locks the public MCP tool surface: every [McpServerTool] attribute (name, flags, description,
/// parameters with defaults) plus every catalog descriptor returned by describe_tool.
/// Regenerate the golden file deliberately with WINGUI_UPDATE_GOLDEN=1 after a spec change in docs/tools.md.
/// </summary>
public class ToolSurfaceSnapshotTests
{
    private static readonly Type[] ToolTypes = [typeof(Phase1Tools), typeof(CompactDiscoveryTools), typeof(ScenarioTools)];

    /// <summary>Scenario tools are MCP-only; they are not catalog (execute_tool) tools.</summary>
    private static readonly Type[] CatalogTypes = [typeof(Phase1Tools), typeof(CompactDiscoveryTools)];

    [Fact]
    public void ToolSurface_MatchesGolden()
    {
        var actual = BuildSurface().ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        var goldenPath = Path.Combine(AppContext.BaseDirectory, "golden", "tool-surface.json");

        if (Environment.GetEnvironmentVariable("WINGUI_UPDATE_GOLDEN") == "1")
        {
            var sourcePath = Path.Combine(FindTestsSourceDir(), "golden", "tool-surface.json");
            Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
            File.WriteAllText(sourcePath, actual + Environment.NewLine);
            return;
        }

        Assert.True(File.Exists(goldenPath), $"Golden file missing: {goldenPath}");
        var expected = File.ReadAllText(goldenPath).TrimEnd().ReplaceLineEndings("\n");
        Assert.Equal(expected, actual.ReplaceLineEndings("\n"));
    }

    private static JsonObject BuildSurface()
    {
        var attributeTools = new JsonArray();
        foreach (var type in ToolTypes)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance).OrderBy(m => m.Name, StringComparer.Ordinal))
            {
                var attr = method.GetCustomAttribute<McpServerToolAttribute>();
                if (attr is null)
                    continue;

                var parameters = new JsonArray();
                foreach (var p in method.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)))
                {
                    parameters.Add(new JsonObject
                    {
                        ["name"] = p.Name,
                        ["type"] = Nullable.GetUnderlyingType(p.ParameterType) is { } inner ? inner.Name + "?" : p.ParameterType.Name,
                        ["optional"] = p.HasDefaultValue,
                        ["default"] = p.HasDefaultValue ? JsonValue.Create(p.DefaultValue?.ToString()) : null
                    });
                }

                attributeTools.Add(new JsonObject
                {
                    ["type"] = type.Name,
                    ["name"] = attr.Name,
                    ["readOnly"] = attr.ReadOnly,
                    ["destructive"] = attr.Destructive,
                    ["openWorld"] = attr.OpenWorld,
                    ["structured"] = attr.UseStructuredContent,
                    ["description"] = method.GetCustomAttribute<DescriptionAttribute>()?.Description,
                    ["parameters"] = parameters
                });
            }
        }

        var names = attributeTools.Where(t => CatalogTypes.Any(c => c.Name == t!["type"]!.GetValue<string>()))
            .Select(t => t!["name"]!.GetValue<string>()).Distinct().OrderBy(n => n, StringComparer.Ordinal);
        var catalog = new JsonArray();
        foreach (var name in names)
        {
            var described = JsonSerializer.SerializeToNode(ToolCatalogRegistry.DescribeTool(name))!.AsObject();
            catalog.Add(new JsonObject
            {
                ["name"] = described["name"]?.DeepClone(),
                ["plugin"] = described["plugin"]?.DeepClone(),
                ["readOnly"] = described["readOnly"]?.DeepClone(),
                ["description"] = described["description"]?.DeepClone(),
                ["inputSchema"] = described["inputSchema"]?.DeepClone()
            });
        }

        return new JsonObject { ["attributeTools"] = attributeTools, ["catalog"] = catalog };
    }

    private static string FindTestsSourceDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WinGuiMcp.sln")))
            dir = dir.Parent;
        if (dir is not null)
            return Path.Combine(dir.FullName, "tests", "WinGuiMcp.Tests");
        // Artifacts builds live outside the repo; fall back to the env var.
        return Environment.GetEnvironmentVariable("WINGUI_TESTS_DIR")
            ?? throw new InvalidOperationException("Set WINGUI_TESTS_DIR to tests/WinGuiMcp.Tests when building outside the repo.");
    }
}
