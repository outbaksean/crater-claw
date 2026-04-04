using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CraterClaw.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CraterClaw.Api.Tests;

public sealed class ProfilesMcpAgenticEndpointTests
{
    private static readonly IReadOnlyList<BehaviorProfile> TestProfiles =
    [
        new("no-tools", "No Tools", "Basic chat with no plugins.", "You are a helpful assistant.", null, null, null, false, []),
        new("qbittorrent-manager", "qBitTorrent Manager", "Manage torrents.", "You are a torrent manager.", null, null, null, false,
            [new PluginBinding("qbittorrent", [], new Dictionary<string, string>())])
    ];

    // --- Profiles ---

    [Fact]
    public async Task GetProfiles_ReturnsAllProfiles()
    {
        var fake = new FakeBehaviorProfileService(TestProfiles);
        using var factory = new CraterClawApiFactory(
            services => services.AddSingleton<IBehaviorProfileService>(fake));
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/profiles");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var profiles = body.EnumerateArray().ToList();
        Assert.Equal(2, profiles.Count);
        Assert.Equal("no-tools", profiles[0].GetProperty("id").GetString());
        Assert.Equal("qbittorrent-manager", profiles[1].GetProperty("id").GetString());
    }

    // --- Agentic ---

    [Fact]
    public async Task AgenticExecute_KnownEndpointAndProfile_ReturnsResponse()
    {
        var fakeAgentic = new FakeAgenticExecutionService(
            new AgenticResponse("Done.", AgenticFinishReason.Completed, ["ListTorrents"]));
        var fakeProfiles = new FakeBehaviorProfileService(TestProfiles);
        using var factory = new CraterClawApiFactory(services =>
        {
            services.AddSingleton<IAgenticExecutionService>(fakeAgentic);
            services.AddSingleton<IBehaviorProfileService>(fakeProfiles);
        });
        var client = factory.CreateClient();

        var request = new { modelName = "llama3.2", prompt = "List my torrents", profileId = "no-tools", maxIterations = 5 };
        var response = await client.PostAsJsonAsync("/api/providers/test/agentic", request);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Done.", body.GetProperty("content").GetString());
        Assert.Equal("Completed", body.GetProperty("finishReason").GetString());
        Assert.Equal("ListTorrents", body.GetProperty("toolsInvoked")[0].GetString());
    }

    [Fact]
    public async Task AgenticExecute_UnknownProfile_Returns400()
    {
        var fakeProfiles = new FakeBehaviorProfileService(TestProfiles);
        using var factory = new CraterClawApiFactory(
            services => services.AddSingleton<IBehaviorProfileService>(fakeProfiles));
        var client = factory.CreateClient();

        var request = new { modelName = "llama3.2", prompt = "Hi", profileId = "nonexistent", maxIterations = 5 };
        var response = await client.PostAsJsonAsync("/api/providers/test/agentic", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AgenticExecute_UnknownEndpoint_Returns404()
    {
        var fakeProfiles = new FakeBehaviorProfileService(TestProfiles);
        using var factory = new CraterClawApiFactory(
            services => services.AddSingleton<IBehaviorProfileService>(fakeProfiles));
        var client = factory.CreateClient();

        var request = new { modelName = "llama3.2", prompt = "Hi", profileId = "no-tools", maxIterations = 5 };
        var response = await client.PostAsJsonAsync("/api/providers/unknown/agentic", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
