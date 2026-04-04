namespace CraterClaw.Core;

internal static class PluginExecutionContext
{
    public static AsyncLocal<ProviderEndpoint?> CurrentEndpoint { get; } = new();
    public static AsyncLocal<int> CurrentDepth { get; } = new();
    public static AsyncLocal<Func<string, string, Task>?> ChildStreamChunk { get; } = new();
    public static AsyncLocal<Func<string, string, Task>?> ChildStreamThinking { get; } = new();
    public static AsyncLocal<Func<string, string, Task>?> ChildStreamStart { get; } = new();
}
