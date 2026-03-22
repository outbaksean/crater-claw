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

        var request = new AgenticRequest("test-model", "Do a task.", [], MaxIterations: 10);
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

        var request = new AgenticRequest("test-model", "Simple question.", [], MaxIterations: 10);
        var result = await service.ExecuteAsync(TestEndpoint, request, CancellationToken.None);

        Assert.Empty(result.ToolsInvoked);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsIterationLimitReached_WhenMaxIterationsExhausted()
    {
        var items = new ChatMessageContentItemCollection
        {
            new FunctionCallContent("TestFunction", "TestPlugin", "call-1")
        };
        var fake = new FakeChatCompletionService(
            new ChatMessageContent(AuthorRole.Assistant, items));
        var service = BuildService(fake);

        var request = new AgenticRequest("test-model", "Do something.", [], MaxIterations: 1);
        var result = await service.ExecuteAsync(TestEndpoint, request, CancellationToken.None);

        Assert.Equal(AgenticFinishReason.IterationLimitReached, result.FinishReason);
    }

    [Fact]
    public async Task ExecuteAsync_TracksToolsInvoked_WhenFunctionIsCalledAndReturned()
    {
        var functionCallItems = new ChatMessageContentItemCollection
        {
            new FunctionCallContent("GetValue", "TestPlugin", "call-1")
        };
        var fake = new FakeChatCompletionService(
            new ChatMessageContent(AuthorRole.Assistant, functionCallItems),
            new ChatMessageContent(AuthorRole.Assistant, "The value is 42."));

        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<IChatCompletionService>(fake);
        var kernel = builder.Build();
        kernel.Plugins.AddFromFunctions("TestPlugin",
            [KernelFunctionFactory.CreateFromMethod(() => "42", "GetValue")]);

        var service = new SemanticKernelAgenticExecutionService(
            new FakeKernelFactory(kernel),
            NullLogger<SemanticKernelAgenticExecutionService>.Instance,
            NullLoggerFactory.Instance);

        var request = new AgenticRequest("test-model", "Get the value.", [], MaxIterations: 10);
        var result = await service.ExecuteAsync(TestEndpoint, request, CancellationToken.None);

        Assert.Single(result.ToolsInvoked);
        Assert.Equal("GetValue", result.ToolsInvoked[0]);
        Assert.Equal("The value is 42.", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_WithSystemPrompt_AddsSystemMessageAsFirstChatMessage()
    {
        ChatHistory? captured = null;
        var fake = new CapturingChatCompletionService(
            h => captured = new ChatHistory(h),
            new ChatMessageContent(AuthorRole.Assistant, "Done."));
        var service = BuildService(fake);

        var request = new AgenticRequest("test-model", "Hello.", [], MaxIterations: 10, SystemPrompt: "You are a test bot.");
        await service.ExecuteAsync(TestEndpoint, request, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(AuthorRole.System, captured[0].Role);
        Assert.Equal("You are a test bot.", captured[0].Content);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullSystemPrompt_DoesNotAddSystemMessage()
    {
        ChatHistory? captured = null;
        var fake = new CapturingChatCompletionService(
            h => captured = new ChatHistory(h),
            new ChatMessageContent(AuthorRole.Assistant, "Done."));
        var service = BuildService(fake);

        var request = new AgenticRequest("test-model", "Hello.", [], MaxIterations: 10, SystemPrompt: null);
        await service.ExecuteAsync(TestEndpoint, request, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.DoesNotContain(captured, m => m.Role == AuthorRole.System);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptySystemPrompt_DoesNotAddSystemMessage()
    {
        ChatHistory? captured = null;
        var fake = new CapturingChatCompletionService(
            h => captured = new ChatHistory(h),
            new ChatMessageContent(AuthorRole.Assistant, "Done."));
        var service = BuildService(fake);

        var request = new AgenticRequest("test-model", "Hello.", [], MaxIterations: 10, SystemPrompt: "");
        await service.ExecuteAsync(TestEndpoint, request, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.DoesNotContain(captured, m => m.Role == AuthorRole.System);
    }

    private static SemanticKernelAgenticExecutionService BuildService(IChatCompletionService chatService)
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(chatService);
        var kernel = builder.Build();
        return new SemanticKernelAgenticExecutionService(
            new FakeKernelFactory(kernel),
            NullLogger<SemanticKernelAgenticExecutionService>.Instance,
            NullLoggerFactory.Instance);
    }

    private sealed class FakeKernelFactory(Kernel kernel) : IKernelFactory
    {
        public Kernel Create(ProviderEndpoint endpoint, string modelId) => kernel;
    }

    private sealed class CapturingChatCompletionService(
        Action<ChatHistory> onCapture,
        params ChatMessageContent[] responses) : IChatCompletionService
    {
        private int _index;

        public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

        public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            onCapture(chatHistory);
            var response = responses[Math.Min(_index++, responses.Length - 1)];
            return Task.FromResult<IReadOnlyList<ChatMessageContent>>([response]);
        }

        public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class FakeChatCompletionService(
        params ChatMessageContent[] responses) : IChatCompletionService
    {
        private int _index;

        public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

        public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            var response = responses[Math.Min(_index++, responses.Length - 1)];
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
