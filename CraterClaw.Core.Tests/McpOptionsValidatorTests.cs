namespace CraterClaw.Core.Tests;

public sealed class McpOptionsValidatorTests
{
    [Fact]
    public void Validate_Passes_ForValidStdioServerWithEnv()
    {
        var options = new McpOptions
        {
            Servers = new Dictionary<string, McpServerOptions>
            {
                ["qbittorrent"] = new()
                {
                    Label = "qBitTorrent",
                    Transport = McpTransport.Stdio,
                    Command = "uvx",
                    Env = new() { ["QBITTORRENT_URL"] = "" },
                    Enabled = true
                }
            }
        };

        var result = new McpOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_Passes_ForValidHttpServer()
    {
        var options = new McpOptions
        {
            Servers = new Dictionary<string, McpServerOptions>
            {
                ["searxng"] = new()
                {
                    Label = "SearXNG",
                    Transport = McpTransport.Http,
                    BaseUrl = "http://localhost:8080",
                    Enabled = true
                }
            }
        };

        var result = new McpOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_Fails_ForHttpServerWithInvalidBaseUrl()
    {
        var options = new McpOptions
        {
            Servers = new Dictionary<string, McpServerOptions>
            {
                ["searxng"] = new()
                {
                    Label = "SearXNG",
                    Transport = McpTransport.Http,
                    BaseUrl = "not-a-url",
                    Enabled = true
                }
            }
        };

        var result = new McpOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_Fails_ForStdioServerWithEmptyCommand()
    {
        var options = new McpOptions
        {
            Servers = new Dictionary<string, McpServerOptions>
            {
                ["qbittorrent"] = new()
                {
                    Label = "qBitTorrent",
                    Transport = McpTransport.Stdio,
                    Command = "",
                    Enabled = true
                }
            }
        };

        var result = new McpOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_Passes_ForEmptyServers()
    {
        var options = new McpOptions { Servers = [] };

        var result = new McpOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded);
    }
}
