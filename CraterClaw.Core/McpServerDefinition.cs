namespace CraterClaw.Core;

public sealed record McpServerDefinition(
    string Name,
    string Label,
    McpTransport Transport,
    string? BaseUrl,
    string? Command,
    IReadOnlyList<string>? Args,
    IReadOnlyDictionary<string, string>? Env,
    bool Enabled);
