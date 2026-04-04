using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace CraterClaw.Core.Tests;

public sealed class SubAgentPluginTests
{
    private static readonly ProviderEndpoint TestEndpoint = new("local", "http://localhost:11434");

    private static readonly BehaviorProfile TestProfile = new(
        "child-profile", "Child", "Desc", "System prompt", null, "llama3.2", null, false, []);

    [Fact]
    public async Task RunAsync_ReturnsError_WhenCurrentEndpointIsNull()
    {
        PluginExecutionContext.CurrentEndpoint.Value = null;
        var plugin = BuildPlugin("child-profile", new FakeProfileService(TestProfile), new RecordingAgenticService(), new FakePluginRegistry());

        var result = await plugin.RunAsync("do something", CancellationToken.None);

        Assert.Contains("endpoint", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_ReturnsError_WhenProfileNotFound()
    {
        PluginExecutionContext.CurrentEndpoint.Value = TestEndpoint;
        PluginExecutionContext.CurrentDepth.Value = 0;
        var plugin = BuildPlugin("missing-profile", new FakeProfileService(null), new RecordingAgenticService(), new FakePluginRegistry());

        var result = await plugin.RunAsync("do something", CancellationToken.None);

        Assert.Contains("missing-profile", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_CallsAgenticServiceWithDepthPlusOne()
    {
        PluginExecutionContext.CurrentEndpoint.Value = TestEndpoint;
        PluginExecutionContext.CurrentDepth.Value = 0;
        var agenticService = new RecordingAgenticService();
        var plugin = BuildPlugin("child-profile", new FakeProfileService(TestProfile), agenticService, new FakePluginRegistry());

        await plugin.RunAsync("the prompt", CancellationToken.None);

        Assert.NotNull(agenticService.LastRequest);
        Assert.Equal(1, agenticService.LastRequest.Depth);
    }

    [Fact]
    public async Task RunAsync_CallsChildStreamStart_WithFunctionNameAndPrompt()
    {
        PluginExecutionContext.CurrentEndpoint.Value = TestEndpoint;
        PluginExecutionContext.CurrentDepth.Value = 0;

        var emitted = new List<(string source, string prompt)>();
        PluginExecutionContext.ChildStreamStart.Value = (source, p) =>
        {
            emitted.Add((source, p));
            return Task.CompletedTask;
        };

        var plugin = BuildPlugin("child-profile", new FakeProfileService(TestProfile), new RecordingAgenticService(), new FakePluginRegistry());
        await plugin.RunAsync("the prompt", CancellationToken.None);

        Assert.Single(emitted);
        Assert.Equal("RunChild", emitted[0].source);
        Assert.Equal("the prompt", emitted[0].prompt);

        PluginExecutionContext.ChildStreamStart.Value = null;
    }

    [Fact]
    public async Task RunAsync_PassesChildStreamCallback_WhenContextHasChildStreamChunk()
    {
        PluginExecutionContext.CurrentEndpoint.Value = TestEndpoint;
        PluginExecutionContext.CurrentDepth.Value = 0;

        var emitted = new List<(string source, string text)>();
        PluginExecutionContext.ChildStreamChunk.Value = (source, text) =>
        {
            emitted.Add((source, text));
            return Task.CompletedTask;
        };

        AgenticRequest? capturedRequest = null;
        var agenticService = new CapturingAgenticService(r => capturedRequest = r);
        var plugin = BuildPlugin("child-profile", new FakeProfileService(TestProfile), agenticService, new FakePluginRegistry());

        await plugin.RunAsync("the prompt", CancellationToken.None);

        Assert.NotNull(capturedRequest?.StreamChunk);
        await capturedRequest!.StreamChunk!("hello");
        Assert.Single(emitted);
        Assert.Equal("RunChild", emitted[0].source);
        Assert.Equal("hello", emitted[0].text);

        PluginExecutionContext.ChildStreamChunk.Value = null;
    }

    [Fact]
    public async Task RunAsync_PassesNullStreamChunk_WhenContextHasNoChildStreamChunk()
    {
        PluginExecutionContext.CurrentEndpoint.Value = TestEndpoint;
        PluginExecutionContext.CurrentDepth.Value = 0;
        PluginExecutionContext.ChildStreamChunk.Value = null;

        AgenticRequest? capturedRequest = null;
        var agenticService = new CapturingAgenticService(r => capturedRequest = r);
        var plugin = BuildPlugin("child-profile", new FakeProfileService(TestProfile), agenticService, new FakePluginRegistry());

        await plugin.RunAsync("the prompt", CancellationToken.None);

        Assert.Null(capturedRequest?.StreamChunk);
    }

    [Fact]
    public async Task RunAsync_ReturnsAgenticResponseContent_OnSuccess()
    {
        PluginExecutionContext.CurrentEndpoint.Value = TestEndpoint;
        PluginExecutionContext.CurrentDepth.Value = 0;
        var agenticService = new RecordingAgenticService(responseContent: "child result");
        var plugin = BuildPlugin("child-profile", new FakeProfileService(TestProfile), agenticService, new FakePluginRegistry());

        var result = await plugin.RunAsync("the prompt", CancellationToken.None);

        Assert.Equal("child result", result);
    }

    private static SubAgentPlugin BuildPlugin(
        string profileId,
        IBehaviorProfileService profileService,
        IAgenticExecutionService agenticService,
        IPluginRegistry pluginRegistry) =>
        new(profileId, "RunChild", "Runs the child agent", profileService, agenticService, pluginRegistry);

    private sealed class FakeProfileService(BehaviorProfile? profile) : IBehaviorProfileService
    {
        public IReadOnlyList<BehaviorProfile> GetAll() => profile is not null ? [profile] : [];
        public BehaviorProfile? GetById(string id) => profile?.Id == id ? profile : null;
    }

    private sealed class CapturingAgenticService(Action<AgenticRequest> onCapture) : IAgenticExecutionService
    {
        public Task<AgenticResponse> ExecuteAsync(
            ProviderEndpoint endpoint,
            AgenticRequest request,
            CancellationToken cancellationToken)
        {
            onCapture(request);
            return Task.FromResult(new AgenticResponse("ok", AgenticFinishReason.Completed, []));
        }
    }

    private sealed class RecordingAgenticService(string responseContent = "ok") : IAgenticExecutionService
    {
        public AgenticRequest? LastRequest { get; private set; }

        public Task<AgenticResponse> ExecuteAsync(
            ProviderEndpoint endpoint,
            AgenticRequest request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new AgenticResponse(responseContent, AgenticFinishReason.Completed, []));
        }
    }

    private sealed class FakePluginRegistry : IPluginRegistry
    {
        public IReadOnlyList<KernelPlugin> Resolve(IEnumerable<PluginBinding> plugins) => [];
    }
}
