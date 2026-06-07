namespace WinGuiMcp.Platform;

public sealed class LaunchResult
{
    public required int Handle { get; init; }
    public required string Title { get; init; }
    public required int Pid { get; init; }
}
