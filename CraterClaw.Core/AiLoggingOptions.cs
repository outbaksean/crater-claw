namespace CraterClaw.Core;

public sealed class AiLoggingOptions
{
    public bool Enabled { get; init; } = false;
    public string Path { get; init; } = string.Empty;
}
