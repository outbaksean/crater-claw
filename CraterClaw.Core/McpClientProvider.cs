using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace CraterClaw.Core;

internal sealed class McpClientProvider(ILoggerFactory loggerFactory) : IMcpClientProvider
{
    public async Task<McpClient> CreateClientAsync(McpServerDefinition server, CancellationToken cancellationToken)
    {
        var transport = CreateTransport(server);
        var options = new McpClientOptions
        {
            ClientInfo = new Implementation { Name = "CraterClaw", Version = "1.0" }
        };
        return await McpClient.CreateAsync(transport, options, loggerFactory, cancellationToken);
    }

    internal static IClientTransport CreateTransport(McpServerDefinition server) =>
        server.Transport switch
        {
            McpTransport.Stdio => new StdioClientTransport(new StdioClientTransportOptions
            {
                Command = server.Command
                    ?? throw new InvalidOperationException("Stdio transport requires a Command."),
                Arguments = server.Args?.ToList(),
                EnvironmentVariables = MergeEnv(server.Env)
            }),
            McpTransport.Http => new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri(server.BaseUrl
                    ?? throw new InvalidOperationException("Http transport requires a BaseUrl."))
            }),
            _ => throw new InvalidOperationException($"Unsupported transport: {server.Transport}")
        };

    internal static IDictionary<string, string?> MergeEnv(IReadOnlyDictionary<string, string>? env)
    {
        var merged = Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(e => (string)e.Key, e => (string?)e.Value);

        if (env is not null)
        {
            foreach (var (key, value) in env)
                merged[key] = value;
        }

        return merged;
    }
}
