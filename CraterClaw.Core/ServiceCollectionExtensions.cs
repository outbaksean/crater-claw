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

        services.AddOptions<McpOptions>()
            .Bind(configuration.GetSection("mcp"))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<McpOptions>, McpOptionsValidator>();

        services.AddTransient<IProviderStatusService, OllamaProviderStatusService>();
        services.AddTransient<IModelListingService, OllamaModelListingService>();
        services.AddTransient<IModelExecutionService, OllamaModelExecutionService>();
        services.AddTransient<IMcpAvailabilityService, McpAvailabilityService>();
        services.AddSingleton<IBehaviorProfileService, BehaviorProfileService>();

        return services;
    }
}
