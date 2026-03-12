namespace CraterClaw.Core;

public sealed record ProviderStatus(bool IsReachable, string? ErrorMessage);
