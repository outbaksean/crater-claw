using Microsoft.Extensions.DependencyInjection;

namespace CraterClaw.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCraterClawCore(
        this IServiceCollection services,
        string? providerConfigurationPath = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<IProviderStatusService, OllamaProviderStatusService>();
        services.AddTransient<IModelListingService, OllamaModelListingService>();
        services.AddTransient<IModelExecutionService, OllamaModelExecutionService>();
        var resolvedPath = string.IsNullOrWhiteSpace(providerConfigurationPath)
            ? Path.Combine(Environment.CurrentDirectory, "provider-config.json")
            : providerConfigurationPath;
        services.AddSingleton<IProviderConfigurationService>(_ =>
            new FileProviderConfigurationService(resolvedPath));

        return services;
    }
}
