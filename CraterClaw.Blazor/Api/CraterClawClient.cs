using System.Net.Http.Json;

namespace CraterClaw.Blazor.Api;

public class CraterClawClient(HttpClient http)
{
    public async Task<List<ProviderEndpoint>> GetProvidersAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<ProviderEndpoint>>("api/providers", ct) ?? [];

    public async Task<ProviderStatus> GetProviderStatusAsync(string name, CancellationToken ct = default)
        => await http.GetFromJsonAsync<ProviderStatus>($"api/providers/{name}/status", ct)
           ?? new ProviderStatus(false, "No response");

    public async Task<List<ModelItem>> GetModelsAsync(string providerName, CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<ModelItem>>($"api/providers/{providerName}/models", ct) ?? [];

    public async Task<ExecutionResponse> PostExecuteAsync(
        string providerName,
        ExecutionRequest request,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/providers/{providerName}/execute", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ExecutionResponse>(ct)
               ?? throw new InvalidOperationException("Empty response body");
    }

    public async Task<List<BehaviorProfile>> GetProfilesAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<BehaviorProfile>>("api/profiles", ct) ?? [];

    public async Task<AgenticResponse> PostAgenticAsync(
        string providerName,
        AgenticRequest request,
        CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/providers/{providerName}/agentic", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgenticResponse>(ct)
               ?? throw new InvalidOperationException("Empty response body");
    }
}
