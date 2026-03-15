using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CraterClaw.Core;

internal sealed class OllamaModelListingService(
    HttpClient httpClient,
    ILogger<OllamaModelListingService> logger) : IModelListingService
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
        logger.LogInformation("Listing models for endpoint {EndpointName}", endpoint.Name);

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
            logger.LogWarning("Model listing failed for endpoint {EndpointName}: {ErrorMessage}", endpoint.Name, ex.Message);
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
            logger.LogWarning("Model listing failed for endpoint {EndpointName}: {ErrorMessage}", endpoint.Name, ex.Message);
            throw new InvalidOperationException("Model list response JSON is invalid.", ex);
        }

        if (document?.Models is null)
        {
            logger.LogInformation("Found 0 model(s) for endpoint {EndpointName}", endpoint.Name);
            return [];
        }

        var models = document.Models
            .Select(m => new ModelDescriptor(m.Name ?? string.Empty, m.Size, m.ModifiedAt))
            .ToList();

        logger.LogInformation("Found {ModelCount} model(s) for endpoint {EndpointName}", models.Count, endpoint.Name);
        return models;
    }

    private sealed record OllamaTagsDocument(List<OllamaModelDocument>? Models);

    private sealed record OllamaModelDocument(string? Name, long Size, DateTimeOffset ModifiedAt);
}
