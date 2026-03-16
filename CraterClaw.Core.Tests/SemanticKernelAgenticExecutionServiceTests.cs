using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace CraterClaw.Core.Tests;

public sealed class SemanticKernelAgenticExecutionServiceTests
{
    private static readonly ProviderEndpoint TestEndpoint = new("local", "http://localhost:11434");

    [Fact]
    public async Task ExecuteAsync_ReturnsContent_WhenAgentRespondsDirectly()
    {
        var fake = new FakeChatCompletionService(
            new ChatMessageContent(AuthorRole.Assistant, "Task complete."));
        var service = BuildService(fake);

        var request = new AgenticRequest("test-model", "Do a task.", [], 10);
        var result = await service.ExecuteAsync(TestEndpoint, request, CancellationToken.None);

        Assert.Equal("Task complete.", result.Content);
        Assert.Equal(AgenticFinishReason.Completed, result.FinishReason);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsEmptyToolsInvoked_WhenNoToolsUsed()
    {
        var fake = new FakeChatCompletionService(
            new ChatMessageContent(AuthorRole.Assistant, "Done."));
        var service = BuildService(fake);

        var request = new AgenticRequest("test-model", "Simple question.", [], 10);
        var result = await service.ExecuteAsync(TestEndpoint, request, CancellationToken.None);

        Assert.Empty(result.ToolsInvoked);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsIterationLimitReached_WhenLastMessageHasFunctionCall()
    {
        var items = new ChatMessageContentItemCollection
        {
            new FunctionCallContent("TestFunction", "TestPlugin", "call-1")
        };
        var fake = new FakeChatCompletionService(
            new ChatMessageContent(AuthorRole.Assistant, items));
        var service = BuildService(fake);

        var request = new AgenticRequest("test-model", "Do something.", [], 10);
        var result = await service.ExecuteAsync(TestEndpoint, request, CancellationToken.None);

        Assert.Equal(AgenticFinishReason.IterationLimitReached, result.FinishReason);
    }

    [Fact]
    public async Task ExecuteAsync_TracksToolsInvoked_WhenFunctionResultInHistory()
    {
        var fake = new FakeChatCompletionService(
            new ChatMessageContent(AuthorRole.Assistant, "Here are your torrents."),
            sideEffect: chatHistory =>
            {
                var toolMessage = new ChatMessageContent(AuthorRole.Tool, string.Empty);
                toolMessage.Items.Add(new FunctionResultContent("ListTorrents", "QBitTorrent", "call-1", "[]"));
                chatHistory.Add(toolMessage);
            });
        var service = BuildService(fake);

        var request = new AgenticRequest("test-model", "List torrents.", [], 10);
        var result = await service.ExecuteAsync(TestEndpoint, request, CancellationToken.None);

        Assert.Single(result.ToolsInvoked);
        Assert.Equal("ListTorrents", result.ToolsInvoked[0]);
    }

    private static SemanticKernelAgenticExecutionService BuildService(IChatCompletionService chatService)
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(chatService);
        var kernel = builder.Build();
        return new SemanticKernelAgenticExecutionService(
            new FakeKernelFactory(kernel),
            NullLogger<SemanticKernelAgenticExecutionService>.Instance);
    }

    private sealed class FakeKernelFactory(Kernel kernel) : IKernelFactory
    {
        public Kernel Create(ProviderEndpoint endpoint, string modelId) => kernel;
    }

    private sealed class FakeChatCompletionService(
        ChatMessageContent response,
        Action<ChatHistory>? sideEffect = null) : IChatCompletionService
    {
        public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

        public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            sideEffect?.Invoke(chatHistory);
            return Task.FromResult<IReadOnlyList<ChatMessageContent>>([response]);
        }

        public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
