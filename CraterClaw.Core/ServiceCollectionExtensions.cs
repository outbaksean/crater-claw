using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddOptions<ProviderOptions>()
            .Bind(configuration.GetSection("providers"))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ProviderOptions>, ProviderOptionsValidator>();

        services.AddTransient<IProviderStatusService, OllamaProviderStatusService>();
        services.AddTransient<IModelListingService, OllamaModelListingService>();
        services.AddTransient<IModelExecutionService, OllamaModelExecutionService>();

        return services;
    }
}
