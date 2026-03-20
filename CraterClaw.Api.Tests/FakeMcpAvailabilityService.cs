using CraterClaw.Core;

namespace CraterClaw.Api.Tests;

internal sealed class FakeMcpAvailabilityService(McpAvailabilityResult result) : IMcpAvailabilityService
{
    public Task<McpAvailabilityResult> CheckAvailabilityAsync(McpServerDefinition server, CancellationToken cancellationToken)
        => Task.FromResult(result);
}
