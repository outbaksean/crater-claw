using Microsoft.SemanticKernel;

namespace CraterClaw.Core;

public sealed record AgenticRequest(
    string ModelName,
    string Prompt,
    IReadOnlyList<KernelPlugin> Plugins,
    int MaxIterations,
    Func<string, Task>? StreamChunk = null,
    Func<string, Task>? StreamThinkingChunk = null,
    string? SystemPrompt = null);
