using CraterClaw.Core;

namespace CraterClaw.Api.Models;

internal sealed record ProviderEndpointResponse(string Name, string BaseUrl);
internal sealed record ProviderStatusResponse(bool IsReachable, string? ErrorMessage);
internal sealed record ModelApiItem(string Name, long SizeBytes, DateTimeOffset ModifiedAt);
internal sealed record ExecutionApiRequest(
    string ModelName,
    IReadOnlyList<MessageApiItem> Messages,
    double? Temperature = null,
    int? MaxTokens = null);
internal sealed record MessageApiItem(MessageRole Role, string Content);
internal sealed record ExecutionApiResponse(string Content, string ModelName, FinishReason FinishReason);
internal sealed record AgenticApiRequest(
    string ModelName,
    string Prompt,
    string ProfileId,
    int? MaxIterations = null,
    bool? ShowThinking = null);
internal sealed record AgenticApiResponse(string Content, AgenticFinishReason FinishReason, IReadOnlyList<string> ToolsInvoked);
internal sealed record PluginBindingApiItem(string Name, IReadOnlyList<string> Tools);
internal sealed record AgenticSseChunk(string Type, string Content);
internal sealed record AgenticSseThinking(string Type, string Content);
internal sealed record AgenticSseChildStart(string Type, string Source, string Prompt);
internal sealed record AgenticSseChildChunk(string Type, string Source, string Content);
internal sealed record AgenticSseChildThinking(string Type, string Source, string Content);
internal sealed record AgenticSseDone(string Type, AgenticFinishReason FinishReason, IReadOnlyList<string> ToolsInvoked);

internal sealed record BehaviorProfileApiItem(
    string Id,
    string Name,
    string Description,
    string SystemPrompt,
    string? PreferredProviderName,
    string? PreferredModelName,
    IReadOnlyList<PluginBindingApiItem> Plugins);
