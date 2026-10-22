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

    public static string GateDebugDirectory =>
        Path.Combine(Path.GetTempPath(), "WinGuiMcpGateDebug");

    /// <summary>
    /// Saves a window screenshot for gate debugging. Returns the PNG path or null on failure.
    /// </summary>
    public static string? TrySaveDebugScreenshot(ToolService tools, int? handle, string stepLabel)
    {
        try
        {
            var safeLabel = string.Concat(stepLabel.Select(ch =>
                char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-'));
            Directory.CreateDirectory(GateDebugDirectory);
            var path = Path.Combine(
                GateDebugDirectory,
                $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{safeLabel}.png");

            var screenshot = tools.Screenshot(handle, null, scale: 1.0);
            var json = ToolResponse.Serialize(screenshot);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.GetProperty("ok").GetBoolean())
                return null;

            var base64 = doc.RootElement.GetProperty("imageBase64").GetString();
            if (string.IsNullOrWhiteSpace(base64))
                return null;

            File.WriteAllBytes(path, Convert.FromBase64String(base64));
            return path;
        }
        catch
        {
            return null;
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
        catch (Exception)
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

    public static bool TryClickSaveInDialog(ToolService tools, int dialogHandle)
    {
        TryFocus(tools, dialogHandle);
        Thread.Sleep(200);

        if (TryClickButton(tools, dialogHandle, "Сохранить", "Save"))
        {
            Thread.Sleep(1000);
            return true;
        }

        try
        {
            AssertResponseOk(tools.Hotkey("enter", dialogHandle, includeSnapshot: false));
        }
        catch (ToolException)
        {
            // Enter may not submit on all dialog variants.
        }

        Thread.Sleep(1000);
        return TryFindSaveDialogHandle(tools) == 0;
    }

    public static bool TrySetFilenameInSaveDialog(ToolService tools, int dialogHandle, string filePath)
    {
        TryFocus(tools, dialogHandle);
        Thread.Sleep(400);

        var folder = Path.GetDirectoryName(filePath);
        var fileName = Path.GetFileName(filePath);

        if (!string.IsNullOrEmpty(folder))
        {
            TryNavigateSaveDialogToFolder(tools, dialogHandle, folder);
            Thread.Sleep(600);
            TrySaveDebugScreenshot(tools, dialogHandle, "save-after-folder-nav");
        }

        if (TrySetFilenameField(tools, dialogHandle, fileName, filePath))
            return true;

        return TrySetFilenameField(tools, dialogHandle, filePath);
    }

    private static bool TrySetFilenameField(
        ToolService tools,
        int dialogHandle,
        string value,
        string? fullPathFallback = null)
    {
        tools.Snapshot(dialogHandle, "window", "control", includeOffscreen: true);

        foreach (var (by, locatorValue, match) in new (string, string, string)[]
                 {
                     ("automationId", "FileNameControlHost", "exact"),
                     ("automationId", "1001", "exact"),
                     ("automationId", "1148", "exact"),
                     ("name", "File name:", "contains"),
                     ("name", "Имя файла:", "contains"),
                 })
        {
            for (var nth = 0; nth < 4; nth++)
            {
                string @ref;
                try
                {
                    @ref = ReadRef(tools.FindElement(dialogHandle, by, locatorValue, nth: nth, match: match));
                }
                catch (ToolException)
                {
                    break;
                }

                foreach (var candidate in new[] { value, fullPathFallback }.Where(v => !string.IsNullOrEmpty(v)))
                {
                    try
                    {
                        TryFocus(tools, dialogHandle);
                        AssertResponseOk(tools.SetValue(@ref, candidate!, includeSnapshot: false));
                        Thread.Sleep(200);
                        try
                        {
                            AssertResponseOk(tools.Hotkey("enter", dialogHandle, includeSnapshot: false));
                        }
                        catch (ToolException)
                        {
                            // Save may require an explicit button click on some builds.
                        }

                        return true;
                    }
                    catch (ToolException)
                    {
                        // Try next value, nth, or locator.
                    }
                }
            }
        }

        return false;
    }

    public static void TryDismissSaveDialog(ToolService tools, int dialogHandle)
    {
        TryFocus(tools, dialogHandle);
        try
        {
            AssertResponseOk(tools.Hotkey("escape", dialogHandle, includeSnapshot: false));
        }
        catch (ToolException)
        {
            // Best-effort dismiss.
        }
    }

    private static void TryNavigateSaveDialogToFolder(ToolService tools, int dialogHandle, string folderPath)
    {
        TryFocus(tools, dialogHandle);
        try
        {
            foreach (var keys in new[] { "ctrl+l", "alt+d" })
            {
                AssertResponseOk(tools.Hotkey(keys, dialogHandle, includeSnapshot: false));
                Thread.Sleep(350);
                AssertResponseOk(tools.TypeText(folderPath, null, clearFirst: true, submit: true, includeSnapshot: false));
                Thread.Sleep(900);
                if (GateTreeContains(tools, dialogHandle, Path.GetFileName(folderPath.TrimEnd('\\'))))
                    return;
            }
        }
        catch (ToolException)
        {
            // Address bar navigation is best-effort; filename field may still accept a full path.
        }
    }

    private static bool GateTreeContains(ToolService tools, int handle, string text)
    {
        try
        {
            var snapshot = tools.Snapshot(handle, "window", "control", includeOffscreen: true);
            return ToolResponse.Serialize(snapshot.Tree).Contains(text, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static bool TryTypeInTextEditor(ToolService tools, int handle, string text)
    {
        foreach (var (by, value, match) in new (string, string, string)[]
                 {
                     ("role", "Document", "exact"),
                     ("role", "Edit", "exact"),
                     ("role", "Text", "exact"),
                 })
        {
            if (!TryFindElementRef(tools, handle, by, value, match, out var @ref))
                continue;

            try
            {
                AssertResponseOk(tools.TypeText(text, @ref, clearFirst: true, submit: false, includeSnapshot: false));
                return true;
            }
            catch (ToolException)
            {
                // Try next editor locator.
            }
        }

        return false;
    }

    public static bool TryRenameSelectedExplorerItem(ToolService tools, int handle, string newName)
    {
        TryFocus(tools, handle);

        if (TryClickButton(tools, handle, "Rename", "Переименовать"))
        {
            Thread.Sleep(500);
        }
        else
        {
            AssertResponseOk(tools.Hotkey("f2", handle, includeSnapshot: false));
            Thread.Sleep(700);
        }

        tools.Snapshot(handle, "window", "control", includeOffscreen: true);

        foreach (var (by, value, match) in new (string, string, string)[]
                 {
                     ("automationId", "System.ItemNameDisplay", "exact"),
                     ("role", "Edit", "exact"),
                 })
        {
            if (!TryFindElementRef(tools, handle, by, value, match, out var nameEdit))
                continue;

            try
            {
                AssertResponseOk(tools.SetValue(nameEdit, newName, includeSnapshot: false));
                AssertResponseOk(tools.Hotkey("enter", handle, includeSnapshot: false));
                return true;
            }
            catch (ToolException)
            {
                try
                {
                    AssertResponseOk(tools.TypeText(newName, nameEdit, clearFirst: true, submit: true, includeSnapshot: false));
                    return true;
                }
                catch (ToolException)
                {
                    // Try next locator.
                }
            }
        }

        try
        {
            AssertResponseOk(tools.TypeText(newName, null, clearFirst: true, submit: true, includeSnapshot: false));
            return true;
        }
        catch (ToolException)
        {
            return false;
        }
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

    public static int TryFindSaveDialogHandle(ToolService tools, int excludeHandle = 0)
    {
        foreach (var fragment in new[]
                 {
                     "Save As",
                     "Сохранить как",
                     "Сохранение",
                 })
        {
            var handle = TryFindWindowHandle(tools, fragment);
            if (handle != 0 && handle != excludeHandle)
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
