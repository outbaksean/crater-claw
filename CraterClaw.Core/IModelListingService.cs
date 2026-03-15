namespace CraterClaw.Core;

public interface IModelListingService
{
    Task<IReadOnlyList<ModelDescriptor>> ListModelsAsync(ProviderEndpoint endpoint, CancellationToken cancellationToken);
}
