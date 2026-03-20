using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CraterClaw.Core.Tests;

public sealed class QBitTorrentPluginTests
{
    private const string BaseUrl = "http://localhost:8080";
    private const string TestSid = "test_session_id";

    private static QBitTorrentPlugin CreatePlugin(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var client = new HttpClient(new DelegatingTestHandler(handler));
        var options = Options.Create(new QBitTorrentOptions
        {
            BaseUrl = BaseUrl,
            Username = "admin",
            Password = "password"
        });
        return new QBitTorrentPlugin(client, options, NullLogger<QBitTorrentPlugin>.Instance);
    }

    private static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> QueuedResponses(
        params HttpResponseMessage[] responses)
    {
        var queue = new Queue<HttpResponseMessage>(responses);
        return (_, _) => Task.FromResult(queue.Dequeue());
    }

    private static HttpResponseMessage LoginOkResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Ok.")
        };
        response.Headers.TryAddWithoutValidation("Set-Cookie", $"SID={TestSid}; path=/");
        return response;
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage OkResponse() =>
        new(HttpStatusCode.OK) { Content = new StringContent("Ok.") };

    [Fact]
    public async Task ListTorrentsAsync_ReturnsJson_WhenAuthenticated()
    {
        const string json = """[{"name":"test.torrent","state":"downloading","added_on":1700000000}]""";
        var plugin = CreatePlugin(QueuedResponses(LoginOkResponse(), JsonResponse(json)));

        var result = await plugin.ListTorrentsAsync();

        Assert.DoesNotContain("Error:", result);
        Assert.Contains("test.torrent", result);
        Assert.Contains("downloading", result);
    }

    [Fact]
    public async Task ListTorrentsAsync_ReauthenticatesAndRetries_On403()
    {
        const string json = """[{"name":"test.torrent","state":"downloading","added_on":1700000000}]""";
        var plugin = CreatePlugin(QueuedResponses(
            LoginOkResponse(),
            new HttpResponseMessage(HttpStatusCode.Forbidden),
            LoginOkResponse(),
            JsonResponse(json)));

        var result = await plugin.ListTorrentsAsync();

        Assert.DoesNotContain("Error:", result);
        Assert.Contains("test.torrent", result);
    }

    [Fact]
    public async Task ListTorrentsAsync_ReturnsError_WhenLoginFails()
    {
        var plugin = CreatePlugin(QueuedResponses(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("Fails.") }));

        var result = await plugin.ListTorrentsAsync();

        Assert.StartsWith("Error:", result);
    }

    [Fact]
    public async Task AddTorrentByUrlAsync_ReturnsSuccess_WhenOkResponse()
    {
        var plugin = CreatePlugin(QueuedResponses(LoginOkResponse(), OkResponse()));

        var result = await plugin.AddTorrentByUrlAsync("magnet:?xt=urn:btih:test");

        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task PauseTorrentAsync_ReturnsSuccess()
    {
        var plugin = CreatePlugin(QueuedResponses(LoginOkResponse(), OkResponse()));

        var result = await plugin.PauseTorrentAsync("abc123");

        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task ResumeTorrentAsync_ReturnsSuccess()
    {
        var plugin = CreatePlugin(QueuedResponses(LoginOkResponse(), OkResponse()));

        var result = await plugin.ResumeTorrentAsync("abc123");

        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task DeleteTorrentAsync_ReturnsSuccess()
    {
        var plugin = CreatePlugin(QueuedResponses(LoginOkResponse(), OkResponse()));

        var result = await plugin.DeleteTorrentAsync("abc123", deleteFiles: true);

        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task GetTransferStatsAsync_ReturnsJson_WhenAuthenticated()
    {
        const string json = """{"dl_info_speed":1024,"up_info_speed":512,"dl_info_data":100000}""";
        var plugin = CreatePlugin(QueuedResponses(LoginOkResponse(), JsonResponse(json)));

        var result = await plugin.GetTransferStatsAsync();

        Assert.DoesNotContain("Error:", result);
        Assert.Contains("dl_info_speed", result);
    }

    private sealed class DelegatingTestHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => handler(request, cancellationToken);
    }
}
