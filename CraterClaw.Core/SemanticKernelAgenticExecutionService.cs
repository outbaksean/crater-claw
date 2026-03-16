using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace CraterClaw.Core;

internal sealed class SemanticKernelAgenticExecutionService(
    IKernelFactory kernelFactory,
    ILogger<SemanticKernelAgenticExecutionService> logger) : IAgenticExecutionService
{
    public async Task<AgenticResponse> ExecuteAsync(
        ProviderEndpoint endpoint,
        AgenticRequest request,
        CancellationToken cancellationToken)
    {
        var kernel = kernelFactory.Create(endpoint, request.ModelName);

        foreach (var plugin in request.Plugins)
            kernel.Plugins.AddFromObject(plugin);

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(request.Prompt);

        var settings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var messages = await chatService.GetChatMessageContentsAsync(
            chatHistory, settings, kernel, cancellationToken);

        var toolsInvoked = chatHistory
            .Where(m => m.Role == AuthorRole.Tool)
            .SelectMany(m => m.Items.OfType<FunctionResultContent>())
            .Select(f => f.FunctionName ?? string.Empty)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        foreach (var tool in toolsInvoked)
            logger.LogInformation("Tool invoked: {Tool}", tool);

        var iterationLimitReached = messages.LastOrDefault()
            ?.Items.Any(i => i is FunctionCallContent) ?? false;

        var finishReason = iterationLimitReached
            ? AgenticFinishReason.IterationLimitReached
            : AgenticFinishReason.Completed;

        logger.LogInformation("Agentic task finished: {FinishReason}", finishReason);

        var content = messages.LastOrDefault(m => m.Role == AuthorRole.Assistant)?.Content
            ?? string.Empty;

        return new AgenticResponse(content, finishReason, toolsInvoked.AsReadOnly());
    }
}
