using CraterClaw.Core;

namespace CraterClaw.Api.Tests;

internal sealed class FakeModelExecutionService(ExecutionResponse response) : IModelExecutionService
{
    public Task<ExecutionResponse> ExecuteAsync(ProviderEndpoint endpoint, ExecutionRequest request, CancellationToken cancellationToken)
        => Task.FromResult(response);
}
