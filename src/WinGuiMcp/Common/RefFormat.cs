using System.Text.RegularExpressions;

namespace WinGuiMcp.Common;

public static partial class RefFormat
{
    [GeneratedRegex(@"^w\d+e\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex RefPattern();

    public static bool IsValid(string? refValue) =>
        !string.IsNullOrEmpty(refValue) && RefPattern().IsMatch(refValue);

    public static (int WindowIndex, int ElementIndex) Parse(string refValue)
    {
        if (!IsValid(refValue))
            throw new ToolException(ErrorCodes.InvalidArgument, $"Invalid ref format: {refValue}");

        var body = refValue[1..];
        var parts = body.Split('e', 2);
        return (int.Parse(parts[0]), int.Parse(parts[1]));
    }

    public static string Format(int windowIndex, int elementIndex) => $"w{windowIndex}e{elementIndex}";
}
