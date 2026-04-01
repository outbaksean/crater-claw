using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CraterClaw.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CraterClaw.Api.Tests;

public sealed class AgenticStreamEndpointTests
{
    private static readonly IReadOnlyList<BehaviorProfile> TestProfiles =
    [
        new("no-tools", "No Tools", "Basic chat.", "You are a helpful assistant.", null, null, []),
    ];

    private static readonly AgenticResponse DefaultResponse =
        new("Hello world", AgenticFinishReason.Completed, ["ListTorrents"]);

    private static IEnumerable<JsonElement> ParseSseEvents(string body) =>
        body.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim())
            .Where(line => line.StartsWith("data: "))
            .Select(line => JsonSerializer.Deserialize<JsonElement>(line[6..]));

    private CraterClawApiFactory MakeFactory(FakeAgenticExecutionService agenticFake) =>
        new(services =>
        {
            services.AddSingleton<IAgenticExecutionService>(agenticFake);
            services.AddSingleton<IBehaviorProfileService>(new FakeBehaviorProfileService(TestProfiles));
        });

    [Fact]
    public async Task PostAgenticStream_StreamsChunksAndDoneEvent()
    {
        using var factory = MakeFactory(new FakeAgenticExecutionService(DefaultResponse, ["Hello ", "world"]));
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/providers/test/agentic/stream", new
        {
            modelName = "test-model",
            prompt = "list torrents",
            profileId = "no-tools"
        });

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var events = ParseSseEvents(await response.Content.ReadAsStringAsync()).ToList();

        Assert.Equal(3, events.Count);
        Assert.Equal("chunk", events[0].GetProperty("type").GetString());
        Assert.Equal("Hello ", events[0].GetProperty("content").GetString());
        Assert.Equal("chunk", events[1].GetProperty("type").GetString());
        Assert.Equal("world", events[1].GetProperty("content").GetString());
        Assert.Equal("done", events[2].GetProperty("type").GetString());
        Assert.Equal("Completed", events[2].GetProperty("finishReason").GetString());
        Assert.Contains("ListTorrents",
            events[2].GetProperty("toolsInvoked").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task PostAgenticStream_NoChunks_OnlyDoneEvent()
    {
        using var factory = MakeFactory(new FakeAgenticExecutionService(DefaultResponse));
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/providers/test/agentic/stream", new
        {
            modelName = "test-model",
            prompt = "test",
            profileId = "no-tools"
        });

        response.EnsureSuccessStatusCode();
        var events = ParseSseEvents(await response.Content.ReadAsStringAsync()).ToList();

        Assert.Single(events);
        Assert.Equal("done", events[0].GetProperty("type").GetString());
    }

    [Fact]
    public async Task PostAgenticStream_WithShowThinking_EmitsThinkingEvents()
    {
        using var factory = MakeFactory(new FakeAgenticExecutionService(
            DefaultResponse,
            chunks: ["Hello"],
            thinkingChunks: ["I should greet the user."]));
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/providers/test/agentic/stream", new
        {
            modelName = "test-model",
            prompt = "say hello",
            profileId = "no-tools",
            showThinking = true,
        });

        response.EnsureSuccessStatusCode();
        var events = ParseSseEvents(await response.Content.ReadAsStringAsync()).ToList();

        Assert.Contains(events, e =>
            e.GetProperty("type").GetString() == "thinking" &&
            e.GetProperty("content").GetString() == "I should greet the user.");
    }

    [Fact]
    public async Task PostAgenticStream_WithoutShowThinking_NoThinkingEvents()
    {
        using var factory = MakeFactory(new FakeAgenticExecutionService(
            DefaultResponse,
            chunks: ["Hello"],
            thinkingChunks: ["I should greet the user."]));
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/providers/test/agentic/stream", new
        {
            modelName = "test-model",
            prompt = "say hello",
            profileId = "no-tools",
        });

        response.EnsureSuccessStatusCode();
        var events = ParseSseEvents(await response.Content.ReadAsStringAsync()).ToList();

        Assert.DoesNotContain(events, e => e.GetProperty("type").GetString() == "thinking");
    }

    [Fact]
    public async Task PostAgenticStream_UnknownProvider_Returns404()
    {
        using var factory = MakeFactory(new FakeAgenticExecutionService(DefaultResponse));
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/providers/unknown/agentic/stream", new
        {
            modelName = "test-model",
            prompt = "test",
            profileId = "no-tools"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostAgenticStream_UnknownProfile_Returns400()
    {
        using var factory = MakeFactory(new FakeAgenticExecutionService(DefaultResponse));
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/providers/test/agentic/stream", new
        {
            modelName = "test-model",
            prompt = "test",
            profileId = "nonexistent-profile"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
