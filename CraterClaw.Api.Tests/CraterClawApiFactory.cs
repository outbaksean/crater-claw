using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CraterClaw.Api.Tests;

internal sealed class CraterClawApiFactory : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection>? _configureServices;
    private readonly IReadOnlyDictionary<string, string?>? _extraConfig;

    public CraterClawApiFactory(
        Action<IServiceCollection>? configureServices = null,
        IReadOnlyDictionary<string, string?>? extraConfig = null)
    {
        _configureServices = configureServices;
        _extraConfig = extraConfig;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["providers:endpoints:test:baseUrl"] = "http://localhost:11434"
            };

            if (_extraConfig is not null)
                foreach (var kvp in _extraConfig)
                    values[kvp.Key] = kvp.Value;

            config.AddInMemoryCollection(values);
        });

        if (_configureServices is not null)
            builder.ConfigureServices(_configureServices);
    }
}
