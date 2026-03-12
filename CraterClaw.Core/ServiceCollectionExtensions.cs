using Microsoft.Extensions.DependencyInjection;

namespace CraterClaw.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCraterClawCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<IProviderStatusService, OllamaProviderStatusService>();
        return services;
    }
}
