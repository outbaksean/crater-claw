using System.Net;
using System.Text;

namespace CraterClaw.Core.Tests;

public sealed class OllamaModelListingServiceTests
{
    [Fact]
    public async Task ListModelsAsync_ReturnsDescriptors_ForValidResponse()
    {
        const string json = """
            {
              "models": [
                {
                  "name": "llama3.2:latest",
                  "size": 2019393189,
                  "modified_at": "2024-10-21T14:30:00Z"
                },
                {
                  "name": "qwen2.5:7b",
                  "size": 4683075632,
                  "modified_at": "2024-11-01T09:00:00Z"
                }
              ]
            }
            """;

        using var client = CreateClient(HttpStatusCode.OK, json);
        var service = new OllamaModelListingService(client);

        var models = await service.ListModelsAsync(
            new ProviderEndpoint("local", "http://localhost:11434"),
            CancellationToken.None);

        Assert.Equal(2, models.Count);

        Assert.Equal("llama3.2:latest", models[0].Name);
        Assert.Equal(2019393189L, models[0].SizeBytes);
        Assert.Equal(DateTimeOffset.Parse("2024-10-21T14:30:00Z"), models[0].ModifiedAt);

        Assert.Equal("qwen2.5:7b", models[1].Name);
        Assert.Equal(4683075632L, models[1].SizeBytes);
    }

    [Fact]
    public async Task ListModelsAsync_ReturnsEmptyList_ForResponseWithNoModels()
    {
        const string json = """{ "models": [] }""";

        using var client = CreateClient(HttpStatusCode.OK, json);
        var service = new OllamaModelListingService(client);

        var models = await service.ListModelsAsync(
            new ProviderEndpoint("local", "http://localhost:11434"),
            CancellationToken.None);

        Assert.Empty(models);
    }

    [Fact]
    public async Task ListModelsAsync_ThrowsInvalidOperationException_ForMalformedJson()
    {
        using var client = CreateClient(HttpStatusCode.OK, "{ not valid json }");
        var service = new OllamaModelListingService(client);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ListModelsAsync(
                new ProviderEndpoint("local", "http://localhost:11434"),
                CancellationToken.None));
    }

    [Fact]
    public async Task ListModelsAsync_PropagatesCancellation_WhenTokenIsCancelled()
    {
        using var client = CreateClient(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var service = new OllamaModelListingService(client);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ListModelsAsync(
                new ProviderEndpoint("local", "http://localhost:11434"),
                cts.Token));
    }

    private static HttpClient CreateClient(HttpStatusCode statusCode, string responseBody)
    {
        return CreateClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            }));
    }

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        return new HttpClient(new DelegatingTestHandler(handler));
    }

    private sealed class DelegatingTestHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return handler(request, cancellationToken);
        }
    }
}
