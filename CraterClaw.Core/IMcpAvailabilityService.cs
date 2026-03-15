namespace CraterClaw.Core;

public interface IMcpAvailabilityService
{
    Task<McpAvailabilityResult> CheckAvailabilityAsync(
        McpServerDefinition server,
        CancellationToken cancellationToken);
}
