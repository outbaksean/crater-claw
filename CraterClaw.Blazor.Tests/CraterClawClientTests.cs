using System.Net;
using System.Net.Http.Json;
using CraterClaw.Blazor.Api;

namespace CraterClaw.Blazor.Tests;

public class CraterClawClientTests
{
    private static CraterClawClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var http = new HttpClient(new TestHttpHandler(respond))
        {
            BaseAddress = new Uri("http://test/")
        };
        return new CraterClawClient(http);
    }

    [Fact]
    public async Task GetProvidersAsync_CallsCorrectEndpoint()
    {
        string? capturedPath = null;
        var expected = new List<ProviderEndpoint> { new("local", "http://localhost:11434") };
        var client = CreateClient(req =>
        {
            capturedPath = req.RequestUri?.PathAndQuery;
            return TestHttpHandler.Json(expected);
        });

        var result = await client.GetProvidersAsync();

        Assert.Equal("/api/providers", capturedPath);
        Assert.Single(result);
        Assert.Equal("local", result[0].Name);
    }

    [Fact]
    public async Task GetProviderStatusAsync_CallsCorrectEndpoint()
    {
        string? capturedPath = null;
        var client = CreateClient(req =>
        {
            capturedPath = req.RequestUri?.PathAndQuery;
            return TestHttpHandler.Json(new ProviderStatus(true, null));
        });

        var result = await client.GetProviderStatusAsync("local");

        Assert.Equal("/api/providers/local/status", capturedPath);
        Assert.True(result.IsReachable);
    }

    [Fact]
    public async Task GetModelsAsync_CallsCorrectEndpoint()
    {
        string? capturedPath = null;
        var expected = new List<ModelItem> { new("llama3", 4000000000, "2024-01-01T00:00:00Z") };
        var client = CreateClient(req =>
        {
            capturedPath = req.RequestUri?.PathAndQuery;
            return TestHttpHandler.Json(expected);
        });

        var result = await client.GetModelsAsync("local");

        Assert.Equal("/api/providers/local/models", capturedPath);
        Assert.Single(result);
        Assert.Equal("llama3", result[0].Name);
    }

    [Fact]
    public async Task PostExecuteAsync_PostsToCorrectEndpoint()
    {
        string? capturedPath = null;
        string? capturedMethod = null;
        var client = CreateClient(req =>
        {
            capturedPath = req.RequestUri?.PathAndQuery;
            capturedMethod = req.Method.Method;
            return TestHttpHandler.Json(new ExecutionResponse("Hello", "llama3", "Stop"));
        });

        var request = new ExecutionRequest("llama3", [new MessageItem("User", "hi")]);
        var result = await client.PostExecuteAsync("local", request);

        Assert.Equal("/api/providers/local/execute", capturedPath);
        Assert.Equal("POST", capturedMethod);
        Assert.Equal("Hello", result.Content);
    }

    [Fact]
    public async Task GetProfilesAsync_CallsCorrectEndpoint()
    {
        string? capturedPath = null;
        var expected = new List<BehaviorProfile>
        {
            new("no-tools", "No Tools", "Desc", "System", null, null, [])
        };
        var client = CreateClient(req =>
        {
            capturedPath = req.RequestUri?.PathAndQuery;
            return TestHttpHandler.Json(expected);
        });

        var result = await client.GetProfilesAsync();

        Assert.Equal("/api/profiles", capturedPath);
        Assert.Single(result);
    }

    [Fact]
    public async Task PostAgenticAsync_PostsToCorrectEndpoint()
    {
        string? capturedPath = null;
        string? capturedMethod = null;
        var client = CreateClient(req =>
        {
            capturedPath = req.RequestUri?.PathAndQuery;
            capturedMethod = req.Method.Method;
            return TestHttpHandler.Json(new AgenticResponse("Done", "Completed", []));
        });

        var request = new AgenticRequest("llama3", "Search for X", "no-tools");
        var result = await client.PostAgenticAsync("local", request);

        Assert.Equal("/api/providers/local/agentic", capturedPath);
        Assert.Equal("POST", capturedMethod);
        Assert.Equal("Done", result.Content);
    }
}
