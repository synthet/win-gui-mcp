using System.Diagnostics;
using System.Text.Json;
using WinGuiMcp.Common;
using WinGuiMcp.Tools;

namespace WinGuiMcp.Tests;

internal static class GateTestHelpers
{
    public static int ReadHandle(object launchResult)
    {
        var json = ToolResponse.Serialize(launchResult);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
        var handle = doc.RootElement.GetProperty("handle").GetInt32();
        Assert.NotEqual(0, handle);
        return handle;
    }

    public static string ReadRef(object findResult)
    {
        var json = ToolResponse.Serialize(findResult);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
        var @ref = doc.RootElement.GetProperty("ref").GetString();
        Assert.False(string.IsNullOrWhiteSpace(@ref));
        return @ref!;
    }

    public static void AssertResponseOk(object response)
    {
        var json = ToolResponse.Serialize(response);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean(), json);
    }

    public static void TryFocus(ToolService tools, int handle)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                tools.FocusWindow(handle);
                return;
            }
            catch (ToolException ex) when (ex.Error.Code == ErrorCodes.FocusFailed)
            {
                Thread.Sleep(300);
            }
        }
    }

    public static bool TryClickButton(ToolService tools, int handle, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryClickByLocator(tools, handle, "name", name, "exact"))
                return true;
        }

        return false;
    }

    public static bool TryClickByLocator(
        ToolService tools,
        int handle,
        string by,
        string value,
        string match = "contains")
    {
        try
        {
            var @ref = ReadRef(tools.FindElement(handle, by, value, nth: 0, match: match));
            var click = tools.Click(@ref, null, null, "left", 1, includeSnapshot: false);
            var clickJson = ToolResponse.Serialize(click);
            return clickJson.Contains("InvokePattern", StringComparison.OrdinalIgnoreCase) ||
                   clickJson.Contains("SendInput", StringComparison.OrdinalIgnoreCase) ||
                   clickJson.Contains("TogglePattern", StringComparison.OrdinalIgnoreCase);
        }
        catch (ToolException)
        {
            return false;
        }
    }

    public static bool TryFindElementRef(
        ToolService tools,
        int handle,
        string by,
        string value,
        string match,
        out string @ref)
    {
        @ref = string.Empty;
        try
        {
            @ref = ReadRef(tools.FindElement(handle, by, value, nth: 0, match: match));
            return true;
        }
        catch (ToolException)
        {
            return false;
        }
    }

    public static bool TrySetValueByLocator(
        ToolService tools,
        int handle,
        string by,
        string value,
        string text,
        string match = "contains")
    {
        if (!TryFindElementRef(tools, handle, by, value, match, out var @ref))
            return false;

        try
        {
            AssertResponseOk(tools.SetValue(@ref, text, includeSnapshot: false));
            return true;
        }
        catch (ToolException)
        {
            return false;
        }
    }

    public static bool TrySetFilenameInSaveDialog(ToolService tools, int dialogHandle, string filePath)
    {
        foreach (var (by, value, match) in new (string, string, string)[]
                 {
                     ("name", "File name:", "contains"),
                     ("name", "Имя файла", "contains"),
                     ("role", "ComboBox", "exact"),
                     ("role", "Edit", "exact"),
                 })
        {
            if (TrySetValueByLocator(tools, dialogHandle, by, value, filePath, match))
                return true;
        }

        return false;
    }

    public static string CreateTempGateDirectoryWithFile(string fileName = "gate-rename-me.txt")
    {
        var path = CreateTempGateDirectory();
        File.WriteAllText(Path.Combine(path, fileName), "WinGuiMcp rename gate");
        return path;
    }

    public static int TryFindWindowHandleAny(ToolService tools, params string[] titleFragments)
    {
        foreach (var fragment in titleFragments)
        {
            var handle = TryFindWindowHandle(tools, fragment);
            if (handle != 0)
                return handle;
        }

        return 0;
    }

    public static bool TreeContainsText(ToolService tools, int handle, string text)
    {
        try
        {
            var snapshot = tools.Snapshot(handle, "window", "control", includeOffscreen: true);
            var treeJson = ToolResponse.Serialize(snapshot.Tree);
            return treeJson.Contains(text, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    public static int WaitForWindow(ToolService tools, string titleFragment, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            var handle = TryFindWindowHandle(tools, titleFragment);
            if (handle != 0)
                return handle;

            Thread.Sleep(200);
        }

        throw new InvalidOperationException($"Window '{titleFragment}' did not appear within {timeout.TotalSeconds:0}s.");
    }

    public static int TryFindWindowHandle(ToolService tools, string titleFragment)
    {
        var windows = tools.ListWindows(titleFragment, includeMinimized: false);
        var json = ToolResponse.Serialize(windows);
        using var doc = JsonDocument.Parse(json);
        var match = doc.RootElement.GetProperty("windows").EnumerateArray()
            .FirstOrDefault(w => w.GetProperty("title").GetString()?.Contains(titleFragment, StringComparison.OrdinalIgnoreCase) == true);

        return match.ValueKind == JsonValueKind.Undefined ? 0 : match.GetProperty("handle").GetInt32();
    }

    public static string CreateTempGateDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "WinGuiMcpGate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "gate-marker.txt"), "WinGuiMcp gate folder");
        return path;
    }

    public static void DeleteDirectoryIfExists(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup for test hygiene.
        }
    }
}
