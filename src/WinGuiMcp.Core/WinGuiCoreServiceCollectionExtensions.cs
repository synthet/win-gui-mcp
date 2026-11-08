using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using WinGuiMcp.Action;
using WinGuiMcp.Capture;
using WinGuiMcp.Perception;
using WinGuiMcp.Platform;
using WinGuiMcp.Session;
using WinGuiMcp.Tools;

namespace WinGuiMcp;

public sealed class WinGuiCoreOptions
{
    /// <summary>Extra directory scanned for *.plugin.json alias manifests.</summary>
    public string? PluginsDir { get; set; }

    /// <summary>Write a stderr warning when the process is not Per-Monitor-V2 DPI aware.</summary>
    public bool WarnIfNotPerMonitorV2 { get; set; } = true;
}

public static partial class WinGuiCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the WinGuiMcp core services (the same singletons the WinGuiMcp host used before the split).
    /// The tool catalog is process-wide static state; one host per process is assumed (ADR-0001).
    /// </summary>
    public static IServiceCollection AddWinGuiCore(this IServiceCollection services, Action<WinGuiCoreOptions>? configure = null)
    {
        var options = new WinGuiCoreOptions();
        configure?.Invoke(options);

        ToolCatalogRegistry.EnsurePluginsLoaded(options.PluginsDir);

        if (options.WarnIfNotPerMonitorV2 && !IsPerMonitorV2())
            Console.Error.WriteLine("warning: process is not Per-Monitor-V2 DPI aware; coordinates may not match physical pixels. Ship an app.manifest with <dpiAwareness>PerMonitorV2</dpiAwareness>.");

        services.AddSingleton(options);
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<UiaDispatcher>();
        services.AddSingleton<DpiManager>();
        services.AddSingleton<WindowEnumerator>();
        services.AddSingleton<SnapshotSession>();
        services.AddSingleton<UiaSnapshotBuilder>();
        services.AddSingleton<InputSimulator>();
        services.AddSingleton<PatternExecutor>();
        services.AddSingleton<WindowCapture>();
        services.AddSingleton<ToolService>();
        return services;
    }

    public static bool IsPerMonitorV2()
    {
        try
        {
            // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 == (HANDLE)-4
            return AreDpiAwarenessContextsEqual(GetThreadDpiAwarenessContext(), new IntPtr(-4));
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetThreadDpiAwarenessContext();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AreDpiAwarenessContextsEqual(IntPtr a, IntPtr b);
}
