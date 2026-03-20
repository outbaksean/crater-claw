using CraterClaw.Core;

namespace CraterClaw.Api.Tests;

internal sealed class FakeProviderStatusService(ProviderStatus status) : IProviderStatusService
{
    public Task<ProviderStatus> CheckStatusAsync(ProviderEndpoint endpoint, CancellationToken cancellationToken)
        => Task.FromResult(status);
}
