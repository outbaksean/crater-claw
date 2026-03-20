namespace CraterClaw.Core;

public interface IAgenticExecutionService
{
    Task<AgenticResponse> ExecuteAsync(
        ProviderEndpoint endpoint,
        AgenticRequest request,
        CancellationToken cancellationToken);
}
