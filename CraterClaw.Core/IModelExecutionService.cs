namespace CraterClaw.Core;

public interface IModelExecutionService
{
    Task<ExecutionResponse> ExecuteAsync(
        ProviderEndpoint endpoint,
        ExecutionRequest request,
        CancellationToken cancellationToken);
}
