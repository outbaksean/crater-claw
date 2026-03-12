namespace CraterClaw.Core;

public interface IProviderStatusService
{
    Task<ProviderStatus> CheckStatusAsync(ProviderEndpoint endpoint, CancellationToken cancellationToken);
}
