using Microsoft.SemanticKernel;

namespace CraterClaw.Core;

internal interface IKernelFactory
{
    Kernel Create(ProviderEndpoint endpoint, string modelId);
}
