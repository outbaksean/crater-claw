namespace CraterClaw.Core;

internal sealed class McpAvailabilityService(HttpClient httpClient) : IMcpAvailabilityService
{
    public async Task<McpAvailabilityResult> CheckAvailabilityAsync(
        McpServerDefinition server,
        CancellationToken cancellationToken)
    {
        return server.Transport switch
        {
            McpTransport.Http => await CheckHttpAsync(server, cancellationToken),
            McpTransport.Stdio => CheckStdio(server),
            _ => new McpAvailabilityResult(server.Name, false, $"Unsupported transport: {server.Transport}")
        };
    }

    private async Task<McpAvailabilityResult> CheckHttpAsync(
        McpServerDefinition server,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(server.BaseUrl))
            return new McpAvailabilityResult(server.Name, false, "BaseUrl is not configured.");

        try
        {
            using var response = await httpClient.GetAsync(server.BaseUrl, cancellationToken);
            return new McpAvailabilityResult(server.Name, true, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            return new McpAvailabilityResult(server.Name, false, $"Connection failed: {ex.Message}");
        }
    }

    private static McpAvailabilityResult CheckStdio(McpServerDefinition server)
    {
        if (string.IsNullOrWhiteSpace(server.Command))
            return new McpAvailabilityResult(server.Name, false, "Command is not configured.");

        if (Path.IsPathRooted(server.Command))
        {
            return File.Exists(server.Command)
                ? new McpAvailabilityResult(server.Name, true, null)
                : new McpAvailabilityResult(server.Name, false, $"Command not found at path: {server.Command}");
        }

        var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var pathExtensions = GetPathExtensions();
        var pathEntries = pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var directory in pathEntries)
        {
            foreach (var extension in pathExtensions)
            {
                var fullPath = Path.Combine(directory, server.Command + extension);
                if (File.Exists(fullPath))
                    return new McpAvailabilityResult(server.Name, true, null);
            }
        }

        return new McpAvailabilityResult(server.Name, false, $"Command '{server.Command}' not found on PATH.");
    }

    private static IEnumerable<string> GetPathExtensions()
    {
        if (!OperatingSystem.IsWindows())
            return [string.Empty];

        var pathExt = Environment.GetEnvironmentVariable("PATHEXT") ?? string.Empty;
        var extensions = pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

        if (!extensions.Contains(string.Empty))
            extensions.Insert(0, string.Empty);

        return extensions;
    }
}
