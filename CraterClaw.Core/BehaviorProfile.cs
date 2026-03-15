namespace CraterClaw.Core;

public sealed record BehaviorProfile(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<string> RecommendedModelTags,
    IReadOnlyList<string> AllowedMcpServerNames);
