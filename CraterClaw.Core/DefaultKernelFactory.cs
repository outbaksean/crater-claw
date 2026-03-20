using Microsoft.SemanticKernel;

namespace CraterClaw.Core;

internal sealed class DefaultKernelFactory : IKernelFactory
{
    public Kernel Create(ProviderEndpoint endpoint, string modelId)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(endpoint.BaseUrl),
            Timeout = TimeSpan.FromMinutes(10)
        };

        return Kernel.CreateBuilder()
            .AddOllamaChatCompletion(modelId, httpClient)
            .Build();
    }
}
