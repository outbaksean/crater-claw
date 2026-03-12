namespace CraterClaw.Core;

public interface IProviderConfigurationService
{
    Task<ProviderConfiguration> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(ProviderConfiguration configuration, CancellationToken cancellationToken);

    Task<ProviderEndpoint> GetActiveEndpointAsync(CancellationToken cancellationToken);

    Task<ProviderEndpoint> SetActiveEndpointAsync(string endpointName, CancellationToken cancellationToken);
}
