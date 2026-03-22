using Microsoft.SemanticKernel;

namespace CraterClaw.Core;

public sealed record AgenticRequest(
    string ModelName,
    string Prompt,
    IReadOnlyList<KernelPlugin> Plugins,
    int MaxIterations,
    Action<string>? StreamChunk = null,
    string? SystemPrompt = null);
