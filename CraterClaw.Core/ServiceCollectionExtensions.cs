using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

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
        services.AddTransient<OllamaThinkingHandler>();
        services.AddTransient<OllamaLoggingHandler>();
        services.AddHttpClient("ollama", c => c.Timeout = TimeSpan.FromMinutes(10))
            .AddHttpMessageHandler<OllamaThinkingHandler>()
            .AddHttpMessageHandler<OllamaLoggingHandler>();
        services.AddSingleton<IPluginRegistry>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var pluginLogger = sp.GetRequiredService<ILogger<QBitTorrentPlugin>>();
            var registryLogger = sp.GetRequiredService<ILogger<DefaultPluginRegistry>>();
            var profileService = sp.GetRequiredService<IBehaviorProfileService>();

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

            var pluginFactories = new Dictionary<string, Func<IReadOnlyDictionary<string, string>, KernelPlugin>>
            {
                ["subagent"] = config =>
                {
                    var profileId = config.GetValueOrDefault("profileId") ?? string.Empty;
                    var functionName = config.GetValueOrDefault("functionName") ?? "RunSubAgent";
                    var description = config.GetValueOrDefault("description") ?? string.Empty;
                    var agenticService = sp.GetRequiredService<IAgenticExecutionService>();
                    var registry = sp.GetRequiredService<IPluginRegistry>();
                    var subAgent = new SubAgentPlugin(profileId, functionName, description, profileService, agenticService, registry);
                    var function = KernelFunctionFactory.CreateFromMethod(
                        subAgent.RunAsync,
                        functionName: functionName,
                        description: description);
                    return KernelPluginFactory.CreateFromFunctions(functionName, [function]);
                }
            };

            return new DefaultPluginRegistry(factories, pluginFactories, registryLogger);
        });

        return services;
    }
}
