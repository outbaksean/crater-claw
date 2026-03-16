using System.ComponentModel;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace CraterClaw.Core;

public sealed class QBitTorrentPlugin(
    HttpClient httpClient,
    IOptions<QBitTorrentOptions> options,
    ILogger<QBitTorrentPlugin> logger)
{
    private readonly QBitTorrentOptions _options = options.Value;
    private string? _sid;

    private bool IsConfigured => !string.IsNullOrWhiteSpace(_options.BaseUrl);

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (_sid is not null)
            return;

        logger.LogDebug("Authenticating with qBitTorrent at {BaseUrl}", _options.BaseUrl);

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
            return await response.Content.ReadAsStringAsync(cancellationToken);
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

    public static IReadOnlyList<(string Name, string Description)> GetFunctionDescriptions() =>
    [
        ("ListTorrents", "List all torrents with name, hash, status, progress, and size."),
        ("AddTorrentByUrl", "Add a torrent from a magnet link or HTTP URL."),
        ("PauseTorrent", "Pause a torrent by its hash."),
        ("ResumeTorrent", "Resume a paused torrent by its hash."),
        ("DeleteTorrent", "Delete a torrent by its hash, with optional file deletion."),
        ("GetTransferStats", "Get current transfer statistics including speeds and session totals.")
    ];
}
