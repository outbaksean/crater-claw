using ModelContextProtocol.Client;

namespace CraterClaw.Core;

public interface IMcpClientProvider
{
    Task<McpClient> CreateClientAsync(McpServerDefinition server, CancellationToken cancellationToken);
}
