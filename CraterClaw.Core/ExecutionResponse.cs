namespace CraterClaw.Core;

public sealed record ExecutionResponse(string Content, string ModelName, FinishReason FinishReason);
