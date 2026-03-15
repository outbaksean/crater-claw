namespace CraterClaw.Core;

public sealed record ExecutionRequest(
    string ModelName,
    IReadOnlyList<ConversationMessage> Messages,
    double? Temperature = null,
    int? MaxTokens = null);
