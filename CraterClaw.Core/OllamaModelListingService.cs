using System.Text.Json;

namespace CraterClaw.Core;

internal sealed class OllamaModelListingService(HttpClient httpClient) : IModelListingService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<IReadOnlyList<ModelDescriptor>> ListModelsAsync(
        ProviderEndpoint endpoint,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(endpoint.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("BaseUrl is not a valid absolute URI.");
        }

        var tagsUri = new Uri(baseUri, "/api/tags");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(tagsUri, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to retrieve model list: {ex.Message}", ex);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        OllamaTagsDocument? document;
        try
        {
            document = await JsonSerializer.DeserializeAsync<OllamaTagsDocument>(
                stream,
                SerializerOptions,
                cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Model list response JSON is invalid.", ex);
        }

        if (document?.Models is null)
        {
            return [];
        }

        return document.Models
            .Select(m => new ModelDescriptor(m.Name ?? string.Empty, m.Size, m.ModifiedAt))
            .ToList();
    }

    private sealed record OllamaTagsDocument(List<OllamaModelDocument>? Models);

    private sealed record OllamaModelDocument(string? Name, long Size, DateTimeOffset ModifiedAt);
}
