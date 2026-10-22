namespace WinGuiMcp.Tests;

public class OsWaitConditionsTests
{
    [Fact]
    public void IsOsLevelCondition_RecognizesProcessAndFileConditions()
    {
        Assert.True(WinGuiMcp.Platform.OsWaitConditions.IsOsLevelCondition("process-exists"));
        Assert.True(WinGuiMcp.Platform.OsWaitConditions.IsOsLevelCondition("file-stable"));
        Assert.False(WinGuiMcp.Platform.OsWaitConditions.IsOsLevelCondition("element-exists"));
    }

    [Fact]
    public void FileStable_ReturnsTrueWhenSizeSettles()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wingui-wait-{Guid.NewGuid():N}.bin");
        try
        {
            File.WriteAllBytes(path, [1, 2, 3]);
            Assert.True(WinGuiMcp.Platform.OsWaitConditions.IsFileStable(path, settleMs: 100));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
