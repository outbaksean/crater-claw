namespace CraterClaw.Blazor.Api;

public record ProviderEndpoint(string Name, string BaseUrl);

public record ProviderStatus(bool IsReachable, string? ErrorMessage);

public record ModelItem(string Name, long SizeBytes, string ModifiedAt);

public record MessageItem(string Role, string Content);

public record ExecutionRequest(
    string ModelName,
    MessageItem[] Messages,
    double? Temperature = null,
    int? MaxTokens = null);

public record ExecutionResponse(string Content, string ModelName, string FinishReason);

public record PluginBinding(string Name, string[] Tools);

public record BehaviorProfile(
    string Id,
    string Name,
    string Description,
    string SystemPrompt,
    string? PreferredProviderName,
    string? PreferredModelName,
    PluginBinding[] Plugins);

public record AgenticRequest(string ModelName, string Prompt, string ProfileId, int? MaxIterations = null);

public record AgenticResponse(string Content, string FinishReason, string[] ToolsInvoked);
