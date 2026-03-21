using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace CraterClaw.Core;

internal sealed class SemanticKernelAgenticExecutionService(
    IKernelFactory kernelFactory,
    ILogger<SemanticKernelAgenticExecutionService> logger,
    ILoggerFactory loggerFactory) : IAgenticExecutionService
{
    private readonly ILogger _aiLogger = loggerFactory.CreateLogger("CraterClaw.AiTraffic");
    public async Task<AgenticResponse> ExecuteAsync(
        ProviderEndpoint endpoint,
        AgenticRequest request,
        CancellationToken cancellationToken)
    {
        var kernel = kernelFactory.Create(endpoint, request.ModelName);

        foreach (var plugin in request.Plugins)
            kernel.Plugins.AddFromObject(plugin);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(
            "You are a helpful assistant with access to tools. " +
            "When a tool returns results, use those results to directly and concisely answer the user's original question. " +
            "Do not describe or explain the raw data — just answer the question.");
        chatHistory.AddUserMessage(request.Prompt);

        var settings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(
                functions: null, autoInvoke: false, options: null)
        };

        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var finishReason = AgenticFinishReason.Completed;

        for (var iteration = 0; iteration < request.MaxIterations; iteration++)
        {
            _aiLogger.LogDebug("Iteration {Iteration}: sending {Count} messages to LLM", iteration, chatHistory.Count);
            foreach (var msg in chatHistory)
            {
                var functionCallSummary = string.Join(", ", msg.Items.OfType<FunctionCallContent>().Select(f => f.FunctionName));
                var functionResultSummary = string.Join(", ", msg.Items.OfType<FunctionResultContent>().Select(f => $"{f.FunctionName}={f.Result}"));
                _aiLogger.LogDebug("  [{Role}] content={Content} calls=[{Calls}] results=[{Results}]",
                    msg.Role, msg.Content, functionCallSummary, functionResultSummary);
            }

            List<FunctionCallContent> functionCalls;

            if (request.StreamChunk is not null)
            {
                var contentBuilder = new StringBuilder();
                var fcBuilder = new FunctionCallContentBuilder();

                await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(
                    chatHistory, settings, kernel, cancellationToken))
                {
                    if (chunk.Content is not null)
                    {
                        request.StreamChunk(chunk.Content);
                        contentBuilder.Append(chunk.Content);
                    }
                    fcBuilder.Append(chunk);
                }

                functionCalls = [.. fcBuilder.Build()];

                if (functionCalls.Count > 0)
                {
                    var items = new ChatMessageContentItemCollection();
                    foreach (var fc in functionCalls)
                        items.Add(fc);
                    chatHistory.Add(new ChatMessageContent(AuthorRole.Assistant, items));
                }
                else
                {
                    chatHistory.Add(new ChatMessageContent(AuthorRole.Assistant, contentBuilder.ToString()));
                }

                _aiLogger.LogDebug("Iteration {Iteration}: stream complete, content={Content} calls=[{Calls}]",
                    iteration, contentBuilder.ToString(), string.Join(", ", functionCalls.Select(f => f.FunctionName)));
            }
            else
            {
                var messages = await chatService.GetChatMessageContentsAsync(
                    chatHistory, settings, kernel, cancellationToken);

                _aiLogger.LogDebug("Iteration {Iteration}: received {Count} messages from LLM", iteration, messages.Count);
                foreach (var msg in messages)
                {
                    var functionCallSummary = string.Join(", ", msg.Items.OfType<FunctionCallContent>().Select(f => f.FunctionName));
                    _aiLogger.LogDebug("  [{Role}] content={Content} calls=[{Calls}]", msg.Role, msg.Content, functionCallSummary);
                }

                foreach (var msg in messages)
                    chatHistory.Add(msg);

                functionCalls = messages
                    .SelectMany(m => m.Items.OfType<FunctionCallContent>())
                    .ToList();
            }

            if (functionCalls.Count == 0)
                break;

            if (iteration == request.MaxIterations - 1)
            {
                finishReason = AgenticFinishReason.IterationLimitReached;
                break;
            }

            foreach (var functionCall in functionCalls)
            {
                var result = await functionCall.InvokeAsync(kernel, cancellationToken);
                chatHistory.Add(result.ToChatMessage());
            }
        }

        var toolsInvoked = chatHistory
            .Where(m => m.Role == AuthorRole.Tool)
            .SelectMany(m => m.Items.OfType<FunctionResultContent>())
            .Select(f => f.FunctionName ?? string.Empty)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        foreach (var tool in toolsInvoked)
            logger.LogInformation("Tool invoked: {Tool}", tool);

        logger.LogInformation("Agentic task finished: {FinishReason}", finishReason);

        var content = chatHistory
            .Where(m => m.Role == AuthorRole.Assistant)
            .LastOrDefault(m => !string.IsNullOrEmpty(m.Content))
            ?.Content ?? string.Empty;

        return new AgenticResponse(content, finishReason, toolsInvoked.AsReadOnly());
    }
}
