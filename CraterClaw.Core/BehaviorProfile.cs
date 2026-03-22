namespace CraterClaw.Core;

public sealed record PluginBinding(
    string Name,
    IReadOnlyList<string> Tools,
    IReadOnlyDictionary<string, string> Config);

public sealed record BehaviorProfile(
    string Id,
    string Name,
    string Description,
    string SystemPrompt,
    string? PreferredProviderName,
    string? PreferredModelName,
    IReadOnlyList<PluginBinding> Plugins);
