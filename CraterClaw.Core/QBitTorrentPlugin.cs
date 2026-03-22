using System.ComponentModel;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace CraterClaw.Core;

public sealed class QBitTorrentPlugin(
    HttpClient httpClient,
    QBitTorrentOptions options,
    ILogger<QBitTorrentPlugin> logger)
{
    private readonly QBitTorrentOptions _options = options;
    private string? _sid;

    private bool IsConfigured => !string.IsNullOrWhiteSpace(_options.BaseUrl);

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (_sid is not null)
            return;

        logger.LogDebug("Authenticating with qBitTorrent");

        var baseUri = new Uri(_options.BaseUrl!);
        var origin = $"{baseUri.Scheme}://{baseUri.Authority}";

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/api/v2/auth/login")
        {
            Content = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("username", _options.Username ?? string.Empty),
                new KeyValuePair<string, string>("password", _options.Password ?? string.Empty)
            ])
        };
        request.Headers.TryAddWithoutValidation("Referer", $"{_options.BaseUrl}/");
        request.Headers.TryAddWithoutValidation("Origin", origin);

        var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!body.Trim().Equals("Ok.", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"qBitTorrent login failed: {body.Trim()}");

        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookie in cookies)
            {
                if (cookie.StartsWith("SID=", StringComparison.OrdinalIgnoreCase))
                {
                    var eqIndex = cookie.IndexOf('=');
                    _sid = cookie[(eqIndex + 1)..].Split(';')[0].Trim();
                    logger.LogInformation("Authenticated with qBitTorrent");
                    return;
                }
            }
        }

        throw new InvalidOperationException("qBitTorrent login succeeded but no SID cookie was returned.");
    }

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var request = requestFactory();
        request.Headers.TryAddWithoutValidation("Cookie", $"SID={_sid}");
        var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            logger.LogWarning("qBitTorrent returned 403, re-authenticating");
            _sid = null;
            await EnsureAuthenticatedAsync(cancellationToken);
            request = requestFactory();
            request.Headers.TryAddWithoutValidation("Cookie", $"SID={_sid}");
            response = await httpClient.SendAsync(request, cancellationToken);
        }

        return response;
    }

    [KernelFunction, Description("List all torrents with name, hash, status, progress, and size.")]
    public async Task<string> ListTorrentsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return "Error: qBitTorrent is not configured.";
        try
        {
            var response = await SendAuthenticatedAsync(
                () => new HttpRequestMessage(HttpMethod.Get, $"{_options.BaseUrl}/api/v2/torrents/info"),
                cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var torrents = JsonNode.Parse(json)?.AsArray();
            if (torrents is null) return "[]";
            var trimmed = torrents.Select(t => new
            {
                name = t?["name"]?.ToString(),
                state = t?["state"]?.ToString(),
                added_on = t?["added_on"]?.GetValue<long>()
            });
            return JsonSerializer.Serialize(trimmed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ListTorrents failed");
            return $"Error: {ex.Message}";
        }
    }

    [KernelFunction, Description("Add a torrent from a magnet link or HTTP URL.")]
    public async Task<string> AddTorrentByUrlAsync(
        [Description("The magnet link or HTTP URL.")] string url,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return "Error: qBitTorrent is not configured.";
        try
        {
            var response = await SendAuthenticatedAsync(
                () => new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/api/v2/torrents/add")
                {
                    Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("urls", url)])
                },
                cancellationToken);
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AddTorrentByUrl failed");
            return $"Error: {ex.Message}";
        }
    }

    [KernelFunction, Description("Pause a torrent by its hash.")]
    public async Task<string> PauseTorrentAsync(
        [Description("The torrent hash.")] string hash,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return "Error: qBitTorrent is not configured.";
        try
        {
            var response = await SendAuthenticatedAsync(
                () => new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/api/v2/torrents/pause")
                {
                    Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("hashes", hash)])
                },
                cancellationToken);
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PauseTorrent failed");
            return $"Error: {ex.Message}";
        }
    }

    [KernelFunction, Description("Resume a paused torrent by its hash.")]
    public async Task<string> ResumeTorrentAsync(
        [Description("The torrent hash.")] string hash,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return "Error: qBitTorrent is not configured.";
        try
        {
            var response = await SendAuthenticatedAsync(
                () => new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/api/v2/torrents/resume")
                {
                    Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("hashes", hash)])
                },
                cancellationToken);
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ResumeTorrent failed");
            return $"Error: {ex.Message}";
        }
    }

    [KernelFunction, Description("Delete a torrent by its hash, with optional file deletion.")]
    public async Task<string> DeleteTorrentAsync(
        [Description("The torrent hash.")] string hash,
        [Description("Whether to also delete downloaded files.")] bool deleteFiles = false,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return "Error: qBitTorrent is not configured.";
        try
        {
            var response = await SendAuthenticatedAsync(
                () => new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/api/v2/torrents/delete")
                {
                    Content = new FormUrlEncodedContent([
                        new KeyValuePair<string, string>("hashes", hash),
                        new KeyValuePair<string, string>("deleteFiles", deleteFiles ? "true" : "false")
                    ])
                },
                cancellationToken);
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DeleteTorrent failed");
            return $"Error: {ex.Message}";
        }
    }

    [KernelFunction, Description("Get current transfer statistics including speeds and session totals.")]
    public async Task<string> GetTransferStatsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return "Error: qBitTorrent is not configured.";
        try
        {
            var response = await SendAuthenticatedAsync(
                () => new HttpRequestMessage(HttpMethod.Get, $"{_options.BaseUrl}/api/v2/transfer/info"),
                cancellationToken);
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetTransferStats failed");
            return $"Error: {ex.Message}";
        }
    }

    [KernelFunction, Description("Search for torrents using installed search plugins. Returns a JSON array of results with fileName, fileUrl, fileSize, nbSeeders, nbLeechers, and siteUrl.")]
    public async Task<string> SearchTorrentsAsync(
        [Description("The search query string.")] string query,
        [Description("The search category. Use 'all' for all categories, or a specific category: 'movies', 'music', 'software', 'games', 'anime', 'books'.")] string category = "all",
        [Description("Maximum number of results to return. Defaults to 10.")] int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return "Error: qBitTorrent is not configured.";
        try
        {
            var startResponse = await SendAuthenticatedAsync(
                () => new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/api/v2/search/start")
                {
                    Content = new FormUrlEncodedContent([
                        new KeyValuePair<string, string>("pattern", query),
                        new KeyValuePair<string, string>("plugins", "all"),
                        new KeyValuePair<string, string>("category", category)
                    ])
                },
                cancellationToken);
            var startJson = await startResponse.Content.ReadAsStringAsync(cancellationToken);
            var searchId = JsonNode.Parse(startJson)?["id"]?.GetValue<int>()
                ?? throw new InvalidOperationException("Search start did not return an id.");

            logger.LogDebug("SearchTorrents started job {SearchId} category '{Category}'", searchId, category);

            var searchComplete = false;
            for (var i = 0; i < 30; i++)
            {
                if (i > 0)
                    await Task.Delay(500, cancellationToken);

                var statusResponse = await SendAuthenticatedAsync(
                    () => new HttpRequestMessage(HttpMethod.Get, $"{_options.BaseUrl}/api/v2/search/status?id={searchId}"),
                    cancellationToken);
                var statusJson = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
                var statuses = JsonNode.Parse(statusJson)?.AsArray();
                if (statuses is not null)
                {
                    foreach (var entry in statuses)
                    {
                        if (entry?["id"]?.GetValue<int>() == searchId &&
                            string.Equals(entry["status"]?.ToString(), "Stopped", StringComparison.OrdinalIgnoreCase))
                        {
                            searchComplete = true;
                            break;
                        }
                    }
                }
                if (searchComplete) break;
            }

            if (!searchComplete)
                logger.LogWarning("SearchTorrents job {SearchId} did not complete within the poll limit", searchId);

            var resultsResponse = await SendAuthenticatedAsync(
                () => new HttpRequestMessage(HttpMethod.Get, $"{_options.BaseUrl}/api/v2/search/results?id={searchId}&limit={maxResults}&offset=0"),
                cancellationToken);
            var resultsJson = await resultsResponse.Content.ReadAsStringAsync(cancellationToken);
            var resultsArray = JsonNode.Parse(resultsJson)?["results"]?.AsArray() ?? new JsonArray();

            var projected = resultsArray.Select(r =>
            {
                var fileName = r?["fileName"]?.ToString() ?? string.Empty;
                if (fileName.Length > 120) fileName = fileName[..120];

                var fileUrl = r?["fileUrl"]?.ToString() ?? string.Empty;
                if (fileUrl.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
                {
                    var trIndex = fileUrl.IndexOf("&tr=", StringComparison.OrdinalIgnoreCase);
                    if (trIndex >= 0) fileUrl = fileUrl[..trIndex];
                }

                return new
                {
                    fileName,
                    fileUrl,
                    fileSize = r?["fileSize"]?.GetValue<long>() ?? 0L,
                    nbSeeders = r?["nbSeeders"]?.GetValue<int>() ?? 0,
                    nbLeechers = r?["nbLeechers"]?.GetValue<int>() ?? 0,
                    siteUrl = r?["siteUrl"]?.ToString() ?? string.Empty
                };
            }).ToList();

            logger.LogInformation("SearchTorrents job {SearchId} returned {Count} results", searchId, projected.Count);

            try
            {
                await SendAuthenticatedAsync(
                    () => new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/api/v2/search/delete")
                    {
                        Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("id", searchId.ToString())])
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete search job {SearchId}", searchId);
            }

            return JsonSerializer.Serialize(projected);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SearchTorrents failed");
            return $"Error: {ex.Message}";
        }
    }

    public static IReadOnlyList<(string Name, string Description)> GetFunctionDescriptions() =>
    [
        ("ListTorrents", "List all torrents with name, hash, status, progress, and size."),
        ("AddTorrentByUrl", "Add a torrent from a magnet link or HTTP URL."),
        ("PauseTorrent", "Pause a torrent by its hash."),
        ("ResumeTorrent", "Resume a paused torrent by its hash."),
        ("DeleteTorrent", "Delete a torrent by its hash, with optional file deletion."),
        ("GetTransferStats", "Get current transfer statistics including speeds and session totals."),
        ("SearchTorrents", "Search for torrents using installed search plugins.")
    ];
}
