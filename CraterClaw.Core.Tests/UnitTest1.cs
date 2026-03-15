using System.Net;
using System.Net.Http;

namespace CraterClaw.Core.Tests;

public sealed class ProviderContractTests
{
    [Fact]
    public void ProviderEndpoint_StoresNameAndBaseUrl()
    {
        var endpoint = new ProviderEndpoint("ollama", "http://localhost:11434");

        Assert.Equal("ollama", endpoint.Name);
        Assert.Equal("http://localhost:11434", endpoint.BaseUrl);
    }

    [Fact]
    public void ProviderStatus_RepresentsSuccessAndFailureStates()
    {
        var success = new ProviderStatus(true, null);
        var failure = new ProviderStatus(false, "Connection failed");

        Assert.True(success.IsReachable);
        Assert.Null(success.ErrorMessage);

        Assert.False(failure.IsReachable);
        Assert.False(string.IsNullOrWhiteSpace(failure.ErrorMessage));
    }
}

public sealed class OllamaProviderStatusServiceTests
{
    [Fact]
    public async Task CheckStatusAsync_ReturnsReachable_WhenApiTagsReturns200()
    {
        using var client = CreateClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var service = new OllamaProviderStatusService(client);

        var status = await service.CheckStatusAsync(
            new ProviderEndpoint("ollama", "http://localhost:11434"),
            CancellationToken.None);

        Assert.True(status.IsReachable);
        Assert.Null(status.ErrorMessage);
    }

    [Fact]
    public async Task CheckStatusAsync_ReturnsUnreachable_WhenHttpRequestThrows()
    {
        using var client = CreateClient((_, _) => throw new HttpRequestException("Host unreachable"));
        var service = new OllamaProviderStatusService(client);

        var status = await service.CheckStatusAsync(
            new ProviderEndpoint("ollama", "http://unreachable-host"),
            CancellationToken.None);

        Assert.False(status.IsReachable);
        Assert.False(string.IsNullOrWhiteSpace(status.ErrorMessage));
    }

    [Fact]
    public async Task CheckStatusAsync_ReturnsUnreachable_WhenApiTagsReturnsNonSuccess()
    {
        using var client = CreateClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var service = new OllamaProviderStatusService(client);

        var status = await service.CheckStatusAsync(
            new ProviderEndpoint("ollama", "http://localhost:11434"),
            CancellationToken.None);

        Assert.False(status.IsReachable);
        Assert.False(string.IsNullOrWhiteSpace(status.ErrorMessage));
    }

    [Fact]
    public async Task CheckStatusAsync_PropagatesCancellation_WhenTokenIsCancelled()
    {
        using var client = CreateClient(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var service = new OllamaProviderStatusService(client);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.CheckStatusAsync(
                new ProviderEndpoint("ollama", "http://localhost:11434"),
                cts.Token));
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
