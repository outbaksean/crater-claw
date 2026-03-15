using Microsoft.Extensions.Logging;

namespace CraterClaw.Core;

internal sealed class OllamaProviderStatusService(
    HttpClient httpClient,
    ILogger<OllamaProviderStatusService> logger) : IProviderStatusService
{
    public async Task<ProviderStatus> CheckStatusAsync(ProviderEndpoint endpoint, CancellationToken cancellationToken)
    {
        logger.LogInformation("Checking provider status for endpoint {EndpointName} at {BaseUrl}", endpoint.Name, endpoint.BaseUrl);

        if (!Uri.TryCreate(endpoint.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            return new ProviderStatus(false, "BaseUrl is not a valid absolute URI.");
        }

        var tagsUri = new Uri(baseUri, "/api/tags");

        try
        {
            using var response = await httpClient.GetAsync(tagsUri, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Endpoint {EndpointName} is reachable", endpoint.Name);
                return new ProviderStatus(true, null);
            }

            var errorMessage = $"Provider returned HTTP {(int)response.StatusCode} ({response.StatusCode}).";
            logger.LogWarning("Endpoint {EndpointName} is unreachable: {ErrorMessage}", endpoint.Name, errorMessage);
            return new ProviderStatus(false, errorMessage);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var errorMessage = $"Connectivity check failed: {ex.Message}";
            logger.LogWarning("Endpoint {EndpointName} is unreachable: {ErrorMessage}", endpoint.Name, errorMessage);
            return new ProviderStatus(false, errorMessage);
        }
    }
}
