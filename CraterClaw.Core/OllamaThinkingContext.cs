namespace CraterClaw.Core;

internal static class OllamaThinkingContext
{
    internal static readonly AsyncLocal<bool> ThinkingEnabled = new();
}
