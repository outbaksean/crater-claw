using Microsoft.SemanticKernel;

namespace CraterClaw.Core;

public sealed record AgenticRequest(
    string ModelName,
    string Prompt,
    IReadOnlyList<KernelPlugin> Plugins,
    int MaxIterations,
    Func<string, Task>? StreamChunk = null,
    Func<string, Task>? StreamThinkingChunk = null,
    string? SystemPrompt = null,
    int? MaxContext = null,
    int Depth = 0,
    Func<string, string, Task>? StreamChildChunk = null,
    Func<string, string, Task>? StreamChildThinking = null,
    Func<string, string, Task>? StreamChildStart = null);
