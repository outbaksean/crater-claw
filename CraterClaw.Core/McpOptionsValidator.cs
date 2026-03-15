using Microsoft.Extensions.Options;

namespace CraterClaw.Core;

internal sealed class McpOptionsValidator : IValidateOptions<McpOptions>
{
    public ValidateOptionsResult Validate(string? name, McpOptions options)
    {
        var failures = new List<string>();

        foreach (var (key, server) in options.Servers)
        {
            if (string.IsNullOrWhiteSpace(server.Label))
                failures.Add($"Server '{key}' must have a non-empty label.");

            if (server.Transport == McpTransport.Http)
            {
                if (!Uri.TryCreate(server.BaseUrl, UriKind.Absolute, out _))
                    failures.Add($"Server '{key}' is Http transport but has an invalid or missing BaseUrl.");
            }
            else if (server.Transport == McpTransport.Stdio)
            {
                if (string.IsNullOrWhiteSpace(server.Command))
                    failures.Add($"Server '{key}' is Stdio transport but has an empty Command.");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
