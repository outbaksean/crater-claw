using Microsoft.SemanticKernel;

namespace CraterClaw.Core;

internal sealed class DefaultKernelFactory(IHttpClientFactory httpClientFactory) : IKernelFactory
{
    public Kernel Create(ProviderEndpoint endpoint, string modelId)
    {
        var httpClient = httpClientFactory.CreateClient("ollama");
        httpClient.BaseAddress = new Uri(endpoint.BaseUrl);

        return Kernel.CreateBuilder()
            .AddOllamaChatCompletion(modelId, httpClient)
            .Build();
    }
}
