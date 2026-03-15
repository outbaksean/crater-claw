using System.Net;
using System.Net.Http;

namespace CraterClaw.Core.Tests;

public sealed class McpAvailabilityServiceTests
{
    [Fact]
    public async Task CheckAvailabilityAsync_ReturnsAvailable_WhenHttpGetReceivesAnyResponse()
    {
        using var client = CreateClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var service = new McpAvailabilityService(client);

        var server = new McpServerDefinition(
            "searxng", "SearXNG", McpTransport.Http, "http://localhost:8080",
            null, null, null, true);

        var result = await service.CheckAvailabilityAsync(server, CancellationToken.None);

        Assert.True(result.IsAvailable);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_ReturnsUnavailable_WhenHttpConnectionFails()
    {
        using var client = CreateClient((_, _) =>
            throw new HttpRequestException("Connection refused"));
        var service = new McpAvailabilityService(client);

        var server = new McpServerDefinition(
            "searxng", "SearXNG", McpTransport.Http, "http://localhost:8080",
            null, null, null, true);

        var result = await service.CheckAvailabilityAsync(server, CancellationToken.None);

        Assert.False(result.IsAvailable);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_PropagatesCancellation_ForHttpServer()
    {
        using var client = CreateClient(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var service = new McpAvailabilityService(client);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var server = new McpServerDefinition(
            "searxng", "SearXNG", McpTransport.Http, "http://localhost:8080",
            null, null, null, true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.CheckAvailabilityAsync(server, cts.Token));
    }

    [Fact]
    public async Task CheckAvailabilityAsync_ReturnsAvailable_WhenStdioCommandFoundOnPath()
    {
        var command = OperatingSystem.IsWindows() ? "cmd" : "sh";
        var service = new McpAvailabilityService(new HttpClient());

        var server = new McpServerDefinition(
            "test", "Test", McpTransport.Stdio, null,
            command, null, null, true);

        var result = await service.CheckAvailabilityAsync(server, CancellationToken.None);

        Assert.True(result.IsAvailable);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_ReturnsUnavailable_WhenStdioCommandNotFound()
    {
        var service = new McpAvailabilityService(new HttpClient());

        var server = new McpServerDefinition(
            "test", "Test", McpTransport.Stdio, null,
            "this-command-does-not-exist-xyz-abc", null, null, true);

        var result = await service.CheckAvailabilityAsync(server, CancellationToken.None);

        Assert.False(result.IsAvailable);
        Assert.NotNull(result.ErrorMessage);
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
