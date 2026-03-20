using CraterClaw.Core;

namespace CraterClaw.Api.Tests;

internal sealed class FakeModelListingService(IReadOnlyList<ModelDescriptor> models) : IModelListingService
{
    public Task<IReadOnlyList<ModelDescriptor>> ListModelsAsync(ProviderEndpoint endpoint, CancellationToken cancellationToken)
        => Task.FromResult(models);
}
