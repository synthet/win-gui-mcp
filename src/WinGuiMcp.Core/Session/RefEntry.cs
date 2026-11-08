using FlaUI.Core.AutomationElements;
using WinGuiMcp.Perception;

namespace WinGuiMcp.Session;

public sealed class RefEntry
{
    public required string Ref { get; init; }
    public required nint Hwnd { get; init; }
    public required AutomationElement Element { get; init; }
    public required Bbox Bbox { get; init; }
    public required string Source { get; init; }
    public required IReadOnlyList<string> Patterns { get; init; }
    public required Guid SnapshotId { get; init; }
    public required UemNode Node { get; init; }
}
