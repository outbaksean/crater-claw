namespace CraterClaw.Core;

public sealed record AgenticResponse(
    string Content,
    AgenticFinishReason FinishReason,
    IReadOnlyList<string> ToolsInvoked);
