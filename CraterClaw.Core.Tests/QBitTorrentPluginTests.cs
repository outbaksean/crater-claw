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

    [Fact]
    public async Task SearchTorrentsAsync_ReturnsResults_WhenSearchCompletesAfterPolling()
    {
        const string results = """
            {"results":[
                {"fileName":"file1.mkv","fileUrl":"magnet:?xt=urn:btih:aaa","fileSize":1000,"nbSeeders":5,"nbLeechers":2,"siteUrl":"example.com"},
                {"fileName":"file2.mkv","fileUrl":"magnet:?xt=urn:btih:bbb","fileSize":2000,"nbSeeders":3,"nbLeechers":1,"siteUrl":"example.com"}
            ],"status":"Stopped","total":2}
            """;
        var plugin = CreatePlugin(QueuedResponses(
            LoginOkResponse(),
            JsonResponse("""{"id":1}"""),
            JsonResponse("""[{"id":1,"status":"Running","total":0}]"""),
            JsonResponse("""[{"id":1,"status":"Stopped","total":2}]"""),
            JsonResponse(results),
            OkResponse()));

        var result = await plugin.SearchTorrentsAsync("test query");

        Assert.DoesNotContain("Error:", result);
        Assert.Contains("file1.mkv", result);
        Assert.Contains("file2.mkv", result);
    }

    [Fact]
    public async Task SearchTorrentsAsync_ReturnsResults_WhenFirstStatusIsStopped()
    {
        const string results = """
            {"results":[
                {"fileName":"file1.mkv","fileUrl":"http://example.com/file.torrent","fileSize":500,"nbSeeders":10,"nbLeechers":0,"siteUrl":"example.com"}
            ],"status":"Stopped","total":1}
            """;
        var plugin = CreatePlugin(QueuedResponses(
            LoginOkResponse(),
            JsonResponse("""{"id":2}"""),
            JsonResponse("""[{"id":2,"status":"Stopped","total":1}]"""),
            JsonResponse(results),
            OkResponse()));

        var result = await plugin.SearchTorrentsAsync("test query");

        Assert.DoesNotContain("Error:", result);
        Assert.Contains("file1.mkv", result);
    }

    [Fact]
    public async Task SearchTorrentsAsync_ReturnsError_WhenNotConfigured()
    {
        var client = new HttpClient(new DelegatingTestHandler((_, _) => throw new InvalidOperationException("should not be called")));
        var options = Options.Create(new QBitTorrentOptions { BaseUrl = string.Empty });
        var plugin = new QBitTorrentPlugin(client, options, NullLogger<QBitTorrentPlugin>.Instance);

        var result = await plugin.SearchTorrentsAsync("ubuntu");

        Assert.StartsWith("Error:", result);
    }

    [Fact]
    public async Task SearchTorrentsAsync_ReturnsEmptyArray_WhenResultsAreEmpty()
    {
        var deleteCallCount = 0;
        var plugin = CreatePlugin((req, _) =>
        {
            if (req.RequestUri!.AbsolutePath.EndsWith("/auth/login"))
                return Task.FromResult(LoginOkResponse());
            if (req.RequestUri.AbsolutePath.EndsWith("/search/start"))
                return Task.FromResult(JsonResponse("""{"id":3}"""));
            if (req.RequestUri.AbsolutePath.EndsWith("/search/status"))
                return Task.FromResult(JsonResponse("""[{"id":3,"status":"Stopped","total":0}]"""));
            if (req.RequestUri.AbsolutePath.EndsWith("/search/results"))
                return Task.FromResult(JsonResponse("""{"results":[],"status":"Stopped","total":0}"""));
            if (req.RequestUri.AbsolutePath.EndsWith("/search/delete"))
            {
                deleteCallCount++;
                return Task.FromResult(OkResponse());
            }
            throw new InvalidOperationException($"Unexpected request: {req.RequestUri}");
        });

        var result = await plugin.SearchTorrentsAsync("nothing");

        Assert.Equal("[]", result);
        Assert.Equal(1, deleteCallCount);
    }

    [Fact]
    public async Task SearchTorrentsAsync_TrimsMagnetTrackers()
    {
        const string magnetUrl = "magnet:?xt=urn:btih:abc123&dn=some+file&tr=udp://tracker.example.com:1337&tr=udp://other.tracker.com:80";
        var results = $$"""
            {"results":[
                {"fileName":"some file.mkv","fileUrl":"{{magnetUrl}}","fileSize":100,"nbSeeders":1,"nbLeechers":0,"siteUrl":"example.com"}
            ],"status":"Stopped","total":1}
            """;
        var plugin = CreatePlugin(QueuedResponses(
            LoginOkResponse(),
            JsonResponse("""{"id":4}"""),
            JsonResponse("""[{"id":4,"status":"Stopped","total":1}]"""),
            JsonResponse(results),
            OkResponse()));

        var result = await plugin.SearchTorrentsAsync("some file");

        Assert.DoesNotContain("&tr=", result);
        Assert.Contains("urn:btih:abc123", result);
    }

    [Fact]
    public async Task SearchTorrentsAsync_TruncatesLongFileName()
    {
        var longName = new string('a', 200);
        var results = $$"""
            {"results":[
                {"fileName":"{{longName}}","fileUrl":"http://example.com/f.torrent","fileSize":100,"nbSeeders":1,"nbLeechers":0,"siteUrl":"example.com"}
            ],"status":"Stopped","total":1}
            """;
        var plugin = CreatePlugin(QueuedResponses(
            LoginOkResponse(),
            JsonResponse("""{"id":5}"""),
            JsonResponse("""[{"id":5,"status":"Stopped","total":1}]"""),
            JsonResponse(results),
            OkResponse()));

        var result = await plugin.SearchTorrentsAsync("aaa");

        var parsed = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(result);
        var fileName = parsed![0].GetProperty("fileName").GetString();
        Assert.Equal(120, fileName!.Length);
    }

    private sealed class DelegatingTestHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => handler(request, cancellationToken);
    }
}
