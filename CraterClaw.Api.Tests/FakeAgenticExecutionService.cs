using CraterClaw.Core;

namespace CraterClaw.Api.Tests;

internal sealed class FakeAgenticExecutionService(AgenticResponse response) : IAgenticExecutionService
{
    public Task<AgenticResponse> ExecuteAsync(ProviderEndpoint endpoint, AgenticRequest request, CancellationToken cancellationToken)
        => Task.FromResult(response);
}
