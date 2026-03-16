namespace CraterClaw.Core;

public sealed record AgenticRequest(
    string ModelName,
    string Prompt,
    IReadOnlyList<object> Plugins,
    int MaxIterations);
