using System.Net;
using System.Net.Http;

namespace CraterClaw.Core.Tests;

public sealed class ProviderContractTests
{
    [Fact]
    public void ProviderEndpoint_StoresNameAndBaseUrl()
    {
        var endpoint = new ProviderEndpoint("ollama", "http://localhost:11434");

        Assert.Equal("ollama", endpoint.Name);
        Assert.Equal("http://localhost:11434", endpoint.BaseUrl);
    }

    [Fact]
    public void ProviderStatus_RepresentsSuccessAndFailureStates()
    {
        var success = new ProviderStatus(true, null);
        var failure = new ProviderStatus(false, "Connection failed");

        Assert.True(success.IsReachable);
        Assert.Null(success.ErrorMessage);

        Assert.False(failure.IsReachable);
        Assert.False(string.IsNullOrWhiteSpace(failure.ErrorMessage));
    }
}

public sealed class OllamaProviderStatusServiceTests
{
    [Fact]
    public async Task CheckStatusAsync_ReturnsReachable_WhenApiTagsReturns200()
    {
        using var client = CreateClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var service = new OllamaProviderStatusService(client);

        var status = await service.CheckStatusAsync(
            new ProviderEndpoint("ollama", "http://localhost:11434"),
            CancellationToken.None);

        Assert.True(status.IsReachable);
        Assert.Null(status.ErrorMessage);
    }

    [Fact]
    public async Task CheckStatusAsync_ReturnsUnreachable_WhenHttpRequestThrows()
    {
        using var client = CreateClient((_, _) => throw new HttpRequestException("Host unreachable"));
        var service = new OllamaProviderStatusService(client);

        var status = await service.CheckStatusAsync(
            new ProviderEndpoint("ollama", "http://unreachable-host"),
            CancellationToken.None);

        Assert.False(status.IsReachable);
        Assert.False(string.IsNullOrWhiteSpace(status.ErrorMessage));
    }

    [Fact]
    public async Task CheckStatusAsync_ReturnsUnreachable_WhenApiTagsReturnsNonSuccess()
    {
        using var client = CreateClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var service = new OllamaProviderStatusService(client);

        var status = await service.CheckStatusAsync(
            new ProviderEndpoint("ollama", "http://localhost:11434"),
            CancellationToken.None);

        Assert.False(status.IsReachable);
        Assert.False(string.IsNullOrWhiteSpace(status.ErrorMessage));
    }

    [Fact]
    public async Task CheckStatusAsync_PropagatesCancellation_WhenTokenIsCancelled()
    {
        using var client = CreateClient(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var service = new OllamaProviderStatusService(client);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.CheckStatusAsync(
                new ProviderEndpoint("ollama", "http://localhost:11434"),
                cts.Token));
    }

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        return new HttpClient(new DelegatingTestHandler(handler));
    }

    private sealed class DelegatingTestHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return handler(request, cancellationToken);
        }
    }
}

public sealed class ProviderConfigurationContractTests
{
    [Fact]
    public void Validate_ReturnsNoErrors_ForValidConfiguration()
    {
        var configuration = new ProviderConfiguration(
            [
                new ProviderEndpoint("local", "http://localhost:11434"),
                new ProviderEndpoint("remote", "http://example.local:11434")
            ],
            "local");

        var errors = configuration.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ReturnsError_ForDuplicateEndpointNames()
    {
        var configuration = new ProviderConfiguration(
            [
                new ProviderEndpoint("local", "http://localhost:11434"),
                new ProviderEndpoint("LOCAL", "http://192.168.1.10:11434")
            ],
            "local");

        var errors = configuration.Validate();

        Assert.Contains(errors, e => e.Contains("Duplicate provider endpoint name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ReturnsError_ForMalformedEndpointBaseUrl()
    {
        var configuration = new ProviderConfiguration(
            [new ProviderEndpoint("local", "not-a-url")],
            "local");

        var errors = configuration.Validate();

        Assert.Contains(errors, e => e.Contains("invalid BaseUrl", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ReturnsError_WhenActiveProviderIsMissingFromEndpoints()
    {
        var configuration = new ProviderConfiguration(
            [new ProviderEndpoint("local", "http://localhost:11434")],
            "remote");

        var errors = configuration.Validate();

        Assert.Contains(errors, e => e.Contains("Active provider", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class FileProviderConfigurationServiceTests
{
    [Fact]
    public async Task LoadAsync_ReturnsConfiguration_ForValidJson()
    {
        await using var fixture = await ProviderConfigurationFixture.CreateAsync(
            """
            {
              "endpoints": [
                { "name": "local", "baseUrl": "http://localhost:11434" },
                { "name": "remote", "baseUrl": "http://example.local:11434" }
              ],
              "activeProviderName": "local"
            }
            """);

        var service = new FileProviderConfigurationService(fixture.ConfigurationPath);

        var configuration = await service.LoadAsync(CancellationToken.None);

        Assert.Equal(2, configuration.Endpoints.Count);
        Assert.Equal("local", configuration.ActiveProviderName);
    }

    [Fact]
    public async Task SaveAsync_PersistsConfiguration_AndCanReload()
    {
        await using var fixture = await ProviderConfigurationFixture.CreateEmptyAsync();
        var service = new FileProviderConfigurationService(fixture.ConfigurationPath);

        var configuration = new ProviderConfiguration(
            [new ProviderEndpoint("local", "http://localhost:11434")],
            "local");

        await service.SaveAsync(configuration, CancellationToken.None);
        var reloaded = await service.LoadAsync(CancellationToken.None);

        Assert.Single(reloaded.Endpoints);
        Assert.Equal("local", reloaded.ActiveProviderName);
        Assert.Equal("http://localhost:11434", reloaded.Endpoints[0].BaseUrl);

        var savedJson = await File.ReadAllTextAsync(fixture.ConfigurationPath);
        Assert.Contains("\"activeProviderName\"", savedJson, StringComparison.Ordinal);
        Assert.Contains("\"endpoints\"", savedJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SetActiveEndpointAsync_UpdatesAndPersistsSelection()
    {
        await using var fixture = await ProviderConfigurationFixture.CreateAsync(
            """
            {
              "endpoints": [
                { "name": "local", "baseUrl": "http://localhost:11434" },
                { "name": "remote", "baseUrl": "http://example.local:11434" }
              ],
              "activeProviderName": "local"
            }
            """);

        var service = new FileProviderConfigurationService(fixture.ConfigurationPath);

        var selected = await service.SetActiveEndpointAsync("remote", CancellationToken.None);
        var reloaded = await service.LoadAsync(CancellationToken.None);

        Assert.Equal("remote", selected.Name);
        Assert.Equal("remote", reloaded.ActiveProviderName);
    }

    [Fact]
    public async Task SetActiveEndpointAsync_ThrowsForUnknownEndpointName()
    {
        await using var fixture = await ProviderConfigurationFixture.CreateAsync(
            """
            {
              "endpoints": [
                { "name": "local", "baseUrl": "http://localhost:11434" }
              ],
              "activeProviderName": "local"
            }
            """);

        var service = new FileProviderConfigurationService(fixture.ConfigurationPath);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetActiveEndpointAsync("remote", CancellationToken.None));

        Assert.Contains("was not found", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsync_ThrowsForInvalidJson()
    {
        await using var fixture = await ProviderConfigurationFixture.CreateAsync("{ invalid json }");
        var service = new FileProviderConfigurationService(fixture.ConfigurationPath);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LoadAsync(CancellationToken.None));

        Assert.Contains("JSON is invalid", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetActiveEndpointAsync_ReturnsConfiguredActiveEndpoint()
    {
        await using var fixture = await ProviderConfigurationFixture.CreateAsync(
            """
            {
              "endpoints": [
                { "name": "local", "baseUrl": "http://localhost:11434" },
                { "name": "remote", "baseUrl": "http://example.local:11434" }
              ],
              "activeProviderName": "remote"
            }
            """);

        var service = new FileProviderConfigurationService(fixture.ConfigurationPath);
        var endpoint = await service.GetActiveEndpointAsync(CancellationToken.None);

        Assert.Equal("remote", endpoint.Name);
        Assert.Equal("http://example.local:11434", endpoint.BaseUrl);
    }

    private sealed class ProviderConfigurationFixture : IAsyncDisposable
    {
        private readonly string _directoryPath;

        private ProviderConfigurationFixture(string directoryPath, string configurationPath)
        {
            _directoryPath = directoryPath;
            ConfigurationPath = configurationPath;
        }

        public string ConfigurationPath { get; }

        public static async Task<ProviderConfigurationFixture> CreateAsync(string json)
        {
            var directory = Path.Combine(Path.GetTempPath(), $"CraterClawTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);

            var configurationPath = Path.Combine(directory, "provider-config.json");
            await File.WriteAllTextAsync(configurationPath, json);

            return new ProviderConfigurationFixture(directory, configurationPath);
        }

        public static Task<ProviderConfigurationFixture> CreateEmptyAsync()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"CraterClawTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var configurationPath = Path.Combine(directory, "provider-config.json");
            return Task.FromResult(new ProviderConfigurationFixture(directory, configurationPath));
        }

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(_directoryPath))
            {
                Directory.Delete(_directoryPath, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
