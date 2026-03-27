using CraterClaw.Api.Services;
using CraterClaw.Core;
using Microsoft.Extensions.Options;

namespace CraterClaw.Api.Tests;

public sealed class ProviderResolverTests
{
    [Fact]
    public void Resolve_KnownProvider_ReturnsEndpoint()
    {
        var opts = Options.Create(new ProviderOptions
        {
            Endpoints = new Dictionary<string, ProviderEndpointOptions>
            {
                ["home"] = new() { BaseUrl = "http://localhost:11434" }
            }
        });
        var resolver = new ProviderResolver(opts);

        var result = resolver.Resolve("home");

        Assert.NotNull(result);
        Assert.Equal("home", result.Name);
        Assert.Equal("http://localhost:11434", result.BaseUrl);
    }

    [Fact]
    public void Resolve_UnknownProvider_ReturnsNull()
    {
        var opts = Options.Create(new ProviderOptions
        {
            Endpoints = new Dictionary<string, ProviderEndpointOptions>
            {
                ["home"] = new() { BaseUrl = "http://localhost:11434" }
            }
        });
        var resolver = new ProviderResolver(opts);

        var result = resolver.Resolve("unknown");

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_EmptyEndpoints_ReturnsNull()
    {
        var opts = Options.Create(new ProviderOptions());
        var resolver = new ProviderResolver(opts);

        var result = resolver.Resolve("any");

        Assert.Null(result);
    }

    [Fact]
    public void GetAll_ReturnsAllConfiguredEndpoints()
    {
        var opts = Options.Create(new ProviderOptions
        {
            Endpoints = new Dictionary<string, ProviderEndpointOptions>
            {
                ["home"] = new() { BaseUrl = "http://localhost:11434" },
                ["seedbox"] = new() { BaseUrl = "http://192.168.1.10:11434" }
            }
        });
        var resolver = new ProviderResolver(opts);

        var results = resolver.GetAll().ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, e => e.Name == "home" && e.BaseUrl == "http://localhost:11434");
        Assert.Contains(results, e => e.Name == "seedbox" && e.BaseUrl == "http://192.168.1.10:11434");
    }
}
