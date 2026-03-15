namespace CraterClaw.Core;

public sealed record McpAvailabilityResult(
    string Name,
    bool IsAvailable,
    string? ErrorMessage);
