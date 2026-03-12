using System.Text.Json;

namespace CraterClaw.Core;

internal sealed class FileProviderConfigurationService(string configurationPath) : IProviderConfigurationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public async Task<ProviderConfiguration> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(configurationPath))
        {
            throw new FileNotFoundException(
                $"Provider configuration file was not found at '{configurationPath}'.",
                configurationPath);
        }

        await using var stream = File.OpenRead(configurationPath);

        ProviderConfigurationDocument? document;
        try
        {
            document = await JsonSerializer.DeserializeAsync<ProviderConfigurationDocument>(
                stream,
                SerializerOptions,
                cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Provider configuration JSON is invalid.", ex);
        }

        if (document is null)
        {
            throw new InvalidOperationException("Provider configuration JSON is empty.");
        }

        var configuration = document.ToConfiguration();
        configuration.ValidateOrThrow();
        return configuration;
    }

    public async Task SaveAsync(ProviderConfiguration configuration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.ValidateOrThrow();

        var directory = Path.GetDirectoryName(configurationPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(configurationPath);
        var document = ProviderConfigurationDocument.FromConfiguration(configuration);

        await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken);
    }

    public async Task<ProviderEndpoint> GetActiveEndpointAsync(CancellationToken cancellationToken)
    {
        var configuration = await LoadAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(configuration.ActiveProviderName))
        {
            throw new InvalidOperationException("Active provider is not set.");
        }

        var endpoint = configuration.Endpoints
            .FirstOrDefault(e => string.Equals(e.Name, configuration.ActiveProviderName, StringComparison.OrdinalIgnoreCase));

        if (endpoint is null)
        {
            throw new InvalidOperationException($"Active provider '{configuration.ActiveProviderName}' is not configured.");
        }

        return endpoint;
    }

    public async Task<ProviderEndpoint> SetActiveEndpointAsync(string endpointName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(endpointName))
        {
            throw new ArgumentException("Endpoint name is required.", nameof(endpointName));
        }

        var configuration = await LoadAsync(cancellationToken);
        var endpoint = configuration.Endpoints
            .FirstOrDefault(e => string.Equals(e.Name, endpointName, StringComparison.OrdinalIgnoreCase));

        if (endpoint is null)
        {
            throw new InvalidOperationException($"Provider endpoint '{endpointName}' was not found.");
        }

        var updatedConfiguration = configuration with { ActiveProviderName = endpoint.Name };
        await SaveAsync(updatedConfiguration, cancellationToken);

        return endpoint;
    }

    private sealed record ProviderConfigurationDocument(
        List<ProviderEndpointDocument>? Endpoints,
        string? ActiveProviderName)
    {
        public ProviderConfiguration ToConfiguration()
        {
            var endpoints = Endpoints?
                .Select(e => new ProviderEndpoint(e.Name ?? string.Empty, e.BaseUrl ?? string.Empty))
                .ToList() ?? [];

            return new ProviderConfiguration(endpoints, ActiveProviderName);
        }

        public static ProviderConfigurationDocument FromConfiguration(ProviderConfiguration configuration)
        {
            return new ProviderConfigurationDocument(
                configuration.Endpoints
                    .Select(e => new ProviderEndpointDocument(e.Name, e.BaseUrl))
                    .ToList(),
                configuration.ActiveProviderName);
        }
    }

    private sealed record ProviderEndpointDocument(string? Name, string? BaseUrl);
}