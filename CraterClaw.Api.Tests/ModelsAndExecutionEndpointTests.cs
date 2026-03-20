using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CraterClaw.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CraterClaw.Api.Tests;

public sealed class ModelsAndExecutionEndpointTests
{
    [Fact]
    public async Task GetModels_KnownEndpoint_ReturnsModels()
    {
        var models = new List<ModelDescriptor>
        {
            new("llama3.2", 2_000_000_000, DateTimeOffset.UtcNow),
            new("mistral", 4_000_000_000, DateTimeOffset.UtcNow)
        };
        var fake = new FakeModelListingService(models);
        using var factory = new CraterClawApiFactory(
            services => services.AddSingleton<IModelListingService>(fake));
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/providers/test/models");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("llama3.2", items[0].GetProperty("name").GetString());
        Assert.Equal(2_000_000_000, items[0].GetProperty("sizeBytes").GetInt64());
        Assert.Equal("mistral", items[1].GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetModels_UnknownEndpoint_Returns404()
    {
        using var factory = new CraterClawApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/providers/unknown/models");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Execute_KnownEndpoint_ReturnsResponse()
    {
        var fakeResponse = new ExecutionResponse("Hello there", "llama3.2", FinishReason.Stop);
        var fake = new FakeModelExecutionService(fakeResponse);
        using var factory = new CraterClawApiFactory(
            services => services.AddSingleton<IModelExecutionService>(fake));
        var client = factory.CreateClient();

        var request = new
        {
            modelName = "llama3.2",
            messages = new[] { new { role = "User", content = "Hi" } }
        };
        var response = await client.PostAsJsonAsync("/api/providers/test/execute", request);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Hello there", body.GetProperty("content").GetString());
        Assert.Equal("llama3.2", body.GetProperty("modelName").GetString());
        Assert.Equal("Stop", body.GetProperty("finishReason").GetString());
    }

    [Fact]
    public async Task Execute_UnknownEndpoint_Returns404()
    {
        using var factory = new CraterClawApiFactory();
        var client = factory.CreateClient();

        var request = new { modelName = "llama3.2", messages = Array.Empty<object>() };
        var response = await client.PostAsJsonAsync("/api/providers/unknown/execute", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
