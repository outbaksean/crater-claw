using Microsoft.SemanticKernel;

namespace CraterClaw.Core;

internal sealed class DefaultKernelFactory : IKernelFactory
{
    public Kernel Create(ProviderEndpoint endpoint, string modelId)
    {
        return Kernel.CreateBuilder()
            .AddOllamaChatCompletion(modelId, new Uri(endpoint.BaseUrl))
            .Build();
    }
}
