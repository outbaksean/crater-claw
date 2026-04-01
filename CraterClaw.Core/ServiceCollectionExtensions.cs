using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CraterClaw.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCraterClawCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddLogging();

        services.AddOptions<ProviderOptions>()
            .Bind(configuration.GetSection("providers"))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ProviderOptions>, ProviderOptionsValidator>();

        services.AddOptions<AiLoggingOptions>()
            .Bind(configuration.GetSection("aiLogging"));

        services.AddOptions<Dictionary<string, BehaviorEntry>>()
            .Bind(configuration.GetSection("behaviors"));

        services.AddTransient<IProviderStatusService, OllamaProviderStatusService>();
        services.AddTransient<IModelListingService, OllamaModelListingService>();
        services.AddTransient<IModelExecutionService, OllamaModelExecutionService>();
        services.AddSingleton<IBehaviorProfileService, BehaviorProfileService>();
        services.AddSingleton<IKernelFactory, DefaultKernelFactory>();
        services.AddTransient<IAgenticExecutionService, SemanticKernelAgenticExecutionService>();
        services.AddHttpClient("qbittorrent");
        services.AddTransient<OllamaLoggingHandler>();
        services.AddHttpClient("ollama", c => c.Timeout = TimeSpan.FromMinutes(10))
            .AddHttpMessageHandler<OllamaLoggingHandler>();
        services.AddSingleton<IPluginRegistry>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var pluginLogger = sp.GetRequiredService<ILogger<QBitTorrentPlugin>>();
            var registryLogger = sp.GetRequiredService<ILogger<DefaultPluginRegistry>>();
            var factories = new Dictionary<string, Func<IReadOnlyDictionary<string, string>, object>>
            {
                ["qbittorrent"] = config => new QBitTorrentPlugin(
                    httpClientFactory.CreateClient("qbittorrent"),
                    new QBitTorrentOptions
                    {
                        BaseUrl = config.GetValueOrDefault("baseUrl"),
                        Username = config.GetValueOrDefault("username"),
                        Password = config.GetValueOrDefault("password")
                    },
                    pluginLogger)
            };
            return new DefaultPluginRegistry(factories, registryLogger);
        });

        return services;
    }
}
