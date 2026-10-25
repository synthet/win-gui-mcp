namespace WinGuiMcp.Tests;

internal static class SamplePathHelper
{
    public static string RequireSampleExe(string sampleFolder, string exeName)
    {
        var repoRoot = FindRepoRoot();
        var candidates = new[]
        {
            Path.Combine(repoRoot, "samples", sampleFolder, "bin", "Debug", "net10.0-windows", exeName),
            Path.Combine(repoRoot, "samples", sampleFolder, "bin", "Release", "net10.0-windows", exeName)
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        throw new InvalidOperationException(
            $"Sample executable not found. Build samples first: dotnet build samples/{sampleFolder}/{sampleFolder}.csproj");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "WinGuiMcp.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test output directory.");
    }
}
