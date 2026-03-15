namespace CraterClaw.Core;

public sealed class McpOptions
{
    public Dictionary<string, McpServerOptions> Servers { get; set; } = [];
}
