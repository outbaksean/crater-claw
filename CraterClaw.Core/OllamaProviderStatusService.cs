namespace CraterClaw.Core;

internal sealed class OllamaProviderStatusService(HttpClient httpClient) : IProviderStatusService
{
    public async Task<ProviderStatus> CheckStatusAsync(ProviderEndpoint endpoint, CancellationToken cancellationToken)
    {
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
                return new ProviderStatus(true, null);
            }

            return new ProviderStatus(
                false,
                $"Provider returned HTTP {(int)response.StatusCode} ({response.StatusCode}).");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ProviderStatus(false, $"Connectivity check failed: {ex.Message}");
        }
    }
}
