using ModelContextProtocol.Protocol;
using WinGuiMcp.Tools;

namespace WinGuiMcp.Tests;

public class McpResultContractTests
{
    [Fact]
    public async Task InvalidWait_IsAnActualMcpToolError()
    {
        object response = await new Phase1Tools(TestServiceFactory.Create()).WaitFor(0, "unsupported-condition");
        var result = Assert.IsType<CallToolResult>(response);
        Assert.True(result.IsError);
        Assert.NotEmpty(result.Content);
        Assert.NotNull(result.StructuredContent);
    }
}
