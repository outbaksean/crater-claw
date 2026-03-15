namespace CraterClaw.Core;

public sealed class ProviderOptions
{
    public string? Active { get; set; }
    public Dictionary<string, ProviderEndpointOptions> Endpoints { get; set; } = [];
}
