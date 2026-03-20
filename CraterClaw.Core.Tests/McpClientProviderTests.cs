using ModelContextProtocol.Client;

namespace CraterClaw.Core.Tests;

public sealed class McpClientProviderTests
{
    [Fact]
    public void CreateTransport_WithStdioDefinition_ReturnsStdioClientTransport()
    {
        var server = new McpServerDefinition(
            "test", "Test", McpTransport.Stdio, null,
            "uvx", ["--from", "some-package", "some-server"], null, true);

        var transport = McpClientProvider.CreateTransport(server);

        Assert.IsType<StdioClientTransport>(transport);
    }

    [Fact]
    public void CreateTransport_WithHttpDefinition_ReturnsHttpClientTransport()
    {
        var server = new McpServerDefinition(
            "test", "Test", McpTransport.Http, "http://localhost:8080",
            null, null, null, true);

        var transport = McpClientProvider.CreateTransport(server);

        Assert.IsType<HttpClientTransport>(transport);
    }

    [Fact]
    public void CreateTransport_WithUnsupportedTransport_ThrowsInvalidOperationException()
    {
        var server = new McpServerDefinition(
            "test", "Test", (McpTransport)99, null, null, null, null, true);

        Assert.Throws<InvalidOperationException>(() => McpClientProvider.CreateTransport(server));
    }

    [Fact]
    public void CreateTransport_WithStdioMissingCommand_ThrowsInvalidOperationException()
    {
        var server = new McpServerDefinition(
            "test", "Test", McpTransport.Stdio, null, null, null, null, true);

        Assert.Throws<InvalidOperationException>(() => McpClientProvider.CreateTransport(server));
    }

    [Fact]
    public void CreateTransport_WithHttpMissingBaseUrl_ThrowsInvalidOperationException()
    {
        var server = new McpServerDefinition(
            "test", "Test", McpTransport.Http, null, null, null, null, true);

        Assert.Throws<InvalidOperationException>(() => McpClientProvider.CreateTransport(server));
    }

    [Fact]
    public void MergeEnv_WithNullEnv_ReturnsCurrentEnvironmentVariables()
    {
        var result = McpClientProvider.MergeEnv(null);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void MergeEnv_MergesDefinitionEnvIntoEnvironment()
    {
        var env = new Dictionary<string, string> { ["CRATERCLAW_TEST_VAR"] = "test_value" };

        var result = McpClientProvider.MergeEnv(env);

        Assert.Equal("test_value", result["CRATERCLAW_TEST_VAR"]);
    }

    [Fact]
    public void MergeEnv_DefinitionValueOverridesExistingKey()
    {
        var env = new Dictionary<string, string> { ["PATH"] = "overridden_path" };

        var result = McpClientProvider.MergeEnv(env);

        Assert.Equal("overridden_path", result["PATH"]);
    }
}
