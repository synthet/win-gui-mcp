namespace WinGuiMcp.Common;

public sealed class ToolException : Exception
{
    public ToolError Error { get; }

    public ToolException(ToolError error) : base(error.Message)
    {
        Error = error;
    }

    public ToolException(string code, string message, Dictionary<string, object?>? details = null)
        : this(ToolError.Create(code, message, details))
    {
    }
}
