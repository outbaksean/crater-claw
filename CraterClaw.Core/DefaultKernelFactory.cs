using Microsoft.SemanticKernel;

namespace CraterClaw.Core;

internal sealed class DefaultKernelFactory(IHttpClientFactory httpClientFactory) : IKernelFactory
{
    public Kernel Create(ProviderEndpoint endpoint, string modelId)
    {
        var httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(endpoint.BaseUrl);
        httpClient.Timeout = TimeSpan.FromMinutes(10);

        return Kernel.CreateBuilder()
            .AddOllamaChatCompletion(modelId, httpClient)
            .Build();
    }
}
