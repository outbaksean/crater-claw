namespace CraterClaw.Core;

public sealed class McpServerOptions
{
    public string Label { get; set; } = string.Empty;
    public McpTransport Transport { get; set; }
    public string? BaseUrl { get; set; }
    public string? Command { get; set; }
    public List<string>? Args { get; set; }
    public Dictionary<string, string>? Env { get; set; }
    public bool Enabled { get; set; } = true;
}
