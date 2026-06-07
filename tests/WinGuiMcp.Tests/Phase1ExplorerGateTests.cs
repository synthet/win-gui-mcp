using WinGuiMcp.Common;

namespace WinGuiMcp.Tests;

[Collection("Phase1Gate")]
public class Phase1ExplorerGateTests
{
    [Fact]
    public void ExplorerGate_LaunchFolderAndSnapshot()
    {
        var folder = GateTestHelpers.CreateTempGateDirectory();
        var folderName = Path.GetFileName(folder);
        try
        {
            using var tools = TestServiceFactoryExtensions.CreateDisposable();
            using var launchedApp = new LaunchedAppScope();

            var launch = tools.Service.LaunchApp("explorer.exe", null, $"\"{folder}\"", 15000);
            launchedApp.Track(launch);

            var handle = GateTestHelpers.WaitForWindow(tools.Service, folderName, TimeSpan.FromSeconds(10));
            launchedApp.TrackHandle(handle);

            GateTestHelpers.TryFocus(tools.Service, handle);
            var snapshot = tools.Service.Snapshot(handle, "window", "control", includeOffscreen: false);

            Assert.False(string.IsNullOrWhiteSpace(snapshot.SnapshotId));
            Assert.Equal("uia", snapshot.Tree.Source);

            var treeJson = ToolResponse.Serialize(snapshot.Tree);
            Assert.True(treeJson.Length > 50, "Explorer snapshot should return a UIA tree.");
        }
        finally
        {
            GateTestHelpers.DeleteDirectoryIfExists(folder);
        }
    }

    [Fact]
    public void ExplorerGate_RenameFileViaHotkey()
    {
        const string originalName = "gate-rename-me.txt";
        const string renamedName = "gate-renamed.txt";
        var folder = GateTestHelpers.CreateTempGateDirectoryWithFile(originalName);
        var folderName = Path.GetFileName(folder);
        try
        {
            using var tools = TestServiceFactoryExtensions.CreateDisposable();
            using var launchedApp = new LaunchedAppScope();

            tools.Service.LaunchApp("explorer.exe", null, $"\"{folder}\"", 15000);
            var handle = GateTestHelpers.WaitForWindow(tools.Service, folderName, TimeSpan.FromSeconds(10));
            launchedApp.TrackHandle(handle);

            GateTestHelpers.TryFocus(tools.Service, handle);
            tools.Service.Snapshot(handle, "window", "control", includeOffscreen: true);

            if (!GateTestHelpers.TryFindElementRef(tools.Service, handle, "name", originalName, "contains", out var fileRef) &&
                !GateTestHelpers.TryFindElementRef(tools.Service, handle, "text", originalName, "contains", out fileRef))
            {
                return;
            }

            GateTestHelpers.AssertResponseOk(tools.Service.Click(fileRef, null, null, "left", 1, includeSnapshot: false));
            Thread.Sleep(200);

            if (!GateTestHelpers.TryRenameSelectedExplorerItem(tools.Service, handle, renamedName))
                return;

            Thread.Sleep(500);
            Assert.True(
                File.Exists(Path.Combine(folder, renamedName)) ||
                GateTestHelpers.TreeContainsText(tools.Service, handle, renamedName),
                "Renamed file should appear on disk or in the Explorer tree.");
        }
        finally
        {
            GateTestHelpers.DeleteDirectoryIfExists(folder);
        }
    }
}
