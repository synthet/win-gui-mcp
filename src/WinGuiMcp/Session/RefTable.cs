using System.Text.RegularExpressions;
using WinGuiMcp.Common;
using WinGuiMcp.Perception;

namespace WinGuiMcp.Session;

public sealed class RefTable
{
    private readonly Dictionary<string, RefEntry> _entries = new(StringComparer.Ordinal);
    private readonly List<RefEntry> _orderedEntries = [];

    public void ClearForWindow(int windowIndex)
    {
        var prefix = $"w{windowIndex}e";
        var toRemove = _entries.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        foreach (var key in toRemove)
            _entries.Remove(key);

        _orderedEntries.RemoveAll(e => e.Ref.StartsWith(prefix, StringComparison.Ordinal));
    }

    public void Clear()
    {
        _entries.Clear();
        _orderedEntries.Clear();
    }

    public void Add(RefEntry entry)
    {
        _entries[entry.Ref] = entry;
        _orderedEntries.Add(entry);
    }

    public RefEntry Resolve(string refValue, Guid? currentSnapshotId)
    {
        if (!RefFormat.IsValid(refValue))
            throw new ToolException(ErrorCodes.InvalidArgument, $"Invalid ref format: {refValue}");

        if (!_entries.TryGetValue(refValue, out var entry))
            throw new ToolException(ErrorCodes.ElementNotFound, $"Ref {refValue} not found.", new Dictionary<string, object?> { ["ref"] = refValue });

        if (currentSnapshotId.HasValue && entry.SnapshotId != currentSnapshotId.Value)
        {
            throw new ToolException(
                ErrorCodes.ElementNotFound,
                $"Ref {refValue} is stale; take a new snapshot.",
                new Dictionary<string, object?>
                {
                    ["ref"] = refValue,
                    ["stale"] = true,
                    ["snapshotId"] = entry.SnapshotId.ToString()
                });
        }

        return entry;
    }

    public UemNode GetProperties(string refValue, Guid? currentSnapshotId)
    {
        var entry = Resolve(refValue, currentSnapshotId);
        return entry.Node.CloneWithoutChildren();
    }

    public RefEntry FindElement(
        int windowIndex,
        Guid snapshotId,
        string by,
        string value,
        string match,
        int nth)
    {
        var all = _orderedEntries
            .Where(e => e.SnapshotId == snapshotId && RefFormat.Parse(e.Ref).WindowIndex == windowIndex)
            .Where(e => Matches(e.Node, by, value, match))
            .ToList();

        if (all.Count == 0)
            throw new ToolException(ErrorCodes.ElementNotFound, "Locator matched no elements.");

        if (nth < 0 || nth >= all.Count)
            throw new ToolException(ErrorCodes.Ambiguous, $"Locator matched {all.Count} elements; narrow criteria or set nth.");

        return all[nth];
    }

    private static bool Matches(UemNode node, string by, string value, string matchMode)
    {
        var fieldValue = by switch
        {
            "name" => node.Name,
            "role" => node.Role,
            "automationId" => node.AutomationId,
            "className" => node.ClassName,
            "text" => node.Value ?? node.Name,
            _ => throw new ToolException(ErrorCodes.InvalidArgument, $"Unknown locator by: {by}")
        };

        if (fieldValue is null)
            return false;

        return matchMode switch
        {
            "exact" => string.Equals(fieldValue, value, StringComparison.OrdinalIgnoreCase),
            "contains" => fieldValue.Contains(value, StringComparison.OrdinalIgnoreCase),
            "regex" => Regex.IsMatch(fieldValue, value, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
            _ => throw new ToolException(ErrorCodes.InvalidArgument, $"Unknown match mode: {matchMode}")
        };
    }
}
